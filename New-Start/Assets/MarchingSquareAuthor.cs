// #define DEBUG_DRAW_CAVE_GRID
using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;
using static CaveGridSystem.Singleton;

class MarchingSquareAuthor : MonoBehaviour
{
    public MarchSquareSetSprites[] sets;
    public MarchingSquareTile spriteTargetPrefab;
    public AnimatedSpriteAuthor gridLockPrefab;
}

[Serializable]
class MarchSquareSetSprites
{
    public Sprite[] sprites = new Sprite[16];
}

[InternalBufferCapacity(1)]
struct MarchSquareSet : IBufferElementData
{
    // Left Top, Right Top, Right Bottom, Left Bottom
    public float2 offset0; // empty
    public float2 offset1; // 0001 - corner: LB
    public float2 offset2; // 0010 - corner: RB
    public float2 offset3; // 0011 - flat: bottom
    public float2 offset4; // 0100 - corner: RT
    public float2 offset5; // 0101 - diagonal: LB-RT
    public float2 offset6; // 0110 - flat: right
    public float2 offset7; // 0111 - curve: RT-RB-LB
    public float2 offset8; // 1000 - corner: LT
    public float2 offset9; // 1001 - flat: left
    public float2 offsetA; // 1010 - diagonal: LT-RB
    public float2 offsetB; // 1011 - curve: LT-RB-LB
    public float2 offsetC; // 1100 - flat: top
    public float2 offsetD; // 1101 - curve: LT-RT-LB
    public float2 offsetE; // 1110 - curve: LT-RT-RB
    public float2 offsetF; // full

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe float2 GetOffset(int val) 
        => UnsafeUtility.ReadArrayElement<float2>(UnsafeUtility.AddressOf(ref offset0), val);
}

struct MarchSquareData : IComponentData
{
    public Entity spriteTargetPrefab;
    public Entity gridLockPrefab;
}

class MarchingSquareBaker : Baker<MarchingSquareAuthor>
{
    public override void Bake(MarchingSquareAuthor authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new MarchSquareData
        {
            spriteTargetPrefab = GetEntity(authoring.spriteTargetPrefab, TransformUsageFlags.Renderable),
            gridLockPrefab = GetEntity(authoring.gridLockPrefab, TransformUsageFlags.Renderable)
        });
        var buffer = AddBuffer<MarchSquareSet>(entity);

        var textureSheetTexelSize = authoring.spriteTargetPrefab.spriteTextureSheet.texelSize;
        
        foreach (var set in authoring.sets)
        {
            if (set.sprites.Length != 16)
                throw new Exception("MarchingSquareAuthor: set must have 16 sprites");
            
            buffer.Add(new MarchSquareSet
            {
                offset0 = set.sprites[0].rect.position * textureSheetTexelSize,
                offset1 = set.sprites[1].rect.position * textureSheetTexelSize,
                offset2 = set.sprites[2].rect.position * textureSheetTexelSize,
                offset3 = set.sprites[3].rect.position * textureSheetTexelSize,
                offset4 = set.sprites[4].rect.position * textureSheetTexelSize,
                offset5 = set.sprites[5].rect.position * textureSheetTexelSize,
                offset6 = set.sprites[6].rect.position * textureSheetTexelSize,
                offset7 = set.sprites[7].rect.position * textureSheetTexelSize,
                offset8 = set.sprites[8].rect.position * textureSheetTexelSize,
                offset9 = set.sprites[9].rect.position * textureSheetTexelSize,
                offsetA = set.sprites[10].rect.position * textureSheetTexelSize,
                offsetB = set.sprites[11].rect.position * textureSheetTexelSize,
                offsetC = set.sprites[12].rect.position * textureSheetTexelSize,
                offsetD = set.sprites[13].rect.position * textureSheetTexelSize,
                offsetE = set.sprites[14].rect.position * textureSheetTexelSize,
                offsetF = set.sprites[15].rect.position * textureSheetTexelSize,
            });
        }
    }
}

