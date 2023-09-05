using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Runtime
{
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
    
    [UpdateBefore(typeof(SimplePhysics))]
    partial struct MoveNSWESystem : ISystem
    {
        [BurstCompile]
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
}
