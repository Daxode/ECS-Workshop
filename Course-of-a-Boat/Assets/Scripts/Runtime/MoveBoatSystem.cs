using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Runtime
{
    public partial struct MoveBoatSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!Input.GetKey(KeyCode.Space))
                return;
            
            foreach (var (velocityRef, ltRef, followRef) in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<LocalTransform>, RefRO<FollowMarkerData>>())
            {
                // go forward by speed
                velocityRef.ValueRW.Linear = ltRef.ValueRO.Forward() * (SystemAPI.Time.DeltaTime * followRef.ValueRO.speed);
            }
        }
    }
}
