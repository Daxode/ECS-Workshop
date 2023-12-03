using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class SelectableAuthor : MonoBehaviour
{
    [SerializeField] AnimatedSpriteAuthor drawnOutline;
    
    class Baker : Baker<SelectableAuthor>
    {
        public override void Bake(SelectableAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new Selectable
            {
                outlineEntity = authoring.drawnOutline ? GetEntity(authoring.drawnOutline, TransformUsageFlags.Dynamic) : Entity.Null
            });
            SetComponentEnabled<Selectable>(entity, false);
        }
    }
}

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