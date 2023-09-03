using System;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class BuyingStationAuthor : MonoBehaviour
{
    [Serializable]
    public struct BoatItem
    {
        public BoatAuthor boat;
        public int price;
    }

    public BoatItem[] boats;

    public class BuyingStationBaker : Baker<BuyingStationAuthor>
    {
        public override void Bake(BuyingStationAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new BuyingStationData
            {
                selectedBoat = -1
            });
            var items = AddBuffer<BoatShopItemElement>(entity);
            foreach (var boatShopItem in authoring.boats)
            {
                var boatPrefab = GetEntity(boatShopItem.boat, TransformUsageFlags.Dynamic);
                var boatPreview = GetEntity(boatShopItem.boat.PreviewModel, TransformUsageFlags.Renderable);
                items.Add(new BoatShopItemElement 
                {
                    boatPrefab = boatPrefab,
                    previewPrefab = boatPreview,
                    price = boatShopItem.price,
                });
            }
        }
    }
}

public struct BoatShopItemElement : IBufferElementData
{
    public Entity boatPrefab;
    public Entity previewPrefab;
    public int price;
    
    public Entity spawnedPreview;
}

public struct BuyingStationData : IComponentData
{
    public int money;
    public int selectedBoat;
}


partial struct BuyingSystem : ISystem, ISystemStartStop
{
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

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (buyingStation, boatShopItems, ltw) in SystemAPI.Query<RefRW<BuyingStationData>, DynamicBuffer<BoatShopItemElement>, LocalToWorld>())
        {
            // store which number is pressed
            for (var key = KeyCode.Alpha0; key < KeyCode.Alpha9; key++)
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
                var boat = state.EntityManager.Instantiate(selectedBoat.boatPrefab);
                SystemAPI.SetComponent(boat, LocalTransform.FromMatrix(ltw.Value));
            }
        }
    }
}