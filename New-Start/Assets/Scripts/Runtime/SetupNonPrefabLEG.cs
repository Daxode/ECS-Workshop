using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

partial struct SetupNonPrefabLegSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // if no leg but has children, add leg
        foreach (var entity in SystemAPI.QueryBuilder().WithAll<Child>().WithNone<LinkedEntityGroup, Parent>()
                     .Build().ToEntityArray(state.WorldUpdateAllocator))
        {
            var leg = state.EntityManager.AddBuffer<LinkedEntityGroup>(entity);
            var childQueue = new NativeQueue<Entity>(state.WorldUpdateAllocator);
            childQueue.Enqueue(entity);
            while (childQueue.TryDequeue(out var parent))
            {
                leg.Add(parent);
                if (!SystemAPI.HasBuffer<Child>(parent))
                    continue;
                foreach (var child in SystemAPI.GetBuffer<Child>(parent))
                    childQueue.Enqueue(child.Value);
            }
        }
    }
}