using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class StructVisitor : CSharpSyntaxWalker
    {
        public readonly List<StructDeclarationSyntax> NonReadonlyStructs = new List<StructDeclarationSyntax>();

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            if (node.Modifiers.HasReadOnlyModifier() || node.Modifiers.HasUnsafeModifier())
                return;

            NonReadonlyStructs.Add(node);
        }
    }
}