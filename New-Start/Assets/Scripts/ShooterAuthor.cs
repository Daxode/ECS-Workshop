using System;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
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
            AddComponentObject(entity, new ShooterManaged
            {
                shootInput = authoring.shootInput,
                shootAltInput = authoring.shootAltInput
            });
        }
    }
}

class ShooterManaged : IComponentData
{
    public InputAction shootAltInput;
    public InputAction shootInput;
}

struct Shooter : IComponentData
{
    public Entity projectile;
    public float shotForce;
    public Entity shootLocationEntity;
}

[UpdateBefore(typeof(TransformSystemGroup))]
partial struct ShooterSystem : ISystem, ISystemStartStop
{
    public void OnUpdate(ref SystemState state)
    {
        SystemAPI.ManagedAPI.TryGetSingleton<PlayerTurnAroundManaged>(out var turnsAround);
        foreach (var (shooterRef, shooterManaged, shootingEntity) in SystemAPI.Query<RefRW<Shooter>, ShooterManaged>().WithEntityAccess())
        {
            if (!(shooterManaged.shootInput.triggered || shooterManaged.shootAltInput.triggered)) continue;

            // if last press was alt, set turnsAround.followMouseInstead to true
            if (turnsAround != null)
                turnsAround.followMouseInstead = shooterManaged.shootAltInput.triggered;

            // shoot
            var instance = state.EntityManager.Instantiate(shooterRef.ValueRO.projectile);
            var shootLtw = SystemAPI.GetComponent<LocalToWorld>(shooterRef.ValueRO.shootLocationEntity); // gets last frame's transform
            SystemAPI.SetComponent(instance, LocalTransform.FromPosition(shootLtw.Position));
            SystemAPI.SetComponent(instance, new PhysicsVelocity { Linear = shootLtw.Forward * shooterRef.ValueRO.shotForce });
            SystemAPI.GetComponentRW<AttackDamage>(instance).ValueRW.owningEntity = shootingEntity;
        }
    }

    public void OnCreate(ref SystemState state) 
        => state.RequireForUpdate<Shooter>();

    public void OnStartRunning(ref SystemState state)
    {
        foreach (var shooterManaged in SystemAPI.Query<ShooterManaged>())
        {
            shooterManaged.shootInput.Enable();
            shooterManaged.shootAltInput.Enable();
        }
    }

    public void OnStopRunning(ref SystemState state)
    {
        foreach (var shooterManaged in SystemAPI.Query<ShooterManaged>())
        {
            shooterManaged.shootInput.Disable();
            shooterManaged.shootAltInput.Disable();
        }
    }
}
