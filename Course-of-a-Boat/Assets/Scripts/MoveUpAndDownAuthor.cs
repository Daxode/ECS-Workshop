using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

public class MoveUpAndDownAuthor : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 1.0f;
    public float amplitude = 0.2f;
    public float offset = 0.0f;
    
    [Header("Extra Settings")]
    public bool addGameObjectYToOffset = true;
}

public class MoveUpAndDownBaker : Baker<MoveUpAndDownAuthor>
{
    public override void Bake(MoveUpAndDownAuthor authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        var data = new MoveUpAndDown
        {
            speed = authoring.speed,
            amplitude = authoring.amplitude,
            offset = authoring.offset + (authoring.addGameObjectYToOffset ? authoring.transform.localPosition.y : 0f)
        };
        AddComponent(entity, data);
    }
}

public struct MoveUpAndDown : IComponentData
{
    public float speed;
    public float amplitude;
    public float offset;
}

partial struct MoveUpAndDownSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var i = 0;
        foreach (var (trsRef, moveUpAndDown) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveUpAndDown>>())
        {
            var yLevel = moveUpAndDown.ValueRO.offset;
            yLevel += math.sin((float)SystemAPI.Time.ElapsedTime * moveUpAndDown.ValueRO.speed + i*1.5f) * moveUpAndDown.ValueRO.amplitude;
            trsRef.ValueRW.Position.y = yLevel;
            
            i++;
        }
    }
}