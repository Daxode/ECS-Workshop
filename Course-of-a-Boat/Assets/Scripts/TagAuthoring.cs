
    using Unity.Entities;
    using UnityEngine;

    public enum TagType
    {
        None,
        Marker
    }

    public struct MarkerTag : IComponentData {}

    public class TagAuthoring : MonoBehaviour
    {
        public TagType tagType;
        
        class TagBaker : Baker<TagAuthoring>
        {
            public override void Bake(TagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                switch (authoring.tagType)
                {
                    case TagType.Marker:
                        AddComponent(entity, new MarkerTag());
                        break;
                }
            }
        }
    }