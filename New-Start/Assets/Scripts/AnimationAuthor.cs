using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DefaultNamespace
{
    public class AnimationAuthor : MonoBehaviour
    {
        [SerializeField] Animator animator;

        public class AnimationManaged : IComponentData
        {
            public Animator animator;
        }

        class AnimationAuthorBaker : Baker<AnimationAuthor>
        {
            public override void Bake(AnimationAuthor authoring)
            {
                if (authoring.animator == null)
                    return;

                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponentObject(entity, new AnimationManaged
                {
                    animator = authoring.animator
                });

                if (IsBakingForEditor())
                    AddComponent(entity, new DrawInEditor
                    {
                        entity = GetEntity(authoring.animator,
                            TransformUsageFlags.Renderable | TransformUsageFlags.WorldSpace)
                    });
            }
        }

        internal struct DrawInEditor : IComponentData
        {
            public Entity entity;
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
    [UpdateAfter(typeof(TransformSystemGroup))]
    partial struct SpawnToDrawInEditorSystem : ISystem, ISystemStartStop
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AnimationAuthor.DrawInEditor>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            // ReSharper disable once Unity.Entities.SingletonMustBeRequested
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Spawn the entity to draw in editor
            foreach (var (drawnEntities, parent) in SystemAPI.Query<AnimationAuthor.DrawInEditor>().WithEntityAccess())
            {
                var e = state.EntityManager.Instantiate(drawnEntities.entity);
                if (SystemAPI.HasComponent<Parent>(e))
                    SystemAPI.SetComponent(e, new Parent { Value = parent });
                else
                    ecb.AddComponent(e, new Parent { Value = parent });
            }

            state.EntityManager.RemoveComponent<AnimationAuthor.DrawInEditor>(
                SystemAPI.QueryBuilder().WithAll<AnimationAuthor.DrawInEditor>().Build());
        }

        public void OnStopRunning(ref SystemState state) {}
    }

    class CleanupAnimation : ICleanupComponentData
    {
        public Animator animatorToCleanup;
    }

    partial struct AnimationSystem : ISystem, ISystemStartStop
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AnimationAuthor.AnimationManaged>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            // ReSharper disable once Unity.Entities.SingletonMustBeRequested
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (animationManaged, entity) in SystemAPI.Query<AnimationAuthor.AnimationManaged>()
                         .WithEntityAccess())
            {
                var animatorInstance = Object.Instantiate(animationManaged.animator);
                ecb.AddComponent(entity, animatorInstance);
                ecb.AddComponent(entity, new CleanupAnimation { animatorToCleanup = animatorInstance });
            }
        }

        public void OnStopRunning(ref SystemState state) {}
    }

    [UpdateAfter(typeof(TransformSystemGroup))]
    partial struct SyncTransformWithGoSystem : ISystem
    {
        static readonly int k_DeathAnimatorID = Animator.StringToHash("Death");

        public void OnUpdate(ref SystemState state)
        {
            // sync transform with animator
            foreach (var (ltw, animator) in
                     SystemAPI.Query<RefRO<LocalToWorld>, SystemAPI.ManagedAPI.UnityEngineComponent<Animator>>())
            {
                var animatorTransform = animator.Value.transform;
                animatorTransform.SetPositionAndRotation(ltw.ValueRO.Position, ltw.ValueRO.Rotation);
            }

            // ReSharper disable once Unity.Entities.SingletonMustBeRequested
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // if has no animator but has cleanup component, destroy the animator
            foreach (var (animator, e) in SystemAPI.Query<CleanupAnimation>().WithNone<Parent>().WithEntityAccess())
            {
                animator.animatorToCleanup.SetTrigger(k_DeathAnimatorID);
                Object.Destroy(animator.animatorToCleanup.gameObject, 2f);
                ecb.RemoveComponent<CleanupAnimation>(e);
            }

            // if no leg but has children, add leg
            foreach (var entity in SystemAPI.QueryBuilder().WithAll<Child>().WithNone<LinkedEntityGroup, Parent>()
                         .Build().ToEntityArray(state.WorldUpdateAllocator))
            {
                var leg = state.EntityManager.AddBuffer<LinkedEntityGroup>(entity);
                var childQueue = new NativeQueue<Entity>(state.WorldUpdateAllocator);
                childQueue.Enqueue(entity);
                while (childQueue.TryDequeue(out var parent))
                {
                    leg.Add(parent);
                    if (!SystemAPI.HasBuffer<Child>(parent))
                        continue;
                    foreach (var child in SystemAPI.GetBuffer<Child>(parent))
                        childQueue.Enqueue(child.Value);
                }
            }
        }
    }
}
