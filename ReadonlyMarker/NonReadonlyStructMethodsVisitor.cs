using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class NonReadonlyStructMethodsVisitor : CSharpSyntaxWalker
    {
        public readonly List<MethodDeclarationSyntax> NonReadonlyMethods = new List<MethodDeclarationSyntax>();
        public readonly List<AccessorDeclarationSyntax> NonReadonlyGetters = new List<AccessorDeclarationSyntax>();
        public int MethodCount => NonReadonlyGetters.Count + NonReadonlyMethods.Count;
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

            if (node.Keyword.ValueText == "set")
                return;

            if (property.Modifiers.HasStaticModifier())
                return;
            
            if (node.Modifiers.HasReadOnlyModifier())
                return;

            NonReadonlyGetters.Add(node);
        }
    }
}