using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(UntypedMethodRef))]
public class MethodReferenceDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        // create container
        var container = new VisualElement();
        container.AddMethodReferencePicker(property, typeof(Action), null);
        return container;
    }
}

internal static class MethodReferenceDrawerUtility {
    internal static void AddMethodReferencePicker(this VisualElement container, SerializedProperty property, Type propertyDelegateType, string nameOverride) {
        // find all methods for this delegate type
        var validMethodsToPick = new List<MethodInfo>(
            TypeCache.GetMethodsWithAttribute(typeof(MethodAllowsCallsFromAttribute))
                .Where(m => m.GetCustomAttribute<MethodAllowsCallsFromAttribute>()?.DelegateSupported == propertyDelegateType)
        );

        // Assert matches signature of delegate type
        for (var i = validMethodsToPick.Count - 1; i >= 0; i--) {
            var validMethodToPick = validMethodsToPick[i];
            if (Delegate.CreateDelegate(propertyDelegateType, validMethodToPick, false) is null) {
                Debug.LogError($"Method {validMethodToPick} does not match delegate type {propertyDelegateType}");
                validMethodsToPick.RemoveAtSwapBack(i);
            }
        }

        if (validMethodsToPick.Count == 0) {
            container.Add(new Label($"No methods found for delegate type {propertyDelegateType}"));
            return;
        }
        
        // find currently serialized method
        var methodIndexProperty = property.FindPropertyRelative("methodIndex");
        var methodTypeProperty = property.FindPropertyRelative("typeName");
        var currentMethodInfo = UntypedMethodRef.TryGetRaw(methodTypeProperty?.stringValue, methodIndexProperty!.intValue);
        currentMethodInfo ??= validMethodsToPick[0]; // default to first method if none found
        currentMethodInfo.TryApplyTo(methodTypeProperty, methodIndexProperty);
        
        // Set up type method picker
        var methodSelectionField = new PopupField<MethodInfo>(nameOverride ?? property.name, validMethodsToPick, currentMethodInfo,
            MethodInfoToNiceName, MethodInfoToNiceName);
        methodSelectionField.RegisterValueChangedCallback(e
            => e.newValue.TryApplyTo(methodTypeProperty, methodIndexProperty));
        
        container.Add(methodSelectionField);
    }

    static void TryApplyTo(this MethodInfo methodToSet, SerializedProperty methodTypeProperty, SerializedProperty methodIndexProperty) {
        if (methodToSet?.DeclaringType is not {} methodType) return; 

        // set type
        if (methodTypeProperty != null) 
            methodTypeProperty.stringValue = methodType.AssemblyQualifiedName;
            
        // find index
        if (methodIndexProperty != null) {
            var methodsOnType = methodType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            methodIndexProperty.intValue = Array.IndexOf(methodsOnType, methodToSet);
        }
        
        (methodTypeProperty ?? methodIndexProperty)?.serializedObject.ApplyModifiedProperties();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static string MethodInfoToNiceName(MethodInfo m) 
        => $"{m?.DeclaringType?.FullName}.{m?.Name}";
}