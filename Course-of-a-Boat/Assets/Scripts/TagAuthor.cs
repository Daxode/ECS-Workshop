using Runtime;
using Unity.Entities;
using UnityEngine;

public enum TagType
{
    None,
    Marker
}

public class TagAuthor : MonoBehaviour
{
    public TagType tagType;
}

class TagBaker : Baker<TagAuthor>
{
    public override void Bake(TagAuthor authoring)
    {
        switch (authoring.tagType)
        {
            case TagType.Marker:
                AddComponent(GetEntity(TransformUsageFlags.Dynamic), new MarkerTag());
                break;
        }
    }
}