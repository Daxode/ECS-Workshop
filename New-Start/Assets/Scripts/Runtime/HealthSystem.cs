using System;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

struct HealthData : IComponentData
{
    // Health
    public int health;
    public int maxHealth;
    
    // Hit invincibility
    public float hitInvincibilityTimer;
    public bool hitIsTriggered;
    
    // Effects
    public Entity damageParticles;
}

[UpdateBefore(typeof(HealthSystem))]
partial struct PreHealthSystem : ISystem
{
    public void OnCreate(ref SystemState state) 
        => state.RequireForUpdate<HealthData>();

    public void OnUpdate(ref SystemState state)
    {
        foreach (var dataRef in SystemAPI.Query<RefRW<HealthData>>())
        {
            // skip if dead
            if (dataRef.ValueRO.health <= 0) continue;

            // skip if not hit this frame
            if (!dataRef.ValueRO.hitIsTriggered) continue;

            // play damage particles
            if (dataRef.ValueRO.damageParticles != Entity.Null 
                && SystemAPI.ManagedAPI.HasComponent<ParticleSystem>(dataRef.ValueRO.damageParticles))
            {
                var damageParticles = SystemAPI.ManagedAPI.GetComponent<ParticleSystem>(dataRef.ValueRO.damageParticles);
                damageParticles.Play();
            }
        }
    }
}

partial struct HealthSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) 
        => state.RequireForUpdate<HealthData>();

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (Input.GetKeyDown(KeyCode.Space))
            foreach (var data in SystemAPI.Query<RefRW<HealthData>>().WithAll<Selectable>()) 
                data.ValueRW.health--;

        // ReSharper disable once Unity.Entities.SingletonMustBeRequested - Reason: this is known to always exist
        var ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (dataRef, offsetXYScaleZw, frames, e) in SystemAPI.Query<RefRW<HealthData>, RefRW<MaterialOverrideOffsetXYScaleZW>, DynamicBuffer<SpriteFrameElement>>().WithEntityAccess())
        {
            // update invincibility timer
            if (dataRef.ValueRW.hitInvincibilityTimer > 0)
                dataRef.ValueRW.hitInvincibilityTimer -= SystemAPI.Time.DeltaTime;

            offsetXYScaleZw.ValueRW.Value.xy = frames[(int)((frames.Length-1) * (dataRef.ValueRO.health / (float)dataRef.ValueRO.maxHealth))].offset;
            
            // if dead, destroy and skip
            if (dataRef.ValueRO.health <= 0)
            {
                ecb.DestroyEntity(e);
                continue;
            }
            
            // reset hit trigger
            if (!dataRef.ValueRO.hitIsTriggered) continue;
            dataRef.ValueRW.hitIsTriggered = false;
        }
    }
}
