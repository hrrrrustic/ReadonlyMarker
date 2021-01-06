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

        public bool CheckFullMethod(MethodDeclarationSyntax method)
        {
            var t = method
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .ToList();

            var invokedMethods = method
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Select(k => ModelExtensions.GetSymbolInfo(_model, k).Symbol as IMethodSymbol)
                .Select(k => k?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax)
                .Where(k => k is not null)
                .ToList();

            var innerResult = invokedMethods
                .Select(CheckMethod)
                .All(k => k);

            if (!innerResult)
                return false;

            return CheckMethod(method);
        }

        private bool CheckMethod(MethodDeclarationSyntax method)
        {
            if (method.Modifiers.Any(k => k.ValueText == "readonly"))
                return true;

            var filter = new MethodFilterVisitor(_model);
            filter.Visit(method);

            return filter.IsValidMethod;
        }
    }
}