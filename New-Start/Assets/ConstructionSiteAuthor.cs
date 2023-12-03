using System;
using Unity.Entities;
using UnityEngine;

public class ConstructionSiteAuthor : MonoBehaviour
{
    [SerializeField] GameObject builtPrefab;
    public int neededResources = 100;
    public int currentResources;
    public TextMesh resourceText;

    class Baker : Baker<ConstructionSiteAuthor>
    {
        public override void Bake(ConstructionSiteAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new ConstructionSite
            {
                builtPrefab = GetEntity(authoring.builtPrefab, TransformUsageFlags.None),
                neededResources = authoring.neededResources,
                currentResources = authoring.currentResources
            });
            
            AddComponentObject(entity, new ConstructionText()
            {
                resourceText = authoring.resourceText
            });
        }
    }
}

struct ConstructionSite : IComponentData
{
    public Entity builtPrefab;
    public int neededResources;
    public int currentResources;
}

class ConstructionText : IComponentData
{
    public TextMesh resourceText;
}