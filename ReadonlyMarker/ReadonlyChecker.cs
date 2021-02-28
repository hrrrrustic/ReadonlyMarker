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
        private readonly HashSet<string> _bannedMethods = new() {"Dispose"};

        public ReadonlyChecker(SemanticModel model)
        {
            _model = model;
        }

        public bool CanBeMarkedAsReadOnly(MethodDeclarationSyntax method)
        {
            var methodName = method.Identifier.ToString().Trim();
            return !_bannedMethods.Contains(methodName) && InternalCanBeMarkedAsReadOnly(method);
        }

        public bool CanBeMarkedAsReadOnly(PropertyDeclarationSyntax property)
        {
            if (property.HasGetter())
                return false;

            return InternalCanBeMarkedAsReadOnly(property);
        }

        public bool CanBeMarkedAsReadOnly(AccessorDeclarationSyntax accessor)
        {
            if (accessor.HasReadOnlyModifier() || accessor.IsSetter())
                return false;

            return IsSafeCall(accessor);
        }

        public bool CanBeMarkedAsReadOnly(IndexerDeclarationSyntax indexer)
        {
            return InternalCanBeMarkedAsReadOnly(indexer);
        }

        private bool InternalCanBeMarkedAsReadOnly(MemberDeclarationSyntax member)
        {
            if (member.HasReadOnlyModifier() || member.HasStaticModifier())
                return false;

            return IsSafeCall(member);
        }

        private bool IsSafeCall(MemberDeclarationSyntax member)
        {
            if (member.HasReadOnlyModifier())
                return true;

            return InternalCanBeMarkedAsReadOnly((SyntaxNode)member);
        }

        private bool IsSafeCall(AccessorDeclarationSyntax accessorCall)
        {
            if (accessorCall.HasReadOnlyModifier())
                return true;

            return InternalCanBeMarkedAsReadOnly(accessorCall);
        }

        private bool InternalCanBeMarkedAsReadOnly(SyntaxNode node)
        {
            var cache = new CheckedNodesCache();
            cache.AddCurrentNode(node);
            var filter = new MethodFilterVisitor(_model, cache);
            filter.Visit(node);
            cache.AddNode(node, filter.IsValidMethod);
            return filter.IsValidMethod;
        }
    }
}