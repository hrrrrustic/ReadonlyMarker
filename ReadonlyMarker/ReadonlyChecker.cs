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
            => PropertyHasSetter(getter) && (CheckInnerMethods(getter) && InternalCheckGetter(getter));

        private bool PropertyHasSetter(AccessorDeclarationSyntax getter) 
            => getter
                .Ancestors()
                .OfType<PropertyDeclarationSyntax>()
                .First()
                .DescendantNodes()
                .OfType<AccessorDeclarationSyntax>()
                .Count() == 2;

        public bool CheckMethod(MethodDeclarationSyntax method) 
            => CheckInnerMethods(method) && InternalCheckMethod(method);

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