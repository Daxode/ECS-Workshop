using Unity.Entities;
using UnityEngine;

public class SpawnAuthor : MonoBehaviour
{
    [SerializeField] GameObject prefab;
    [SerializeField] float spawnInterval;
    [SerializeField] bool spawnOnStart;

    class Baker : Baker<SpawnAuthor>
    {
        public override void Bake(SpawnAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new SpawnerData
            {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.None),
                spawnInterval = authoring.spawnInterval,
                spawnTimer = authoring.spawnOnStart ? 0 : authoring.spawnInterval
            });
        }
    }
}
