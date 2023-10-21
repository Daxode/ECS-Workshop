using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

struct SpawnerData : IComponentData
{
    public Entity enemyPrefab;
    public float spawnInterval;
    public float spawnTimer;
}

public partial struct EnemySpawner : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var dataRef in SystemAPI.Query<RefRW<SpawnerData>>())
        {
            if (dataRef.ValueRO.spawnTimer > 0)
                dataRef.ValueRW.spawnTimer -= SystemAPI.Time.DeltaTime;
            else
            {
                dataRef.ValueRW.spawnTimer = dataRef.ValueRO.spawnInterval;
                var instance = state.EntityManager.Instantiate(dataRef.ValueRO.enemyPrefab);
                SystemAPI.SetComponent(instance, new LocalTransform
                {
                    Position = new float3(0, 0, 0),
                    Rotation = quaternion.identity,
                    Scale = 1
                });
            }
        }
    }
}
