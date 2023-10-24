using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

struct PlayerMovement : IComponentData
{
    public float speed;
}

partial struct PlayerMovementSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var playerEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(playerEntity,  new PlayerMovement
        {
            speed = 1.0f
        });
        state.EntityManager.SetName(playerEntity, "Player");
        state.EntityManager.AddComponentData(playerEntity, LocalTransform.Identity);
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (playerMovementRef, localTransformRef) in SystemAPI.Query<RefRO<PlayerMovement>, RefRW<LocalTransform>>())
        {
            var input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            var velocity = new float3(input.x, 0, input.y) * playerMovementRef.ValueRO.speed;
            localTransformRef.ValueRW.Position += velocity * SystemAPI.Time.DeltaTime;
            Debug.Log($"Player position: {localTransformRef.ValueRW.Position}");
        }
    }
}
