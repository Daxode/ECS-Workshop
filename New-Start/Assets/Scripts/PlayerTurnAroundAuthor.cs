using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(StillRotationModelAuthor))]
class PlayerTurnAroundAuthor : MonoBehaviour
{
    [SerializeField] InputAction directionalInput;

    class Baker : Baker<PlayerTurnAroundAuthor>
    {
        public override void Bake(PlayerTurnAroundAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, new PlayerTurnAroundManaged
            {
                directionalInput = authoring.directionalInput,
                followMouseInstead = true
            });
        }
    }
}

class PlayerTurnAroundManaged : IComponentData
{
    public InputAction directionalInput;
    public bool followMouseInstead;
}

[UpdateAfter(typeof(PlayerMovementSystem))]
partial struct PlayerTurnSystem : ISystem, ISystemStartStop
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerTurnAroundManaged>();
    }

    public void OnStartRunning(ref SystemState state)
    {
        foreach (var turnManaged in SystemAPI.Query<PlayerTurnAroundManaged>()) turnManaged.directionalInput.Enable();
    }

    public void OnStopRunning(ref SystemState state)
    {
        foreach (var turnManaged in SystemAPI.Query<PlayerTurnAroundManaged>()) turnManaged.directionalInput.Disable();
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (turnManaged, towardsDataRef, lt) in SystemAPI.Query<
                     PlayerTurnAroundManaged, RefRW<RotateTowardsData>, LocalTransform>())
        {
            if (turnManaged.followMouseInstead)
            {
                var mousePos = Mouse.current.position.ReadValue();
                var mouseRay = Camera.main.ScreenPointToRay(mousePos);
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(mouseRay, out var distance))
                {
                    float3 point = mouseRay.GetPoint(distance);
                    towardsDataRef.ValueRW.direction = math.normalize(point - lt.Position);
                }

                continue;
            }

            // else use input
            var input = turnManaged.directionalInput.ReadValue<Vector2>();
            var lookDirection = new float3(input.x, 0, input.y);
            if (math.lengthsq(lookDirection) > 0.1f)
                towardsDataRef.ValueRW.direction = lookDirection;
        }
    }
}
