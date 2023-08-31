using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

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
                accelerationToSet = authoring.accelerationToSetInDirection,
                maxForce = authoring.maxForce,
                drag = authoring.drag,
                
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
    public float accelerationToSet;
    public float maxForce;
    public float2 force;
    public float drag;
    
    public KeyCode north;
    public KeyCode south;
    public KeyCode east;
    public KeyCode west;
}

partial struct PushInDirectionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (trsRef, pushInDirectionRef) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<PushInDirection>>())
        {
            // Setup
            ref var pushInDirection = ref pushInDirectionRef.ValueRW;
            ref var trs = ref trsRef.ValueRW;
            var forward = math.forward().xz;
            var right = math.right().xz;
            var acceleration = float2.zero;
            
            // Vertical
            if (Input.GetKey(pushInDirection.north))
                acceleration += forward * pushInDirection.accelerationToSet;
            if (Input.GetKey(pushInDirection.south))
                acceleration -= forward * pushInDirection.accelerationToSet;
            
            // Horizontal
            if (Input.GetKey(pushInDirection.east))
                acceleration += right * pushInDirection.accelerationToSet;
            if (Input.GetKey(pushInDirection.west))
                acceleration -= right * pushInDirection.accelerationToSet;
            
            // Apply
            pushInDirection.force += acceleration*SystemAPI.Time.DeltaTime;
            pushInDirection.force = math.clamp(pushInDirection.force, -pushInDirection.maxForce, pushInDirection.maxForce);
            pushInDirection.force *= 1.0f - pushInDirection.drag*SystemAPI.Time.DeltaTime;
            
            trs.Position.xz += pushInDirection.force;
        }
    }
}