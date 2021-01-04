using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class StructVisitor : CSharpSyntaxWalker
    {
        public List<StructDeclarationSyntax> NonReadoblyStructs = new List<StructDeclarationSyntax>();

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            if (node.Modifiers.Any(k => k.ValueText == "readonly"))
                return;

            NonReadoblyStructs.Add(node);
        }
    }
}