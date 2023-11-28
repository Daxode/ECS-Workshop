using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

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
            AddComponent<SetRaisedCollisionEvents>(entity);
        }
    }
}