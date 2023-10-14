using Microsoft.CodeAnalysis;

public static class Diagnostics
{
    // Parameter length
    public const string ID_MRA0001 = "MRA0001";
    public static readonly DiagnosticDescriptor k_MRA0001Descriptor
        = new (ID_MRA0001, "MethodAllowsCallsFrom parameter length mismatch",
            "Your method '{0}' has to match the signature of the callback '{1}'. Your method has {2} parameters, but the callback has {3} parameters.",
            "MethodRef", DiagnosticSeverity.Error, isEnabledByDefault: true,
            description: "Callback in MethodAllowsCallsFrom doesn't match signature of method.");

    // Parameter type
    public const string ID_MRA0002 = "MRA0002";
    public static readonly DiagnosticDescriptor k_MRA0002Descriptor
        = new (ID_MRA0002, "MethodAllowsCallsFrom mismatch in parameter types",
            "Your method '{0}' has to match the signature of the callback '{1}. Your method has a parameter of type '{2}', but the callback has a parameter of type '{3}'.",
            "MethodRef", DiagnosticSeverity.Error, isEnabledByDefault: true,
            description: "Callback in MethodAllowsCallsFrom doesn't match signature of method.");

    // Return type
    public const string ID_MRA0003 = "MRA0003";
    public static readonly DiagnosticDescriptor k_MRA0003Descriptor
        = new (ID_MRA0003, "MethodAllowsCallsFrom mismatch in return type",
            "Your method '{0}' has to match the signature of the callback '{1}'. Your method has a return type of '{2}', but the callback has a return type of '{3}'.",
            "MethodRef", DiagnosticSeverity.Error, isEnabledByDefault: true,
            description: "Callback in MethodAllowsCallsFrom doesn't match signature of method.");

    // Non-static method
    public const string ID_MRA0004 = "MRA0004";
    public static readonly DiagnosticDescriptor k_MRA0004Descriptor
        = new (ID_MRA0004, "MethodAllowsCallsFrom used on a non-static method",
            "MethodAllowsCallsFrom on a non-static method '{0}'. Please define as static.",
            "MethodRef", DiagnosticSeverity.Error, isEnabledByDefault: true,
            description: "MethodAllowsCallsFrom is only valid on static methods, as it would otherwise require a reference to the instance.");
    
    // IL2CPP MonoPInvokeCallback
    public const string ID_MRA0005 = "MRA0005";
    public static readonly DiagnosticDescriptor k_MRA0005Descriptor
        = new (ID_MRA0005, "MethodAllowsCallsFrom used on a method without MonoPInvokeCallback",
            "MethodAllowsCallsFrom on method '{0}' without MonoPInvokeCallback in IL2CPP build. Please add [MonoPInvokeCallback(typeof({1}))].",
            "MethodRef", DiagnosticSeverity.Error, isEnabledByDefault: true,
            description: "With IL2CPP enabled, MethodAllowsCallsFrom requires MonoPInvokeCallback.");

    
}