using Unity.Entities;
using UnityEngine;

public struct BoatTag : IComponentData {}

public class BoatAuthor : MonoBehaviour
{
    public class BoatBaker : Baker<BoatAuthor>
    {
        public override void Bake(BoatAuthor author)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent<BoatTag>(entity);
        }
    }
}