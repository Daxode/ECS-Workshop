using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class MoveUpAndDownAuthor : MonoBehaviour
{
    public float speed = 20.0f;
    public float amplitude = 0.5f;
}

public class MoveUpAndDownBaker : Baker<MoveUpAndDownAuthor>
{
    public override void Bake(MoveUpAndDownAuthor authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        var data = new MoveUpAndDown
        {
            speed = authoring.speed,
            amplitude = authoring.amplitude
        };
        AddComponent(entity, data);
    }
}

public struct MoveUpAndDown : IComponentData
{
    public float speed;
    public float amplitude;
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
partial struct MoveUpAndDownSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var i = 0;
        foreach (var (trsRef, moveUpAndDown) in SystemAPI.Query<RefRW<LocalToWorld>, RefRO<MoveUpAndDown>>())
        {
            trsRef.ValueRW.Value.c3.y += 
                math.sin((float)SystemAPI.Time.ElapsedTime * moveUpAndDown.ValueRO.speed + i*1.5f) * moveUpAndDown.ValueRO.amplitude;
            i++;
        }
    }
}