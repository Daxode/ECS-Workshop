using Unity.Entities;
using UnityEngine;

public class EnemySpawnAuthor : MonoBehaviour
{
    [SerializeField] GameObject enemyPrefab;
    [SerializeField] float spawnInterval;
    
    class Baker : Baker<EnemySpawnAuthor>
    {
        public override void Bake(EnemySpawnAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new SpawnerData
            {
                enemyPrefab = GetEntity(authoring.enemyPrefab, TransformUsageFlags.Dynamic),
                spawnInterval = authoring.spawnInterval,
                spawnTimer = authoring.spawnInterval
            });
        }
    }
}
