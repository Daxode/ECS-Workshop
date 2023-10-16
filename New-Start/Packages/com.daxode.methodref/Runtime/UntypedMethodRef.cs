using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Scripting;

namespace Daxode.MethodReference {
    [Serializable]
    public class UntypedMethodRef {
        public string typeName;
        public string name;
        public int overloadIndex;
        public bool hasBurstCompileAttribute;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MethodInfo TryGet() => TryGetRaw(typeName, name, overloadIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo TryGetRaw(string typeName, string name, int overloadIndex) {
            if (Type.GetType(typeName) is not {} type) return null;
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods) {
                if (method.Name != name) continue;
                if (overloadIndex-- > 0) continue;
                return method;
            }
            return null;
        }
    }

    [RequireAttributeUsages]
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class MethodAllowsCallsFromAttribute : Attribute {
        public Type DelegateSupported;
        public MethodAllowsCallsFromAttribute(Type delegateSupported) {
            Debug.Assert(delegateSupported.IsSubclassOf(typeof(Delegate)));
            DelegateSupported = delegateSupported;
        }
    }
}
