using System;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(LockRigidBodyAuthor))]
class StillRotationModelAuthor : MonoBehaviour
{
    [SerializeField] Transform model;
    [SerializeField] float rotateSpeed;

    class Baker : Baker<StillRotationModelAuthor>
    {
        public override void Bake(StillRotationModelAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Make sure the model spins to face the direction of movement
            AddComponent(entity, new ModelForEntity
            {
                modelEntity = authoring.model ? GetEntity(authoring.model, TransformUsageFlags.Dynamic) : Entity.Null
            });
            AddComponent(entity, new RotateTowardsData
            {
                speed = authoring.rotateSpeed
            });
        }
    }
}
