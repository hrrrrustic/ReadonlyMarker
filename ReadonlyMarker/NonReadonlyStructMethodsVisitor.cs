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
        private readonly List<MethodDeclarationSyntax> _nonReadonlyMethods = new();
        private readonly List<AccessorDeclarationSyntax> _nonReadonlyGetters = new();
        private readonly List<PropertyDeclarationSyntax> _arrowedProperties = new();
        private readonly List<IndexerDeclarationSyntax> _indexers = new();

        public MatchedNodes GetPotentialNodes(SyntaxNode node)
        {
            Visit(node);
            return new MatchedNodes(_nonReadonlyMethods, _nonReadonlyGetters, _arrowedProperties, _indexers);
        }
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.HasReadOnlyModifier() || node.HasUnsafeModifier() || node.HasStaticModifier())
                return;

            if (IsInNestedClass(node))
                return;

            _nonReadonlyMethods.Add(node);
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

        public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            if (node.HasStaticModifier() || node.HasReadOnlyModifier())
                return;

            if (IsInNestedClass(node))
                return;

            _indexers.Add(node);
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
                    _nonReadonlyGetters.Add(getter);
                    return;
                }
            }

            if (node.ChildNodes().Skip(1).First() is ArrowExpressionClauseSyntax)
                _arrowedProperties.Add(node);
        }
    }
}