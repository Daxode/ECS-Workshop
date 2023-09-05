using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
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
    }
    
    public partial struct BuyingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var harbour = state.EntityManager.CreateEntity();
            state.EntityManager.SetName(harbour, "Harbour");
            state.EntityManager.AddComponentData(harbour, new BuyingStationData
            {
                selectedBoat = -1,
            });
            
            var buffer = state.EntityManager.AddBuffer<BoatShopItemElement>(harbour);
            buffer.Add(new BoatShopItemElement { price = 5 });
            buffer.Add(new BoatShopItemElement { price = 10 });
            buffer.Add(new BoatShopItemElement { price = 15 });
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (buyingStation, boatShopItems) in SystemAPI.Query<RefRW<BuyingStationData>, DynamicBuffer<BoatShopItemElement>>())
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
                            // select new preview
                            buyingStation.ValueRW.selectedBoat = index;
                            
                            if (index >= 0)
                                Debug.Log($"Selected boat {index}, price: {boatShopItems[index].price}");
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
                }
            }
        }
    }
}


