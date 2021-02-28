using System;
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
            if (method.ExplicitInterfaceSpecifier is null || method.Modifiers.Count != 0)
                return method
                    .WithModifiers(method.Modifiers.WithReadonly());

            var returnType = method.ReturnType.ToString();
            return method.WithReturnType(SyntaxFactory.ParseTypeName($"readonly {returnType} "));
        }

        public static AccessorDeclarationSyntax AsReadOnlyGetter(this AccessorDeclarationSyntax getter) 
            => getter
                .WithModifiers(getter.Modifiers.WithReadonly());

        public static IndexerDeclarationSyntax AsReadonlyIndexer(this IndexerDeclarationSyntax indexer)
        {
            if (indexer.ExplicitInterfaceSpecifier is null)
                return indexer
                    .WithModifiers(indexer.Modifiers.WithReadonly());

            string type = indexer.Type.ToString();
            return indexer.WithType(SyntaxFactory.ParseTypeName($"readonly {type}"));
        }

        public static PropertyDeclarationSyntax AsReadOnlyProperty(this PropertyDeclarationSyntax property)
        {
            if(property.ExplicitInterfaceSpecifier is null)
                return property
                    .WithModifiers(property.Modifiers.WithReadonly());

            string type = property.Type.ToString();
            return property.WithType(SyntaxFactory.ParseTypeName($"readonly {type} "));
        }

        private static SyntaxTokenList WithReadonly(this SyntaxTokenList modifiers) 
            => SyntaxFactory
                .TokenList(modifiers
                    .Append(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword).WithTrailingTrivia(SyntaxFactory.Space)));

        public static bool HasStaticModifier(this MemberDeclarationSyntax member)
            => HasModifier(member.Modifiers, "static");
        public static bool HasReadOnlyModifier(this AccessorDeclarationSyntax accessor) 
            => HasModifier(accessor.Modifiers, "readonly");
        public static bool HasReadOnlyModifier(this MemberDeclarationSyntax member)
            => HasModifier(member.Modifiers, "readonly");
        public static bool HasUnsafeModifier(this MemberDeclarationSyntax member)
            => HasModifier(member.Modifiers, "unsafe");

        public static bool HasGetterAndSetter(this PropertyDeclarationSyntax property) 
            => HasGetter(property) && HasSetter(property);
        public static bool HasGetter(this PropertyDeclarationSyntax property)
            => property.AccessorList is not null && 
               property.AccessorList.Accessors.Any(k => k.IsKind(SyntaxKind.GetAccessorDeclaration));
        public static bool HasSetter(this PropertyDeclarationSyntax property)
            => property.AccessorList is not null &&
               property.AccessorList.Accessors.Any(k => k.IsKind(SyntaxKind.SetAccessorDeclaration));

        public static bool IsGetter(this AccessorDeclarationSyntax accessor)
            => accessor.IsKind(SyntaxKind.GetAccessorDeclaration);
        public static bool IsSetter(this AccessorDeclarationSyntax accessor)
            => accessor.IsKind(SyntaxKind.SetAccessorDeclaration);  
        private static bool HasModifier(SyntaxTokenList tokens, string modifier) => tokens.Any(k => k.ValueText == modifier);
    }
}