using System;
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
}