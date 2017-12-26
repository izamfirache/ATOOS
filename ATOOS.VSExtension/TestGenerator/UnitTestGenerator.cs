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
        private string _packagesFolder;
        private Type[] _assemblyExportedTypes = new Type[100];

        public UnitTestGenerator(string generatedTestClassesDirectory, string packagesFolder)
        {
            _generatedTestClassesDirectory = generatedTestClassesDirectory;
            _packagesFolder = packagesFolder;
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
                            AddTestClassConstructor(classSourceName, targetClass, type);

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
                                        parameters[j] = new CodePrimitiveExpression(ResolveParameter(p.ParameterType.Name));
                                    }
                                    else
                                    {
                                        CodeObjectCreateExpression createObjectExpression = CreateCustomType(p.ParameterType.Name);
                                        parameters[j] = createObjectExpression;
                                    }
                                    j++;
                                }

                                AddTestIfResultIsNotNullTest(targetClass, m.Name, parameters, type);
                            }

                            // generate the c# code based on the created code unit
                            string generatedTestClassPath = compileHelper.GenerateCSharpCode(codeUnit, classSourceName);

                            // compile the above generated code into a DLL/EXE
                            bool isGeneratedClassCompiled = compileHelper.CompileAsDLL(classSourceName, new List<string>()
                        {
                            string.Format("{0}\\{1}", _packagesFolder, "NUnit.3.9.0\\lib\\net45\\nunit.framework.dll"),
                            string.Format("{0}\\{1}", _packagesFolder, "Moq.4.7.145\\lib\\net45\\Moq.dll"),
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

        private void AddTestClassConstructor(string constructorName, CodeTypeDeclaration targetClass, Type type)
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
                        }

                        // - inside the constructor, instantiate all the above declared mocked objects
                        // example -- _objectMock = new Mock<ObjectToBeMocked>();
                        foreach (string key in globallyDeclaredObjects.Keys)
                        {
                            AddMockObjectInstantiationToConstructor(testClassConstructor, key, globallyDeclaredObjects[key]);
                        }
                    }
                }
                else if(pi.ParameterType.IsInterface)
                {
                    var mockTypeName = string.Format("Mock<{0}>", pi.ParameterType.Name);
                    var mockObjectName = string.Format("{0}Mock", pi.ParameterType.Name);
                    AddMockObjectDeclarationToConstructor(targetClass, mockTypeName, mockObjectName);
                    AddMockObjectInstantiationToConstructor(testClassConstructor, mockObjectName, mockTypeName);
                }
            }

            // - for each discovered/mockable method in type ObjectToBeMocked,
            // - build the mocking statement (including the lambda expression),
            // - generate the mocked method result in a random fashion
            // - save into a dictionary<MethodName, MockedResult> in order to 
            //   use them later at the ASSERT phase when the unit tests will be build
            // - add all the above created statements to the constructor

            // - for each mocked method, generate a new unit test method in which to test the mocking logic

            // done!
            targetClass.Members.Add(testClassConstructor);
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

        private void AddTestIfResultIsNotNullTest(CodeTypeDeclaration targetClass, string methodName, CodeExpression[] methodParameters, Type targetType)
        {
            // generate test method name
            CodeMemberMethod testMethod = CreateTestMethodSignature(methodName);
            
            // ACT, create the method invocation statement
            CodeExpression invokeMethodExpression = new CodeExpression();

            var targetTypeConstrucor = targetType.GetConstructors().Where(c => c.GetParameters().Length != 0).FirstOrDefault();
            CodeExpression[] ctorParams = new CodeExpression[targetTypeConstrucor.GetParameters().Length];
            var j = 0;
            foreach (ParameterInfo pi in targetTypeConstrucor.GetParameters())
            {
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
            CodeVariableDeclarationStatement variableDeclaration = new CodeVariableDeclarationStatement(
                    typeof(object),  // Type of the variable to declare.
                    "result");      // Name of the variable to declare.

            invokeMethodExpression = new CodeMethodInvokeExpression(
                    mthodInvokeTargetObject,  // targetObject that contains the method to invoke.
                    methodName,              // methodName indicates the method to invoke.
                    methodParameters);      // parameters array contains the parameters for the method.

            CodeAssignStatement assignMethodInvocatonResult = new CodeAssignStatement(
                new CodeVariableReferenceExpression("result"), invokeMethodExpression);


            // ASSERT, create the result not null assertion statement
            CodeExpressionStatement assertNotNullStatement = new CodeExpressionStatement();
            CodeExpression[] assertNotNullParameters = new CodeExpression[1];
            assertNotNullParameters[0] = assignMethodInvocatonResult.Left;
            assertNotNullStatement.Expression = new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("Assert"), // targetObject that contains the method to invoke.
                    "NotNull",                                // methodName indicates the method to invoke.
                    assertNotNullParameters);                // parameters array contains the parameters for the method.


            // add the above created expressions to the testMethod
            testMethod.Statements.Add(variableDeclaration);
            testMethod.Statements.Add(assignMethodInvocatonResult);
            testMethod.Statements.Add(assertNotNullStatement);

            targetClass.Members.Add(testMethod);
        }

        private CodeMemberMethod CreateTestMethodSignature(string methodName)
        {
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

            return testMethod;
        }

        #region Private functionality

        private CodeObjectCreateExpression CreateCustomType(string typeToResolve)
        {
            Type typeToResolveInfo = _assemblyExportedTypes.Where(aet => aet.Name == typeToResolve).FirstOrDefault();

            if (typeToResolveInfo != null)
            {
                if (typeToResolveInfo.IsClass)
                {
                    var typeToResolveConstructor = typeToResolveInfo.GetConstructors()
                        .Where(c => c.GetParameters().Length != 0).FirstOrDefault();

                    CodeExpression[] ctorParams = new CodeExpression[typeToResolveConstructor.GetParameters().Length];
                    var j = 0;
                    foreach (ParameterInfo pi in typeToResolveConstructor.GetParameters())
                    {
                        if (pi.ParameterType.Name == "String" || pi.ParameterType.Name == "Int32")
                        {
                            ctorParams[j] = new CodePrimitiveExpression(ResolveParameter(pi.ParameterType.Name));
                        }
                        else
                        {
                            CodeObjectCreateExpression createObjectExpression = CreateCustomType(pi.ParameterType.Name);
                            ctorParams[j] = createObjectExpression;
                        }
                        //ctorParams[j] = new CodePrimitiveExpression(ResolveParameter(pi.ParameterType.Name));
                        j++;
                    }

                    CodeObjectCreateExpression objectCreationExpression =
                        new CodeObjectCreateExpression(typeToResolveInfo.FullName, ctorParams);

                    return objectCreationExpression;
                }
                else //if(typeToResolveInfo.IsInterface)
                {
                    // interface -- find all exported types that implement that interface
                    List<Type> interfaceTypes = (from t in _assemblyExportedTypes
                                         where !t.IsInterface && !t.IsAbstract
                                         where typeToResolveInfo.IsAssignableFrom(t)
                                         select t).ToList();

                    if (interfaceTypes.Count != 0)
                    {
                        return CreateCustomType(interfaceTypes.FirstOrDefault().Name);
                    }
                    else
                    {
                        throw new Exception(string.Format("Can not find a type that implements : ", typeToResolve));
                    }
                }
            }
            else
            {
                throw new Exception(string.Format("Can not resolve type : ", typeToResolve));
            }
        }

        private object ResolveParameter(string typeToResolve)
        {
            switch (typeToResolve)
            {
                case "String":
                    return GetRandomString();
                case "Int32":
                    return GetRandomInteger();
                default: throw new Exception(string.Format("Can not resolve type : ", typeToResolve));
            }
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
