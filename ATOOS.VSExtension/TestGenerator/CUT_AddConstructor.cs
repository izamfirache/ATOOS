using ATOOS.VSExtension.ATOOS.Core;
using ATOOS.VSExtension.InputGenerators;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ATOOS.VSExtension.TestGenerator
{
    public class CUT_AddConstructor
    {
        public CUT_AddConstructor(InputParamGenerator inputParamGenerator, string selectedProjectName)
        {
            _inputParamGenerator = inputParamGenerator;
            _selectedProjectName = selectedProjectName;
        }

        public void AddTestClassConstructor(string constructorName, CodeTypeDeclaration targetClass,
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
                else if (pi.ParameterType.IsInterface)
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

        #region Private functionality

        private InputParamGenerator _inputParamGenerator;
        private string _selectedProjectName;

        private void AddMockObjectDeclarationToConstructor(CodeTypeDeclaration targetClass, string mockTypeName, string mockObjectName)
        {
            CodeMemberField mockedObjectDeclaration = new CodeMemberField(mockTypeName, mockObjectName)
            {
                Attributes = MemberAttributes.Private
            };

            targetClass.Members.Add(mockedObjectDeclaration);
        }

        private void AddMockObjectInstantiationToConstructor(CodeConstructor testClassConstructor, string mockObjectName, string mockTypeName)
        {
            CodeObjectCreateExpression createMockObjectExpression =
                                new CodeObjectCreateExpression(mockTypeName);

            CodeAssignStatement mockedObjectCreationStatement = new CodeAssignStatement(
                new CodeVariableReferenceExpression(mockObjectName), createMockObjectExpression);

            testClassConstructor.Statements.Add(mockedObjectCreationStatement);
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
                        _inputParamGenerator.ResolveParameter(methodReturnType));

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
            foreach (AnalyzedProject proj in analyzedSolution.Projects)
            {
                if (proj.Name == _selectedProjectName)
                {
                    foreach (Class cls in proj.Classes)
                    {
                        if (cls.Name == classUnerTestType.Name)
                        {
                            foreach (Method cutMethod in cls.Methods)
                            {
                                foreach (Statement stm in cutMethod.MethodBody.Statements)
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
        #endregion Private functionality
    }
}
