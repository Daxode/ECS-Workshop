using System;
using Unity.Entities;
using UnityEngine;

class BuildingAuthor : MonoBehaviour
{
    // [SerializeField] BuildingType type;

    class Baker : Baker<BuildingAuthor>
    {
        public override void Bake(BuildingAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new BuildingData
            {
                // type = authoring.type
            });
        }
    }
}

struct BuildingData : IComponentData
{
}
