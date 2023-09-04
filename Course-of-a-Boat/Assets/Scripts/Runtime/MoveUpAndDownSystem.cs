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
