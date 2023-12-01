using Unity.Entities;
using UnityEngine;

public class BuildingsAuthor : MonoBehaviour
{
    [SerializeField] ConstructionSiteAuthor[] constructionSites;

    class Baker : Baker<BuildingsAuthor>
    {
        public override void Bake(BuildingsAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var buffer = AddBuffer<ConstructionSiteElement>(entity);
            foreach (var constructionSite in authoring.constructionSites)
                buffer.Add(new ConstructionSiteElement
                {
                    constructionSite = GetEntity(constructionSite, TransformUsageFlags.None)
                });
        }
    }
}

struct ConstructionSiteElement : IBufferElementData
{
    public Entity constructionSite;
}