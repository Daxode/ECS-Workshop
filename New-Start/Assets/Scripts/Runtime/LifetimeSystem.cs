using System;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

struct Lifetime : IComponentData
{
    public float timeLeft;
}

// destroy projectiles after lifetime
partial struct LifetimeSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // ReSharper disable once Unity.Entities.SingletonMustBeRequested - Reason: this is known to always exist
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (lifetimeRef, e) in SystemAPI.Query<RefRW<Lifetime>>().WithEntityAccess())
        {
            lifetimeRef.ValueRW.timeLeft -= Time.deltaTime;
            if (lifetimeRef.ValueRW.timeLeft <= 0f) ecb.DestroyEntity(e);
        }
    }
}
