using System;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using SphereCollider = UnityEngine.SphereCollider;

[RequireComponent(typeof(SphereCollider))]
public class ProjectileAuthor : MonoBehaviour
{
    public int damage = 1;
    public float lifetime = 10f;

    [Tooltip("The model that will be rotated towards the direction of movement. If not set, the projectile will not rotate.")]
    [SerializeField] Transform model;

    class Baker : Baker<ProjectileAuthor>
    {
        public override void Bake(ProjectileAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AttackDamage
            {
                damage = authoring.damage
            });
            AddComponent<PhysicsVelocity>(entity);
            AddComponent(entity, new Lifetime
            {
                timeLeft = authoring.lifetime
            });
            
            // add model and rotate towards
            if (authoring.model)
            {
                AddComponent(entity, new ModelForEntity
                {
                    modelEntity = GetEntity(authoring.model, TransformUsageFlags.Dynamic)
                });
                AddComponent(entity, new RotateTowardsData
                {
                    speed = 1f,
                });
            }
        }
    }
}