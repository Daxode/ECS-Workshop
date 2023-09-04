using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Runtime
{
    public struct MoveUpAndDown : IComponentData
    {
        public float speed;
        public float amplitude;
        public float offset;
    }

    partial struct MoveUpAndDownSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var e = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(e, new MoveUpAndDown
            {
                speed = 1.0f,
                amplitude = 0.2f,
                offset = 0.0f
            });
            state.EntityManager.AddComponent<LocalTransform>(e);
            
            var e2 = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(e2, new MoveUpAndDown
            {
                speed = 1.0f,
                amplitude = 0.2f,
                offset = 0.0f
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var i = 0;
            foreach (var (trsRef, moveUpAndDown) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveUpAndDown>>())
            {
                var yLevel = moveUpAndDown.ValueRO.offset;
                yLevel += math.sin((float)SystemAPI.Time.ElapsedTime * moveUpAndDown.ValueRO.speed + i*1.5f) * moveUpAndDown.ValueRO.amplitude;
                trsRef.ValueRW.Position.y = yLevel;
            
                i++;
            }
        }
    }
}
