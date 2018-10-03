using ATOOS.VSExtension.ATOOS.Core;
using ATOOS.VSExtension.InputGenerators;
using ATOOS.VSExtension.ObjectFactory;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Moq;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ATOOS.VSExtension.TestGenerator
{
    public class UnitTestGenerator
    {
        public UnitTestGenerator(string generatedTestClassesDirectory, string packagesFolder, 
            string selectedProjectName)
        {
            _generatedTestClassesDirectory = generatedTestClassesDirectory;
            _packagesFolder = packagesFolder;
            this.selectedProjectName = selectedProjectName;
        }

        public List<string> GenerateUnitTestsForClass(string solutionPath, 
            string generatedUnitTestProject, List<string> projClasses)
        {
            List<string> generatedTestClasses = new List<string>();

            // analyze solution, discover basic information about each project
            var solutionAnalyzer = new SolutionAnalyzer(solutionPath);
            var analyedSolution = solutionAnalyzer.AnalyzeSolution();
            CompilerHelper compileHelper = new CompilerHelper(_generatedTestClassesDirectory);

            foreach (AnalyzedProject proj in analyedSolution.Projects)
            {
                if (proj.Name != generatedUnitTestProject)
                {
                    var assembly = Assembly.LoadFile(proj.OutputFilePath);
                    _assemblyExportedTypes = assembly.GetExportedTypes();
                    inputParamGenerator = new InputParamGenerator(_assemblyExportedTypes);

                    foreach (Type type in _assemblyExportedTypes)
                    {
                        if (projClasses.Any(pc => pc == type.Name))
                        {
                            if (!type.IsInterface) // don't want to write unit tests for interfaces
                            {
                                // create a class
                                CodeTypeDeclaration targetClass = new CodeTypeDeclaration
                                    (string.Format("{0}UnitTestsClass", type.Name))
                                {
                                    IsClass = true,
                                    TypeAttributes = TypeAttributes.Public
                                };

                                // create a code unit (the in-memory representation of a class)
                                CodeCompileUnit codeUnit = CreateCodeCompileUnit(proj.Name, type.Name, targetClass);
                                string classSourceName = string.Format("{0}UnitTestsClass.cs", type.Name);

                                // generate the constructor for the unit test class in which all the 
                                // external dependencies/calls will be mocked
                                var cut_ConstructorGenerator = new CUT_AddConstructor(inputParamGenerator, selectedProjectName);
                                cut_ConstructorGenerator.AddTestClassConstructor(classSourceName, targetClass, type, analyedSolution);

                                // generate a unit test for each method
                                // the method will be called and a NotNull assertion will be added
                                var methods = type.GetMethods(BindingFlags.Public
                                    | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                                foreach (MethodInfo m in methods)
                                {
                                    // randomly generate method parameters
                                    var methodParameters = m.GetParameters();
                                    CodeExpression[] parameters = new CodeExpression[methodParameters.Length];
                                    int j = 0;
                                    foreach (ParameterInfo p in methodParameters)
                                    {
                                        // TODO: Rethink this !!!
                                        if (p.ParameterType.Name == "String" || p.ParameterType.Name == "Int32")
                                        {
                                            parameters[j] = new CodePrimitiveExpression(
                                                inputParamGenerator.ResolveParameter(p.ParameterType.Name));
                                        }
                                        else
                                        {
                                            CodeObjectCreateExpression createObjectExpression =
                                                inputParamGenerator.CreateCustomType(p.ParameterType.Name);
                                            parameters[j] = createObjectExpression;
                                        }
                                        j++;
                                    }

                                    var cut_addTestMethod = new CUT_AddTestMethod(inputParamGenerator);

                                    // Assert.NotNull(result);
                                    // Assert.NotThrow(() => targetObj.SomePublicMethod())
                                    cut_addTestMethod.AddTestMethod_ShouldNotThrowException_ResultShouldNotBeNull(targetClass, m.Name, parameters, type);

                                    // Assert.AreEqual(result, new object { });
                                    //AddTestTheResultShouldbeTheExpectedOne(targetClass, m.Name, parameters, type);
                                }

                                // generate the c# code based on the created code unit
                                string generatedTestClassPath = compileHelper.GenerateCSharpCode(codeUnit, classSourceName);

                                // compile the above generated code into a DLL/EXE
                                bool isGeneratedClassCompiled = compileHelper.CompileAsDLL(classSourceName, new List<string>()
                            {
                                string.Format("{0}\\{1}", _packagesFolder, "NUnit.3.10.1\\lib\\net45\\nunit.framework.dll"),
                                string.Format("{0}\\{1}", _packagesFolder, "Moq.4.10.0\\lib\\net45\\Moq.dll"),
                                proj.OutputFilePath,
                                typeof(System.Linq.Enumerable).Assembly.Location
                            });

                                if (!string.IsNullOrEmpty(generatedTestClassPath) && isGeneratedClassCompiled)
                                {
                                    generatedTestClasses.Add(generatedTestClassPath);
                                }
                            }
                        }
                    }
                }
            }

            return generatedTestClasses;
        }

        #region Private functionality

        private string _generatedTestClassesDirectory;
        private string _packagesFolder;
        private Type[] _assemblyExportedTypes = new Type[100];
        private string selectedProjectName;
        InputParamGenerator inputParamGenerator;

        private CodeCompileUnit CreateCodeCompileUnit(string projectName, string typeName, CodeTypeDeclaration targetClass)
        {
            CodeCompileUnit codeUnit = new CodeCompileUnit();

            // create a namespace
            CodeNamespace codeUnitNamespace = new CodeNamespace(string.Format("{0}.UnitTestsNamespace", typeName));
            codeUnitNamespace.Imports.Add(new CodeNamespaceImport("System"));
            codeUnitNamespace.Imports.Add(new CodeNamespaceImport("NUnit.Framework"));
            codeUnitNamespace.Imports.Add(new CodeNamespaceImport("Moq"));
            codeUnitNamespace.Imports.Add(new CodeNamespaceImport(projectName));
            
            codeUnitNamespace.Types.Add(targetClass);
            codeUnit.Namespaces.Add(codeUnitNamespace);

            return codeUnit;
        }

        #endregion Private funtionality
    }
}