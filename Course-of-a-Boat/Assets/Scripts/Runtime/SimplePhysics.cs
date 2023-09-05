using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Runtime
{
    public struct PushInDirection : IComponentData
    {
        // Settings
        public float maxForce;
        public float drag;

        // Runtime Only
        public float2 force;
        public float2 accelerationXZ;
    
    }
    
    public partial struct SimplePhysics : ISystem
    {
        [BurstCompile]
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
}
