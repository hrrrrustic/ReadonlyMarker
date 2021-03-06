﻿using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class StructVisitor : CSharpSyntaxWalker
    {
        public readonly List<StructDeclarationSyntax> NonReadonlyStructs = new();

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            if (node.HasReadOnlyModifier() || node.HasUnsafeModifier())
                return;

            NonReadonlyStructs.Add(node);
        }
    }
}