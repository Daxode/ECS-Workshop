using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;
using Collider = Unity.Physics.Collider;

[RequireComponent(typeof(Rigidbody))]
public class LockRigidBodyAuthor : MonoBehaviour
{
    public bool3 lockPosition = new(false, true, false);
    public bool3 lockRotation = true;

    class Baker : Baker<LockRigidBodyAuthor>
    {
        public override void Bake(LockRigidBodyAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // lock to xz plane
            var jointEntity = CreateAdditionalEntity(TransformUsageFlags.None);
            AddComponent(jointEntity, PhysicsJoint.CreateLimitedDOF(
                new RigidTransform(authoring.transform.localToWorldMatrix),
                authoring.lockPosition, authoring.lockRotation));
            AddComponent(jointEntity, new PhysicsConstrainedBodyPair(entity, Entity.Null, false));
            AddComponent<PhysicsWorldIndex>(jointEntity);

            AddComponent(entity, new JointReference
            {
                JointEntity = jointEntity
            });
        }
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[UpdateAfter(typeof(EndColliderBakingSystem))]
partial struct BakeCollisionResponseSystem : ISystem
{
    [BurstCompile]
    public unsafe void OnUpdate(ref SystemState state)
    {
        foreach (var colRef in SystemAPI.Query<PhysicsCollider>().WithAll<JointReference>())
        {
            ref var collider = ref UnsafeUtility.AsRef<Collider>(colRef.ColliderPtr);
            collider.SetCollisionResponse(CollisionResponsePolicy.CollideRaiseCollisionEvents);
        }
    }
}