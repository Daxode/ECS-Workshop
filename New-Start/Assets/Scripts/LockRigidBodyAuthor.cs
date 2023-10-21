using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;
using Collider = Unity.Physics.Collider;

struct CleanupJoint : ICleanupComponentData
{
    public Entity JointEntity;
}

struct JointReference : IComponentData
{
    public Entity JointEntity;
}

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
    public unsafe void OnUpdate(ref SystemState state)
    {
        foreach (var colRef in SystemAPI.Query<PhysicsCollider>().WithAll<JointReference>())
        {
            ref var collider = ref UnsafeUtility.AsRef<Collider>(colRef.ColliderPtr);
            collider.SetCollisionResponse(CollisionResponsePolicy.CollideRaiseCollisionEvents);
        }
    }
}

partial struct CleanupJointSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Setup joints
        var entitiesToSetup = SystemAPI.QueryBuilder().WithAll<JointReference>().WithNone<CleanupJoint>().Build();
        foreach (var e in entitiesToSetup.ToEntityArray(state.WorldUpdateAllocator))
            state.EntityManager.AddComponentData(e, new CleanupJoint { JointEntity = SystemAPI.GetComponent<JointReference>(e).JointEntity });

        // Destroy joints that are not connected to anything
        var entitiesToClean = SystemAPI.QueryBuilder().WithAll<CleanupJoint>().WithNone<JointReference>().Build();
        foreach (var j in entitiesToClean.ToComponentDataArray<CleanupJoint>(state.WorldUpdateAllocator))
            state.EntityManager.DestroyEntity(j.JointEntity);
        state.EntityManager.RemoveComponent<CleanupJoint>(entitiesToClean);
    }
}
