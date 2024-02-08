using Unity.Entities;
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