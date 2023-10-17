using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(StillRotationModelAuthor))]
public class PlayerMovementAuthor : MonoBehaviour {
    [SerializeField] InputAction directionalInput;
    [SerializeField] float speed;
    
    public class PlayerInputManaged : IComponentData {
        public InputAction directionalInput;
        public float speed;
    }
    
    class Baker : Baker<PlayerMovementAuthor> {
        public override void Bake(PlayerMovementAuthor authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, new PlayerInputManaged {
                directionalInput = authoring.directionalInput,
                speed = authoring.speed,
            });
        }
    }
}

partial struct PlayerMovementSystem : ISystem, ISystemStartStop {
    static readonly int k_Blend = Animator.StringToHash("Blend");

    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<PlayerMovementAuthor.PlayerInputManaged>();
    }

    public void OnStartRunning(ref SystemState state) {
        foreach (var playerInputManaged in SystemAPI.Query<PlayerMovementAuthor.PlayerInputManaged>()) {
            playerInputManaged.directionalInput.Enable();
        }
    }

    public void OnStopRunning(ref SystemState state)
    {
        foreach (var playerInputManaged in SystemAPI.Query<PlayerMovementAuthor.PlayerInputManaged>()) {
            playerInputManaged.directionalInput.Disable();
        }
    }

    public void OnUpdate(ref SystemState state) {
        foreach (var (playerInputManaged, velocityRef, model, towardsDataRef) in SystemAPI.Query<
                     PlayerMovementAuthor.PlayerInputManaged, RefRW<PhysicsVelocity>, 
                     ModelForEntity, RefRW<RotateTowardsData>>()) {
            var input = playerInputManaged.directionalInput.ReadValue<Vector2>();
            var preserveY = velocityRef.ValueRO.Linear.y;
            velocityRef.ValueRW.Linear = new float3(input.x, 0, input.y) * playerInputManaged.speed;
            velocityRef.ValueRW.Linear.y = preserveY;
            towardsDataRef.ValueRW.direction = velocityRef.ValueRO.Linear;

            // check if model is Animator and set the Animator's speed
            if (SystemAPI.ManagedAPI.HasComponent<Animator>(model.modelEntity)) {
                var animator = SystemAPI.ManagedAPI.GetComponent<Animator>(model.modelEntity);
                animator.SetFloat(k_Blend, math.length(velocityRef.ValueRO.Linear));
            }
        }
    }
}