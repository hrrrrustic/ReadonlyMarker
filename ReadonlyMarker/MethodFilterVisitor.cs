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
        private readonly CheckedNodesCache _checkedNodes;
        public bool IsValidMethod { get; private set; } = true;

        private readonly HashSet<String> _bannedMethods = new()
        {
            "Add", "Append", "Remove", 
            "Push", "Queue", "Sort", 
            "Pop", "Dequeue", "Clear", 
            "Insert", "AddRange", "Dispose",
            "MoveNext", "Reset"
        };
        public MethodFilterVisitor(SemanticModel semantic, CheckedNodesCache checkedNodes)
        {
            _semantic = semantic;
            _checkedNodes = checkedNodes;
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            base.VisitAssignmentExpression(node);

            var type = _semantic.GetSymbolInfo(node.Left);
            if (IsFieldOrPropertyOrUndefined(type))
            {
                IsValidMethod = false;
                return;
            }


            if (!node.Ancestors().OfType<InitializerExpressionSyntax>().Any())
                IsValidMethod = false;
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            base.VisitPostfixUnaryExpression(node);
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

            if(node.Expression is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax)
                return;

            var method = _semantic.GetSymbolInfo(node).Symbol as IMethodSymbol;
            if (method is null)
            {
                IsValidMethod = false;
                return;
            }

            var references = method
                .DeclaringSyntaxReferences
                .Select(k => k.GetSyntax());

            foreach (var syntaxNode in references)
            {
                if (_checkedNodes.TryGetValue(syntaxNode, out bool result))
                {
                    if(result)
                        continue;

                    IsValidMethod = false;
                    return;
                }

                _checkedNodes.AddCurrentNode(syntaxNode);
                var visitor = new MethodFilterVisitor(_semantic, _checkedNodes);
                visitor.Visit(syntaxNode);

                _checkedNodes.AddNode(syntaxNode, visitor.IsValidMethod);
                if (!visitor.IsValidMethod)
                {
                    IsValidMethod = false;
                    return;
                }
            }
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            var property = _semantic.GetSymbolInfo(node).Symbol as IPropertySymbol;
            if (property is null)
                return;
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