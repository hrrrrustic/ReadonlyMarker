using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class ReadonlyMarker
    {
        private CSharpCompilation _fileCompilation;
        private SemanticModel _semantic;

        private readonly List<(MethodDeclarationSyntax old, MethodDeclarationSyntax update)> _changedMethods = new List<(MethodDeclarationSyntax old, MethodDeclarationSyntax update)>();
        private readonly string _filePath;

        public ReadonlyMarker(String filePath)
        {
            _filePath = filePath;
        }

        public int MethodCount { get; private set; }
        public void CheckFile()
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(_filePath));
            _fileCompilation = CSharpCompilation.Create(null).AddSyntaxTrees(tree);
            _semantic = _fileCompilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var structs = GetNonReadOnlyStructs(root);
            foreach (var @struct in structs)
                CheckStruct(@struct);

            if(_changedMethods.Count == 0)
                return;

            SyntaxNode newRoot = root
                .ReplaceNodes(_changedMethods.Select(k => k.old),
                    (syntax, declarationSyntax) => _changedMethods.First(k => k.old == syntax).update).NormalizeWhitespace();
            Console.WriteLine(newRoot.ToFullString());
        }

        private void CheckStruct(StructDeclarationSyntax currentStruct)
        {
            var methods = GetNonReadOnlyMethods(currentStruct);
            foreach (MethodDeclarationSyntax method in methods)
                CheckMethod(method);
        }

        private void CheckMethod(MethodDeclarationSyntax method)
        {
            var filter = new MethodFilterVisitor(_semantic);
            filter.Visit(method);
            if (filter.ValidMethod)
            {
                var newMethod = method
                    .WithModifiers(SyntaxFactory
                        .TokenList(method
                            .Modifiers
                            .Append(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))))
                    .NormalizeWhitespace();

                _changedMethods.Add((method, newMethod));
                MethodCount++;
            }
        }

        private List<StructDeclarationSyntax> GetNonReadOnlyStructs(SyntaxNode node)
        {
            var structVisitor = new StructVisitor();
            structVisitor.Visit(node);
            return structVisitor.NonReadonlyStructs;
        }

        private List<MethodDeclarationSyntax> GetNonReadOnlyMethods(SyntaxNode node)
        {
            var methodsVisitor = new NonReadonlyStructMethodsVisitor();
            methodsVisitor.Visit(node);
            return methodsVisitor.NonReadonlyMethods;
        }
    }
}