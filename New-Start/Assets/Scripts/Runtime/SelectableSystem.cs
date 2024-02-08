using Unity.Entities;
using Unity.Transforms;

partial struct SelectableSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (data, selectState) in SystemAPI
                     .Query<Selectable, EnabledRefRO<Selectable>>()
                     .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
        {
            SystemAPI.SetComponent(data.outlineEntity, selectState.ValueRO ? LocalTransform.Identity : default);
        }
    }
}