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
        // TODO: move each region into its own class

        private string _generatedTestClassesDirectory;
        private string _packagesFolder;
        private Type[] _assemblyExportedTypes = new Type[100];
        private string selectedProjectName;
        InputParamGenerator inputParamGenerator;

        public UnitTestGenerator(string generatedTestClassesDirectory, string packagesFolder, 
            string selectedProjectName)
        {
            _generatedTestClassesDirectory = generatedTestClassesDirectory;
            _packagesFolder = packagesFolder;
            this.selectedProjectName = selectedProjectName;
        }

        public List<string> GenerateUnitTestsForClass(string solutionPath, string generatedUnitTestProject)
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
                            AddTestClassConstructor(classSourceName, targetClass, type, analyedSolution);

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

                                // Assert.NotNull(result);
                                AddTestIfResultIsNotNullTest(targetClass, m.Name, parameters, type);

                                // Assert.NotThrow(() => targetObj.SomePublicMethod())
                                AddTestShouldNotThrowExceptionTest(targetClass, m.Name, parameters, type);

                                // Assert.AreEqual(result, new object { });
                                AddTestTheResultShouldbeTheExpectedOne(targetClass, m.Name, parameters, type);
                            }

                            // generate the c# code based on the created code unit
                            string generatedTestClassPath = compileHelper.GenerateCSharpCode(codeUnit, classSourceName);

                            // compile the above generated code into a DLL/EXE
                            bool isGeneratedClassCompiled = compileHelper.CompileAsDLL(classSourceName, new List<string>()
                            {
                                string.Format("{0}\\{1}", _packagesFolder, "NUnit.3.9.0\\lib\\net45\\nunit.framework.dll"),
                                string.Format("{0}\\{1}", _packagesFolder, "Moq.4.8.0\\lib\\net45\\Moq.dll"),
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

            return generatedTestClasses;
        }

        private void AddTestTheResultShouldbeTheExpectedOne(CodeTypeDeclaration targetClass, string methodName, 
            CodeExpression[] methodParameters, Type targetType)
        {
            // generate test method name
            CodeMemberMethod testMethod = CreateTestMethodSignature(methodName, "ResultShouldHaveExpectedValue");

            // ACT, create the method invocation statement
            CodeExpression invokeMethodExpression = new CodeExpression();

            var targetTypeConstrucor = targetType.GetConstructors()
                .Where(c => c.GetParameters().Length != 0).FirstOrDefault();

            CodeExpression[] ctorParams = inputParamGenerator.ResolveInputParametersForCtorOrMethod(
                targetTypeConstrucor.GetParameters().Length,
                targetTypeConstrucor.GetParameters(),
                testMethod);

            CodeObjectCreateExpression mthodInvokeTargetObject =
                new CodeObjectCreateExpression(targetType.FullName, ctorParams);

            // declare the target object
            CodeVariableDeclarationStatement assignTargetObjectToVariable =
                new CodeVariableDeclarationStatement(
                    targetType, targetType.Name.ToLower(), mthodInvokeTargetObject);

            testMethod.Statements.Add(assignTargetObjectToVariable);

            invokeMethodExpression = new CodeMethodInvokeExpression(
                    // targetObject that contains the method to invoke.
                    new CodeVariableReferenceExpression(targetType.Name.ToLower()),
                    methodName,              // methodName indicates the method to invoke.
                    methodParameters);      // parameters array contains the parameters for the method.

            // declare result variable
            CodeVariableDeclarationStatement assignMethodInvocatonResult = new CodeVariableDeclarationStatement(
                    typeof(object), "result", invokeMethodExpression);
            testMethod.Statements.Add(assignMethodInvocatonResult);

            // ASSERT, create the result not null assertion statement
            CodeExpressionStatement assertNotNullStatement = new CodeExpressionStatement();
            CodeExpression[] assertNotNullParameters = new CodeExpression[2];
            assertNotNullParameters[0] = new CodeVariableReferenceExpression("result");
            assertNotNullParameters[1] = new CodeSnippetExpression("new { }");

            CodeComment comment = new CodeComment("Please insert here the expected result", false);
            CodeCommentStatement commentStatement = new CodeCommentStatement(comment);
            testMethod.Statements.Add(commentStatement);
            
            assertNotNullStatement.Expression = new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("Assert"), // targetObject that contains the method to invoke.
                    "AreEqual",                                // methodName indicates the method to invoke.
                    assertNotNullParameters);                // parameters array contains the parameters for the method.


            // add the above created expressions to the testMethod
            testMethod.Statements.Add(assertNotNullStatement);

            targetClass.Members.Add(testMethod);
        }
        
        private void AddTestShouldNotThrowExceptionTest(CodeTypeDeclaration targetClass, string methodName,
            CodeExpression[] methodParameters, Type targetType)
        {
            // generate test method name
            CodeMemberMethod testMethod = CreateTestMethodSignature(methodName, "CallShouldNotThrowAnyException");

            // ACT, create the method invocation statement
            CodeExpression invokeMethodExpression = new CodeExpression();

            var targetTypeConstrucor = targetType.GetConstructors()
                .Where(c => c.GetParameters().Length != 0).FirstOrDefault();

            CodeExpression[] ctorParams = inputParamGenerator.ResolveInputParametersForCtorOrMethod(
                targetTypeConstrucor.GetParameters().Length,
                targetTypeConstrucor.GetParameters(),
                testMethod);

            // create the target object
            CodeObjectCreateExpression createTargetObjectExpression =
                new CodeObjectCreateExpression(targetType.FullName, ctorParams);

            // assign created target object to a variable
            var targetObjectVariableName = targetType.Name.ToLower();
            CodeVariableDeclarationStatement assignMethodInvocatonResult = new CodeVariableDeclarationStatement(
                    targetType, targetObjectVariableName, createTargetObjectExpression);

            // create the method invocation expression using the above created targetObject
            invokeMethodExpression = new CodeMethodInvokeExpression(
                    new CodeVariableReferenceExpression(targetObjectVariableName),  // targetObject that contains the method to invoke.
                    methodName,              // methodName indicates the method to invoke.
                    methodParameters);      // parameters array contains the parameters for the method.

            StringWriter writer = new StringWriter();
            CSharpCodeProvider csProvider = new CSharpCodeProvider();
            csProvider.GenerateCodeFromExpression(invokeMethodExpression, writer, new CodeGeneratorOptions());
            string invokeMethodExpressionAsString = writer.ToString();

            // ASSERT
            var lambdaExpr = string.Format("() => {0}", invokeMethodExpressionAsString);
            var shouldNotThrowExceptionExpression = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("Assert"),
                "DoesNotThrow", 
                new CodeSnippetExpression(lambdaExpr));

            // add the above created expressions to the testMethod
            testMethod.Statements.Add(assignMethodInvocatonResult);
            testMethod.Statements.Add(shouldNotThrowExceptionExpression);

            targetClass.Members.Add(testMethod);
        }

        private void AddTestIfResultIsNotNullTest(CodeTypeDeclaration targetClass, string methodName,
            CodeExpression[] methodParameters, Type targetType)
        {
            // generate test method name
            CodeMemberMethod testMethod = CreateTestMethodSignature(methodName, "TheResultShouldNotBeNull");

            // ACT, create the method invocation statement
            CodeExpression invokeMethodExpression = new CodeExpression();

            var targetTypeConstrucor = targetType.GetConstructors()
                .Where(c => c.GetParameters().Length != 0).FirstOrDefault();

            CodeExpression[] ctorParams = inputParamGenerator.ResolveInputParametersForCtorOrMethod(
                targetTypeConstrucor.GetParameters().Length, 
                targetTypeConstrucor.GetParameters(),
                testMethod);

            CodeObjectCreateExpression mthodInvokeTargetObject =
                new CodeObjectCreateExpression(targetType.FullName, ctorParams);

            // declare the target object
            CodeVariableDeclarationStatement assignTargetObjectToVariable =
                new CodeVariableDeclarationStatement(
                    targetType, targetType.Name.ToLower(), mthodInvokeTargetObject);

            testMethod.Statements.Add(assignTargetObjectToVariable);

            invokeMethodExpression = new CodeMethodInvokeExpression(
                    // targetObject that contains the method to invoke.
                    new CodeVariableReferenceExpression(targetType.Name.ToLower()),
                    methodName,              // methodName indicates the method to invoke.
                    methodParameters);      // parameters array contains the parameters for the method.

            // declare result variable
            CodeVariableDeclarationStatement assignMethodInvocatonResult = new CodeVariableDeclarationStatement(
                    typeof(object), "result", invokeMethodExpression);

            // ASSERT, create the result not null assertion statement
            CodeExpressionStatement assertNotNullStatement = new CodeExpressionStatement();
            CodeExpression[] assertNotNullParameters = new CodeExpression[1];
            assertNotNullParameters[0] = new CodeVariableReferenceExpression("result"); // assignMethodInvocatonResult.Left;
            assertNotNullStatement.Expression = new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("Assert"), // targetObject that contains the method to invoke.
                    "NotNull",                                // methodName indicates the method to invoke.
                    assertNotNullParameters);                // parameters array contains the parameters for the method.


            // add the above created expressions to the testMethod
            testMethod.Statements.Add(assignMethodInvocatonResult);
            testMethod.Statements.Add(assertNotNullStatement);

            targetClass.Members.Add(testMethod);
        }

        private void AddTestClassConstructor(string constructorName, CodeTypeDeclaration targetClass, 
            Type type, AnalyzedSolution analyzedSolution)
        {
            // create a code unit for a consturctor
            CodeConstructor testClassConstructor = new CodeConstructor()
            {
                Name = constructorName,
                Attributes = MemberAttributes.Public
            };

            // decide if this type has a mockable parameter
            // ex: a custom object wich implements an interface or has virtual methods
            // if yes -- mock all methods that can be mocked
            // if not -- nothing to be done

            var targetTypeConstructor = type.GetConstructors().Where(c => c.GetParameters().Length != 0).FirstOrDefault();
            foreach (ParameterInfo pi in targetTypeConstructor.GetParameters())
            {
                if (pi.ParameterType.Name != "String" 
                    && pi.ParameterType.Name != "Int32" 
                    && pi.ParameterType.IsClass) // it is a non-primitive class, TODO: rethink this!
                { 
                    // check if it implements an interface
                    var implementedInterfaces = pi.ParameterType.GetInterfaces().ToList();
                    if (implementedInterfaces.Count != 0)
                    {
                        // - globally declare a mock object for each dependency to be mocked 
                        // example -- private Mock<ObjectToBeMocked> _objectMock;

                        // mock each interface ???
                        Dictionary<string, string> globallyDeclaredObjects = new Dictionary<string, string>();
                        foreach (Type implementedInterface in implementedInterfaces)
                        {
                            var mockTypeName = string.Format("Mock<{0}>", implementedInterface.Name);
                            var mockObjectName = string.Format("{0}Mock", implementedInterface.Name);
                            AddMockObjectDeclarationToConstructor(targetClass, mockTypeName, mockObjectName);
                            globallyDeclaredObjects.Add(mockObjectName, mockTypeName);

                            // - inside the constructor, instantiate all the above declared mocked objects
                            // example -- _objectMock = new Mock<ObjectToBeMocked>();
                            AddMockObjectInstantiationToConstructor(testClassConstructor, mockObjectName, mockTypeName);

                            MockAllExternalDependencyMethods(implementedInterface, testClassConstructor, analyzedSolution, type);

                            // - for each mocked method, generate a new unit test method in which to test the mocking logic
                        }
                    }
                }
                else if(pi.ParameterType.IsInterface)
                {
                    var mockTypeName = string.Format("Mock<{0}>", pi.ParameterType.Name);
                    var mockObjectName = string.Format("{0}Mock", pi.ParameterType.Name);
                    AddMockObjectDeclarationToConstructor(targetClass, mockTypeName, mockObjectName);
                    AddMockObjectInstantiationToConstructor(testClassConstructor, mockObjectName, mockTypeName);

                    MockAllExternalDependencyMethods(pi.ParameterType, testClassConstructor, analyzedSolution, type);

                    // - for each mocked method, generate a new unit test method in which to test the mocking logic
                }
            }

            // done!
            targetClass.Members.Add(testClassConstructor);
        }

        private void MockAllExternalDependencyMethods(Type type, CodeConstructor testClassConstructor, 
            AnalyzedSolution analyzedSolution, Type classUnderTestType)
        {
            // - for each discovered/mockable method in type ObjectToBeMocked
            var methods = type.GetMethods(BindingFlags.Public
                            | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (MethodInfo m in methods)
            {
                // see if the method should be mocked
                // -- search in the code for all the methods in the CUT to see if the method is invoked somewhere
                // --- if yes, then the method should be mocked
                // --- if no, then that method is not used and it should not be mocked
                bool shouldBeMocked = CheckIfTheMethodShouldBeMocked(type, analyzedSolution, m, classUnderTestType);

                if (shouldBeMocked)
                {
                    // dynamically generate the code for a lamda expression
                    var parameter = Expression.Parameter(type, "m");
                    MethodInfo methodInfo = type.GetMethod(m.Name);

                    // randomly generate method parameters
                    var methodParameters = m.GetParameters();
                    string parameters = "";
                    int j = 1;
                    foreach (ParameterInfo p in methodParameters)
                    {
                        var separator = j == methodParameters.Count() ? "" : ", ";
                        parameters += string.Format("It.IsAny<{0}>()", p.ParameterType) + separator;
                        j++;
                    }

                    var lambdaExpr = string.Format("m => m.{0}({1})", m.Name, parameters);
                    var mockSetupMethod = new CodeMethodInvokeExpression(
                        new CodeVariableReferenceExpression(string.Format("{0}Mock", type.Name)),
                        "Setup", new CodeSnippetExpression(lambdaExpr));

                    // resolve method return type
                    var methodReturnType = m.ReturnType.Name;
                    CodeExpression[] mockReturnMethodParameter = new CodeExpression[1];
                    mockReturnMethodParameter[0] = new CodePrimitiveExpression(
                        inputParamGenerator.ResolveParameter(methodReturnType));

                    var mockReturnMethod = new CodeMethodInvokeExpression(
                        mockSetupMethod,
                        "Returns", mockReturnMethodParameter);

                    testClassConstructor.Statements.Add(mockReturnMethod);
                }
            }
        }

        private bool CheckIfTheMethodShouldBeMocked(Type type, AnalyzedSolution analyzedSolution, 
            MethodInfo dependencyMethod, Type classUnerTestType)
        {
            foreach(AnalyzedProject proj in analyzedSolution.Projects)
            {
                if(proj.Name == selectedProjectName)
                {
                    foreach(Class cls in proj.Classes)
                    {
                        if(cls.Name == classUnerTestType.Name)
                        {
                            foreach(Method cutMethod in cls.Methods)
                            {
                                foreach(Statement stm in cutMethod.MethodBody.Statements)
                                {
                                    if (stm.Content.Contains(dependencyMethod.Name))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void AddMockObjectInstantiationToConstructor(CodeConstructor testClassConstructor, string mockObjectName, string mockTypeName)
        {
            CodeObjectCreateExpression createMockObjectExpression =
                                new CodeObjectCreateExpression(mockTypeName);

            CodeAssignStatement mockedObjectCreationStatement = new CodeAssignStatement(
                new CodeVariableReferenceExpression(mockObjectName), createMockObjectExpression);

            testClassConstructor.Statements.Add(mockedObjectCreationStatement);
        }

        private void AddMockObjectDeclarationToConstructor(CodeTypeDeclaration targetClass, string mockTypeName, string mockObjectName)
        {
            CodeMemberField mockedObjectDeclaration = new CodeMemberField(mockTypeName, mockObjectName)
            {
                Attributes = MemberAttributes.Private
            };

            targetClass.Members.Add(mockedObjectDeclaration);
        }

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
        
        private CodeMemberMethod CreateTestMethodSignature(string methodName, string unitTestType)
        {
            CodeMemberMethod testMethod = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Public,
                Name = string.Format("{0}_{1}_{2}", methodName, "RandomInput", unitTestType),
                ReturnType = new CodeTypeReference(typeof(void)),
                CustomAttributes =
                {
                    new CodeAttributeDeclaration
                    {
                        Name = "TestCase"
                    }
                }
            };

            return testMethod;
        }
    }
}