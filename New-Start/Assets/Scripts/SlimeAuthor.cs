using Unity.Entities;
using UnityEngine;

public class SlimeAuthor : MonoBehaviour
{
    class Baker : Baker<SlimeAuthor>
    {
        public override void Bake(SlimeAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<PathGoal>(entity);
            AddBuffer<PathNode>(entity);
            AddComponent<PathMoveState>(entity);
            SetComponentEnabled<PathGoal>(entity, false);
            SetComponentEnabled<PathNode>(entity, false);
            SetComponentEnabled<PathMoveState>(entity, false);
        }
    }
}