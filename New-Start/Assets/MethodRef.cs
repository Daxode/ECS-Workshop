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
    [DontSerialize] FunctionPointer<TDelegate> m_CachedAction; // ready at runtime
    [SerializeField] UntypedMethodRef untypedMethodRef;
    
    public FunctionPointer<TDelegate> Get() {
        if (!m_CachedAction.IsCreated) UpdateCachedAction();
        return m_CachedAction;
    }

    public void UpdateCachedAction() {
        // get methods with matching name, then pick the one with the right overload index
        var method = untypedMethodRef.TryGet();
#if ENABLE_IL2CPP
        var ptr = Marshal.GetFunctionPointerForDelegate(method.CreateDelegate(typeof(TDelegate)));
#else
        var ptr = method.MethodHandle.GetFunctionPointer();
#endif
        m_CachedAction = new FunctionPointer<TDelegate>(ptr);
        if (!m_CachedAction.IsCreated)
            throw new Exception("Action failed to resolve");
    }
}