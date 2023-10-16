using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Burst;
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
                .Where(m => 
                    m.GetCustomAttribute<MethodAllowsCallsFromAttribute>()?.DelegateSupported == propertyDelegateType
                    && m.MethodImplementationFlags == MethodImplAttributes.IL)
        );
        
        // if no methods found, mention it
        if (validMethodsToPick.Count == 0) { 
            var field = new PopupField<string>(nameOverride ?? property.name, new List<string> {
                $"No methods found for delegate type {propertyDelegateType}"
            }, 0);
            field.SetEnabled(false);
            container.Add(field);
            return;
        }
        
        // find currently serialized method
        var overloadIndexProperty = property.FindPropertyRelative("overloadIndex");
        var typeNameProperty = property.FindPropertyRelative("typeName");
        var nameProperty = property.FindPropertyRelative("name");
        var hasBurstCompileAttributeProperty = property.FindPropertyRelative("hasBurstCompileAttribute");
        var currentMethodInfo = UntypedMethodRef.TryGetRaw(typeNameProperty?.stringValue, nameProperty?.stringValue, overloadIndexProperty!.intValue);
        currentMethodInfo = validMethodsToPick.FirstOrDefault(m => m == currentMethodInfo);
        currentMethodInfo ??= validMethodsToPick[0]; // default to first method if none found
        currentMethodInfo.TryApplyTo(typeNameProperty, nameProperty, overloadIndexProperty, hasBurstCompileAttributeProperty);
        
        // Set up type method picker
        var methodSelectionField = new PopupField<MethodInfo>(nameOverride ?? property.name, validMethodsToPick, currentMethodInfo,
            MethodInfoToNiceName, MethodInfoToNiceName);
        methodSelectionField.RegisterValueChangedCallback(e
            => e.newValue.TryApplyTo(typeNameProperty, nameProperty, overloadIndexProperty, hasBurstCompileAttributeProperty));
        
        container.Add(methodSelectionField);
    }

    static void TryApplyTo(this MethodInfo methodToSet,
        SerializedProperty typeNameProperty,
        SerializedProperty nameProperty,
        SerializedProperty overloadIndexProperty,
        SerializedProperty hasBurstCompileAttributeProperty = null) {
        if (methodToSet?.DeclaringType is not {} methodType) return; 

        // set type
        if (typeNameProperty != null) 
            typeNameProperty.stringValue = methodType.AssemblyQualifiedName;
            
        // find index
        if (nameProperty != null)
            nameProperty.stringValue = methodToSet.Name;
        
        if (overloadIndexProperty != null) {
            var methodsOnType = methodType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var overloadIndex = 0;
            foreach (var methodInfo in methodsOnType) {
                if (methodInfo.Name != methodToSet.Name) continue;
                if (methodInfo == methodToSet) break;
                overloadIndex++;
            }
            overloadIndexProperty.intValue = overloadIndex;
        }
        
        // check if has burst compile attribute
        if (hasBurstCompileAttributeProperty is not null)
            hasBurstCompileAttributeProperty.boolValue = methodToSet.GetCustomAttribute<BurstCompileAttribute>() != null;
        
        typeNameProperty?.serializedObject.ApplyModifiedProperties();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static string MethodInfoToNiceName(MethodInfo m) 
        => $"{m?.DeclaringType?.FullName}.{m?.Name}";
}