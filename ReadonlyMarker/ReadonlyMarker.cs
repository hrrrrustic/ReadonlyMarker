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
        private readonly List<(SyntaxNode old, SyntaxNode update)> _changedMethods = new();
        private readonly string _filePath;

        private readonly ReadonlyChecker _checker;

        public ReadonlyMarker(String filePath)
        {
            _filePath = filePath;
            _tree = CSharpSyntaxTree.ParseText(File.ReadAllText(_filePath));
            CSharpCompilation fileCompilation = CSharpCompilation.Create(null).AddSyntaxTrees(_tree);
            _semantic = fileCompilation.GetSemanticModel(_tree);
            _checker = new ReadonlyChecker(_semantic);
        }

        public int MethodCount => _changedMethods.Count;

        public void MarkFile()
        {
            var root = _tree.GetRoot();
            var structs = GetNonReadOnlyStructs(root);

            foreach (var @struct in structs)
                CheckStruct(@struct);

            if (_changedMethods.Count == 0)
                return;

            var newRoot = root
                .ReplaceNodes(_changedMethods
                    .Select(k => k.old), 
                    (syntax, declarationSyntax) => _changedMethods
                        .First(k => k.old == syntax)
                        .update
                        .WithLeadingTrivia(syntax.GetLeadingTrivia()));
            File.WriteAllText(_filePath, newRoot.ToFullString());
            Console.WriteLine();
        }

        private void CheckStruct(StructDeclarationSyntax currentStruct)
        {
            var methods = GetNonReadOnlyMethods(currentStruct);
            foreach (MethodDeclarationSyntax method in methods)
                MarkMethod(method);

            var getters = GetNonReadOnlyGetters(currentStruct);
            foreach (var getter in getters)
                MarkGetter(getter);
        }

        private void MarkMethod(MethodDeclarationSyntax method)
        {
            if (_checker.CheckMethod(method))
            {
                var newMethod = method.AsReadOnlyMethod();
                _changedMethods.Add((method, newMethod));
            }
        }
        
        private void MarkGetter(AccessorDeclarationSyntax getter)
        {
            if (_checker.CheckGetter(getter))
            {
                var newGetter = getter.AsReadOnlyGetter();
                _changedMethods.Add((getter, newGetter));
            }
        }

        private List<StructDeclarationSyntax> GetNonReadOnlyStructs(SyntaxNode node)
        {
            var structVisitor = new StructVisitor();
            structVisitor.Visit(node);
            return structVisitor.NonReadonlyStructs;
        }

        private List<MethodDeclarationSyntax> GetNonReadOnlyMethods(StructDeclarationSyntax node)    
        {
            var methodsVisitor = new NonReadonlyStructMethodsVisitor();
            methodsVisitor.Visit(node);
            return methodsVisitor.NonReadonlyMethods;
        }

        private List<AccessorDeclarationSyntax> GetNonReadOnlyGetters(StructDeclarationSyntax node)
        {
            var methodsVisitor = new NonReadonlyStructMethodsVisitor();
            methodsVisitor.Visit(node);
            return methodsVisitor.NonReadonlyGetters;
        }
    }
}