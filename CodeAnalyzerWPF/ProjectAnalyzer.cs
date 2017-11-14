using ATOOS.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace ATOOS
{
    public class ProjectAnalyzer
    {
        private List<Class> _discoveredClasses;
        private string _pathToSolution;
        private string _projectName;

        public ProjectAnalyzer(string pathToSolution, string projectName, List<Class> discoveredClasses)
        {
            _pathToSolution = pathToSolution;
            _projectName = projectName;
            _discoveredClasses = discoveredClasses;
        }

        public List<Class> AnalyzeProject()
        {
            var basicMetricsProvider = new BasicMetricsProvider();
            var sampleToAnalyzeCompilation = GetCompiledAssembly();
            var projectClasses = basicMetricsProvider.GetProjectClasses(sampleToAnalyzeCompilation);

            foreach (ClassDeclarationSyntax c in projectClasses)
            {
                var newClass = new Class() { Name = c.Identifier.ToString() };
                IEnumerable<MethodDeclarationSyntax> methods = c.Members.OfType<MethodDeclarationSyntax>().ToList();
                IEnumerable<FieldDeclarationSyntax> attributes = c.Members.OfType<FieldDeclarationSyntax>().ToList();
                IEnumerable<PropertyDeclarationSyntax> properties = c.Members.OfType<PropertyDeclarationSyntax>().ToList();
                ConstructorDeclarationSyntax constructor = c.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();

                if (constructor != null)
                {
                    newClass.Constructor = new Constructor();
                    foreach (var cp in constructor.ParameterList.Parameters)
                    {
                        newClass.Constructor.Parameters.Add(new MethodParameter()
                        {
                            Name = cp.Identifier.ToString(),
                            Type = cp.Type.ToString()
                        });
                    }
                }

                foreach (var m in methods)
                {
                    var methodParameters = new List<MethodParameter>();
                    foreach (var mp in m.ParameterList.Parameters)
                    {
                        methodParameters.Add(new MethodParameter()
                        {
                            Name = mp.Identifier.ToString(),
                            Type = mp.Type.ToString()
                        });
                    }
                    newClass.Methods.Add(new Method()
                    {
                        Accessor = m.Modifiers.First().ToString(),
                        ReturnType = m.ReturnType.ToString(),
                        Name = m.Identifier.ToString(),
                        Parameters = methodParameters
                    });
                }
                foreach (var a in attributes)
                {
                    var attDeclaration = a.Declaration.ToString();
                    newClass.Attributes.Add(new Atribute()
                    {
                        Accessor = a.Modifiers.First().ToString(),
                        Name = a.Declaration.ToString().Split(' ')[1],
                        Type = a.Declaration.ToString().Split(' ')[0]
                    });
                }
                foreach (var p in properties)
                {
                    newClass.Attributes.Add(new Atribute()
                    {
                        Accessor = p.Modifiers.First().ToString(),
                        Name = p.Identifier.ToString(),
                        Type = p.Type.ToString()
                    });
                }

                _discoveredClasses.Add(newClass);
            }

            return _discoveredClasses;
        }

        private Compilation GetCompiledAssembly()
        {
            Solution solutionToAnalyze = GetSolution(_pathToSolution);

            // get the project we want to analyze out
            Project sampleProjectToAnalyze = solutionToAnalyze.Projects.Where((proj) => proj.Name == _projectName).First();

            // get the project's compiled assembly
            Compilation sampleToAnalyzeCompilation = sampleProjectToAnalyze.GetCompilationAsync().Result;

            return sampleToAnalyzeCompilation;
        }

        private Solution GetSolution(string pathToSolution)
        {
            // start Roslyn workspace
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();

            // open solution we want to analyze
            Solution solutionToAnalyze = workspace.OpenSolutionAsync(pathToSolution).Result;

            return solutionToAnalyze;
        }

        #region Old functionality
        //private IEnumerable<MethodDeclarationSyntax> GetClassMethods(ClassDeclarationSyntax projClass)
        //{
        //    IEnumerable<MethodDeclarationSyntax> methods = projClass.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();

        //    return methods;
        //}

        //private IEnumerable<FieldDeclarationSyntax> GetClassAttributes(ClassDeclarationSyntax projClass)
        //{
        //    IEnumerable<FieldDeclarationSyntax> attributes = projClass.DescendantNodes().OfType<FieldDeclarationSyntax>().ToList();

        //    return attributes;
        //}

        //private void InstectProjectClasses(List<ClassDeclarationSyntax> classes)
        //{
        //    foreach (ClassDeclarationSyntax projClass in classes)
        //    {
        //        // Display methods for each class
        //        _resultBox.AppendText(Environment.NewLine);
        //        DisplayClassMethods(projClass);

        //        // Display attributes for each class
        //        DisplayClassAttributes(projClass);
        //    }
        //}

        //private void DisplayClassesNames(List<ClassDeclarationSyntax> classes)
        //{
        //    // Display the name for all classes in project (custom classes)
        //    _resultBox.AppendText(Environment.NewLine);
        //    _resultBox.AppendText(string.Format("Project {0} has {1} classes: \r\n", _projectName, classes.Count()));

        //    foreach (ClassDeclarationSyntax projClass in classes)
        //    {
        //        _resultBox.AppendText(projClass.Identifier.ToString() + "\r");
        //    }
        //}


        //private IEnumerable<MethodDeclarationSyntax> GetClassMethods(ClassDeclarationSyntax projClass)
        //{
        //    IEnumerable<MethodDeclarationSyntax> methods = projClass.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
        //    //_resultBox.AppendText(String.Format("Class {0} has {1} methods \r", projClass.Identifier, methods.Count()));

        //    //foreach (var method in methods)
        //    //{
        //    //    var programClass = GetProgramClass(projClass);
        //    //    var methodSymbol = programClass.GetMembers(method.Identifier.Value.ToString()).First();
        //    //    var methodReferences = FindAllReferences(methodSymbol, GetSolution(_pathToSolution));
        //    //    _resultBox.AppendText("-- " + method.Modifiers.First() + " " + method.ReturnType + " " + method.Identifier + ";  " + "Callers: " + methodReferences.Count() + '\r');
        //    //}

        //    return methods;
        //}

        //private IEnumerable<FieldDeclarationSyntax> GetClassAttributes(ClassDeclarationSyntax projClass)
        //{
        //    IEnumerable<FieldDeclarationSyntax> attributes = projClass.DescendantNodes().OfType<FieldDeclarationSyntax>().ToList();
        //    //_resultBox.AppendText(String.Format("\r... and {0} attributes \r", attributes.Count()));

        //    //foreach (var attribute in attributes)
        //    //{
        //    //    var programClass = GetProgramClass(projClass);
        //    //    var attName = attribute.Declaration.ToString().Split(' ')[1];
        //    //    var attributeSymbol = programClass.GetMembers(attName).First();
        //    //    var attributeReferences = FindAllReferences(attributeSymbol, GetSolution(_pathToSolution));
        //    //    _resultBox.AppendText("-- " + attribute.Modifiers.First() + " " + attribute.Declaration + ";  " + "References: " + attributeReferences.Count() + '\r');
        //    //}

        //    return attributes;
        //}

        //private INamedTypeSymbol GetProgramClass(ClassDeclarationSyntax projClass)
        //{
        //    NamespaceDeclarationSyntax namespaceDeclarationSyntax = null;
        //    SyntaxNodeHelper.TryGetParentSyntax(projClass, out namespaceDeclarationSyntax);
        //    var namespaceName = namespaceDeclarationSyntax.Name.ToString();

        //    var compilation = GetCompiledAssembly();
        //    var programClass = compilation.GetTypeByMetadataName(namespaceName + "." + projClass.Identifier.Value);

        //    return programClass;
        //}

        //public IEnumerable<ReferencedSymbol> FindAllReferences(ISymbol symbol, Solution solution)
        //{
        //    var references = SymbolFinder.FindReferencesAsync(symbol, solution).Result.ToList();

        //    return references;
        //}
        #endregion Old 
    }
}
