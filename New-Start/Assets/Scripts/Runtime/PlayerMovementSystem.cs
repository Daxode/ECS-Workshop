using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

class PlayerMovementManaged : IComponentData
{
    public InputAction directionalInput;
    public float speed;
}

partial struct PlayerMovementSystem : ISystem, ISystemStartStop
{
    static readonly int k_Blend = Animator.StringToHash("Blend");

    public void OnCreate(ref SystemState state) 
        => state.RequireForUpdate<PlayerMovementManaged>();

    public void OnStartRunning(ref SystemState state)
    {
        foreach (var playerInputManaged in SystemAPI.Query<PlayerMovementManaged>())
            playerInputManaged.directionalInput.Enable();
    }

    public void OnStopRunning(ref SystemState state)
    {
        foreach (var playerInputManaged in SystemAPI.Query<PlayerMovementManaged>())
            playerInputManaged.directionalInput.Disable();
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (playerInputManaged, velocityRef, animatorEntity, towardsDataRef) in SystemAPI.Query<
                     PlayerMovementManaged, RefRW<PhysicsVelocity>,
                     ModelForEntity, RefRW<RotateTowardsData>>())
        {
            var input = playerInputManaged.directionalInput.ReadValue<Vector2>();
            var preserveY = velocityRef.ValueRO.Linear.y;
            velocityRef.ValueRW.Linear = new float3(input.x, 0, input.y) * playerInputManaged.speed;
            velocityRef.ValueRW.Linear.y = preserveY;
            towardsDataRef.ValueRW.direction = velocityRef.ValueRO.Linear;

            // check if model is Animator and set the Animator's speed
            if (SystemAPI.ManagedAPI.HasComponent<Animator>(animatorEntity.modelEntity))
            {
                var animator = SystemAPI.ManagedAPI.GetComponent<Animator>(animatorEntity.modelEntity);
                animator.SetFloat(k_Blend, math.length(velocityRef.ValueRO.Linear));
            }
        }
    }
}
