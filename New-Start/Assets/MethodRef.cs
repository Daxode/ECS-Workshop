using System;
using Unity.Burst;
using Unity.Serialization;
using UnityEngine;
#if ENABLE_IL2CPP
using System.Runtime.InteropServices;
#endif

[Serializable]
public class MethodRef<TDelegate> where TDelegate : Delegate
{
    // Stores a managed reference to the method
    [SerializeField] UntypedMethodRef untypedMethodRef;
    
    // Can only be used at runtime
    [DontSerialize] FunctionPointer<TDelegate> m_CachedAction;
    public FunctionPointer<TDelegate> Get() {
        if (!m_CachedAction.IsCreated) UpdateCachedAction();
        return m_CachedAction;
    }

    // This converts the authoring data to the runtime data (used at runtime)
    public void UpdateCachedAction() {
        // Get methods with matching name, then pick the one with the right overload index
        var method = untypedMethodRef.TryGet();
        if (method == null) 
            throw new Exception($"Method '{untypedMethodRef.typeName}.{untypedMethodRef.name}' with overload '{untypedMethodRef.overloadIndex}' not found");

        // Get function pointer
#if ENABLE_IL2CPP
        var ptr = Marshal.GetFunctionPointerForDelegate(method.CreateDelegate(typeof(TDelegate)));
#else
        var ptr = method.MethodHandle.GetFunctionPointer();
#endif
        m_CachedAction = new FunctionPointer<TDelegate>(ptr);
    }
}