[UpdateBefore(typeof(CaveGridSystem))]
partial struct DebugDrawingSystem : ISystem
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
            var tileIndex = WorldPosToTileIndex(mousePos.xy);

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
            var (snappedPos, snappedToTile) = SnapToTileOrGrid(mousePos.xy);

            if (snappedToTile)
            {
                var tileIndex = WorldPosToTileIndex(snappedPos);
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
                var gridIndex = WorldPosToGridIndex(snappedPos);
                if (gridIndex >= 0 && gridIndex < caveGrid.Length && caveGrid[gridIndex] != CaveMaterialType.Air) {
                    var gridLockPrefab = SystemAPI.GetSingleton<MarchSquareData>().gridLockPrefab;
                    var gridLockEntity = state.EntityManager.Instantiate(gridLockPrefab);
                    SystemAPI.SetComponent(gridLockEntity, 
                        LocalTransform.FromPosition(new float3(SnapWorldPosToGridPos(mousePos.xy), -2)));
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
                var (snappedPos, snappedToTile) = SnapToTileOrGrid(mousePos.xy);
                foreach (var (walkState, selectState) in SystemAPI.Query<RefRW<WalkState>, EnabledRefRW<Selectable>>().WithAll<Selectable>())
                {
                    walkState.ValueRW.target = snappedPos;
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

public enum CaveMaterialType : byte
{
    Rock,
    Air,
    Ore,
    Water,
}


[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
[UpdateAfter(typeof(TransformSystemGroup))]
public partial struct CaveGridSystem : ISystem, ISystemStartStop
{
    public struct Singleton : IComponentData
    {
        public NativeList<CaveMaterialType> CaveGrid;
        public const int CaveGridWidth = 20;
        public int GetCaveGridHeight() => CaveGrid.Length / CaveGridWidth;
        
        public NativeList<Entity> CaveTiles;
        public const int CaveTilesWidth = CaveGridWidth-1;
        public int GetCaveTileHeight() => CaveTiles.Length / CaveTilesWidth;
        
        public static int WorldPosToTileIndex(float2 worldPos)
        {
            var tilePos = (int2)math.round(worldPos);
            return tilePos.x + -tilePos.y * CaveTilesWidth;
        }
    
        public static int2 WorldPosToGridPos(float2 worldPos) => 
            (int2) math.round(worldPos+new float2(0.5f, -0.5f));
        public static float2 SnapWorldPosToGridPos(float2 worldPos) => 
            WorldPosToGridPos(worldPos) - new float2(0.5f, -0.5f);

        public static int WorldPosToGridIndex(float2 worldPos)
        {
            var gridPos = WorldPosToGridPos(worldPos);
            return gridPos.x + -gridPos.y * CaveGridWidth;
        }
        
        public static (float2 pos, bool snappedToTile) SnapToTileOrGrid(float2 worldPos)
        {
            var gridPos = SnapWorldPosToGridPos(worldPos);
            var tilePos = math.round(worldPos);
            var snappedToTile = math.distancesq(worldPos, tilePos) < math.distancesq(worldPos, gridPos);
            return (math.select(gridPos, tilePos, snappedToTile), snappedToTile);
        }
    }

    // Generate the cave grid
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var caveGrid = new NativeList<CaveMaterialType>(CaveGridWidth * 128, Allocator.Persistent);
        var caveTiles = new NativeList<Entity>(CaveTilesWidth * 127, Allocator.Persistent);
        state.EntityManager.AddComponentData(state.SystemHandle, new Singleton { CaveGrid = caveGrid, CaveTiles = caveTiles});
        caveGrid.Length = CaveGridWidth * 128;
        caveTiles.Resize(CaveTilesWidth * 127, NativeArrayOptions.ClearMemory);
        for (var i = 0; i < caveGrid.Length; i++)
        {
            var x = i % CaveGridWidth;
            var y = i / CaveGridWidth;
            
            // generate cave grid
            var water = math.select(0, (int)CaveMaterialType.Water, noise.cnoise(new float2(x, -y)*0.1f)>0.4f);     // water
            var ore = math.select(0, (int)CaveMaterialType.Ore, noise.cnoise(new float3(x, -y, 0.3f)*0.1f)>0.2f); // ore
            var air = math.select(0, (int)CaveMaterialType.Air, noise.cnoise(new float3(x, -y, 0.6f)*0.1f)>0.3f); // air
            air = math.max(air, y is 0 or 1 && x is 6 or 7 ? (int)CaveMaterialType.Air : (int)CaveMaterialType.Rock);       // cave entrance
            caveGrid[i] = (CaveMaterialType) math.max(math.max(water, ore), air);

#if DEBUG_DRAW_CAVE_GRID
            // Debug draw the cave grid
            Debug.DrawLine(
                new float3(x, -y, 0), 
                new float3(x, -y, 0) + math.forward(), 
                Color.HSVToRGB((float)caveGrid[i]/4f,1,1), 
                100f);
#endif
        }
        
        state.RequireForUpdate<MarchSquareData>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        SystemAPI.GetComponent<Singleton>(state.SystemHandle).CaveGrid.Dispose();
    }

    // Spawn the tiles
    [BurstCompile]
    public void OnStartRunning(ref SystemState state)
    {
        var spriteTargetPrefab = SystemAPI.GetSingleton<MarchSquareData>().spriteTargetPrefab;
        var caveGridHeight = SystemAPI.GetComponent<Singleton>(state.SystemHandle).GetCaveGridHeight();
        
        for (var y = 0; y > -caveGridHeight + 1; y--)
        {
            for (var x = 0; x < CaveGridWidth - 1; x++)
            {
                // spawn a sprite with the correct offset
                var spriteTarget = state.EntityManager.Instantiate(spriteTargetPrefab);
                SystemAPI.SetComponent(spriteTarget, LocalTransform.FromPosition(new float3(x, y, 0)));
            }
        }
    }

    // Update the tiles
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var marchSquareSets = SystemAPI.GetSingletonBuffer<MarchSquareSet>();
        var caveGrid = SystemAPI.GetComponent<Singleton>(state.SystemHandle).CaveGrid.AsArray();
        
        // update outline tile
        foreach (var (lt, offsetXYScaleZwRef) in SystemAPI.Query<LocalToWorld, RefRW<MaterialOverrideOffsetXYScaleZW>>().WithAll<MarchingSquareTileCarverTag>())
        {
            var corners = caveGrid.GetCornerValues((int2)lt.Position.xy);

            // build a 4-bit value from the 4 corners
            corners = 1 - (int4) math.saturate(corners);
            var valCombined = corners.x | (corners.y << 1) | (corners.z << 2) | (corners.w << 3);
            offsetXYScaleZwRef.ValueRW.Value.xy = marchSquareSets[0].GetOffset(valCombined);
        }
        
        // update mat A tile (the one with the highest corner)
        foreach (var (lt, offsetXYScaleZwRef, cornerStrengthRef) in SystemAPI.Query<LocalToWorld, RefRW<MaterialOverrideOffsetXYScaleZW>, RefRW<MaterialOverrideCornerStrength>>().WithAll<MarchingSquareTileMatATag>())
        {
            var corners = caveGrid.GetCornerValues((int2)lt.Position.xy);
            var highestCorners = SortTheFourNumbers(corners);
            
            // build a 4-bit value from the 4 corners
            corners = 1-(int4) (corners == highestCorners.w);
            var valCombined = corners.x | (corners.y << 1) | (corners.z << 2) | (corners.w << 3);
            offsetXYScaleZwRef.ValueRW.Value.xy = marchSquareSets[math.clamp(highestCorners.w-1, 0, marchSquareSets.Length-1)].GetOffset(valCombined);
        }
        
        // update mat B tile (same as mat A, but with the second highest corner)
        foreach (var (lt, offsetXYScaleZwRef, cornerStrengthRef) in SystemAPI.Query<LocalToWorld, RefRW<MaterialOverrideOffsetXYScaleZW>, RefRW<MaterialOverrideCornerStrength>>().WithAll<MarchingSquareTileMatBTag>())
        {
            var corners = caveGrid.GetCornerValues((int2)lt.Position.xy);
            var highestCorners = SortTheFourNumbers(corners);
            
            // get the second highest corner
            var validSecond = 0;
            if (highestCorners.w != highestCorners.z)
                validSecond = highestCorners.z;
            else if (highestCorners.w != highestCorners.y)
                validSecond = highestCorners.y;
            else if (highestCorners.w != highestCorners.x)
                validSecond = highestCorners.x;
            
            // if there is no second highest corner, then there is no corner
            cornerStrengthRef.ValueRW.Value = validSecond == 0 ? 0 : 1;
            
            
            // build a 4-bit value from the 4 corners
            corners = 1-(int4) (corners == validSecond);
            var valCombined = corners.x | (corners.y << 1) | (corners.z << 2) | (corners.w << 3);
            offsetXYScaleZwRef.ValueRW.Value.xy = marchSquareSets[math.clamp(validSecond-1, 0, marchSquareSets.Length-1)].GetOffset(valCombined);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // Sort the 4 numbers from lowest to highest (lowest in x, highest in w)
    static int4 SortTheFourNumbers(int4 val)
    {
        var lowHigh1 = math.select(val.yx, val.xy, val.x < val.y);
        var lowHigh2 = math.select(val.wz, val.zw, val.z < val.w);
        var lowestMiddle1 = math.select(new int2(lowHigh2.x, lowHigh1.x), new int2(lowHigh1.x, lowHigh2.x), lowHigh1.x < lowHigh2.x);
        var middle2Highest = math.select(new int2(lowHigh2.y, lowHigh1.y), new int2(lowHigh1.y, lowHigh2.y), lowHigh1.y < lowHigh2.y);

        return math.select(
            new int4(lowestMiddle1.x,middle2Highest.x, lowestMiddle1.y, middle2Highest.y), 
            new int4(lowestMiddle1, middle2Highest), 
            lowestMiddle1.y < middle2Highest.x);
    }

    public void OnStopRunning(ref SystemState state) {}
}

static class CaveSystemExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int4 GetCornerValues(this NativeArray<CaveMaterialType> caveGrid, int2 coord)
    {
        // assert that y is negative or ground level. As we're going from top to bottom, y should be negative
        if (coord.y > 0) throw new Exception("y must be negative");

        // get the 4 corners
        var i = coord.x + -coord.y * CaveGridWidth;
        return new int4(
            (int)caveGrid[i + CaveGridWidth], // Bottom Left
            (int)caveGrid[i + CaveGridWidth + 1], // Bottom Right
            (int)caveGrid[i + 1], // Top Right
            (int)caveGrid[i] // Top Left
        );
    }
}
