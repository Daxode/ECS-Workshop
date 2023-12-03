using Unity.Entities;
using UnityEngine;

public partial class TestResourceSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (constructionSite,constructionSiteEntity) in SystemAPI.Query<RefRW<ConstructionSite>>().WithEntityAccess())
        {
            constructionSite.ValueRW.currentResources += 1;
            

            SystemAPI.ManagedAPI.GetComponent<ConstructionText>(constructionSiteEntity).resourceText.text =  $"{constructionSite.ValueRW.currentResources}/100";
            
        }
    }
}
