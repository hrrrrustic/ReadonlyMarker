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
        public static int Counter = 0;
        public static void Main(string[] args)
        {
            var path = "D:\\Development\\TestSanbox";
            foreach (String file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            {
                CheckFile(file);
            }

            Console.WriteLine(Counter);
        }

        private static void CheckFile(string filePath)
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));
            var structVisitor = new StructVisitor();
            structVisitor.Visit(tree.GetRoot());
            if(structVisitor.NonReadonlyStructs.Count == 0)
                return;

            var methodsVisitor = new NonReadonlyStructMethodsVisitor();
            methodsVisitor.Visit(structVisitor.NonReadonlyStructs[0]);
            if(methodsVisitor.NonReadonlyMethods.Count == 0)
                return;

            Counter+= methodsVisitor.NonReadonlyMethods.Count;
            //Console.WriteLine(filePath);
            //Console.WriteLine(String.Join(Environment.NewLine, methodsVisitor.NonReadonlyMethods));
        }
    }
}
