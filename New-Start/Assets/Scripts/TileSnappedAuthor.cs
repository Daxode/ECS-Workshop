using Unity.Entities;
using UnityEngine;

public class TileSnappedAuthor : MonoBehaviour
{
    class Baker : Baker<TileSnappedAuthor>
    {
        public override void Bake(TileSnappedAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new TileSnappedTag());
        }
    }
}
