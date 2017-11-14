using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace SolutionAnalyzer
{
    public class BasicMetricsProvider
    {
        private List<ClassDeclarationSyntax> _projectClasses;

        public List<ClassDeclarationSyntax> GetProjectClasses(Compilation sampleToAnalyzeCompilation)
        {
            // absolute all classes from project -- all system class from the compiled assembly
            //foreach (var @class in sampleToAnalyzeCompilation.GlobalNamespace.GetNamespaceMembers().SelectMany(x => x.GetMembers()))
            //{
            //    Console.WriteLine(@class.Name);
            //    Console.WriteLine(@class.ContainingNamespace.Name);
            //}

            var classVisitor = new ClassVisitor();
            foreach (var syntaxTree in sampleToAnalyzeCompilation.SyntaxTrees)
            {
                classVisitor.Visit(syntaxTree.GetRoot());
            }

            var classes = classVisitor.Classes; // as ClassDeclarationSyntax if needed
            _projectClasses = classVisitor.Classes;

            return classVisitor.Classes;
        }
    }
}
