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
}

struct CursorTagHead : IComponentData {}


partial struct CursorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CursorSelection>();
        state.RequireForUpdate<CursorTagHead>();
        
        Cursor.visible = false;
    }

    public void OnUpdate(ref SystemState state)
    {
        var camera = Camera.main;
        if (camera == null) return;
        
        // Get mouse position in world and round to nearest grid cell 
        var mousePos = (float3) camera.ScreenToWorldPoint(Input.mousePosition);
        var mousePosInt = (math.round(mousePos.xy+new float2(0.5f, -0.5f))) - new float2(0.5f, -0.5f);
        var cursorEntity = SystemAPI.GetSingletonEntity<CursorSelection>();
        ref var cursorSelection = ref SystemAPI.GetComponentRW<CursorSelection>(cursorEntity).ValueRW;
        SystemAPI.SetComponent(cursorEntity, new LocalToWorld
        {
            Value = float4x4.TRS(new float3(mousePos.xy, -2), quaternion.identity, 1)
        });

        // Check if mouse is on object
        if (cursorSelection.cursorToDraw.IsInDefaultMode())
        {
            if (math.distancesq(mousePos.xy, mousePosInt) < 0.1f)
            {
                cursorSelection.cursorToDraw.SetOnObject();
                
                // Set cursor head position to grid cell
                SystemAPI.SetComponent(SystemAPI.GetSingletonEntity<CursorTagHead>(), new LocalToWorld
                {
                    Value = float4x4.TRS(new float3(mousePosInt, -2), quaternion.identity, 1)
                });
            } else if (math.distancesq(mousePos.xy, mousePosInt) < 0.15f)
            {
                cursorSelection.cursorToDraw.SetDefault();
                
                // Set cursor head position to grid cell
                SystemAPI.SetComponent(SystemAPI.GetSingletonEntity<CursorTagHead>(), new LocalToWorld
                {
                    Value = float4x4.TRS(new float3(math.lerp(mousePosInt, mousePos.xy,  math.smoothstep(0.1f, 0.15f, math.distancesq(mousePos.xy, mousePosInt))), -2), quaternion.identity, 1)
                });
            }
            else
            {
                cursorSelection.cursorToDraw.SetDefault();
                
                // Hide cursor head
                SystemAPI.SetComponent(SystemAPI.GetSingletonEntity<CursorTagHead>(), new LocalToWorld
                {
                    Value = float4x4.TRS(0, quaternion.identity, 0)
                });
            }
        }
        
        var buffer = SystemAPI.GetBuffer<SpriteFrameElement>(cursorEntity);
        SystemAPI.GetComponentRW<MaterialOverrideOffsetXYScaleZW>(cursorEntity).ValueRW.Value.xy = buffer[(int)cursorSelection.cursorToDraw].offset;
    }
}
