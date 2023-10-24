using System;
using Unity.Entities;
using UnityEngine;

struct Lifetime : IComponentData
{
    public float timeLeft;
}

partial struct LifetimeSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // create projectile 1
        var projectileEntity1 = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(projectileEntity1, new Lifetime
        {
            timeLeft = 1.0f
        });
        state.EntityManager.SetName(projectileEntity1, "Projectile 1");
        
        // create another projectile
        var projectileEntity2 = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(projectileEntity2, new Lifetime
        {
            timeLeft = 2.0f
        });
        state.EntityManager.SetName(projectileEntity2, "Projectile 2");
    }

    public void OnUpdate(ref SystemState state)
    {
        // create an entity command buffer to destroy entities
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        
        // destroy projectiles when their lifetime is over
        foreach (var (lifetimeRef, e) in SystemAPI.Query<RefRW<Lifetime>>().WithEntityAccess())
        {
            lifetimeRef.ValueRW.timeLeft -= SystemAPI.Time.DeltaTime;
            if (lifetimeRef.ValueRW.timeLeft <= 0f)
            {
                ecb.DestroyEntity(e);
                Debug.Log($"Lifetime over for {state.EntityManager.GetName(e)}");
            }
        }
        
        // play back the entity command buffer
        ecb.Playback(state.EntityManager);
    }
}
