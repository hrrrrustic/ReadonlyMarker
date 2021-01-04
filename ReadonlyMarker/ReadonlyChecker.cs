using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class ReadonlyChecker
    {
        private CSharpCompilation _fileCompilation;
        private SemanticModel _semantic;

        private readonly string _filePath;

        public ReadonlyChecker(String filePath)
        {
            _filePath = filePath;
        }

        public int MethodCount { get; private set; }
        public void CheckFile()
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(_filePath));
            _fileCompilation = CSharpCompilation.Create(null).AddSyntaxTrees(tree);
            _semantic = _fileCompilation.GetSemanticModel(tree);
            var structs = GetNonReadOnlyStructs(tree.GetRoot());
            foreach (var @struct in structs)
                CheckStruct(@struct);
        }

        private void CheckStruct(StructDeclarationSyntax currentStruct)
        {
            var methods = GetNonReadOnlyMethods(currentStruct);
            foreach (MethodDeclarationSyntax method in methods)
            {
                if (CheckMethod(method))
                {
                    Console.WriteLine(_filePath);
                    Console.WriteLine(currentStruct.Identifier.ToFullString());
                    Console.WriteLine(method);
                    Console.WriteLine("==========================");
                    Console.ReadKey();
                }

            }
        }

        private bool CheckMethod(MethodDeclarationSyntax method)
        {
            var filter = new MethodFilterVisitor(_semantic);
            filter.Visit(method);
            if (filter.ValidMethod)
            {
                MethodCount++;
                return true;
            }

            return false;
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