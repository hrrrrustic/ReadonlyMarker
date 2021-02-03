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
            if (node.HasReadOnlyModifier() || node.HasUnsafeModifier() || node.HasStaticModifier())
                return;

            if (IsInNestedClass(node))
                return;

            NonReadonlyMethods.Add(node);
        }

        private bool IsInNestedClass(SyntaxNode node)
        {
            if (!node.Ancestors().OfType<ClassDeclarationSyntax>().Any())
                return false;

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
            if (node.HasStaticModifier() || node.HasReadOnlyModifier())
                return;

            if(IsInNestedClass(node))
                return;

            var getter = node.DescendantNodes().OfType<AccessorDeclarationSyntax>().FirstOrDefault();

            if (getter is not null)
            {
                if (getter.HasReadOnlyModifier())
                    return;

                if (node.HasSetter())
                {
                    NonReadonlyGetters.Add(getter);
                    return;
                }
            }

            if (node.ChildNodes().Skip(1).First() is ArrowExpressionClauseSyntax)
                ArrowedProperties.Add(node);
        }
    }
}