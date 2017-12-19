using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATOOS.VSExtension.CodeAnalyzer
{
    public class BasicMetricsProvider
    {
        private List<ClassDeclarationSyntax> _projectClasses;

        public List<ClassDeclarationSyntax> GetProjectClasses(Compilation sampleToAnalyzeCompilation)
        {
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
