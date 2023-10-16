using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = CSharpCodeFixVerifier<MethodRefAnalyzer, MethodRefCodeFixProvider>;

namespace Daxode.MethodRef.Analyzer.Test;

[TestClass]
public class MethodRefTests {
    [TestMethod]
    public Task MethodAllowsCallsFromValid() {
        const string test = @"
            static class ContainingType
            {
                delegate void ActionWithInt(int i);

                [AOT.MonoPInvokeCallback(typeof(ActionWithInt))]
                [MethodAllowsCallsFrom(typeof({|#0:ActionWithInt|}))]
                public static void TestAction(int i) {}
            }";
        return VerifyCS.VerifyAnalyzerAsync(test);
    }
    
    [TestMethod]
    public Task MethodAllowsCallsFromMismatchedLength() {
        const string test = @"
            static class ContainingType
            {
                delegate void ActionWithInt(int i);                
                [MethodAllowsCallsFrom(typeof({|#0:ActionWithInt|}))]
                public static void TestAction() {}
            }";

        var expected = VerifyCS.Diagnostic(Diagnostics.ID_MRA0001).WithLocation(0)
            .WithArguments("TestAction", "ActionWithInt", "0", "1");
        return VerifyCS.VerifyAnalyzerAsync(test, expected);
    }
    
    [TestMethod]
    public Task MethodAllowsCallsFromMismatchedParameterType() {
        const string test = @"
            static class ContainingType
            {
                delegate void ActionWithInt(int i);
                [MethodAllowsCallsFrom(typeof({|#0:ActionWithInt|}))]
                public static void TestAction(string s) {}
            }";

        var expected = VerifyCS.Diagnostic(Diagnostics.ID_MRA0002).WithLocation(0)
            .WithArguments("TestAction", "ActionWithInt", "string", "int");
        return VerifyCS.VerifyAnalyzerAsync(test, expected);
    }
    
    [TestMethod]
    public Task MethodAllowsCallsFromMismatchedReturnType() {
        const string test = @"
            static class ContainingType
            {
                delegate int ActionReturnInt();
                [MethodAllowsCallsFrom(typeof({|#0:ActionReturnInt|}))]
                public static void TestAction() {}
            }";

        var expected = VerifyCS.Diagnostic(Diagnostics.ID_MRA0003).WithLocation(0)
            .WithArguments("TestAction", "ActionReturnInt", "void", "int");
        return VerifyCS.VerifyAnalyzerAsync(test, expected);
    }
    
    // [TestMethod]
    // public Task MethodAllowsCallsFromMismatchedParameterTypeAndReturnType() {
    //     const string test = @"
    //         static class ContainingType
    //         {
    //             delegate int ActionReturnInt(int i);
    //             [MethodAllowsCallsFrom(typeof({|#0:ActionReturnInt|}))]
    //             public static void TestAction(string s) {}
    //         }";
    //
    //     var expected = VerifyCS.Diagnostic(Diagnostics.ID_MRA0004).WithLocation(0)
    //         .WithArguments("TestAction", "ActionReturnInt", "string", "int", "void", "int");
    //     return VerifyCS.VerifyAnalyzerAsync(test, expected);
    // }
    
    // Test MonoPInvokeCallback
    [TestMethod]
    public Task MethodAllowsCallsFromMonoPInvokeCallback() {
        const string test = @"
            using AOT;
            static class ContainingType
            {
                [MethodAllowsCallsFrom(typeof({|#0:System.Action|}))]
                public static void TestAction() {}
            }";
        
        // should implement code fix to add [MonoPInvokeCallback(typeof(System.Action))]
        // const string fixedSource = @"
        //     using AOT;
        //     static class ContainingType
        //     {
        //         [MonoPInvokeCallback(typeof(System.Action))]
        //         [MethodAllowsCallsFrom(typeof(System.Action))]
        //         public static void TestAction() {}
        //     }";
        var expected = VerifyCS.Diagnostic(Diagnostics.ID_MRA0005).WithLocation(0)
            .WithArguments("TestAction", "System.Action");
        return VerifyCS.VerifyAnalyzerAsync(test, expected);
    }
}