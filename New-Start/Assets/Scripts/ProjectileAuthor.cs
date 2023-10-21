using System;
using Unity.Burst;
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

struct AttackDamage : IComponentData
{
    public int damage;
    public Entity owningEntity;
}

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
