﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using ATOOS.Core.Models;

namespace CodeAnalyzer
{
    public class SolutionAnalyzer
    {
        private string _pathToSolution;
        private string _projectName;

        public SolutionAnalyzer(string pathToSolution, string projectName)
        {
            _pathToSolution = pathToSolution;
            _projectName = projectName;
        }

        public AnalyzedSolution AnalyzeSolution()
        {
            var analyzedSolution = new AnalyzedSolution();
            var basicMetricsProvider = new BasicMetricsProvider();
            var workspace = CreateWorkspace();
            var solutionToAnalyze = GetSolutionToAnalyze(workspace, _pathToSolution);
            foreach (Project proj in solutionToAnalyze.Projects)
            {
                var analyzedProject = new AnalyzedProject();
                var project = GetProjectToAnalyze(solutionToAnalyze, proj.Name);
                var projectCompiledAssembly = GetProjectCompiledAssembly(project);
                analyzedProject.OutputFilePath = project.OutputFilePath;
                analyzedProject.Name = project.Name;
                var projectClasses = basicMetricsProvider.GetProjectClasses(projectCompiledAssembly);

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
                    analyzedProject.Classes.Add(newClass);
                }
                analyzedSolution.Projects.Add(analyzedProject);
            }

            return analyzedSolution;
        }

        private MSBuildWorkspace CreateWorkspace()
        {
            // start Roslyn workspace
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            return workspace;
        }

        private Solution GetSolutionToAnalyze(MSBuildWorkspace workspace, string pathToSolution)
        {
            // open solution we want to analyze
            Solution solutionToAnalyze = workspace.OpenSolutionAsync(pathToSolution).Result;
            return solutionToAnalyze;
        }

        private Project GetProjectToAnalyze(Solution solution, string projectName)
        {
            // get the project we want to analyze out
            Project projectToAnalyze = solution.Projects.Where((proj) => proj.Name == _projectName).First();
            return projectToAnalyze;
        }

        private Compilation GetProjectCompiledAssembly(Project project)
        {
            // get the project's compiled assembly
            Compilation projectCompiledAssembly = project.GetCompilationAsync().Result;
            return projectCompiledAssembly;
        }
    }
}