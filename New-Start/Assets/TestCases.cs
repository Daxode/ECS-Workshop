using System;
using Unity.Burst;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// Authoring Data
public class TestCases : MonoBehaviour {
    [Serializable]
    public class MoveCubeCallSetup : IComponentData {
       public MethodRef<CallbackMoveCube> value;
    }

    public MoveCubeCallSetup moveCubeCall;

    class TestEventsBaker : Baker<TestCases> {
        public override void Bake(TestCases authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring.moveCubeCall);
        }
    }
}

// Runtime Data
public struct MoveCubeCall : IComponentData {
    public FunctionPointer<CallbackMoveCube> Value;
}
public delegate void CallbackMoveCube(ref LocalTransform transform, ref TimeData time);

partial struct MyInitSystem : ISystem, ISystemStartStop {
    EntityQuery m_MoveCubeCallQuery;
    public void OnCreate(ref SystemState state) {
        // Find all entities with events
        m_MoveCubeCallQuery = SystemAPI.QueryBuilder().WithAny<TestCases.MoveCubeCallSetup>().Build();
        state.RequireForUpdate(m_MoveCubeCallQuery);
    }
    
    public void OnStartRunning(ref SystemState state) {
        // Add the unmanaged component
        state.EntityManager.AddComponent<MoveCubeCall>(m_MoveCubeCallQuery);
        foreach (var (moveCubeCallSetup, moveCubeCallRef) in SystemAPI.Query<TestCases.MoveCubeCallSetup, RefRW<MoveCubeCall>>()) {
            moveCubeCallRef.ValueRW = new MoveCubeCall {
                Value = moveCubeCallSetup.value.Get()
            };
        }

        // Remove the managed component
        state.EntityManager.RemoveComponent<TestCases.MoveCubeCallSetup>(m_MoveCubeCallQuery);
    }
    public void OnStopRunning(ref SystemState state) {}
}

partial struct MySystem : ISystem {
    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        var em = state.EntityManager;
        foreach (var (transformRef, moveCubeCall) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveCubeCall>>()) {
            var timeData = SystemAPI.Time;
            moveCubeCall.ValueRO.Value.Invoke(ref transformRef.ValueRW, ref timeData);
        }
    }
}

[BurstCompile]
public static class MoveCalls {
    [BurstCompile]
    [MethodAllowsCallsFrom(typeof(CallbackMoveCube))]
    static void Spin(ref LocalTransform transform, ref TimeData time) 
        => transform.Rotation = math.mul(transform.Rotation, quaternion.RotateY(time.DeltaTime * 3f));
}