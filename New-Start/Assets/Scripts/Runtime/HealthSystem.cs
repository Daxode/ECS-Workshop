using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

struct HealthData : IComponentData
{
    public int health;
    public int maxHealth;
    public float hitInvincibilityTimer;
}

class HealthManagedData : IComponentData
{
    public Entity damageParticles;
    public Sprite[] healthSprites;
    public int lastHealthSpriteIndex;
}

partial struct HealthSystem : ISystem, ISystemStartStop
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<HealthManagedData>();
        state.RequireForUpdate<DashMovementSystem.UISingleton>();
    }

    public void OnStartRunning(ref SystemState state)
    {
        var uiSingleton = SystemAPI.ManagedAPI.GetSingleton<DashMovementSystem.UISingleton>();
        uiSingleton.healthBar = uiSingleton.uiDocument.rootVisualElement.Q("health");
    }

    public void OnStopRunning(ref SystemState state) {}

    public void OnUpdate(ref SystemState state)
    {
        // ReSharper disable once Unity.Entities.SingletonMustBeRequested - Reason: this is known to always exist
        var ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        var uiSingleton = SystemAPI.ManagedAPI.GetSingleton<DashMovementSystem.UISingleton>();
        foreach (var (healthManagedData, dataRef, e) in SystemAPI
                     .Query<HealthManagedData, RefRW<HealthData>>().WithEntityAccess())
        {
            // update invincibility timer
            if (dataRef.ValueRW.hitInvincibilityTimer > 0)
                dataRef.ValueRW.hitInvincibilityTimer -= SystemAPI.Time.DeltaTime;

            // check if dead
            if (dataRef.ValueRO.health <= 0) 
                ecb.DestroyEntity(e);

            // update UI
            if (dataRef.ValueRO.health <= 0) continue;
            var percentage = math.saturate(dataRef.ValueRO.health / (float)dataRef.ValueRO.maxHealth);
            var healthSpriteIndex = (int)math.floor(percentage * (healthManagedData.healthSprites.Length - 1));
            if (healthSpriteIndex != healthManagedData.lastHealthSpriteIndex)
            {
                // display only if player
                if (SystemAPI.ManagedAPI.HasComponent<PlayerMovementManaged>(e))
                    uiSingleton.healthBar.style.backgroundImage =
                        new StyleBackground(healthManagedData.healthSprites[healthSpriteIndex]);
                healthManagedData.lastHealthSpriteIndex = healthSpriteIndex;

                // skip if health is full
                if (healthSpriteIndex == healthManagedData.healthSprites.Length - 1) continue;

                // play damage particles
                if (healthManagedData.damageParticles != Entity.Null &&
                    SystemAPI.ManagedAPI.HasComponent<ParticleSystem>(healthManagedData.damageParticles))
                {
                    var damageParticles =
                        SystemAPI.ManagedAPI.GetComponent<ParticleSystem>(healthManagedData.damageParticles);
                    damageParticles.Play();
                }
            }
        }
    }
}
