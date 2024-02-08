using Unity.Entities;
using UnityEngine;

public class ConstructionSiteAuthor : MonoBehaviour
{
    [SerializeField] GameObject builtPrefab;
    [SerializeField] int neededResources = 100;
    [SerializeField] TextMesh textMesh;

    class Baker : Baker<ConstructionSiteAuthor>
    {
        public override void Bake(ConstructionSiteAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new ConstructionSite
            {
                builtPrefab = GetEntity(authoring.builtPrefab, TransformUsageFlags.None),
                neededResources = authoring.neededResources,
                currentResources = 0,
                textEntity = GetEntity(authoring.textMesh, TransformUsageFlags.None)
            });
        }
    }
}