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
        private readonly SemanticModel _semantic;

        private readonly SyntaxTree _tree;
        private readonly List<(MethodDeclarationSyntax old, MethodDeclarationSyntax update)> _changedMethods = new List<(MethodDeclarationSyntax old, MethodDeclarationSyntax update)>();
        private readonly string _filePath;

        public ReadonlyMarker(String filePath)
        {
            _filePath = filePath;
            _tree = CSharpSyntaxTree.ParseText(File.ReadAllText(_filePath));
            CSharpCompilation fileCompilation = CSharpCompilation.Create(null).AddSyntaxTrees(_tree);
            _semantic = fileCompilation.GetSemanticModel(_tree);
        }

        public int MethodCount { get; private set; }

        public void MarkFile()
        {
            var root = _tree.GetRoot();
            var structs = GetNonReadOnlyStructs(root);

            foreach (var @struct in structs)
                CheckStruct(@struct);

            if (_changedMethods.Count == 0)
                return;

            Console.WriteLine(_filePath);
            root
                .DescendantNodes()
                .OfType<StructDeclarationSyntax>()
                .ToList()
                .Select(k => k
                    .RemoveNodes(k
                        .DescendantNodes()
                        .OfType<MethodDeclarationSyntax>()
                        .Where(e => e.Modifiers.Any(x => x.ValueText == "static"))
                        .Select(e => (SyntaxNode) e)
                        .Union(k
                            .DescendantNodes()
                            .OfType<PropertyDeclarationSyntax>()
                            .Where(e => e.Modifiers.Any(x => x.ValueText == "static")))
                        .Select(e => (SyntaxNode) e), SyntaxRemoveOptions.KeepNoTrivia))
                .ToList()
                .ForEach(k => Console.WriteLine(k.ToFullString()));

            var newRoot = root
                .ReplaceNodes(_changedMethods
                    .Select(k => k.old), 
                    (syntax, declarationSyntax) => _changedMethods
                        .First(k => k.old == syntax)
                        .update
                        .WithLeadingTrivia(syntax.GetLeadingTrivia()));
            File.WriteAllText(_filePath, newRoot.ToFullString());
        }

        private void CheckStruct(StructDeclarationSyntax currentStruct)
        {
            var methods = GetNonReadOnlyMethods(currentStruct);
            foreach (MethodDeclarationSyntax method in methods)
                CheckMethod(method);
        }

        private void CheckMethod(MethodDeclarationSyntax method)
        {
            var checker = new ReadonlyChecker(_semantic);
            var result = checker.CheckFullMethod(method);

            if (result)
            {
                var newMethod = method.AsReadOnlyMethod();
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