
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public partial struct ConstructionSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        //Get the caveTiles for positioning
        var caveSystem = SystemAPI.GetSingletonRW<CaveGridSystem.Singleton>().ValueRW;
        var caveTiles = caveSystem.CaveTiles.AsArray();
        
        //ECB for destruction
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        
        //Find all construction sites
        foreach (var (constructionSite,localToWorld) in SystemAPI.Query<RefRO<ConstructionSite>,LocalToWorld>())
        {
            //Check resources 
            if(constructionSite.ValueRO.currentResources == constructionSite.ValueRO.neededResources)
            {
                    //Instantiate the built construction and set transform
                    var buildingEntity = state.EntityManager.Instantiate(constructionSite.ValueRO.builtPrefab);
                    SystemAPI.SetComponent(buildingEntity, LocalTransform.FromPosition(localToWorld.Position));
                    
                    //Get tileIndex for caveTiles
                    var tileIndex = CaveGridSystem.Singleton.WorldPosToTileIndex(localToWorld.Position.xy);
            
                    //Destroy the construction  (Maybe use cleanup system)
                    ecb.DestroyEntity(caveTiles[tileIndex]);
                    
                    //Set the new entity to be drawn
                    caveTiles[tileIndex] = buildingEntity;
            }
        }
 
    }
}
