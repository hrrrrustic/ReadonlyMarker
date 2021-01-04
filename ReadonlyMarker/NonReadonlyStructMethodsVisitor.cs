using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class NonReadonlyStructMethodsVisitor : CSharpSyntaxWalker
    {
        public List<MethodDeclarationSyntax> NonReadonlyMethods = new List<MethodDeclarationSyntax>();
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Modifiers.Any(k => k.ValueText == "readonly" || k.ValueText == "static"))
                return;

            NonReadonlyMethods.Add(node);
        }
    }
}