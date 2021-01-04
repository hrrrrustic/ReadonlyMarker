using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var path = "D:\\Vector4.cs";
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path));
            var structVisitor = new StructVisitor();
            structVisitor.Visit(tree.GetRoot());

            var methodsVisitor = new NonReadonlyStructMethodsVisitor();
            methodsVisitor.Visit(structVisitor.NonReadoblyStructs[0]);
        }
    }
}
