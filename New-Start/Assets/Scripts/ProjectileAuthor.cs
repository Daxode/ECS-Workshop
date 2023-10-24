using System;
using Unity.Entities;
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
            var projectileEntity = GetEntity(TransformUsageFlags.None);
            AddComponent(projectileEntity, new Lifetime
            {
                timeLeft = authoring.lifetime
            });
        }
    }
}