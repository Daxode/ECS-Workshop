using Unity.Entities;
using UnityEngine;

public class GridSnappedAuthor : MonoBehaviour
{
    class Baker : Baker<GridSnappedAuthor>
    {
        public override void Bake(GridSnappedAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent<GridSnappedTag>(entity);
        }
    }
}