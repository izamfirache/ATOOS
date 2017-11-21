using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace CodeAnalyzer
{
    public class ClassVisitor : CSharpSyntaxRewriter
    {
        public ClassVisitor()
        {
            Classes = new List<ClassDeclarationSyntax>();
        }

        public List<ClassDeclarationSyntax> Classes { get; set; }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            Classes.Add(node); // save your visited classes from project
            return node;
        }
    }
}
