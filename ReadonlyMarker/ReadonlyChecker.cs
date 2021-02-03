using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class ReadonlyChecker
    {
        private readonly SemanticModel _model;
        private readonly Dictionary<SyntaxNode, bool> _checkedNodes = new Dictionary<SyntaxNode, Boolean>();

        private readonly HashSet<string> _bannedMethods = new HashSet<String>()
            {"Dispose "};

        public ReadonlyChecker(SemanticModel model)
        {
            _model = model;
        }

        public bool CanBeMarkedAsReadOnly(MethodDeclarationSyntax method)
        {
            if (method.HasReadOnlyModifier() || method.HasStaticModifier())
                return false;

            var methodName = method.Identifier.ToString().Trim();
            return !_bannedMethods.Contains(methodName) && CheckNode(method);
        }

        public bool CanBeMarkedAsReadOnly(PropertyDeclarationSyntax property)
        {
            if (property.HasReadOnlyModifier() || property.HasStaticModifier() || property.HasGetter())
                return false;

            return CheckNode(property);
        }

        public bool CanBeMarkedAsReadOnly(AccessorDeclarationSyntax accessor)
        {
            if (accessor.HasReadOnlyModifier() || accessor.IsSetter())
                return false;

            return CheckNode(accessor);
        }

        public bool CanBeMarkedAsReadOnly(IndexerDeclarationSyntax indexer)
        {
            throw new NotImplementedException();
        }

        public bool IsSafeCall(InvocationExpressionSyntax invocation)
        {

            return true;


        }

        public bool IsSafeCall(MethodDeclarationSyntax methodCall)
        {
            if (methodCall.HasReadOnlyModifier())
                return true;

            return true;
        }

        public bool IsSafeCall(PropertyDeclarationSyntax propertyCall)
        {
            if (propertyCall.HasReadOnlyModifier())
                return true;

            return true;

        }

        private bool CheckNode(SyntaxNode node)
        {
            if (_checkedNodes.ContainsKey(node))
                return _checkedNodes[node];

            _checkedNodes.Add(node, true);
            var res = CheckInnerMethods(node) && InternalCheckNode(node);
            _checkedNodes[node] = res;
            return res;
        }

        private bool CheckInnerMethods(SyntaxNode node)
        {
            var innerCalls = node
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Select(k => (k, _model.GetSymbolInfo(k).Symbol as IMethodSymbol));

            foreach ((var expression, var method) in innerCalls)
            {
                if (!CanBeMarkedAsReadOnly(expression))
                    return false;

                if(method is null && expression.Expression is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax)
                    continue;

                if (method is null)
                    return false;

                var references = method
                    .DeclaringSyntaxReferences
                    .Select(k => k.GetSyntax());

                if (!references.Where(k => k is MethodDeclarationSyntax or PropertyDeclarationSyntax).All(CheckNode))
                    return false;
            }

            var innerThisCalls = node
                .DescendantNodes()
                .OfType<ThisExpressionSyntax>()
                .Where(k => k.Parent is MemberAccessExpressionSyntax)
                .Select(k => k.Parent!.ChildNodes().OfType<IdentifierNameSyntax>().First())
                .Select(k => _model.GetSymbolInfo(k).Symbol as IMethodSymbol);
            
            foreach (var method in innerThisCalls)
            {
                if (method is null)
                    return false;

                var innerResult = method
                    .DeclaringSyntaxReferences
                    .Select(k => k.GetSyntax())
                    .Where(k => k is MethodDeclarationSyntax or PropertyDeclarationSyntax)
                    .All(CheckNode);

                if (!innerResult)
                    return false;
            }

            return true;
        }

        private bool InternalCheckNode(SyntaxNode node)
        {
            if (node is AccessorDeclarationSyntax accessor)
                return !accessor.HasReadOnlyModifier() && CanBeMarkedAsReadOnly((SyntaxNode) accessor);

            if(node is MethodDeclarationSyntax method)
                return !(method.HasReadOnlyModifier()/* || method.ExplicitInterfaceSpecifier is not null*/) && CanBeMarkedAsReadOnly((SyntaxNode) method);

            throw new NotSupportedException();
        }

        private List<SyntaxNode> GetInnerCalls(SyntaxNode node)
        {

            return null;
        }
        private bool CanBeMarkedAsReadOnly(SyntaxNode node)
        {
            var filter = new MethodFilterVisitor(_model);
            filter.Visit(node);

            return filter.IsValidMethod;
        }
    }
}