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
        foreach (var entity in SystemAPI.QueryBuilder()
                     .WithNone<LinkedEntityGroup, Parent>()
                     .WithAll<Child>()
                     .Build().ToEntityArray(state.WorldUpdateAllocator))
        {
            // Add leg
            var leg = state.EntityManager.AddBuffer<LinkedEntityGroup>(entity);
            
            // Recursively add all children to leg
            using var childQueue = new NativeQueue<Entity>(state.WorldUpdateAllocator);
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