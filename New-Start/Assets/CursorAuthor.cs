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
            AddComponent(entity, new CursorTag());
        }
    }
}

struct CursorTag : IComponentData {}
struct CursorTagHead : IComponentData {}


partial struct CursorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CursorTag>();
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
        var cursorEntity = SystemAPI.GetSingletonEntity<CursorTag>();
        SystemAPI.SetComponent(cursorEntity, new LocalToWorld
        {
            Value = float4x4.TRS(new float3(mousePos.xy, -2), quaternion.identity, 1)
        });

        var buffer = SystemAPI.GetBuffer<SpriteFrameElement>(cursorEntity);
        if (math.distancesq(mousePos.xy, mousePosInt) < 0.1f)
        {
            SystemAPI.GetComponentRW<MaterialOverrideOffsetXYScaleZW>(cursorEntity).ValueRW.Value.xy = buffer[1].offset;
            SystemAPI.SetComponent(SystemAPI.GetSingletonEntity<CursorTagHead>(), new LocalToWorld
            {
                Value = float4x4.TRS(new float3(mousePosInt, -2), quaternion.identity, 1)
            });
        } else if (math.distancesq(mousePos.xy, mousePosInt) < 0.15f)
        {
            SystemAPI.GetComponentRW<MaterialOverrideOffsetXYScaleZW>(cursorEntity).ValueRW.Value.xy = buffer[0].offset;
            SystemAPI.SetComponent(SystemAPI.GetSingletonEntity<CursorTagHead>(), new LocalToWorld
            {
                Value = float4x4.TRS(new float3(math.lerp(mousePosInt, mousePos.xy,  math.smoothstep(0.1f, 0.15f, math.distancesq(mousePos.xy, mousePosInt))), -2), quaternion.identity, 1)
            });
        }
        else
        {
            SystemAPI.GetComponentRW<MaterialOverrideOffsetXYScaleZW>(cursorEntity).ValueRW.Value.xy = buffer[0].offset;
            SystemAPI.SetComponent(SystemAPI.GetSingletonEntity<CursorTagHead>(), new LocalToWorld
            {
                Value = float4x4.TRS(0, quaternion.identity, 0)
            });
        }
    }
}