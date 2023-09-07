using Unity.Entities;
using UnityEngine;

public struct SomeElement : IBufferElementData
{
    public int Value;
}
    
public partial struct MySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var buffer = state.EntityManager.AddBuffer<SomeElement>(state.EntityManager.CreateEntity());
        buffer .Add(new SomeElement { Value = 5 });
        buffer .Add(new SomeElement { Value = 10 });
        buffer .Add(new SomeElement { Value = 15 });
    }
}