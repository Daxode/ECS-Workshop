using Unity.Entities;
using UnityEngine;

class BuildingAuthor : MonoBehaviour
{
    class Baker : Baker<BuildingAuthor>
    {
        public override void Bake(BuildingAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
        }
    }
}
