using Unity.Entities;
using Unity.Mathematics;
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
            
            AddComponent<WalkState>(entity);
        }
    }
}

struct WalkState : IComponentData
{
    public float2 target;
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
        
        foreach (var (ltRef, walkState) in SystemAPI
            .Query<RefRW<LocalTransform>, WalkState>())
        {
            ref var ltw = ref ltRef.ValueRW;
            var target = walkState.target;
            var targetPos = new float3(target.x, target.y, -3);
            var dir = math.normalize(targetPos - ltw.Position);
            var speed = 5f;
            var distance = math.distance(targetPos, ltw.Position);
            if (distance > 0.5f) 
                ltw.Position += dir * speed * SystemAPI.Time.DeltaTime;
        }
    }
}