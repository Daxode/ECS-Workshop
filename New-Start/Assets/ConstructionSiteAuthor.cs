using System;
using Unity.Entities;
using UnityEngine;

public class ConstructionSiteAuthor : MonoBehaviour
{
    [SerializeField] GameObject builtPrefab;

    class Baker : Baker<ConstructionSiteAuthor>
    {
        public override void Bake(ConstructionSiteAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new ConstructionSite
            {
                builtPrefab = GetEntity(authoring.builtPrefab, TransformUsageFlags.None)
            });
        }
    }
}

struct ConstructionSite : IComponentData
{
    public Entity builtPrefab;
}