using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShooterAuthor : MonoBehaviour
{
    [SerializeField] InputAction shootInput;
    [SerializeField] InputAction shootAltInput;

    [SerializeField] ProjectileAuthor projectile;
    [SerializeField] Transform shotPos;
#pragma warning disable CS0414
    [SerializeField] float shotForce = 10f;
#pragma warning restore CS0414
    
    class Baker : Baker<ShooterAuthor>
    {
        public override void Bake(ShooterAuthor authoring)
        {
            DependsOn(authoring.transform);
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new Shooter
            {
                projectile = GetEntity(authoring.projectile.gameObject, TransformUsageFlags.Dynamic),
                shootLocationEntity = authoring.shotPos ? GetEntity(authoring.shotPos, TransformUsageFlags.Renderable) : entity
            });
        }
    }
}