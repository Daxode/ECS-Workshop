using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


public struct CursorSelection : IComponentData
{
    public CursorToDraw cursorToDraw;
    public Entity hoveredEntity;
    
    public enum CursorToDraw
    {
        DestroyDefault,
        DestroyOnObject,
        SelectDefault,
        SelectOnObject,
        DrawDefault,
        DrawOnObject,
        LadderOutline,
        WorkshopOutline,
        StockpileOutline,
    }
}

public static class CursorToDrawExtensions
{
    public static bool IsSelected(this CursorSelection.CursorToDraw cursorToDraw) 
        => cursorToDraw is CursorSelection.CursorToDraw.SelectDefault or CursorSelection.CursorToDraw.SelectOnObject;
    public static bool IsDrawn(this CursorSelection.CursorToDraw cursorToDraw) 
        => cursorToDraw is CursorSelection.CursorToDraw.DrawDefault or CursorSelection.CursorToDraw.DrawOnObject;
    public static bool IsDestroy(this CursorSelection.CursorToDraw cursorToDraw) 
        => cursorToDraw is CursorSelection.CursorToDraw.DestroyDefault or CursorSelection.CursorToDraw.DestroyOnObject;
    public static bool IsInDefaultMode(this CursorSelection.CursorToDraw cursorToDraw)
        => cursorToDraw.IsDrawn() || cursorToDraw.IsSelected() || cursorToDraw.IsDestroy();
    public static bool IsOutline(this CursorSelection.CursorToDraw cursorToDraw)
        => cursorToDraw is CursorSelection.CursorToDraw.LadderOutline or CursorSelection.CursorToDraw.WorkshopOutline or CursorSelection.CursorToDraw.StockpileOutline;
    
    public static void SetOnObject(this ref CursorSelection.CursorToDraw cursorToDraw) 
        => cursorToDraw = cursorToDraw.IsDrawn() 
            ? CursorSelection.CursorToDraw.DrawOnObject 
            : cursorToDraw.IsDestroy() 
                ? CursorSelection.CursorToDraw.DestroyOnObject
                : CursorSelection.CursorToDraw.SelectOnObject;
    public static void SetDefault(this ref CursorSelection.CursorToDraw cursorToDraw)
        => cursorToDraw = cursorToDraw.IsDrawn() 
            ? CursorSelection.CursorToDraw.DrawDefault 
            : cursorToDraw.IsDestroy() 
                ? CursorSelection.CursorToDraw.DestroyDefault
                : CursorSelection.CursorToDraw.SelectDefault;
}

struct GridSnappedTag : IComponentData {}
struct TileSnappedTag : IComponentData {}


struct Selectable : IComponentData, IEnableableComponent
{
    public Entity outlineEntity;
}

