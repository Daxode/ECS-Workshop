using System;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(SerializedMethodData<MyAttackCall>))]
[assembly: RegisterGenericComponentType(typeof(SerializedMethodData<Action>))]
public delegate void MyAttackCall(int a, float b);

public partial class TestCases : MonoBehaviour { 
   public SerializedMethodData<MyAttackCall> myEventsAsset;
   public SerializedMethodData<Action> myNormalAttacks;

    private class TestEventsBaker : Baker<TestCases> {
        public override void Bake(TestCases authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            if (authoring.myEventsAsset == null) return;
            AddComponentObject(entity, authoring.myEventsAsset);
            AddComponentObject(entity, authoring.myNormalAttacks);
        }
    }
}


partial struct MySystem : ISystem {
    public void OnCreate(ref SystemState state) 
        => state.RequireForUpdate<SerializedMethodData<MyAttackCall>>();
    public void OnUpdate(ref SystemState state) {
        var test = SystemAPI.ManagedAPI.GetSingleton<SerializedMethodData<MyAttackCall>>();
        test.Get()(23, 20f);
        if (CoolUtilities.HasCalledLogHi) {
            var e = SystemAPI.ManagedAPI.GetSingletonEntity<SerializedMethodData<MyAttackCall>>();
            SystemAPI.SetComponent(e, LocalTransform.FromPosition(0,2,3));
        }
        state.Enabled = false;
    }
}

public static class Utilities {
    [MethodAllowsCallsFrom(typeof(Action))]
    public static void LogHi() {
        Debug.Log("Hi");
    }
    
    [MethodAllowsCallsFrom(typeof(MyAttackCall))]
    public static void LogHdwadi(int a, float b) {
        Debug.Log($"Hi {a} {b}");
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
    public static void OtherThingy3(int a, float b) {
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