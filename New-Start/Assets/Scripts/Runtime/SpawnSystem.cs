using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

struct SpawnerData : IComponentData
{
    public Entity prefab;
    public float spawnInterval;
    public float spawnTimer;
}

public partial struct SpawnSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (dataRef, ltw) in SystemAPI.Query<RefRW<SpawnerData>, LocalToWorld>())
        {
            if (dataRef.ValueRO.spawnTimer > 0)
                dataRef.ValueRW.spawnTimer -= SystemAPI.Time.DeltaTime;
            else
            {
                dataRef.ValueRW.spawnTimer = dataRef.ValueRO.spawnInterval;
                var instance = state.EntityManager.Instantiate(dataRef.ValueRO.prefab);
                SystemAPI.SetComponent(instance, new LocalTransform
                {
                    Position = ltw.Position,
                    Rotation = quaternion.identity,
                    Scale = 1
                });
                SystemAPI.SetComponent(instance, ltw);
            }
        }
    }
}
