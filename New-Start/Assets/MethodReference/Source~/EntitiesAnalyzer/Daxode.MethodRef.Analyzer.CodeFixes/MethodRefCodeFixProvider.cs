using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MethodRefCodeFixProvider)), Shared]
    public class MethodRefCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Diagnostics.ID_MRA0001);
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var node = root?.FindNode(diagnostic.Location.SourceSpan);
                // if (node is TypeDeclarationSyntax typeDeclarationSyntax) {
                //     if (diagnostic.Id == Diagnostics.ID_MRA0001)
                //     {
                //         context.RegisterCodeFix(
                //             CodeAction.Create(title: "Add MonoPInvoke attribute",
                //                 createChangedDocument: c => AddMonoPInvokeAttribute(context.Document, typeDeclarationSyntax, c),
                //                 equivalenceKey: "AddMonoPInvokeAttribute"),
                //             diagnostic);
                //     }
                // }
            }
        }

    // static async Task<Document> AddMonoPInvokeAttribute(Document document,
    //     TypeDeclarationSyntax typeDeclarationSyntax, CancellationToken cancellationToken)
    // {
    //     var monoPInvokeAttribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("MonoPInvoke"));
    //     var monoPInvokeAttributeList =
    //         SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(new[] {monoPInvokeAttribute}))
    //             .NormalizeWhitespace().WithTrailingTrivia(SyntaxFactory.LineFeed)
    //             .WithLeadingTrivia(typeDeclarationSyntax.GetLeadingTrivia());
    //
    //     var modifiedSyntax = typeDeclarationSyntax.AddAttributeLists(monoPInvokeAttributeList);
    //
    //     var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    //     Debug.Assert(oldRoot != null, nameof(oldRoot) + " != null");
    //
    //     var newRoot = oldRoot.ReplaceNode(typeDeclarationSyntax, modifiedSyntax);
    //     return document.WithSyntaxRoot(newRoot);
    // }
}