using System;
using Runtime;
using Unity.Entities;
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
            var harbour = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(harbour, new BuyingStationData
            {
                selectedBoat = -1
            });
            
            var boatItems = AddBuffer<BoatShopItemElement>(harbour);
            foreach (var boatItem in authoring.boats)
            {
                var boatPrefab = GetEntity(boatItem.boat, TransformUsageFlags.Dynamic);
                var previewPrefab = GetEntity(boatItem.boat.PreviewModel, TransformUsageFlags.Renderable);
                boatItems.Add(new BoatShopItemElement
                {
                    price = boatItem.price,
                    boatPrefab = boatPrefab,
                    previewPrefab = previewPrefab
                });
            }
        }
    }
}