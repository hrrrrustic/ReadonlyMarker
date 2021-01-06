using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class MethodFilterVisitor : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semantic;
        public bool IsValidMethod { get; private set; } = true;
        public MethodFilterVisitor(SemanticModel semantic)
        {
            _semantic = semantic;
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var type = _semantic.GetSymbolInfo(node.Left);
            if (IsFieldOrProperty(type))
                if (!node.Ancestors().OfType<InitializerExpressionSyntax>().Any())
                    IsValidMethod = false;
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            if (!IsIncrementOrDecrement(node.OperatorToken))
                return;

            if (IsFieldOrProperty(_semantic.GetSymbolInfo(node.Operand)))
                IsValidMethod = false;
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            if(!IsIncrementOrDecrement(node.OperatorToken))
                return;

            if (IsFieldOrProperty(_semantic.GetSymbolInfo(node.Operand)))
                IsValidMethod = false;
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);
            var invokeArguments = node.ArgumentList.Arguments;
            if (invokeArguments.Any(k => k.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) && IsFieldOrProperty(_semantic.GetSymbolInfo(k))))
                IsValidMethod = false;
        }

        public override void VisitThrowExpression(ThrowExpressionSyntax node)
        {
            if (node.Parent?.Parent is MethodDeclarationSyntax)
                IsValidMethod = false;
        }

        public override void VisitThrowStatement(ThrowStatementSyntax node)
        {
            if (node.Parent?.Parent is MethodDeclarationSyntax)
                IsValidMethod = false;
        }

        private bool IsFieldOrProperty(SymbolInfo symbol)
        {
            if (symbol.Symbol is null || symbol.Symbol.Kind == SymbolKind.Field || symbol.Symbol.Kind == SymbolKind.Property)
                return true;

            return false;
        }

        private bool IsIncrementOrDecrement(SyntaxToken token)
        {
            if (token.Kind() is (SyntaxKind.PlusPlusToken or SyntaxKind.MinusToken))
                return true;

            return false;
        }
    }
}