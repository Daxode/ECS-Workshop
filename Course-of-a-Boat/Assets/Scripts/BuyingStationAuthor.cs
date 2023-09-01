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
            AddComponent(entity, new BuyingStationData());
            var items = AddBuffer<BoatShopItemElement>(entity);
            foreach (var boatShopItem in authoring.boats)
            {
                items.Add(new BoatShopItemElement 
                {
                    boat = GetEntity(boatShopItem.boat, TransformUsageFlags.Dynamic),
                    price = boatShopItem.price
                });
            }
        }
    }
}

public struct BoatShopItemElement : IBufferElementData
{
    public Entity boat;
    public int price;
}

public struct BuyingStationData : IComponentData { }


partial struct BuyingSystem : ISystem, ISystemStartStop
{
    public void OnStartRunning(ref SystemState state)
    {
        foreach (var (ltw, boatShopItems) in SystemAPI.Query<LocalToWorld, DynamicBuffer<BoatShopItemElement>>())
        {
            foreach (var boatShopItem in boatShopItems)
            {
                var boatEntity = state.EntityManager.Instantiate(boatShopItem.boat);
                state.EntityManager.SetComponentData(boatEntity, LocalTransform.FromMatrix(ltw.Value));
                state.EntityManager.AddComponent<Disabled>(boatEntity);
            }
        }
    }

    public void OnStopRunning(ref SystemState state) {}
}