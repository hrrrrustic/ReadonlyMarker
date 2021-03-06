using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class ReadonlyMarker
    {
        private readonly SyntaxTree _tree;
        private readonly List<(SyntaxNode old, SyntaxNode update)> _changedMethods = new();
        private readonly string _filePath;
        private readonly ReadonlyChecker _checker;
        public ReadonlyMarker(String filePath)
        {
            _filePath = filePath;
            _tree = CSharpSyntaxTree.ParseText(File.ReadAllText(_filePath));
            CSharpCompilation fileCompilation = CSharpCompilation.Create(null).AddSyntaxTrees(_tree);
            SemanticModel semantic = fileCompilation.GetSemanticModel(_tree);
            _checker = new ReadonlyChecker(semantic);
        }

        public int MethodCount => _changedMethods.Count;

        public void MarkFile()
        {
            var root = _tree.GetRoot();
            foreach (var @struct in GetNonReadOnlyStructs(root))
                CheckStruct(@struct);

            if (_changedMethods.Count == 0)
                return;

            var newRoot = root
                .ReplaceNodes(_changedMethods
                    .Select(k => k.old), 
                    (syntax, _) => _changedMethods
                        .First(k => k.old == syntax)
                        .update
                        .WithLeadingTrivia(syntax.GetLeadingTrivia())
                        .WithTrailingTrivia(syntax.GetTrailingTrivia()));

            var value = newRoot.ToFullString();
            if (_changedMethods.Any(k => k.old is AccessorDeclarationSyntax))
                value = Regex.Replace(value, "readonly\\s+get", "readonly get");

            if (_changedMethods.Any(k => k.old is PropertyDeclarationSyntax {ExplicitInterfaceSpecifier: { }} or MethodDeclarationSyntax { ExplicitInterfaceSpecifier: { } }))
                value = ReplaceExtraLine(value);

            static string ReplaceExtraLine(string value)
            {
                string pattern = $"readonly {Environment.NewLine}(.+{Environment.NewLine})";
                var matches = Regex.Matches(value, pattern);
                foreach (Match match in matches)
                    value = value.Replace(match.Value, "readonly " + match.Groups[1].Value.TrimStart());

                return value;
            }

            File.WriteAllText(_filePath, value);
            Console.WriteLine($"Ignore {_filePath} ?");
            var res = Console.ReadKey().KeyChar;
            Console.WriteLine();
            if (res == 'y')
            {
                File.AppendAllLines("IgnoreStructs.txt", new List<String> {_filePath});
                File.WriteAllText(_filePath, root.ToFullString());
            }
        }

        private void CheckStruct(StructDeclarationSyntax currentStruct)
        {
            var nodes = new NonReadonlyStructMethodsVisitor().GetPotentialNodes(currentStruct);

            foreach (MethodDeclarationSyntax method in nodes.NonReadonlyMethods)
                MarkMethod(method);

            foreach (var getter in nodes.NonReadonlyGetters)
                MarkGetter(getter);

            foreach (var arrowedProperties in nodes.ArrowedProperties)
                MarkProperty(arrowedProperties);

            foreach (var indexer in nodes.Indexers)
                MarkIndexer(indexer);
        }

        private void MarkIndexer(IndexerDeclarationSyntax indexer)
        {
            if(_checker.CanBeMarkedAsReadOnly(indexer))
                _changedMethods.Add((indexer, indexer.AsReadonlyIndexer()));
        }

        private void MarkMethod(MethodDeclarationSyntax method)
        {
            if (_checker.CanBeMarkedAsReadOnly(method))
                _changedMethods.Add((method, method.AsReadOnlyMethod()));
        }
        
        private void MarkGetter(AccessorDeclarationSyntax getter)
        {
            if (_checker.CanBeMarkedAsReadOnly(getter))
                _changedMethods.Add((getter, getter.AsReadOnlyGetter()));
        }

        private void MarkProperty(PropertyDeclarationSyntax property)
        {
            if(_checker.CanBeMarkedAsReadOnly(property))
                _changedMethods.Add((property, property.AsReadOnlyProperty()));
        }

        private List<StructDeclarationSyntax> GetNonReadOnlyStructs(SyntaxNode node)
        {
            var structVisitor = new StructVisitor();
            structVisitor.Visit(node);
            return structVisitor.NonReadonlyStructs;
        }
    }
}