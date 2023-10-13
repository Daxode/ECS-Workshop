using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine;

[Serializable]
class UntypedMethodRef {
    public string typeName;
    public int methodIndex;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MethodInfo TryGet() => TryGetRaw(typeName, methodIndex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo TryGetRaw(string typeName, int methodIndex) {
        if (Type.GetType(typeName) is not {} type) return null;
        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (methodIndex < 0 || methodIndex >= methods.Length) return null;
        return methods[methodIndex];
    }
}

[AttributeUsage(AttributeTargets.Method)]
class MethodAllowsCallsFromAttribute : Attribute {
    public Type DelegateSupported;
    public MethodAllowsCallsFromAttribute(Type delegateSupported) {
        Debug.Assert(delegateSupported.IsSubclassOf(typeof(Delegate)));
        DelegateSupported = delegateSupported;
    }
}