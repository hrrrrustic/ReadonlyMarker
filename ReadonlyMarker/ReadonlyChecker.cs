using System;
using System.Collections.Generic;
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
        public ReadonlyChecker(SemanticModel model)
        {
            _model = model;
        }

        public bool CheckArrowedProperty(PropertyDeclarationSyntax property)
        {
            if (property.Modifiers.HasReadOnlyModifier() && property.Modifiers.HasStaticModifier())
                return false;

            var accessorCount = property.DescendantNodes().OfType<AccessorDeclarationSyntax>().Count();
            if (accessorCount == 2)
                return false;

            if (accessorCount == 1)
            {
                return FilterMethod(property
                    .DescendantNodes()
                    .OfType<AccessorDeclarationSyntax>()
                    .First());
            }

            return FilterMethod(property
                .ChildNodes()
                .OfType<ArrowExpressionClauseSyntax>()
                .First());
        }

        public bool CheckGetter(AccessorDeclarationSyntax getter) 
            => getter.HasSetter() && CheckInnerMethods(getter) && InternalCheckGetter(getter);

        public bool CheckMethod(MethodDeclarationSyntax method) 
            => !_bannedMethods.Contains(method
                    .Identifier
                    .ToString()
                    .Trim()) && CheckInnerMethods(method) && InternalCheckMethod(method);

        private bool CheckInnerMethods(SyntaxNode node) =>
            node
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Select(k => _model.GetSymbolInfo(k).Symbol as IMethodSymbol)
                .Select(k => k?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax)
                .Where(k => k is not null)
                .Select(InternalCheckMethod)
                .All(k => k);

        private bool InternalCheckGetter(AccessorDeclarationSyntax getter) 
            => !getter.Modifiers.HasReadOnlyModifier() && FilterMethod(getter);

        private bool InternalCheckMethod(MethodDeclarationSyntax method) 
            => !method.Modifiers.HasReadOnlyModifier() && FilterMethod(method);

        private bool FilterMethod(SyntaxNode node)
        {
            var filter = new MethodFilterVisitor(_model);
            filter.Visit(node);

            return filter.IsValidMethod;
        }
    }
}