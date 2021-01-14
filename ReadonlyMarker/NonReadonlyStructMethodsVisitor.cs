using System;
using System.Collections.Generic;
using System.Linq;
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
            if(node.ExplicitInterfaceSpecifier is not null)
                return;

            if (node.Modifiers.HasReadOnlyModifier() || node.Modifiers.HasUnsafeModifier() || node.Modifiers.HasStaticModifier())
                return;

            var attributes = node
                .AttributeLists
                .SelectMany(k => k.Attributes)
                .Select(k => k.Name.ToString().Trim().ToLower())
                .ToList();

            if (attributes.Contains("obsolete"))
                return;

            NonReadonlyMethods.Add(node);
        }

        public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            if(node?.Parent?.Parent is not PropertyDeclarationSyntax property)
                return;

            if(property.DescendantNodes().OfType<AccessorDeclarationSyntax>().Count() == 1)
                return;

            if (!node.DescendantNodes().Any())
                return;

            if (node.Keyword.ValueText == "set" || property.Modifiers.HasStaticModifier() || node.Modifiers.HasReadOnlyModifier())
                return;

            NonReadonlyGetters.Add(node);
        }
    }
}