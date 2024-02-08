using Unity.Entities;
using UnityEngine;

class HealthAuthor : MonoBehaviour
{
    [SerializeField] int maxHealth;
    [SerializeField] ParticleSystem damageParticles;

    class HealthAuthorBaker : Baker<HealthAuthor>
    {
        public override void Bake(HealthAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            AddComponent(entity, new HealthData
            {
                health = authoring.maxHealth,
                maxHealth = authoring.maxHealth,
                damageParticles = GetEntity(authoring.damageParticles, TransformUsageFlags.Renderable),
            });
        }
    }
}