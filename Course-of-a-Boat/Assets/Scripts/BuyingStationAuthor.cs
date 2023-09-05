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

    class BuyingStationAuthorBaker : Baker<BuyingStationAuthor>
    {
        public override void Bake(BuyingStationAuthor authoring)
        {
            var harbour = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(harbour, new BuyingStationData
            {
                selectedBoat = -1,
            });
            
            var buffer = AddBuffer<BoatShopItemElement>(harbour);
            foreach (var boatItem in authoring.boats)
                buffer.Add(new BoatShopItemElement { price = boatItem.price });
        }
    }
}