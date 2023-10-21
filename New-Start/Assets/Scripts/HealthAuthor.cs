using System;
using Unity.Entities;
using UnityEngine;

class HealthAuthor : MonoBehaviour
{
    [SerializeField] int maxHealth;
    [SerializeField] Sprite[] healthSprites;
    [SerializeField] ParticleSystem damageParticles;

    class HealthAuthorBaker : Baker<HealthAuthor>
    {
        public override void Bake(HealthAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            if (authoring.healthSprites != null)
                foreach (var healthSprite in authoring.healthSprites)
                    DependsOn(healthSprite);

            AddComponentObject(entity, new HealthManagedData
            {
                healthSprites = authoring.healthSprites ?? Array.Empty<Sprite>(),
                damageParticles = GetEntity(authoring.damageParticles, TransformUsageFlags.Renderable),
                lastHealthSpriteIndex = -1
            });
            AddComponent(entity, new HealthData
            {
                health = authoring.maxHealth,
                maxHealth = authoring.maxHealth
            });
        }
    }
}