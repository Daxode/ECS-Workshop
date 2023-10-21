using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct ModelForEntity : IComponentData
{
    public Entity modelEntity;
}

public struct RotateTowardsData : IComponentData
{
    public float speed;
    public float3 direction;
}

partial struct RotateTowardsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (model, data) in SystemAPI.Query<ModelForEntity, RotateTowardsData>())
        {
            if (math.lengthsq(data.direction) < 0.01f) continue;

            var modelLT = SystemAPI.GetComponent<LocalTransform>(model.modelEntity);
            modelLT.Rotation = math.mul(modelLT.Rotation,
                quaternion.RotateY(
                    Vector2.SignedAngle(data.direction.xz, modelLT.Forward().xz) * data.speed * deltaTime));
            SystemAPI.SetComponent(model.modelEntity, modelLT);
        }
    }
}
