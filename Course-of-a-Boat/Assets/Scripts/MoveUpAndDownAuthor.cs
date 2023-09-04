using Runtime;
using Unity.Entities;
using UnityEngine;

public class MoveUpAndDownAuthor : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 1.0f;
    public float amplitude = 0.2f;
    public float offset = 0.0f;
    
    [Header("Extra Settings")]
    public bool addGameObjectYToOffset = true;

    public class MoveUpAndDownBaker : Baker<MoveUpAndDownAuthor>
    {
        public override void Bake(MoveUpAndDownAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var data = new MoveUpAndDown
            {
                speed = authoring.speed,
                amplitude = authoring.amplitude,
                offset = authoring.offset + (authoring.addGameObjectYToOffset ? authoring.transform.position.y : 0f)
            };
            AddComponent(entity, data);
        }
    }
}