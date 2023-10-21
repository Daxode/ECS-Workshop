using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class DashMovementAuthor : MonoBehaviour
{
    [SerializeField] InputAction dashInput;
    [SerializeField] float dashSpeed;
    [SerializeField] float dashInvincibilityDuration;
    [SerializeField] float dashCooldown;

    [Header("UI - Temp stored here")]
    [SerializeField] Sprite[] staminaSprites;

    class Baker : Baker<DashMovementAuthor>
    {
        public override void Bake(DashMovementAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            foreach (var staminaSprite in authoring.staminaSprites) DependsOn(staminaSprite);

            AddComponentObject(entity, new DashMovementManaged
            {
                dashInput = authoring.dashInput,
                dashSpeed = authoring.dashSpeed,
                dashInvincibilityDuration = authoring.dashInvincibilityDuration,
                dashCooldown = authoring.dashCooldown,
                staminaSprites = authoring.staminaSprites ?? Array.Empty<Sprite>(),
                lastStaminaSpriteIndex = -1
            });

            AddComponent<DashMovementData>(entity);
        }
    }
}

class DashMovementManaged : IComponentData
{
    public float dashCooldown;
    public InputAction dashInput;
    public float dashInvincibilityDuration;
    public float dashSpeed;
    public int lastStaminaSpriteIndex;
    public Sprite[] staminaSprites;
}

struct DashMovementData : IComponentData
{
    public bool isDashing;
    public float dashCooldownTimer;
}

[UpdateAfter(typeof(FollowPlayerSystem))]
partial struct DashMovementSystem : ISystem, ISystemStartStop
{
    public class UISingleton : IComponentData
    {
        public VisualElement healthBar;
        public VisualElement staminaBar;
        public UIDocument uiDocument;
    }

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DashMovementManaged>();
    }

    public void OnStartRunning(ref SystemState state)
    {
        foreach (var dashMovementManaged in SystemAPI.Query<DashMovementManaged>())
            dashMovementManaged.dashInput.Enable();

        // create UI
        var uiSingleton = new UISingleton
        {
            uiDocument = Object.FindObjectOfType<UIDocument>()
        };
        uiSingleton.staminaBar = uiSingleton.uiDocument.rootVisualElement.Q<VisualElement>("stamina");
        state.EntityManager.AddComponentObject(state.SystemHandle, uiSingleton);
    }

    public void OnStopRunning(ref SystemState state)
    {
        foreach (var dashMovementManaged in SystemAPI.Query<DashMovementManaged>())
            dashMovementManaged.dashInput.Disable();
    }

    public void OnUpdate(ref SystemState state)
    {
        var uiSingleton = SystemAPI.ManagedAPI.GetSingleton<UISingleton>();
        foreach (var (dashMovementManaged, data) in SystemAPI.Query<DashMovementManaged, RefRW<DashMovementData>>())
        {
            // update UI
            var current = data.ValueRO.dashCooldownTimer;
            var max = dashMovementManaged.dashCooldown;
            var percentage = math.saturate(1 - current / max);
            var spriteIndex = (int)math.floor(percentage * (dashMovementManaged.staminaSprites.Length - 1));
            if (spriteIndex != dashMovementManaged.lastStaminaSpriteIndex)
            {
                dashMovementManaged.lastStaminaSpriteIndex = spriteIndex;
                var sprite = dashMovementManaged.staminaSprites[spriteIndex];
                uiSingleton.staminaBar.style.backgroundImage = new StyleBackground(sprite);
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

// during physics update we dash
[UpdateInGroup(typeof(PhysicsSystemGroup))]
partial struct DashMovementSystemPhysics : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (dashMovementRef, dashMovementManaged, velocityRef, healthData) in SystemAPI.Query<
                     RefRW<DashMovementData>, DashMovementManaged, RefRW<PhysicsVelocity>,
                     RefRW<HealthData>>())
        {
            if (!dashMovementRef.ValueRO.isDashing) continue;
            dashMovementRef.ValueRW.isDashing = false;
            velocityRef.ValueRW.Linear.xz *= 1 + dashMovementManaged.dashSpeed;
            dashMovementRef.ValueRW.dashCooldownTimer = dashMovementManaged.dashCooldown;
            if (healthData.ValueRO.hitInvincibilityTimer > dashMovementManaged.dashInvincibilityDuration) continue;
            healthData.ValueRW.hitInvincibilityTimer = dashMovementManaged.dashInvincibilityDuration;
        }
    }
}
