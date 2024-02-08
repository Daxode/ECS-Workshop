using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

struct ConstructionSiteElement : IBufferElementData
{
    public Entity constructionSite;
}

[UpdateBefore(typeof(CaveGridSystem))]
partial struct GameInputSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CursorSelection>();
        state.RequireForUpdate<CaveGridSystem.Singleton>();
        state.RequireForUpdate<ConstructionSiteElement>();
        state.RequireForUpdate<MarchSquareData>();
    }
    
    public void OnUpdate(ref SystemState state)
    {
        var camera = Camera.main;
        if (camera == null) return;
        var caveSystem = SystemAPI.GetSingletonRW<CaveGridSystem.Singleton>().ValueRW;
        var caveGrid = caveSystem.CaveGrid.AsArray();
        var caveTiles = caveSystem.CaveTiles.AsArray();

        var constructionSites = SystemAPI.GetSingletonBuffer<ConstructionSiteElement>();

        ref var cursorSelection = ref SystemAPI.GetSingletonRW<CursorSelection>().ValueRW;
        ref var cursorToDraw = ref cursorSelection.cursorToDraw;

        if (Input.GetKeyDown(KeyCode.Mouse1) && cursorToDraw.IsOutline())
            cursorToDraw.SetDefault();
        
        if (Input.GetKeyDown(KeyCode.Mouse0) && cursorToDraw.IsOutline())
        {
            // Get the tile index
            float3 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);
            var tileIndex = CaveGridSystem.Singleton.WorldPosToTileIndex(mousePos.xy);

            if (tileIndex >= 0 && tileIndex < caveTiles.Length && caveTiles[tileIndex] == Entity.Null) {
                // check if valid
                var corners = caveGrid.GetCornerValues((int2)(math.round(mousePos.xy)));
                corners = 1 - (int4)math.saturate(corners);
                var marchSetIndex = corners.x | (corners.y << 1) | (corners.z << 2) | (corners.w << 3);
                if ((marchSetIndex == 3 && cursorToDraw is CursorSelection.CursorToDraw.StockpileOutline or CursorSelection.CursorToDraw.WorkshopOutline) 
                    || marchSetIndex == 0 && cursorToDraw is CursorSelection.CursorToDraw.LadderOutline)
                {
                    var constructionIndex = (cursorToDraw - CursorSelection.CursorToDraw.LadderOutline);
                    var constructionSiteEntity = state.EntityManager.Instantiate(constructionSites[constructionIndex].constructionSite);
                    SystemAPI.SetComponent(constructionSiteEntity, LocalTransform.FromPosition(new float3(math.round(mousePos.xy), -2)));
                    caveTiles[tileIndex] = constructionSiteEntity;

                    // deselect the cursor
                    cursorToDraw.SetDefault();
                }
            }
        }
        
        // Queue the grid rock to be destroyed
        if (Input.GetKeyDown(KeyCode.Mouse0) && cursorToDraw.IsDestroy())
        {
            // Get the grid index
            float3 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);
            var (snappedPos, snappedToTile) = CaveGridSystem.Singleton.SnapToTileOrGrid(mousePos.xy);

            if (snappedToTile)
            {
                var tileIndex = CaveGridSystem.Singleton.WorldPosToTileIndex(snappedPos);
                if (tileIndex >= 0 && tileIndex < caveTiles.Length && caveTiles[tileIndex] != Entity.Null) {
                    if (SystemAPI.HasComponent<ConstructionSite>(caveTiles[tileIndex]))
                    {
                        state.EntityManager.DestroyEntity(caveTiles[tileIndex]);
                        caveTiles[tileIndex] = Entity.Null;
                    }
                }
            }
            else
            {
                var gridIndex = CaveGridSystem.Singleton.WorldPosToGridIndex(snappedPos);
                if (gridIndex >= 0 && gridIndex < caveGrid.Length && caveGrid[gridIndex] != CaveMaterialType.Air) {
                    var gridLockPrefab = SystemAPI.GetSingleton<MarchSquareData>().gridLockPrefab;
                    var gridLockEntity = state.EntityManager.Instantiate(gridLockPrefab);
                    SystemAPI.SetComponent(gridLockEntity, 
                        LocalTransform.FromPosition(new float3(CaveGridSystem.Singleton.SnapWorldPosToGridPos(mousePos.xy), -2)));
                }
            }

        }

        if (cursorToDraw.IsSelected())
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (cursorSelection.hoveredEntity != Entity.Null)
                {
                    var isAlreadySelected = SystemAPI.IsComponentEnabled<Selectable>(cursorSelection.hoveredEntity);
                    SystemAPI.SetComponentEnabled<Selectable>(cursorSelection.hoveredEntity, !isAlreadySelected);
                }
            } else if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                float3 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);
                var snappedPos = mousePos.xy * 2 + new float2(1, -1);
                var navigationGridIndex = (int)snappedPos.x + (int)-snappedPos.y * NavigationSystem.navWidth;
                state.EntityManager.SetComponentEnabled<PathGoal>(SystemAPI.QueryBuilder().WithDisabled<PathGoal>().WithAll<Selectable>().Build(), true);
                foreach (var (walkState, selectState) in SystemAPI.Query<RefRW<PathGoal>, EnabledRefRW<Selectable>>().WithAll<Selectable>())
                {
                    walkState.ValueRW.nodeIndex = navigationGridIndex;
                    selectState.ValueRW = false;
                }
            }
        }
        
        
        // draw on the cave grid
        if ((Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1)) && cursorToDraw.IsDrawn())
        {
            float3 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);
            var i = CaveGridSystem.Singleton.WorldPosToGridIndex(mousePos.xy);
            if (i >= 0 && i < caveGrid.Length)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    caveGrid[i] = Input.GetKey(KeyCode.Mouse0) ? CaveMaterialType.Water : CaveMaterialType.Ore;
                else
                    caveGrid[i] = Input.GetKey(KeyCode.Mouse0) ? CaveMaterialType.Air : CaveMaterialType.Rock;
            }
        }
        
        // camera y up/down from scroll
        if (Input.mouseScrollDelta != Vector2.zero) 
            camera.transform.position += new Vector3(0, Input.mouseScrollDelta.y, 0);
    }
}