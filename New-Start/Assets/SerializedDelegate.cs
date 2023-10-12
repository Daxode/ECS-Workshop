using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Serialization;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
class MethodReference {
    public string typeNameToFindStaticMethodOnDeserialization;
    public int overloadIndex;
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SerializedDelegate))]
class SerializedDelegateDr : PropertyDrawer {
    public override VisualElement CreatePropertyGUI(SerializedProperty property) {
        var container = new VisualElement();
        // if (property.objectReferenceValue == null) {
        //     property.objectReferenceValue = ScriptableObject.CreateInstance<SerializedDelegate>();
        //     property.serializedObject.ApplyModifiedProperties();
        //     property.serializedObject.Update();
        // }
        var methodProperty = property.FindPropertyRelative("methodReference");
        var methodField = new PropertyField(methodProperty, "Method");
        methodField.Bind(property.serializedObject);
        container.Add(methodField);
        return container;
    }
}
#endif

//[CreateAssetMenu(menuName = "Create SerializedDelegate", fileName = "SerializedDelegate", order = 0)]
[Serializable]
[ChunkSerializable]
public class SerializedDelegate : IComponentData, ISerializationCallbackReceiver
{
    [DontSerialize] IntPtr Action; // ready at runtime
    [SerializeField] MethodReference methodReference;
    
    public void Invoke() {
        if (Action == IntPtr.Zero) OnAfterDeserialize();
        if (Action == IntPtr.Zero) return;
        var action = Marshal.GetDelegateForFunctionPointer<Action>(Action);
        action();
    }

    public void OnBeforeSerialize() {}

    public void OnAfterDeserialize() {
        Debug.Log($"OnAfterDeserialize {methodReference.typeNameToFindStaticMethodOnDeserialization} {methodReference.overloadIndex}");
        if (string.IsNullOrEmpty(methodReference.typeNameToFindStaticMethodOnDeserialization)) return;
        var type = Type.GetType(methodReference.typeNameToFindStaticMethodOnDeserialization);
        if (type == null) return;
        // get methods with matching name, then pick the one with the right overload index
        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (methods.Length <= methodReference.overloadIndex || methodReference.overloadIndex < 0) return;
        var method = methods[methodReference.overloadIndex];
        Action = method.MethodHandle.GetFunctionPointer();
    }
}