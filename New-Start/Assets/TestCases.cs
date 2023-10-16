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

// Setup MoveCubeCall from MoveCubeCallSetup
partial struct MyInitSystem : ISystem, ISystemStartStop {
    EntityQuery m_NeedsInitQuery;
    public void OnCreate(ref SystemState state) {
        m_NeedsInitQuery = SystemAPI.QueryBuilder().WithAll<TestCases.MoveCubeCallSetup>().WithNone<MoveCubeCall>().Build();
        state.RequireForUpdate(m_NeedsInitQuery);
    }

    public void OnStartRunning(ref SystemState state) {
        // create MoveCubeCall from MoveCubeCallSetup
        state.EntityManager.AddComponent<MoveCubeCall>(m_NeedsInitQuery);
        foreach (var (moveCubeCallSetup, moveCubeCallRef) in SystemAPI.Query<TestCases.MoveCubeCallSetup, RefRW<MoveCubeCall>>()) {
            moveCubeCallRef.ValueRW = new MoveCubeCall {
                Value = moveCubeCallSetup.value.Get()
            };
        }
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
    
    [BurstCompile]
    [MethodAllowsCallsFrom(typeof(CallbackMoveCube))]
    static void Move(ref LocalTransform transform, ref TimeData time) 
        => transform.Position += new float3(0, 0, math.sin((float)time.ElapsedTime) * 0.01f);
    
    [BurstCompile]
    [MethodAllowsCallsFrom(typeof(CallbackMoveCube))]
    static void Scale(ref LocalTransform transform, ref TimeData time) 
        => transform.Scale = math.lerp(transform.Scale, 10, time.DeltaTime * 0.1f);
    
    // bounce up and down
    [BurstCompile]
    [MethodAllowsCallsFrom(typeof(CallbackMoveCube))]
    static void Bounce(ref LocalTransform transform, ref TimeData time) {
        var pos = transform.Position;
        pos.y = math.sin((float)time.ElapsedTime) * 2f;
        transform.Position = pos;
    }
}