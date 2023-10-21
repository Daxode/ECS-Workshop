using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(StillRotationModelAuthor))]
public class FollowPlayerAuthor : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] float2 slowDownRange = new(4, 6);
    [SerializeField] float shootInterval = 0.2f;

    class Baker : Baker<FollowPlayerAuthor>
    {
        public override void Bake(FollowPlayerAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new FollowPlayerData
            {
                speed = authoring.speed,
                slowDownRange = authoring.slowDownRange,
                shootInterval = authoring.shootInterval,
                shootTimer = authoring.shootInterval
            });
            AddComponent(entity, new AttackDamage
            {
                damage = 1,
                owningEntity = entity
            });
        }
    }
}