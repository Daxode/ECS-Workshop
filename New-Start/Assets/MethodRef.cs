using System;
using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Serialization;
using UnityEngine;

[Serializable]
public class SerializedMethodData<TDelegate> where TDelegate : Delegate
{
    [DontSerialize] IntPtr m_CachedAction; // ready at runtime
    [SerializeField] UntypedMethodRef untypedMethodRef;
    
    public TDelegate Get() {
        if (m_CachedAction == IntPtr.Zero) UpdateCachedAction();
        if (m_CachedAction == IntPtr.Zero) throw new Exception("Action failed to resolve");
        return Marshal.GetDelegateForFunctionPointer<TDelegate>(m_CachedAction);
    }

    public void UpdateCachedAction() {
        // get methods with matching name, then pick the one with the right overload index
        var method = untypedMethodRef.TryGet();
        m_CachedAction = method.MethodHandle.GetFunctionPointer();
    }
}