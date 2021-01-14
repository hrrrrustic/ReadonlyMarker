using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public static class Extensions
    {
        public static MethodDeclarationSyntax AsReadOnlyMethod(this MethodDeclarationSyntax method)
        {
            return method
                .WithModifiers(SyntaxFactory
                    .TokenList(method
                        .Modifiers
                        .Append(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword).WithTrailingTrivia(SyntaxFactory.Space))));
        }

        public static AccessorDeclarationSyntax AsReadOnlyGetter(this AccessorDeclarationSyntax getter)
        {
            return getter
                .WithModifiers(SyntaxFactory
                    .TokenList(getter
                        .Modifiers
                        .Append(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))));
        }
        public static bool HasStaticModifier(this SyntaxTokenList tokens) => HasModifier(tokens, "static");
        public static bool HasReadOnlyModifier(this SyntaxTokenList tokens) => HasModifier(tokens, "readonly");
        public static bool HasUnsafeModifier(this SyntaxTokenList tokens) => HasModifier(tokens, "unsafe");
        private static bool HasModifier(SyntaxTokenList tokens, string modifier) => tokens.Any(k => k.ValueText == modifier);
    }
}