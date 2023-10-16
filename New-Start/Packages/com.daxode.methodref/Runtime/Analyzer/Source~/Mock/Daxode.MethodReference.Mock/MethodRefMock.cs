public class MethodRefMock {}

[AttributeUsage(AttributeTargets.Method)]
public class MethodAllowsCallsFromAttribute : Attribute {
    public Type DelegateSupported;
    public MethodAllowsCallsFromAttribute(Type delegateSupported) {
        DelegateSupported = delegateSupported;
    }
}