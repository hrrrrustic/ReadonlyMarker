using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class NonReadonlyStructMethodsVisitor : CSharpSyntaxWalker
    {
        public readonly List<MethodDeclarationSyntax> NonReadonlyMethods = new();
        public readonly List<AccessorDeclarationSyntax> NonReadonlyGetters = new();
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Modifiers.HasReadOnlyModifier() || node.Modifiers.HasUnsafeModifier() || node.Modifiers.HasStaticModifier())
                return;

            NonReadonlyMethods.Add(node);
        }

        public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            if(node?.Parent?.Parent is not PropertyDeclarationSyntax property)
                return;

            if (node.Keyword.ValueText == "set" || property.Modifiers.HasStaticModifier() || node.Modifiers.HasReadOnlyModifier())
                return;

            NonReadonlyGetters.Add(node);
        }
    }
}