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

        public ReadonlyChecker(SemanticModel model)
        {
            _model = model;
        }

        public bool CheckGetter(AccessorDeclarationSyntax getter)
        {
            if (!PropertyHasSetter(getter))
                return false;

            if (!CheckInnerMethods(getter))
                return false;

            return InternalCheckGetter(getter);
        }

        private bool PropertyHasSetter(AccessorDeclarationSyntax getter)
        {
            return getter
                .Ancestors()
                .OfType<PropertyDeclarationSyntax>()
                .First()
                .DescendantNodes()
                .OfType<AccessorDeclarationSyntax>()
                .Count() == 2;
        }

        public bool CheckMethod(MethodDeclarationSyntax method)
        {
            if (!CheckInnerMethods(method))
                return false;

            return InternalCheckMethod(method);
        }

        private bool CheckInnerMethods(SyntaxNode node)
        {
            var invokedMethods = node
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Select(k => _model.GetSymbolInfo(k).Symbol as IMethodSymbol)
                .Select(k => k?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax)
                .Where(k => k is not null)
                .ToList();

            return invokedMethods
                .Select(InternalCheckMethod)
                .All(k => k);
        }

        private bool InternalCheckGetter(AccessorDeclarationSyntax getter)
        {
            if (getter.Modifiers.HasReadOnlyModifier())
                return false;

            return FilterMethod(getter);
        }

        private bool InternalCheckMethod(MethodDeclarationSyntax method)
        {
            if (method.Modifiers.HasReadOnlyModifier())
                return false;

            return FilterMethod(method);
        }

        private bool FilterMethod(SyntaxNode node)
        {
            var filter = new MethodFilterVisitor(_model);
            filter.Visit(node);

            return filter.IsValidMethod;
        }
    }
}