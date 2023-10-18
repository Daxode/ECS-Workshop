using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(StillRotationModelAuthor))]
public class FollowPlayerAuthor : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] float2 m_SlowDownRange = new (4, 6);
    
    class Baker : Baker<FollowPlayerAuthor>
    {
        public override void Bake(FollowPlayerAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new FollowPlayerData
            {
                speed = authoring.speed,
                slowDownRange = authoring.m_SlowDownRange
            });
        }
    }

    internal struct FollowPlayerData : IComponentData
    {
        public Entity playerEntity;
        public float speed;
        public float2 slowDownRange;
    }
}

partial struct FollowPlayerSystem : ISystem
{
    static readonly int k_Blend = Animator.StringToHash("Blend");

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (followPlayerDataRef, ltRef, velRef, rotateTowardsData, model) in SystemAPI.Query<
                     RefRW<FollowPlayerAuthor.FollowPlayerData>, RefRO<LocalTransform>, 
                     RefRW<PhysicsVelocity>, RefRW<RotateTowardsData>, ModelForEntity>())
        {
            var followPlayerData = followPlayerDataRef.ValueRO;
            if (followPlayerData.playerEntity == Entity.Null)
                followPlayerDataRef.ValueRW.playerEntity = SystemAPI.ManagedAPI.GetSingletonEntity<PlayerMovementAuthor.PlayerInputManaged>();
            else {
                if (!SystemAPI.Exists(followPlayerData.playerEntity)) return;
                var playerPosition = SystemAPI.GetComponent<LocalTransform>(followPlayerData.playerEntity).Position;
                var direction = playerPosition - ltRef.ValueRO.Position;
                var dirNormalized = math.normalize(direction);
                var velocity = dirNormalized * followPlayerData.speed;
                
                // slow down when close
                var distance = math.length(direction);
                velocity *= math.smoothstep(followPlayerData.slowDownRange.x, followPlayerData.slowDownRange.y, distance);
                velRef.ValueRW.Linear = velocity;
                
                // set animator
                if (SystemAPI.ManagedAPI.HasComponent<Animator>(model.modelEntity)) {
                    var animator = SystemAPI.ManagedAPI.GetComponent<Animator>(model.modelEntity);
                    animator.SetFloat(k_Blend, math.length(velocity));
                }
                
                // rotate towards
                rotateTowardsData.ValueRW.direction = dirNormalized;
            }
        }
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))] // We are updating after `PhysicsSimulationGroup` - this means that we will get the events of the current frame.
public partial struct HitPlayerEventsSystem : ISystem
{
    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // check if hit player
        var hitPlayerJob = new HitPlayer {
            damageDataLookup = SystemAPI.GetComponentLookup<FollowPlayerAuthor.FollowPlayerData>(),
            healthDataLookup = SystemAPI.GetComponentLookup<HealthData>(),
        };
        var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
        state.Dependency = hitPlayerJob.Schedule(simulationSingleton, state.Dependency);
    }
}


// using collision job check if hit player
struct HitPlayer : ITriggerEventsJob {
    [ReadOnly] public ComponentLookup<FollowPlayerAuthor.FollowPlayerData> damageDataLookup;
    public ComponentLookup<HealthData> healthDataLookup;
    
    public void Execute(TriggerEvent collisionEvent) {
        var damageableEntity = Entity.Null;
        var damagingEntity = Entity.Null;
        if (damageDataLookup.HasComponent(collisionEvent.EntityA)) {
            if (healthDataLookup.HasComponent(collisionEvent.EntityB)) {
                damageableEntity = collisionEvent.EntityB;
                damagingEntity = collisionEvent.EntityA;
            }
        }
        if (damageableEntity == Entity.Null && damageDataLookup.HasComponent(collisionEvent.EntityB)) {
            if (healthDataLookup.HasComponent(collisionEvent.EntityA)) {
                damageableEntity = collisionEvent.EntityA;
                damagingEntity = collisionEvent.EntityB;
            }
        }
        if (damageableEntity == Entity.Null) return;
        
        // damage player by one
        var healthData = healthDataLookup[damageableEntity];
        if (healthData.hitInvincibilityTimer > 0) return;
        healthData.health -= 1;
        healthData.hitInvincibilityTimer = 1;
        healthDataLookup[damageableEntity] = healthData;
    }
}