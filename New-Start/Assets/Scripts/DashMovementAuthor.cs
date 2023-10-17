using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.InputSystem;

public class DashMovementAuthor : MonoBehaviour {
    [SerializeField] InputAction dashInput;
    [SerializeField] float dashSpeed;
    [SerializeField] float dashInvincibilityDuration;
    [SerializeField] float dashCooldown;
    
    class Baker : Baker<DashMovementAuthor> {
        public override void Bake(DashMovementAuthor authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, new DashMovementManaged {
                dashInput = authoring.dashInput,
                dashSpeed = authoring.dashSpeed,
                dashInvincibilityDuration = authoring.dashInvincibilityDuration,
                dashCooldown = authoring.dashCooldown,
            });
            
            AddComponent<DashMovementData>(entity);
        }
    }
}

class DashMovementManaged : IComponentData {
    public InputAction dashInput;
    public float dashSpeed;
    public float dashInvincibilityDuration;
    public float dashCooldown;
    
}

struct DashMovementData : IComponentData {
    public bool isDashing;
    public float dashInvincibilityTimer;
    public float dashCooldownTimer;
}

[UpdateAfter(typeof(FollowPlayerSystem))]
partial struct DashMovementSystem : ISystem, ISystemStartStop {
    public void OnCreate(ref SystemState state) 
        => state.RequireForUpdate<DashMovementManaged>();

    public void OnStartRunning(ref SystemState state) {
        foreach (var dashMovementManaged in SystemAPI.Query<DashMovementManaged>()) {
            dashMovementManaged.dashInput.Enable();
        }
    }

    public void OnStopRunning(ref SystemState state) {
        foreach (var dashMovementManaged in SystemAPI.Query<DashMovementManaged>()) {
            dashMovementManaged.dashInput.Disable();
        }
    }

    public void OnUpdate(ref SystemState state) {
        foreach (var (dashMovementManaged, data) in SystemAPI.Query<DashMovementManaged, RefRW<DashMovementData>>()) {
            // update timers
            if (data.ValueRW.dashInvincibilityTimer > 0) {
                data.ValueRW.dashInvincibilityTimer -= SystemAPI.Time.DeltaTime;
            }
            if (data.ValueRW.dashCooldownTimer > 0) {
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
partial struct DashMovementSystemPhysics : ISystem {
    public void OnUpdate(ref SystemState state) {
        foreach (var (dashMovementRef, dashMovementManaged, velocityRef) in SystemAPI.Query<
                     RefRW<DashMovementData>, DashMovementManaged, RefRW<PhysicsVelocity>>()) {
            if (!dashMovementRef.ValueRO.isDashing) continue;
            dashMovementRef.ValueRW.isDashing = false;
            velocityRef.ValueRW.Linear.xz *= 1 + dashMovementManaged.dashSpeed;
            dashMovementRef.ValueRW.dashInvincibilityTimer = dashMovementManaged.dashInvincibilityDuration;
            dashMovementRef.ValueRW.dashCooldownTimer = dashMovementManaged.dashCooldown;
        }
    }
}
