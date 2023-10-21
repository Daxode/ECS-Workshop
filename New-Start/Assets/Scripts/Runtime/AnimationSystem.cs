using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Object = UnityEngine.Object;

class CleanupAnimation : ICleanupComponentData
{
    public Animator animatorToCleanup;
}

class AnimationManaged : IComponentData
{
    public Animator animator;
}

[UpdateAfter(typeof(TransformSystemGroup))]
partial struct AnimationSystem : ISystem
{
    static readonly int k_DeathAnimatorID = Animator.StringToHash("Death");

    public void OnUpdate(ref SystemState state)
    {
        // ReSharper disable once Unity.Entities.SingletonMustBeRequested
        var ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        
        foreach (var (animationManaged, entity) in SystemAPI
                     .Query<AnimationManaged>().WithNone<CleanupAnimation>().WithEntityAccess())
        {
            var animatorInstance = Object.Instantiate(animationManaged.animator);
            ecb.AddComponent(entity, animatorInstance);
            ecb.AddComponent(entity, new CleanupAnimation { animatorToCleanup = animatorInstance });
        }
        
        // sync transform with animator
        foreach (var (ltw, animator) in
                 SystemAPI.Query<RefRO<LocalToWorld>, SystemAPI.ManagedAPI.UnityEngineComponent<Animator>>())
        {
            var animatorTransform = animator.Value.transform;
            animatorTransform.SetPositionAndRotation(ltw.ValueRO.Position, ltw.ValueRO.Rotation);
        }
        
        // if has no animator but has cleanup component, destroy the animator
        foreach (var (animator, e) in SystemAPI.Query<CleanupAnimation>().WithNone<Parent>().WithEntityAccess())
        {
            animator.animatorToCleanup.SetTrigger(k_DeathAnimatorID);
            Object.Destroy(animator.animatorToCleanup.gameObject, 2f);
            ecb.RemoveComponent<CleanupAnimation>(e);
        }
    }

}

#region Editor
struct DrawInEditor : IComponentData
{
    public Entity entity;
}

[WorldSystemFilter(WorldSystemFilterFlags.Editor)]
[UpdateAfter(typeof(TransformSystemGroup))]
partial struct SpawnToDrawInEditorSystem : ISystem, ISystemStartStop
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DrawInEditor>();
    }

    [BurstCompile]
    public void OnStartRunning(ref SystemState state)
    {
        // ReSharper disable once Unity.Entities.SingletonMustBeRequested
        var ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // Spawn the entity to draw in editor
        foreach (var (drawnEntities, parent) in SystemAPI.Query<DrawInEditor>().WithEntityAccess())
        {
            var e = state.EntityManager.Instantiate(drawnEntities.entity);
            if (SystemAPI.HasComponent<Parent>(e))
                SystemAPI.SetComponent(e, new Parent { Value = parent });
            else
                ecb.AddComponent(e, new Parent { Value = parent });
        }

        state.EntityManager.RemoveComponent<DrawInEditor>(
            SystemAPI.QueryBuilder().WithAll<DrawInEditor>().Build());
    }

    public void OnStopRunning(ref SystemState state) {}
}
#endregion