using Unity.Entities;
using UnityEngine;

public partial class TestResourceSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var constructionSite in SystemAPI.Query<RefRW<ConstructionSite>>())
        {
            if (Input.GetKey(KeyCode.Space))
                constructionSite.ValueRW.currentResources += 1;
            SystemAPI.ManagedAPI.GetComponent<TextMesh>(constructionSite.ValueRW.textEntity).text =  $"{constructionSite.ValueRW.currentResources}/{constructionSite.ValueRW.neededResources}";
        }
    }
}
