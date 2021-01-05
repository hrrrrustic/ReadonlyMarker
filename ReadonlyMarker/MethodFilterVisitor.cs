using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class MethodFilterVisitor : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semantic;
        public bool ValidMethod { get; private set; } = true;
        public MethodFilterVisitor(SemanticModel semantic)
        {
            _semantic = semantic;
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            CheckAssignment(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            if (node.OperatorToken.Kind() == SyntaxKind.SuppressNullableWarningExpression)
                return;

            if (IsFieldOrProperty(_semantic.GetSymbolInfo(node.Operand)))
                ValidMethod = false;
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            if(node.OperatorToken.Kind() is not (SyntaxKind.PreDecrementExpression or SyntaxKind.PreIncrementExpression))
                return;

            if (IsFieldOrProperty(_semantic.GetSymbolInfo(node.Operand)))
                ValidMethod = false;
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var invokeArguments = node.ArgumentList.Arguments;
            if (invokeArguments.Any(k => k.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) && IsFieldOrProperty(_semantic.GetSymbolInfo(k))))
                ValidMethod = false;
        }

        private void CheckAssignment(AssignmentExpressionSyntax node)
        {
            var type = _semantic.GetSymbolInfo(node.Left);
            if (IsFieldOrProperty(type))
                ValidMethod = false;
        }

        private bool IsFieldOrProperty(SymbolInfo symbol)
        {
            if (symbol.Symbol is null || symbol.Symbol.Kind == SymbolKind.Field || symbol.Symbol.Kind == SymbolKind.Property)
                return true;

            return false;
        }
    }
}