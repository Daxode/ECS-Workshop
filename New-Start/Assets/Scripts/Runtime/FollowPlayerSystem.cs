using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

struct FollowPlayerData : IComponentData
{
    public float speed;
    public float2 slowDownRange;
    public float shootInterval;
    public float shootTimer;
}

partial struct FollowPlayerSystem : ISystem
{
    static readonly int k_Blend = Animator.StringToHash("Blend");

    [BurstCompile]
    public void OnCreate(ref SystemState state) 
        => state.RequireForUpdate<PlayerMovementManaged>();
    
    public void OnUpdate(ref SystemState state)
    {
        var playerEntity = SystemAPI.ManagedAPI.GetSingletonEntity<PlayerMovementManaged>();
        var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

        foreach (var (followPlayerDataRef, ltRef, velRef, rotateTowardsData, model, shooterEnabledRef) in SystemAPI.Query<
                     RefRW<FollowPlayerData>, RefRO<LocalTransform>,
                     RefRW<PhysicsVelocity>, RefRW<RotateTowardsData>, ModelForEntity,
                     EnabledRefRW<Shooter>>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
        {
            var followPlayerData = followPlayerDataRef.ValueRO;

            var direction = playerPosition - ltRef.ValueRO.Position;
            var dirNormalized = math.normalize(direction);
            var velocity = dirNormalized * followPlayerData.speed;

            // slow down when close
            var distance = math.length(direction);
            velocity *= math.smoothstep(followPlayerData.slowDownRange.x, followPlayerData.slowDownRange.y, distance);
            velRef.ValueRW.Linear = velocity;

            if (followPlayerDataRef.ValueRO.shootTimer > 0)
                followPlayerDataRef.ValueRW.shootTimer -= SystemAPI.Time.DeltaTime;
            else if (distance < followPlayerData.slowDownRange.y)
            {
                followPlayerDataRef.ValueRW.shootTimer = followPlayerDataRef.ValueRO.shootInterval;
                shooterEnabledRef.ValueRW = true;
            }
            
            // set animator
            if (SystemAPI.ManagedAPI.HasComponent<Animator>(model.modelEntity))
            {
                var animator = SystemAPI.ManagedAPI.GetComponent<Animator>(model.modelEntity);
                animator.SetFloat(k_Blend, math.length(velocity));
            }

            // rotate towards
            rotateTowardsData.ValueRW.direction = dirNormalized;
        }
    }
}
