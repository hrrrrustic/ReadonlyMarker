using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public static class Extensions
    {
        public static MethodDeclarationSyntax AsReadOnlyMethod(this MethodDeclarationSyntax method) =>
            method
                .WithModifiers(AddReadOnlyModifier(method.Modifiers));

        public static AccessorDeclarationSyntax AsReadOnlyGetter(this AccessorDeclarationSyntax getter) =>
            getter
                .WithModifiers(AddReadOnlyModifier(getter.Modifiers));

        private static SyntaxTokenList AddReadOnlyModifier(SyntaxTokenList modifiers) 
            => SyntaxFactory
                .TokenList(modifiers
                    .Append(SyntaxFactory
                        .Token(SyntaxKind.ReadOnlyKeyword)
                        .WithLeadingTrivia(SyntaxFactory.Space)));
        public static bool HasStaticModifier(this SyntaxTokenList tokens) => HasModifier(tokens, "static");
        public static bool HasReadOnlyModifier(this SyntaxTokenList tokens) => HasModifier(tokens, "readonly");
        public static bool HasUnsafeModifier(this SyntaxTokenList tokens) => HasModifier(tokens, "unsafe");
        private static bool HasModifier(SyntaxTokenList tokens, string modifier) => tokens.Any(k => k.ValueText == modifier);
    }
}