using System;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

struct Shooter : IComponentData
{
    public Entity projectile;
    public Entity shootLocationEntity;
}

[UpdateBefore(typeof(TransformSystemGroup))]
partial struct ShooterSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!Input.GetKeyDown(KeyCode.Mouse0))
            return;
        
        foreach (var shooterRef in SystemAPI.Query<RefRO<Shooter>>())
        {
            // shoot
            var instance = state.EntityManager.Instantiate(shooterRef.ValueRO.projectile);
            var shootLtw = SystemAPI.GetComponent<LocalToWorld>(shooterRef.ValueRO.shootLocationEntity); // gets last frame's transform
            SystemAPI.SetComponent(instance, LocalTransform.FromPosition(shootLtw.Position));
        }
    }
}