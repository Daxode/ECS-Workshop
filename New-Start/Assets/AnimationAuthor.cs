using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace DefaultNamespace {
    public class AnimationAuthor : MonoBehaviour {
        [SerializeField] Animator animator;
        public class AnimationManaged : IComponentData {
            public Animator animator;
        }
        
        class AnimationAuthorBaker : Baker<AnimationAuthor> {
            public override void Bake(AnimationAuthor authoring) {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponentObject(entity, new AnimationManaged {
                    animator = authoring.animator
                });

                if (IsBakingForEditor()) {
                    AddComponent(entity, new DrawInEditor {
                        entity = GetEntity(authoring.animator, TransformUsageFlags.Renderable | TransformUsageFlags.WorldSpace)
                    });
                }
            }
        }

        internal struct DrawInEditor : IComponentData {
            public Entity entity;
        }
    }
    
    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
    [UpdateAfter(typeof(TransformSystemGroup))]
    partial struct SpawnToDrawInEditorSystem : ISystem, ISystemStartStop {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<AnimationAuthor.DrawInEditor>();
        }
        public void OnStartRunning(ref SystemState state) {
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (drawnEntities, parent) in SystemAPI.Query<AnimationAuthor.DrawInEditor>().WithEntityAccess()) {
                var e = state.EntityManager.Instantiate(drawnEntities.entity);
                if (SystemAPI.HasComponent<Parent>(e)) {
                    SystemAPI.SetComponent(e, new Parent {Value = parent});
                } else {
                    ecb.AddComponent(e, new Parent {Value = parent});
                }
            }
            state.EntityManager.RemoveComponent<AnimationAuthor.DrawInEditor>(
                    SystemAPI.QueryBuilder().WithAll<AnimationAuthor.DrawInEditor>().Build());
        }

        public void OnStopRunning(ref SystemState state) {
            
        }
    }
    
    
    partial struct AnimationSystem : ISystem, ISystemStartStop {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<AnimationAuthor.AnimationManaged>();
        }
        public void OnStartRunning(ref SystemState state) {
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            foreach (var (animationManaged, entity) in SystemAPI.Query<AnimationAuthor.AnimationManaged>().WithEntityAccess()) {
                var animatorInstance = Object.Instantiate(animationManaged.animator);
                ecb.AddComponent(entity, animatorInstance);
            }
        }

        public void OnStopRunning(ref SystemState state) {}
    }
    
    [UpdateAfter(typeof(TransformSystemGroup))]
    partial struct SyncTransformWithGOSystem : ISystem {
        public void OnUpdate(ref SystemState state) {
            foreach (var (ltw, animator) in 
                     SystemAPI.Query<RefRO<LocalToWorld>, SystemAPI.ManagedAPI.UnityEngineComponent<Animator>>()) {
                var animatorTransform = animator.Value.transform;
                animatorTransform.SetPositionAndRotation(ltw.ValueRO.Position, ltw.ValueRO.Rotation);
            }
        }
    }
}

