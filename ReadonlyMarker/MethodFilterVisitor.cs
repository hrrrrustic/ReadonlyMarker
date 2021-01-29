using System;
using System.Collections.Generic;
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

        private readonly HashSet<String> _bannedMethods = new HashSet<String>()
        {
            "Add", "Append", "Remove", 
            "Push", "Queue", "Sort", 
            "Pop", "Dequeue", "Clear", 
            "Insert", "AddRange", "Dispose"
        };
        public MethodFilterVisitor(SemanticModel semantic)
        {
            _semantic = semantic;
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var type = _semantic.GetSymbolInfo(node.Left);
            if (!IsFieldOrPropertyOrUndefined(type)) 
                return;

            if (!node.Ancestors().OfType<InitializerExpressionSyntax>().Any())
                IsValidMethod = false;
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            CheckIncrementOrDecrement(node.Operand, node.OperatorToken);
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            base.VisitPrefixUnaryExpression(node);
            CheckIncrementOrDecrement(node.Operand, node.OperatorToken);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);
            var invokeArguments = node.ArgumentList.Arguments;
            if (invokeArguments.Any(k => k.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) && IsFieldOrPropertyOrUndefined(_semantic.GetSymbolInfo(k))))
            {
                IsValidMethod = false;
                return;
            }

            var methodName = node
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Take(2)
                .Select(k => k.Identifier.ToString().Trim());

            if (methodName.Any(k => _bannedMethods.Contains(k)))
                IsValidMethod = false;
        }

        public override void VisitThrowExpression(ThrowExpressionSyntax node)
        {
            if (node.Parent?.Parent is not MethodDeclarationSyntax method)
                return;

            if (method.ChildNodes().OfType<ArrowExpressionClauseSyntax>().First().ChildNodes().Count() == 1)
                IsValidMethod = false;
        }

        public override void VisitThrowStatement(ThrowStatementSyntax node)
        {
            if(node.Parent?.Parent is not MethodDeclarationSyntax method)
                return;

            if (method.ChildNodes().OfType<BlockSyntax>().First().ChildNodes().Count() == 1)
                IsValidMethod = false;
        }
        private bool IsFieldOrPropertyOrUndefined(SymbolInfo symbol) 
            => symbol.Symbol is null || symbol.Symbol.Kind == SymbolKind.Field || symbol.Symbol.Kind == SymbolKind.Property;

        private void CheckIncrementOrDecrement(ExpressionSyntax operand, SyntaxToken operation)
        {
            if (!IsUnaryChangingValueOperation(operation))
                return;

            if (IsFieldOrPropertyOrUndefined(_semantic.GetSymbolInfo(operand)))
                IsValidMethod = false;
        }
        private bool IsUnaryChangingValueOperation(SyntaxToken token) 
            => token.Kind() is (SyntaxKind.PlusPlusToken or SyntaxKind.MinusMinusToken or SyntaxKind.MinusEqualsToken or SyntaxKind.PlusEqualsToken);
    }
}