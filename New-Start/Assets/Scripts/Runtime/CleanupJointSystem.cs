using System;
using Unity.Entities;

struct CleanupJoint : ICleanupComponentData
{
    public Entity JointEntity;
}

struct JointReference : IComponentData
{
    public Entity JointEntity;
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
