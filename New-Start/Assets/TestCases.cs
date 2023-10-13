using System;
using AOT;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(MethodRef<MyAttackCall>))]
[assembly: RegisterGenericComponentType(typeof(MethodRef<Action>))]
public delegate void MyAttackCall(IntPtr emPtr, int i, int v);

public partial class TestCases : MonoBehaviour {
    [Serializable]
    public class MyEvents : IComponentData {
       public MethodRef<MyAttackCall> myEventsAsset;
       public MethodRef<Action> myNormalAttacks;
    }
    
    public struct MyUnmanagedEvents : IComponentData {
        public FunctionPointer<MyAttackCall> myEventsAsset;
        public FunctionPointer<Action> myNormalAttacks;
    }

    public MyEvents AttackStyles;

    private class TestEventsBaker : Baker<TestCases> {
        public override void Bake(TestCases authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            if (authoring.AttackStyles is null) return;
            AddComponentObject(entity, authoring.AttackStyles);
        }
    }
}


partial struct MySystem : ISystem, ISystemStartStop {
    public void OnCreate(ref SystemState state) 
        => state.RequireForUpdate<TestCases.MyEvents>();
    
    [BurstCompile]
    public unsafe void OnUpdate(ref SystemState state) {
        // ReSharper disable once Unity.Entities.SingletonMustBeRequested
        var events = SystemAPI.GetSingleton<TestCases.MyUnmanagedEvents>();
        var e = SystemAPI.GetSingletonEntity<TestCases.MyUnmanagedEvents>();
        var em = state.EntityManager;
        events.myEventsAsset.Invoke((IntPtr)UnsafeUtility.AddressOf(ref em), e.Index, e.Version);
        events.myNormalAttacks.Invoke();
        state.Enabled = false;
    }

    public void OnStartRunning(ref SystemState state) {
        var test = SystemAPI.ManagedAPI.GetSingleton<TestCases.MyEvents>();
        var testEntity = SystemAPI.ManagedAPI.GetSingletonEntity<TestCases.MyEvents>();
        state.EntityManager.AddComponentData(testEntity, new TestCases.MyUnmanagedEvents {
            myEventsAsset = test.myEventsAsset.Get(),
            myNormalAttacks = test.myNormalAttacks.Get()
        });

        // if (CoolUtilities.HasCalledLogHi) {
        //     var e = SystemAPI.ManagedAPI.GetSingletonEntity<TestCases.MyEvents>();
        //     SystemAPI.SetComponent(e, LocalTransform.FromPosition(0,2,3));
        // }
    }

    public void OnStopRunning(ref SystemState state) {
        
    }
}

[BurstCompile]
public static class Utilities {
    [MethodAllowsCallsFrom(typeof(Action))]
    [MonoPInvokeCallback(typeof(MyAttackCall))]
    public static void LogHi() {
        Debug.Log("Hi");
    }
    
    [MethodAllowsCallsFrom(typeof(MyAttackCall))]
    [BurstCompile]
    [MonoPInvokeCallback(typeof(MyAttackCall))]
    public unsafe static void LogHdwadi(IntPtr emPtr, int i, int v) {
        ref var em = ref UnsafeUtility.AsRef<EntityManager>((void*)emPtr);
        var entity = new Entity { Index = i, Version = v };
        var burstIsDisabled = false;
        LogIfNotBursted(ref burstIsDisabled);
        em.SetComponentData(entity, burstIsDisabled 
                ? LocalTransform.FromPosition(0, 2, 5) 
                : LocalTransform.FromPosition(5, 2, 3));
    }
    
    [BurstDiscard]
    public static void LogIfNotBursted(ref bool burstIsDisabled) {
        burstIsDisabled = true;
        Debug.Log("I am not bursted");
    }
    
    public static void LogHi(float a) {
        Debug.Log($"Hi {a}");
    }
    
    [MethodAllowsCallsFrom(typeof(Action))]
    public static void OtherThingy() {
        Debug.Log("OtherThingy");
    }
    
    [MethodAllowsCallsFrom(typeof(Action))]
    public static void OtherThingy2() {
        Debug.Log("OtherThingy2");
    }
    
    [MethodAllowsCallsFrom(typeof(MyAttackCall))]
    public static void OtherThingy3(IntPtr emPtr, int i, int v) {
        Debug.Log("OtherThingy3");
    }
    
    [MethodAllowsCallsFrom(typeof(MyAttackCall))]
    public static void Invalid(int a) {
        Debug.Log("Invalid");
    }
}

// CoolUtilities
static class CoolUtilities {
    [MethodAllowsCallsFrom(typeof(Action))]
    public static void LogHid() {
        Debug.Log("Hi");
        HasCalledLogHi = true;
    }

    internal static bool HasCalledLogHi = false;
    
    [MethodAllowsCallsFrom(typeof(Action))]
    static void OtherThingy() {
        Debug.Log("OtherThingy");
    }
}