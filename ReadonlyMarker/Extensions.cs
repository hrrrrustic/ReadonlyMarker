using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public static class Extensions
    {
        public static MethodDeclarationSyntax AsReadOnlyMethod(this MethodDeclarationSyntax method) 
            => method
                .WithModifiers(method.Modifiers.WithReadonly());

        public static AccessorDeclarationSyntax AsReadOnlyGetter(this AccessorDeclarationSyntax getter) 
            => getter
                .WithModifiers(getter.Modifiers.WithReadonly());

        public static PropertyDeclarationSyntax AsReadOnlyProperty(this PropertyDeclarationSyntax property) 
            => property
                .WithModifiers(property.Modifiers.WithReadonly());

        private static SyntaxTokenList WithReadonly(this SyntaxTokenList modifiers) 
            => SyntaxFactory
                .TokenList(modifiers
                    .Append(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword).WithTrailingTrivia(SyntaxFactory.Space)));

        public static bool HasSetter(this PropertyDeclarationSyntax property) 
            => property
                .DescendantNodes()
                .OfType<AccessorDeclarationSyntax>()
                .Count() == 2;

        public static bool PropertyHasSetter(this AccessorDeclarationSyntax getter)
            => HasSetter(getter.Ancestors().OfType<PropertyDeclarationSyntax>().First());

        public static bool HasStaticModifier(this SyntaxTokenList tokens) => HasModifier(tokens, "static");
        public static bool HasReadOnlyModifier(this SyntaxTokenList tokens) => HasModifier(tokens, "readonly");
        public static bool HasUnsafeModifier(this SyntaxTokenList tokens) => HasModifier(tokens, "unsafe");
        private static bool HasModifier(SyntaxTokenList tokens, string modifier) => tokens.Any(k => k.ValueText == modifier);
    }
}