using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class ReadonlyChecker
    {
        private readonly SemanticModel _model;

        private readonly HashSet<String> _bannedMethods = new HashSet<String>()
        {
            "Dispose"
        };

        private readonly Dictionary<SyntaxNode, bool> _checkedNodes = new Dictionary<SyntaxNode, Boolean>();
        public ReadonlyChecker(SemanticModel model)
        {
            _model = model;
        }

        public bool CheckArrowedProperty(PropertyDeclarationSyntax property)
        {
            if (property.ExplicitInterfaceSpecifier is not null || property.Modifiers.HasReadOnlyModifier() || property.Modifiers.HasStaticModifier())
                return false;

            var accessorCount = property.DescendantNodes().OfType<AccessorDeclarationSyntax>().Count();
            if (accessorCount == 2)
                return false;

            if (accessorCount == 1)
            {
                return CheckNode(property
                    .DescendantNodes()
                    .OfType<AccessorDeclarationSyntax>()
                    .First());
            }

            return CheckNode(property
                .ChildNodes()
                .OfType<ArrowExpressionClauseSyntax>()
                .First());
        }

        public bool CheckGetter(AccessorDeclarationSyntax getter) 
            => getter.PropertyHasSetter() && CheckNode(getter);

        public bool CheckMethod(MethodDeclarationSyntax method) 
            => !_bannedMethods.Contains(method
                    .Identifier
                    .ToString()
                    .Trim()) && CheckNode(method);

        private bool CheckNode(SyntaxNode node)
        {
            if (_checkedNodes.ContainsKey(node))
                return _checkedNodes[node];

            _checkedNodes.Add(node, true);
            var res = CheckInnerMethods(node) && InternalCheckNode(node);
            _checkedNodes[node] = res;
            return res;
        }

        private bool CheckInnerMethods(SyntaxNode node) =>
            node
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Select(k => _model.GetSymbolInfo(k).Symbol as IMethodSymbol)
                .Select(k => k?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax)
                .Where(k => k is not null)
                .Select(CheckNode)
                .All(k => k);

        private bool InternalCheckNode(SyntaxNode node)
        {
            if (node is AccessorDeclarationSyntax accessor)
                return !accessor.Modifiers.HasReadOnlyModifier() && FilterMethod(accessor);

            if(node is MethodDeclarationSyntax method)
                return !(method.Modifiers.HasReadOnlyModifier() || method.ExplicitInterfaceSpecifier is not null) && FilterMethod(method);

            return false;
        }
        private bool FilterMethod(SyntaxNode node)
        {
            var filter = new MethodFilterVisitor(_model);
            filter.Visit(node);

            return filter.IsValidMethod;
        }
    }
}