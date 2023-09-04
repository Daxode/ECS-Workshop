using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Runtime
{
    public struct BuyingStationData : IComponentData
    {
        public int money;
        public int selectedBoat;
    }
    
    public struct BoatShopItemElement : IBufferElementData
    {
        public int price;
        public Entity boatPrefab;
        public Entity previewPrefab;
        
        public Entity spawnedPreview;
    }
    
    public partial struct BuyingSystem : ISystem, ISystemStartStop
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BuyingStationData>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            foreach (var shop in SystemAPI.Query<LocalToWorld, DynamicBuffer<BoatShopItemElement>>())
            {
                var ltw = shop.Item1;
                var boatShopItems = shop.Item2;
                for (var i = 0; i < boatShopItems.Length; i++)
                {
                    var boatShopItem = boatShopItems[i];
                    boatShopItem.spawnedPreview = state.EntityManager.Instantiate(boatShopItem.previewPrefab);
                    SystemAPI.SetComponent(boatShopItem.spawnedPreview, LocalTransform.FromMatrix(ltw.Value).WithScale(0));
                    boatShopItems[i] = boatShopItem;
                }
            }
        }
        public void OnStopRunning(ref SystemState state) {}

        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (buyingStation, boatShopItems, ltw) in SystemAPI.Query<RefRW<BuyingStationData>, DynamicBuffer<BoatShopItemElement>, RefRO<LocalToWorld>>())
            {
                // store which number is pressed
                for (var key = KeyCode.Alpha0; key <= KeyCode.Alpha9; key++)
                {
                    if (Input.GetKeyDown(key))
                    {
                        // 1-9 -> 0-8 and 0 -> deselect (-1)
                        var index = (int)key - (int)KeyCode.Alpha1;
                        if (index < boatShopItems.Length && index != buyingStation.ValueRO.selectedBoat)
                        {
                            // hide previous preview
                            if (buyingStation.ValueRO.selectedBoat >= 0) 
                            {
                                var previousPreview = boatShopItems[buyingStation.ValueRO.selectedBoat].spawnedPreview;
                                var previousLT = SystemAPI.GetComponent<LocalTransform>(previousPreview);
                                SystemAPI.SetComponent(previousPreview, previousLT.WithScale(0));
                            }
                            
                            // select new preview
                            buyingStation.ValueRW.selectedBoat = index;
                            
                            // show new preview
                            if (buyingStation.ValueRO.selectedBoat >= 0)
                            {
                                var selectedPreview = boatShopItems[buyingStation.ValueRO.selectedBoat].spawnedPreview;
                                var selectedLT = SystemAPI.GetComponent<LocalTransform>(selectedPreview);
                                SystemAPI.SetComponent(selectedPreview, selectedLT.WithScale(1));
                            }
                        }
                        
                        buyingStation.ValueRW.money += 1;
                        break;
                    }
                }
                
                // buy selected boat
                if (buyingStation.ValueRO.selectedBoat >= 0 && buyingStation.ValueRO.money >= boatShopItems[buyingStation.ValueRO.selectedBoat].price)
                {
                    var selectedBoat = boatShopItems[buyingStation.ValueRO.selectedBoat];
                    buyingStation.ValueRW.money -= selectedBoat.price;
                    Debug.Log($"Bought boat {buyingStation.ValueRO.selectedBoat}, price: {selectedBoat.price}");
                    var boat = state.EntityManager.Instantiate(selectedBoat.boatPrefab);
                    SystemAPI.SetComponent(boat, LocalTransform.FromMatrix(ltw.ValueRO.Value));
                }
            }
        }
    }
}