partial struct CursorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CursorSelection>();
        state.RequireForUpdate<GridSnappedTag>();
        
        Cursor.visible = false;
    }

    public void OnUpdate(ref SystemState state)
    {
        var camera = Camera.main;
        if (camera == null) return;
        
        // Get mouse position in world and round to nearest grid cell 
        var mousePos = (float3) camera.ScreenToWorldPoint(Input.mousePosition);
        var gridSnappedPos = CoordUtility.SnapWorldPosToGridPos(mousePos.xy);
        var tileSnappedPos = math.round(mousePos.xy);
        
        // Setup cursor data
        var cursorEntity = SystemAPI.GetSingletonEntity<CursorSelection>();
        var cursorSpriteOffsets = SystemAPI.GetBuffer<SpriteFrameElement>(cursorEntity);
        ref var cursorSelection = ref SystemAPI.GetComponentRW<CursorSelection>(cursorEntity).ValueRW;
        SystemAPI.SetComponent(cursorEntity, new LocalToWorld
        {
            Value = float4x4.Translate(new float3(mousePos.xy, -3f))
        });
        
        // Hide cursor heads
        var headEntity = SystemAPI.GetSingletonEntity<GridSnappedTag>();
        SystemAPI.SetComponent(headEntity, default(LocalToWorld));
        

        // Check if mouse is on object
        if (cursorSelection.cursorToDraw.IsDrawn())
        {
            // Check if mouse is on object
            if (math.distancesq(mousePos.xy, gridSnappedPos) < 0.1f)
            {
                // Snaps cursor head to grid
                SystemAPI.SetComponent(headEntity, new LocalToWorld
                {
                    Value = float4x4.Translate(new float3(gridSnappedPos, -2f))
                });
                cursorSelection.cursorToDraw.SetOnObject();
                var gridSpriteIndex = cursorSelection.cursorToDraw.IsDestroy() ? 1 : 0;
                var gridSpriteOffset = SystemAPI.GetBuffer<SpriteFrameElement>(headEntity)[gridSpriteIndex].offset;
                SystemAPI.GetComponentRW<MaterialOverrideOffsetXYScaleZW>(headEntity).ValueRW.Value.xy = gridSpriteOffset;
            }
            else
                cursorSelection.cursorToDraw.SetDefault();
        }
        
        // Check if mouse is on object
        if (cursorSelection.cursorToDraw.IsDestroy())
        {
            var (snappedPos, _) = CoordUtility.SnapToTileOrGrid(mousePos.xy);
            
            // Check if mouse is on object
            if (math.distancesq(mousePos.xy, snappedPos) < 0.1f)
            {
                // Snaps cursor head to grid
                SystemAPI.SetComponent(headEntity, new LocalToWorld
                {
                    Value = float4x4.Translate(new float3(snappedPos, -2f))
                });
                cursorSelection.cursorToDraw.SetOnObject();
                var gridSpriteOffset = SystemAPI.GetBuffer<SpriteFrameElement>(headEntity)[1].offset;
                SystemAPI.GetComponentRW<MaterialOverrideOffsetXYScaleZW>(headEntity).ValueRW.Value.xy = gridSpriteOffset;
            }
            else
                cursorSelection.cursorToDraw.SetDefault();
        }
        
        // Check if mouse is on object
        if (cursorSelection.cursorToDraw.IsSelected())
        {
            var minDistSq = float.MaxValue;
            var snappedPos = float2.zero;
            cursorSelection.hoveredEntity = Entity.Null;
            foreach (var (ltw, slimeEntity) in SystemAPI.Query<LocalToWorld>().WithAll<Selectable>().WithEntityAccess().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                var slimePos = ltw.Value.c3.xy;
                var distToSlime = math.distancesq(mousePos.xy, slimePos);
                var slimeIsCloser = distToSlime < minDistSq;
                minDistSq = slimeIsCloser ? distToSlime : minDistSq;
                cursorSelection.hoveredEntity = slimeIsCloser ? slimeEntity : cursorSelection.hoveredEntity;
                snappedPos = slimeIsCloser ? slimePos : snappedPos;
            }
            cursorSelection.hoveredEntity = minDistSq < 0.1f ? cursorSelection.hoveredEntity : Entity.Null;
            
            // Check if mouse is on object
            if (math.distancesq(mousePos.xy, snappedPos) < 0.1f)
            {
                // Snaps cursor head to grid
                SystemAPI.SetComponent(headEntity, new LocalToWorld
                {
                    Value = float4x4.Translate(new float3(snappedPos, -2f))
                });
                cursorSelection.cursorToDraw.SetOnObject();
                var gridSpriteOffset = SystemAPI.GetBuffer<SpriteFrameElement>(headEntity)[0].offset;
                SystemAPI.GetComponentRW<MaterialOverrideOffsetXYScaleZW>(headEntity).ValueRW.Value.xy = gridSpriteOffset;
            }
            else
                cursorSelection.cursorToDraw.SetDefault();
        }
        
        if (cursorSelection.cursorToDraw.IsOutline())
        {
            // Snaps cursor head to grid
            SystemAPI.SetComponent(headEntity, new LocalToWorld
            {
                Value = float4x4.Translate(new float3(tileSnappedPos, -2f))
            });
            var tileSpriteOffset = SystemAPI.GetBuffer<SpriteFrameElement>(headEntity)[0].offset;
            SystemAPI.GetComponentRW<MaterialOverrideOffsetXYScaleZW>(cursorEntity).ValueRW.Value.xy = tileSpriteOffset;
            cursorEntity = headEntity;
        }
        
        SystemAPI.GetComponentRW<MaterialOverrideOffsetXYScaleZW>(cursorEntity).ValueRW.Value.xy = cursorSpriteOffsets[(int)cursorSelection.cursorToDraw].offset;
    }
}