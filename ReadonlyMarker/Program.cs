using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var path = "D:\\Development\\VisualStudio\\OpenSource\\runtime\\src\\libraries";
            var counter = 0;
            foreach (String file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains("\\ref\\") || file.Contains("\\tests\\") || file.Contains("asn.xml", StringComparison.OrdinalIgnoreCase) || file.Contains("asn1", StringComparison.OrdinalIgnoreCase))
                    continue;

                var checker = new ReadonlyMarker(file);
                checker.MarkFile();
                counter += checker.MethodCount;
            }

            Console.WriteLine(counter);
        }
    }
}
