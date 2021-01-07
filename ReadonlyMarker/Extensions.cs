using System.Linq;
using System.Runtime.Serialization;
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
                        .Append(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword).WithTrailingTrivia(SyntaxFactory.Space))));
        }

        public static bool HasStaticModifier(this SyntaxTokenList tokens)
        {
            return tokens.Any(k => k.ValueText == "static");
        }

        public static bool HasReadOnlyModifier(this SyntaxTokenList tokens)
        {
            return tokens.Any(k => k.ValueText == "readonly");
        }

        public static bool HasUnsafeModifier(this SyntaxTokenList tokens)
        {
            return tokens.Any(k => k.ValueText == "unsafe");
        }
    }
}