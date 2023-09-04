using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Runtime
{
    struct MarkerTag : IComponentData {}
    public partial struct MoveBoatSystem : ISystem
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
            
            foreach (var (velocityRef, ltRef, followRef) in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<LocalTransform>, RefRO<FollowMarkerData>>())
            {
                // go forward by speed
                velocityRef.ValueRW.Linear = ltRef.ValueRO.Forward() * (SystemAPI.Time.DeltaTime * followRef.ValueRO.speed);
                
                // Log the distance to the marker
                Debug.Log($"Distance To Marker: {math.distance(ltRef.ValueRO.Position, markerLT.Position)}");
            }
        }
    }
}
