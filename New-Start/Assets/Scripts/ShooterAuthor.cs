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
    [SerializeField] float shotForce = 10f;

    class Baker : Baker<ShooterAuthor>
    {
        public override void Bake(ShooterAuthor authoring)
        {
            DependsOn(authoring.transform);
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new Shooter
            {
                projectile = GetEntity(authoring.projectile.gameObject, TransformUsageFlags.Dynamic),
                shotForce = authoring.shotForce,
                shootLocationEntity = authoring.shotPos ? GetEntity(authoring.shotPos, TransformUsageFlags.Renderable) : entity
            });
            if (authoring.shootInput.bindings.Count > 0)
                AddComponentObject(entity, new ShooterManaged
                {
                    shootInput = authoring.shootInput,
                    shootAltInput = authoring.shootAltInput
                });
            SetComponentEnabled<Shooter>(entity, false);
        }
    }
}