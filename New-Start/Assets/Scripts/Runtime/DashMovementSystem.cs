using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

class DashMovementManaged : IComponentData
{
    public InputAction dashInput;
    public Sprite[] staminaSprites;
    public int lastStaminaSpriteIndex;
}

struct DashMovementData : IComponentData
{
    public bool isDashing;
    public float dashSpeed;
    public float dashCooldown;
    public float dashCooldownTimer;
    public float dashInvincibilityDuration;
}

// during physics update we dash
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
partial struct DashMovementSystemPhysics : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (dashMovementRef, velocityRef, healthData) in SystemAPI.Query<
                     RefRW<DashMovementData>, RefRW<PhysicsVelocity>, RefRW<HealthData>>())
        {
            var data = dashMovementRef.ValueRO;
            if (!data.isDashing) continue;
            dashMovementRef.ValueRW.isDashing = false;
            velocityRef.ValueRW.Linear.xz *= 1 + data.dashSpeed;
            dashMovementRef.ValueRW.dashCooldownTimer = data.dashCooldown;
            if (healthData.ValueRO.hitInvincibilityTimer > data.dashInvincibilityDuration) continue;
            healthData.ValueRW.hitInvincibilityTimer = data.dashInvincibilityDuration;
        }
    }
}

[UpdateAfter(typeof(FollowPlayerSystem))]
partial struct DashMovementSystem : ISystem, ISystemStartStop
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DashMovementManaged>();
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<CanvasSystem.ManagedData>().WithOptions(EntityQueryOptions.IncludeSystems).Build());
    }

    public void OnStartRunning(ref SystemState state)
    {
        foreach (var dashMovementManaged in SystemAPI.Query<DashMovementManaged>())
            dashMovementManaged.dashInput.Enable();
    }

    public void OnStopRunning(ref SystemState state)
    {
        foreach (var dashMovementManaged in SystemAPI.Query<DashMovementManaged>())
            dashMovementManaged.dashInput.Disable();
    }

    public void OnUpdate(ref SystemState state)
    {
        var canvasData = SystemAPI.ManagedAPI.GetSingleton<CanvasSystem.ManagedData>();
        foreach (var (dashMovementManaged, data) in SystemAPI.Query<DashMovementManaged, RefRW<DashMovementData>>())
        {
            // update UI
            var current = data.ValueRO.dashCooldownTimer;
            var max = data.ValueRO.dashCooldown;
            var percentage = math.saturate(1 - current / max);
            var spriteIndex = (int)math.floor(percentage * (dashMovementManaged.staminaSprites.Length - 1));
            if (spriteIndex != dashMovementManaged.lastStaminaSpriteIndex)
            {
                dashMovementManaged.lastStaminaSpriteIndex = spriteIndex;
                var sprite = dashMovementManaged.staminaSprites[spriteIndex];
                canvasData.staminaBar.style.backgroundImage = new StyleBackground(sprite);
            }

            // update timers
            if (data.ValueRW.dashCooldownTimer > 0)
            {
                data.ValueRW.dashCooldownTimer -= SystemAPI.Time.DeltaTime;
                continue;
            }

            var input = dashMovementManaged.dashInput.WasPerformedThisFrame();
            data.ValueRW.isDashing |= input;
        }
    }
}
