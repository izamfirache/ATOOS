using ATOOS.VSExtension.ATOOS.Core;
using ATOOS.VSExtension.ObjectFactory;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ATOOS.VSExtension.TestGenerator
{
    public class UnitTestGenerator
    {
        private string _generatedTestClassesDirectory;
        private Factory _objectFactory;
        private string _packagesFolder;
        public UnitTestGenerator(string generatedTestClassesDirectory, Factory objectFactory, string packagesFolder)
        {
            _generatedTestClassesDirectory = generatedTestClassesDirectory;
            _objectFactory = objectFactory;
            _packagesFolder = packagesFolder;
        }

        private Type[] _assemblyExportedTypes = new Type[100];

        public List<string> GenerateUnitTestsForClass(string solutionPath)
        {
            List<string> generatedTestClasses = new List<string>();

            // analyze solution, discover basic information about each project
            var solutionAnalyzer = new SolutionAnalyzer(solutionPath);
            var analyedSolution = solutionAnalyzer.AnalyzeSolution();

            // create a compile helper
            CompilerHelper compileHelper = new CompilerHelper(_generatedTestClassesDirectory);

            foreach (AnalyzedProject proj in analyedSolution.Projects)
            {
                var assembly = Assembly.LoadFile(proj.OutputFilePath); // WHAT IF THE PROJECT IS NOT COMPILED ??
                _assemblyExportedTypes = assembly.GetExportedTypes();

                foreach (Type type in _assemblyExportedTypes)
                {
                    // ****************************************************************************************************
                    // extract this into a separate method
                    // create a code unit
                    CodeCompileUnit codeUnit = new CodeCompileUnit();

                    // create a namespace
                    CodeNamespace codeUnitNamespace = new CodeNamespace(string.Format("{0}.UnitTestsNamespace", type.Name));
                    codeUnitNamespace.Imports.Add(new CodeNamespaceImport("System"));
                    codeUnitNamespace.Imports.Add(new CodeNamespaceImport("NUnit.Framework"));
                    codeUnitNamespace.Imports.Add(new CodeNamespaceImport(proj.Name));

                    // create a class
                    CodeTypeDeclaration targetClass = new CodeTypeDeclaration(string.Format("{0}UnitTestsClass", type.Name))
                    {
                        IsClass = true,
                        TypeAttributes = TypeAttributes.Public
                    };
                    codeUnitNamespace.Types.Add(targetClass);
                    codeUnit.Namespaces.Add(codeUnitNamespace);

                    string classSourceName = string.Format("{0}UnitTestsClass.cs", type.Name);
                    // ****************************************************************************************************

                    // GENERATE A UNIT TEST FOR EACH METHOD
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (MethodInfo m in methods)
                    {
                        // generate method parameters
                        var methodParameters = m.GetParameters();
                        //var parameters = new List<object>();
                        CodeExpression[] parameters = new CodeExpression[methodParameters.Length];
                        int j = 0;
                        foreach (ParameterInfo p in methodParameters)
                        {
                            if (p.ParameterType.Name == "String" || p.ParameterType.Name == "Int32")
                            {
                                parameters[j] = new CodePrimitiveExpression(ResolveParameter(p.ParameterType.Name));
                            }
                            else
                            {
                                CodeObjectCreateExpression createObjectExpression = CreateCustomType(p.ParameterType.Name);
                                parameters[j] = createObjectExpression;
                            }
                            //var instance = ResolveParameter(p.ParameterType.Name);
                            //parameters.Add(instance);
                            j++;
                        }

                        _objectFactory.Instances.TryGetValue(type.Name, out object objectInstance);
                        AddUnitTestToTestClass(targetClass, m.Name, parameters, type, objectInstance);
                    }

                    // Generate the c# code
                    string generatedTestClassPath = compileHelper.GenerateCSharpCode(codeUnit, classSourceName);

                    // Compile the above generated code into a DLL
                    bool isGeneratedClassCompiled = compileHelper.CompileAsDLL(classSourceName, new List<string>()
                    {
                        string.Format("{0}\\{1}", _packagesFolder, "NUnit.3.9.0\\lib\\net45\\nunit.framework.dll"),
                        proj.OutputFilePath
                    });

                    if (!string.IsNullOrEmpty(generatedTestClassPath) && isGeneratedClassCompiled)
                    {
                        generatedTestClasses.Add(generatedTestClassPath);
                    }
                }
            }

            return generatedTestClasses;
        }

        private void AddUnitTestToTestClass(CodeTypeDeclaration targetClass, string methodName,
            CodeExpression[] methodParameters, Type targetType, object objectInstance)
        {
            // unit test name/structure
            CodeMemberMethod testMethod = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Public,
                Name = string.Format("{0}_{1}_{2}", methodName, "RandomInput", "NotNullResult"),
                ReturnType = new CodeTypeReference(typeof(void)),
                CustomAttributes =
                {
                    new CodeAttributeDeclaration
                    {
                        Name = "TestCase"
                    }
                }
            };

            // build unit test code block
            // 1. act part, create the method invocation statement
            CodeExpression invokeMethodExpression = new CodeExpression();
            //CodeExpression[] methodInvokeParameters = new CodeExpression[methodParameters.Length];
            //int i = 0;
            //foreach(object p in methodParameters)
            //{
            //    methodInvokeParameters[i] = methodParameters[i];
            //    i++;
            //}

            var targetTypeConstrucor = targetType.GetConstructors().Where(c => c.GetParameters().Length != 0).FirstOrDefault();
            CodeExpression[] ctorParams = new CodeExpression[targetTypeConstrucor.GetParameters().Length];
            var j = 0;
            foreach (ParameterInfo pi in targetTypeConstrucor.GetParameters())
            {
                var resolvedParameter = ResolveParameter(pi.ParameterType.Name);
                if (pi.ParameterType.Name == "String" || pi.ParameterType.Name == "Int32")
                {
                    ctorParams[j] = new CodePrimitiveExpression(ResolveParameter(pi.ParameterType.Name));
                }
                else
                {
                    CodeObjectCreateExpression createObjectExpression = CreateCustomType(pi.ParameterType.Name);
                    ctorParams[j] = createObjectExpression;
                }
                j++;
            }

            CodeObjectCreateExpression mthodInvokeTargetObject =
                new CodeObjectCreateExpression(targetType.FullName, ctorParams);

            // declare result variable
            CodeVariableDeclarationStatement variableDeclaration =
                new CodeVariableDeclarationStatement(
                    // Type of the variable to declare.
                    typeof(object),
                    // Name of the variable to declare.
                    "result");

            invokeMethodExpression =
                new CodeMethodInvokeExpression(
                    // targetObject that contains the method to invoke.
                    mthodInvokeTargetObject,
                    // methodName indicates the method to invoke.
                    methodName,
                    // parameters array contains the parameters for the method.
                    methodParameters);
            CodeAssignStatement assignMethodInvocatonResult = new CodeAssignStatement(
                new CodeVariableReferenceExpression("result"), invokeMethodExpression);


            // 2. assert part, create the result not null assertion statement
            CodeExpressionStatement assertNotNullStatement = new CodeExpressionStatement();
            CodeExpression[] assertNotNullParameters = new CodeExpression[1];
            assertNotNullParameters[0] = assignMethodInvocatonResult.Left;
            assertNotNullStatement.Expression =
                new CodeMethodInvokeExpression(
                    // targetObject that contains the method to invoke.
                    new CodeTypeReferenceExpression("Assert"),
                    // methodName indicates the method to invoke.
                    "NotNull",
                    // parameters array contains the parameters for the method.
                    assertNotNullParameters);


            testMethod.Statements.Add(variableDeclaration);
            testMethod.Statements.Add(assignMethodInvocatonResult);
            testMethod.Statements.Add(assertNotNullStatement);

            targetClass.Members.Add(testMethod);
        }

        #region Private functionality
        private object ResolveParameter(string typeToResolve)
        {
            switch (typeToResolve)
            {
                case "String":
                    return GetRandomString();
                case "Int32":
                    return GetRandomInteger();
                default:
                    //_objectFactory._instances.TryGetValue(typeToResolve, out object objectInstance);
                    return null;
            }
        }

        private CodeObjectCreateExpression CreateCustomType(string typeToResolve)
        {
            Type typeToResolveInfo = _assemblyExportedTypes.Where(aet => aet.Name == typeToResolve).FirstOrDefault();
            var typeToResolveConstructor = typeToResolveInfo.GetConstructors()
                .Where(c => c.GetParameters().Length != 0).FirstOrDefault();
            CodeExpression[] ctorParams = new CodeExpression[typeToResolveConstructor.GetParameters().Length];
            var j = 0;
            foreach (ParameterInfo pi in typeToResolveConstructor.GetParameters())
            {
                ctorParams[j] = new CodePrimitiveExpression(ResolveParameter(pi.ParameterType.Name));
                j++;
            }

            CodeObjectCreateExpression objectCreationExpression =
                new CodeObjectCreateExpression(typeToResolveInfo.FullName, ctorParams);

            return objectCreationExpression;
        }

        private int GetRandomInteger()
        {
            Random rnd = new Random();
            return rnd.Next(0, 10000);
        }

        private string GetRandomString()
        {
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
            var builder = new StringBuilder();
            Random rnd = new Random();

            for (var i = 0; i < 10; i++) // harcoded length for now
            {
                var c = pool[rnd.Next(0, pool.Length)];
                builder.Append(c);
            }
            return builder.ToString();
        }
        #endregion
    }
}
