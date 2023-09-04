using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;

namespace Runtime
{
    struct MarkerTag : IComponentData {}
    public partial struct MoveToMarkerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MarkerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var marker = SystemAPI.GetSingletonEntity<MarkerTag>();
            var markerLT = SystemAPI.GetComponent<LocalTransform>(marker);
            
            if (!Input.GetKey(KeyCode.Space))
                return;
            
            foreach (var (velocityRef, ltRef, massRef, followRef) in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<LocalTransform>, RefRO<PhysicsMass>, RefRO<FollowMarkerData>>())
            {
                // check if we are close enough to marker otherwise stop
                if (math.distancesq(markerLT.Position.xz, ltRef.ValueRO.Position.xz) > 10*10f)
                {
                    velocityRef.ValueRW.Linear = float3.zero;
                    velocityRef.ValueRW.Angular = float3.zero;
                    continue;
                }
        
                // go forward by speed
                var speed = math.smoothstep(5*5, 6*6, math.distancesq(markerLT.Position.xz, ltRef.ValueRO.Position.xz)); // slows down at 6m to marker, stops at 5m
                velocityRef.ValueRW.Linear = ltRef.ValueRO.Forward() * (speed * SystemAPI.Time.DeltaTime * followRef.ValueRO.speed);
            
                // rotate towards marker
                var currentForward = ltRef.ValueRO.Forward().xz;
                var targetForward = math.normalize(markerLT.Position.xz - ltRef.ValueRO.Position.xz);
                var angle = Vector2.SignedAngle(targetForward, currentForward);
                angle = angle < 0.1f && angle > -0.1f ? 0f : angle; // deadzone
                velocityRef.ValueRW.SetAngularVelocityWorldSpace(in massRef.ValueRO, ltRef.ValueRO.Rotation, math.up() * (math.sign(angle) * SystemAPI.Time.DeltaTime * followRef.ValueRO.turnSpeed));
            }
        }
    }
}
