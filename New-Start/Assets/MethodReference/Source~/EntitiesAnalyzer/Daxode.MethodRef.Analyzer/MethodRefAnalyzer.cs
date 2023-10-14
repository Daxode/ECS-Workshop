using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MethodRefAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(contextCompilation => {
            var defines = contextCompilation.Compilation.SyntaxTrees.First().Options.PreprocessorSymbolNames;
            var il2CppIsDefined = defines.Contains("ENABLE_IL2CPP");
            contextCompilation.RegisterSyntaxNodeAction(
                c=>AnalyzeAttribute(c, il2CppIsDefined), SyntaxKind.Attribute);
        });
    }
    
    static void AnalyzeAttribute(SyntaxNodeAnalysisContext context, bool il2CppIsDefined)
    {
        if (context.Node is not AttributeSyntax attribute)
            return;
        
        var isMethodAllowsCallsFrom = false;
        ITypeSymbol attributeType;
        // Quick check to see if none of the attributes have burst compile at all in their name
        // This might have false negatives, but should greatly speed up analyzer runs (and is worth it I think)
        if (attribute.Name.ToString().Contains("MethodAllowsCallsFrom"))
        {
            attributeType = context.SemanticModel.GetTypeInfo(attribute).Type;
            // Deeper check of semantic model
            if (attributeType?.ToDisplayString() == "MethodAllowsCallsFromAttribute")
                isMethodAllowsCallsFrom = true;
        }
        if (!isMethodAllowsCallsFrom)
            return;
        if (attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression is not TypeOfExpressionSyntax {Type:{} callbackTypeSyntax})
            return;

        var callbackType = context.SemanticModel.GetTypeInfo(callbackTypeSyntax).Type as INamedTypeSymbol;
        var invokeMethod = callbackType?.DelegateInvokeMethod;
        if (invokeMethod is null)
            return;
        var attributeCallbackParameters = invokeMethod.Parameters;
        
        // Assert on mismatch between method and callbackType
        var methodDeclaration = attribute.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDeclaration is null)
            return;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
        if (methodSymbol is null)
            return;
        
        var methodParameters = methodSymbol.Parameters;
        if (methodParameters.Length != attributeCallbackParameters.Length)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.k_MRA0001Descriptor, callbackTypeSyntax.GetLocation(),
                methodDeclaration.Identifier.ToFullString(), callbackTypeSyntax.ToFullString(),
                methodParameters.Length, attributeCallbackParameters.Length
                ));
            return;
        }
        
        // Assert parameter types match
        for (var i = 0; i < methodParameters.Length; i++)
        {
            var methodParameter = methodParameters[i];
            var attributeCallbackParameter = attributeCallbackParameters[i];
            if (!SymbolEqualityComparer.Default.Equals(methodParameter.Type, attributeCallbackParameter.Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.k_MRA0002Descriptor, callbackTypeSyntax.GetLocation(),
                    methodDeclaration.Identifier.ToFullString(), callbackTypeSyntax.ToFullString(),
                    methodParameter.Type.ToDisplayString(), attributeCallbackParameter.Type.ToDisplayString()));
                return;
            }
        }
        
        // Assert return types match
        if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, invokeMethod.ReturnType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.k_MRA0003Descriptor, callbackTypeSyntax.GetLocation(),
                methodDeclaration.Identifier.ToFullString(), callbackTypeSyntax.ToFullString(), 
                methodSymbol.ReturnType.ToDisplayString(), invokeMethod.ReturnType.ToDisplayString()));
            return;
        }
        
        // if define "ENABLE_IL2CPP" is defined, check for MonoPInvokeCallback attribute
        if (il2CppIsDefined)
        {
            var hasMonoPInvokeCallbackAttribute = methodSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "MonoPInvokeCallbackAttribute");
            if (!hasMonoPInvokeCallbackAttribute)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.k_MRA0005Descriptor, callbackTypeSyntax.GetLocation(),
                    methodDeclaration.Identifier.ToFullString(), callbackTypeSyntax.ToFullString()));
                return;
            }
        }
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        Diagnostics.k_MRA0001Descriptor,
        Diagnostics.k_MRA0002Descriptor,
        Diagnostics.k_MRA0003Descriptor,
        Diagnostics.k_MRA0005Descriptor);
}
