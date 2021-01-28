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
                        .WithLeadingTrivia(syntax.GetLeadingTrivia()));
            var value = newRoot.ToFullString();
            if(_changedMethods.Any(k => k.old is AccessorDeclarationSyntax))
              value = Regex.Replace(value, "readonly\\s+get", "readonly get", RegexOptions.Compiled);

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
            foreach (MethodDeclarationSyntax method in GetNonReadOnlyMethods(currentStruct))
             MarkMethod(method);

            foreach (var getter in GetNonReadOnlyGetters(currentStruct))
            {
                if(getter.HasSetter())
                    MarkGetter(getter);
                else
                    MarkProperty(getter.Ancestors().OfType<PropertyDeclarationSyntax>().First());
            }

            foreach (var arrowedProperties in GetArrowedProperties(currentStruct))
                MarkProperty(arrowedProperties);
        }

        private void MarkMethod(MethodDeclarationSyntax method)
        {
            if (_checker.CheckMethod(method))
                _changedMethods.Add((method, method.AsReadOnlyMethod()));
        }
        
        private void MarkGetter(AccessorDeclarationSyntax getter)
        {
            if (_checker.CheckGetter(getter))
                _changedMethods.Add((getter, getter.AsReadOnlyGetter()));
        }

        private void MarkProperty(PropertyDeclarationSyntax property)
        {
            if (property is null)
                return;

            if(_checker.CheckArrowedProperty(property))
                _changedMethods.Add((property, property.AsReadOnlyProperty()));
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

        private List<PropertyDeclarationSyntax> GetArrowedProperties(StructDeclarationSyntax node)
        {
            var methodsVisitor = new NonReadonlyStructMethodsVisitor();
            methodsVisitor.Visit(node);
            return methodsVisitor.ArrowedProperties;
        }
    }
}