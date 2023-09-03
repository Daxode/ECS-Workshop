using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class MovNSEWAuthoring : MonoBehaviour
{
    [Header("Movement")]
    public float accelerationToSetInDirection = 1.0f;
    public float maxForce = 10.0f;
    public float drag = 0.1f;
    
    [Header("Key Bindings")]
    public KeyCode north = KeyCode.W;
    public KeyCode south = KeyCode.S;
    public KeyCode east = KeyCode.D;
    public KeyCode west = KeyCode.A;
    
    class MoveInDirectionDataBaker : Baker<MovNSEWAuthoring>
    {
        public override void Bake(MovNSEWAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PushInDirection
            {
                maxForce = authoring.maxForce,
                drag = authoring.drag
            });
            AddComponent(entity, new MoveNSWEData
            {    
                accelerationToSet = authoring.accelerationToSetInDirection,
                north = authoring.north,
                south = authoring.south,
                east = authoring.east,
                west = authoring.west
            });
        }
    }
}

public struct PushInDirection : IComponentData
{
    // Settings
    public float maxForce;
    public float drag;

    // Runtime Only
    public float2 force;
    public float2 accelerationXZ;
    
}

struct MoveNSWEData : IComponentData
{
    // Settings
    public float accelerationToSet;
    
    // Key Bindings
    public KeyCode north;
    public KeyCode south;
    public KeyCode east;
    public KeyCode west;
}

[UpdateBefore(typeof(PushInDirectionSystem))]
partial struct MoveNSWESystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (pushInDirectionRef, settingsRef) in SystemAPI.Query<RefRW<PushInDirection>, RefRO<MoveNSWEData>>())
        {
            var settings = settingsRef.ValueRO;
            
            // Setup
            var forward = math.forward().xz;
            var right = math.right().xz;
            var acceleration = float2.zero;
            
            // Vertical
            if (Input.GetKey(settings.north))
                acceleration += forward * settings.accelerationToSet;
            if (Input.GetKey(settings.south))
                acceleration -= forward * settings.accelerationToSet;
            
            // Horizontal
            if (Input.GetKey(settings.east))
                acceleration += right * settings.accelerationToSet;
            if (Input.GetKey(settings.west))
                acceleration -= right * settings.accelerationToSet;
            
            pushInDirectionRef.ValueRW.accelerationXZ = acceleration;
        }
    }
}

partial struct PushInDirectionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (trsRef, pushInDirectionRef) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<PushInDirection>>())
        {
            // Setup
            ref var pushInDirection = ref pushInDirectionRef.ValueRW;

            // Apply
            pushInDirection.force += pushInDirection.accelerationXZ*SystemAPI.Time.DeltaTime;
            pushInDirection.force = math.clamp(pushInDirection.force, -pushInDirection.maxForce, pushInDirection.maxForce);
            pushInDirection.force *= 1.0f - pushInDirection.drag*SystemAPI.Time.DeltaTime;
            
            trsRef.ValueRW.Position.xz += pushInDirection.force;
        }
    }
}