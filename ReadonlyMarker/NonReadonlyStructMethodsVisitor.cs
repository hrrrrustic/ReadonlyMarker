using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class NonReadonlyStructMethodsVisitor : CSharpSyntaxWalker
    {
        public readonly List<MethodDeclarationSyntax> NonReadonlyMethods = new();
        public readonly List<AccessorDeclarationSyntax> NonReadonlyGetters = new();
        public readonly List<PropertyDeclarationSyntax> ArrowedProperties = new();
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

            if (IsInNestedClass(node))
                return;

            NonReadonlyMethods.Add(node);
        }

        private bool IsInNestedClass(MethodDeclarationSyntax node)
        {
            SyntaxNode currentNode = node;
            while (true)
            {
                if (currentNode is null)
                    return true;

                if (currentNode is StructDeclarationSyntax)
                    return false;

                if (currentNode is ClassDeclarationSyntax)
                    return true;

                currentNode = currentNode.Parent;
            }
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (node.Modifiers.HasStaticModifier() || node.Modifiers.HasReadOnlyModifier())
                return;

            var getter = node.DescendantNodes().OfType<AccessorDeclarationSyntax>().FirstOrDefault();

            if (getter is not null)
            {
                if (getter.Modifiers.HasReadOnlyModifier())
                    return;

                if (!getter.DescendantNodes().Any())
                    return;

                NonReadonlyGetters.Add(getter);
            }

            if (node.ChildNodes().Skip(1).First() is ArrowExpressionClauseSyntax)
                ArrowedProperties.Add(node);
        }
    }
}