using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class CursorAuthor : MonoBehaviour
{
    class CursorAuthorBaker : Baker<CursorAuthor>
    {
        public override void Bake(CursorAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new CursorSelection());
        }
    }
}

public struct CursorSelection : IComponentData
{
    public CursorToDraw cursorToDraw;
    
    public enum CursorToDraw
    {
        Default,
        OnObject,
        DefaultDraw,
        OnObjectDraw,
        LadderOutline,
        WorkshopOutline,
        StockpileOutline,
        ShovelTool,
    }
}
public static class CursorToDrawExtensions
{
    public static bool IsDrawn(this CursorSelection.CursorToDraw cursorToDraw) 
        => cursorToDraw is CursorSelection.CursorToDraw.DefaultDraw or CursorSelection.CursorToDraw.OnObjectDraw;
    public static void SetOnObject(this ref CursorSelection.CursorToDraw cursorToDraw) 
        => cursorToDraw = cursorToDraw.IsDrawn() ? CursorSelection.CursorToDraw.OnObjectDraw : CursorSelection.CursorToDraw.OnObject;
    public static void SetDefault(this ref CursorSelection.CursorToDraw cursorToDraw)
        => cursorToDraw = cursorToDraw.IsDrawn() ? CursorSelection.CursorToDraw.DefaultDraw : CursorSelection.CursorToDraw.Default;
    public static bool IsInDefaultMode(this CursorSelection.CursorToDraw cursorToDraw)
        => cursorToDraw is CursorSelection.CursorToDraw.Default or CursorSelection.CursorToDraw.OnObject || cursorToDraw.IsDrawn();
    public static bool IsOutline(this CursorSelection.CursorToDraw cursorToDraw)
        => cursorToDraw is CursorSelection.CursorToDraw.LadderOutline or CursorSelection.CursorToDraw.WorkshopOutline or CursorSelection.CursorToDraw.StockpileOutline;
}

struct GridSnappedTag : IComponentData {}
struct TileSnappedTag : IComponentData {}


partial struct CursorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CursorSelection>();
        state.RequireForUpdate<GridSnappedTag>();
        state.RequireForUpdate<TileSnappedTag>();
        
        Cursor.visible = false;
    }

    public void OnUpdate(ref SystemState state)
    {
        var camera = Camera.main;
        if (camera == null) return;
        
        // Get mouse position in world and round to nearest grid cell 
        var mousePos = (float3) camera.ScreenToWorldPoint(Input.mousePosition);
        var gridSnappedPos = (math.round(mousePos.xy+new float2(0.5f, -0.5f))) - new float2(0.5f, -0.5f);
        var tileSnappedPos = (math.round(mousePos.xy+new float2(0.0f, 0.0f))) - new float2(0.0f, 0.0f);
        
        // Setup cursor data
        var cursorEntity = SystemAPI.GetSingletonEntity<CursorSelection>();
        var cursorSpriteOffsets = SystemAPI.GetBuffer<SpriteFrameElement>(cursorEntity);
        ref var cursorSelection = ref SystemAPI.GetComponentRW<CursorSelection>(cursorEntity).ValueRW;
        SystemAPI.SetComponent(cursorEntity, new LocalToWorld
        {
            Value = float4x4.Translate(new float3(mousePos.xy, -2))
        });
        
        // Hide cursor heads
        var tileSnapEntity = SystemAPI.GetSingletonEntity<TileSnappedTag>();
        SystemAPI.SetComponent(tileSnapEntity, default(LocalToWorld));
        var gridSnapEntity = SystemAPI.GetSingletonEntity<GridSnappedTag>();
        SystemAPI.SetComponent(gridSnapEntity, default(LocalToWorld));
        

        // Check if mouse is on object
        if (cursorSelection.cursorToDraw.IsInDefaultMode())
        {
            // Check if mouse is on object
            if (math.distancesq(mousePos.xy, gridSnappedPos) < 0.1f)
            {
                // Snaps cursor head to grid
                SystemAPI.SetComponent(gridSnapEntity, new LocalToWorld
                {
                    Value = float4x4.Translate(new float3(gridSnappedPos, -2))
                });
                cursorSelection.cursorToDraw.SetOnObject();
            }
            else
                cursorSelection.cursorToDraw.SetDefault();
        }
        
        if (cursorSelection.cursorToDraw.IsOutline())
        {
            // Snaps cursor head to grid
            SystemAPI.SetComponent(tileSnapEntity, new LocalToWorld
            {
                Value = float4x4.Translate(new float3(tileSnappedPos, -2))
            });
            SystemAPI.GetComponentRW<MaterialOverrideOffsetXYScaleZW>(cursorEntity).ValueRW.Value.xy = SystemAPI.GetBuffer<SpriteFrameElement>(tileSnapEntity)[0].offset;
            cursorEntity = tileSnapEntity;
        }
        
        SystemAPI.GetComponentRW<MaterialOverrideOffsetXYScaleZW>(cursorEntity).ValueRW.Value.xy = cursorSpriteOffsets[(int)cursorSelection.cursorToDraw].offset;
    }
}
