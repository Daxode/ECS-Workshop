using System;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.InputSystem;

class ShooterManaged : IComponentData
{
    public InputAction shootAltInput;
    public InputAction shootInput;
}

struct Shooter : IComponentData, IEnableableComponent
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
        foreach (var (shooterRef, shooterManaged) in 
                 SystemAPI.Query<EnabledRefRW<Shooter>, ShooterManaged>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
        {
            if (!(shooterManaged.shootInput.triggered || shooterManaged.shootAltInput.triggered)) continue;

            // if last press was alt, set turnsAround.followMouseInstead to true
            if (turnsAround != null)
                turnsAround.followMouseInstead = shooterManaged.shootAltInput.triggered;
            
            shooterRef.ValueRW = true;
        }
        
        foreach (var (shooterRef, enabled, shootingEntity) in SystemAPI.Query<RefRO<Shooter>, EnabledRefRW<Shooter>>().WithEntityAccess())
        {
            enabled.ValueRW = false;
            
            // shoot
            var instance = state.EntityManager.Instantiate(shooterRef.ValueRO.projectile);
            var shootLtw = SystemAPI.GetComponent<LocalToWorld>(shooterRef.ValueRO.shootLocationEntity); // gets last frame's transform
            SystemAPI.SetComponent(instance, LocalTransform.FromPosition(shootLtw.Position));
            SystemAPI.SetComponent(instance, new PhysicsVelocity { Linear = shootLtw.Forward * shooterRef.ValueRO.shotForce });
            SystemAPI.GetComponentRW<AttackDamage>(instance).ValueRW.owningEntity = shootingEntity;
            if (SystemAPI.HasComponent<RotateTowardsData>(instance))
                SystemAPI.SetComponent(instance, new RotateTowardsData { speed = 1f, direction = shootLtw.Forward });
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
