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
        }
    }
}