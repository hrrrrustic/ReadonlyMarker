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
                        .Append(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))))
                .NormalizeWhitespace();
        }
    }
}