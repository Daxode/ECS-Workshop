using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public partial class TestEvents : MonoBehaviour { 
   public SerializedDelegate myEventsAsset;

    private class TestEventsBaker : Baker<TestEvents> {
        public override void Bake(TestEvents authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            if (authoring.myEventsAsset == null) return;
            // authoring.myEventsAsset.Invoke();
            AddComponentObject(entity, authoring.myEventsAsset);
        }
    }

    public class MyEventComponent : IComponentData {
        public SerializedDelegate myEventsAsset;
    }

    void Awake() {
        myEventsAsset.Invoke();
    }
}
partial struct MySystem : ISystem {
    public void OnCreate(ref SystemState state) 
        => state.RequireForUpdate<SerializedDelegate>();
    public void OnUpdate(ref SystemState state) {
        var test = SystemAPI.ManagedAPI.GetSingleton<SerializedDelegate>();
        test?.Invoke();
        if (CoolUtilities.HasCalledLogHi) {
            var e = SystemAPI.ManagedAPI.GetSingletonEntity<SerializedDelegate>();
            SystemAPI.SetComponent(e, LocalTransform.FromPosition(0,2,3));
        }
        state.Enabled = false;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(MethodReference))]
public class SerializedDelegateDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        // Create property container element.
        var container = new VisualElement();
        
        // if property is null, create it
        // property.objectReferenceValue = ScriptableObject.CreateInstance<SerializedDelegate>();
        // property.serializedObject.ApplyModifiedProperties();
        // property.serializedObject.Update();
        
        // find all
        var validMethodsToPick = new List<MethodInfo>(TypeCache.GetMethodsWithAttribute(typeof(HiMethodAttribute)));
        
        // find currently serialized method
        var methodTypeProperty = property.FindPropertyRelative("typeNameToFindStaticMethodOnDeserialization");
        var methodOverloadIndexProperty = property.FindPropertyRelative("overloadIndex");
        
        // find method
        var methodsOnFoundType = methodTypeProperty == null ? null : Type.GetType(methodTypeProperty.stringValue)?
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo method = null;
        if (methodOverloadIndexProperty != null && methodsOnFoundType != null) {
            method = methodsOnFoundType.Length > methodOverloadIndexProperty.intValue && methodOverloadIndexProperty.intValue >= 0 ? methodsOnFoundType[methodOverloadIndexProperty.intValue] : null;
        }
        var methodNamesField = new PopupField<MethodInfo>("Methods", validMethodsToPick, method ?? validMethodsToPick[0]);
        UpdateProps(method ?? validMethodsToPick[0]);
        void UpdateProps(MethodInfo method) {
            //if (property.objectReferenceValue == null) property.objectReferenceValue = ScriptableObject.CreateInstance<SerializedDelegate>();
            if (methodTypeProperty != null) methodTypeProperty.stringValue = method.DeclaringType?.AssemblyQualifiedName;
            if (methodOverloadIndexProperty != null) methodOverloadIndexProperty.intValue = method.DeclaringType?.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).ToList().IndexOf(method) ?? -1;
            property.serializedObject.ApplyModifiedProperties();
        }

        methodNamesField.RegisterValueChangedCallback(e=> UpdateProps(e.newValue));
        container.Add(methodNamesField);
        
        return container;
    }
}
#endif

[AttributeUsage(AttributeTargets.Method)]
class HiMethodAttribute : Attribute { }

public static class Utilities {
    [HiMethod]
    public static void LogHi() {
        Debug.Log("Hi");
    }
    public static void LogHi(int a, float b) {
        Debug.Log($"Hi {a} {b}");
    }
    public static void LogHi(float a) {
        Debug.Log($"Hi {a}");
    }
    
    [HiMethod]
    public static void OtherThingy() {
        Debug.Log("OtherThingy");
    }
    
    [HiMethod]
    public static void OtherThingy2() {
        Debug.Log("OtherThingy2");
    }
    
    [HiMethod]
    public static void OtherThingy3() {
        Debug.Log("OtherThingy3");
    }
}

// CoolUtilities
static class CoolUtilities {
    [HiMethod]
    public static void LogHid() {
        Debug.Log("Hi");
        HasCalledLogHi = true;
    }

    internal static bool HasCalledLogHi = false;
    
    [HiMethod]
    static void OtherThingy() {
        Debug.Log("OtherThingy");
    }
}