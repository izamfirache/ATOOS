using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitTestExtension.InputGenerators;

namespace UnitTestExtension.TestGenerationUnits
{
    public class CUT_AddTestMethod
    {
        public CUT_AddTestMethod(InputParamGenerator inputParamGenerator)
        {
            _inputParamGenerator = inputParamGenerator;
        }
        
        public void AddTestMethod_ShouldNotThrowExceptionResultShouldNotBeNull
            (CodeTypeDeclaration targetClass, 
            string methodName,
            CodeExpression[] methodParameters, 
            Type targetType,
            string unitTestMethodName)
        {
            // generate test method name
            CodeMemberMethod testMethod = CreateTestMethodSignature(methodName, unitTestMethodName);

            // build the ARRANGE and the ACT part
            // create a CUT instance and call the method under test
            string invokeMethodExpressionAsString = 
                GenerateTheCodeToCallTheMethodUnderTestAndReturnTheInvocationAsString(targetType, testMethod, 
                methodName, methodParameters);

            // build the ASSERT part
            // create the result not null assertion statement, add it to the unit test method
            testMethod.Statements.Add(GetAssertNotNullStatement());

            // create the assertion which will verify that no exception is thrown during execution, add it to the unit test method
            testMethod.Statements.Add(GetAssertShouldNotThrowExceptionStatement(invokeMethodExpressionAsString));

            // add the unit test method to the unit test class
            targetClass.Members.Add(testMethod);
        }

        public void AddTestMethod_ExpectedResultPlaceholder
            (CodeTypeDeclaration targetClass,
            string methodName,
            CodeExpression[] methodParameters,
            Type targetType,
            string unitTestMethodName)
        {
            // generate test method name
            CodeMemberMethod testMethod = CreateTestMethodSignature(methodName, unitTestMethodName);

            // build the ARRANGE and the ACT part
            // create a CUT instance and call the method under test
            string invokeMethodExpressionAsString =
                GenerateTheCodeToCallTheMethodUnderTestAndReturnTheInvocationAsString(targetType, testMethod,
                methodName, methodParameters);

            // build the ASSERT part
            // create the insert expected value assertion statement, add it to the unit test method
            testMethod.Statements.Add(GetAssertExpectedValuePlaceHolderStatement());

            // add the unit test method to the unit test class
            targetClass.Members.Add(testMethod);
        }

        private string GenerateTheCodeToCallTheMethodUnderTestAndReturnTheInvocationAsString(Type targetType, 
            CodeMemberMethod testMethod, string methodName, CodeExpression[] methodParameters)
        {
            // ARRANGE, ACT, create the method invocation statement
            CodeExpression invokeMethodExpression = new CodeExpression();

            var targetTypeConstrucor = targetType.GetConstructors()
                .Where(c => c.GetParameters().Length != 0).FirstOrDefault();

            CodeExpression[] ctorParams = _inputParamGenerator.ResolveInputParametersForCtorOrMethod(
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

            testMethod.Statements.Add(assignMethodInvocatonResult);

            // create the method invocation expression using the above created targetObject
            invokeMethodExpression = new CodeMethodInvokeExpression(
                    new CodeVariableReferenceExpression(targetObjectVariableName),  // targetObject that contains the method to invoke.
                    methodName,              // methodName indicates the method to invoke.
                    methodParameters);      // parameters array contains the parameters for the method.

            StringWriter writer = new StringWriter();
            CSharpCodeProvider csProvider = new CSharpCodeProvider();
            csProvider.GenerateCodeFromExpression(invokeMethodExpression, writer, new CodeGeneratorOptions());
            string invokeMethodExpressionAsString = writer.ToString();
            
            invokeMethodExpression = new CodeMethodInvokeExpression(
                    // targetObject that contains the method to invoke.
                    new CodeVariableReferenceExpression(targetType.Name.ToLower()),
                    methodName,              // methodName indicates the method to invoke.
                    methodParameters);      // parameters array contains the parameters for the method.

            // declare result variable
            CodeVariableDeclarationStatement assignMethodInvocatonResultForNotNull = new CodeVariableDeclarationStatement(
                    typeof(object), "result", invokeMethodExpression);

            testMethod.Statements.Add(assignMethodInvocatonResultForNotNull);

            return invokeMethodExpressionAsString;
        }

        private CodeExpressionStatement GetAssertExpectedValuePlaceHolderStatement()
        {
            var expectedValuePlaceholderStatement = new CodeExpressionStatement();
            CodeExpression[] expectedValuePlaceholderParameters = new CodeExpression[2];
            expectedValuePlaceholderParameters[0] = new CodeVariableReferenceExpression("result");
            expectedValuePlaceholderParameters[1] = new CodePrimitiveExpression("Insert expected value here.");
            expectedValuePlaceholderStatement.Expression = new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("Assert"), // targetObject that contains the method to invoke.
                    "AreEqual",                                // methodName indicates the method to invoke.
                    expectedValuePlaceholderParameters);       // parameters array contains the parameters for the method.

            return expectedValuePlaceholderStatement;
        }

        private CodeMethodInvokeExpression GetAssertShouldNotThrowExceptionStatement(string invokeMethodExpressionAsString)
        {
            var lambdaExpr = string.Format("() => {0}", invokeMethodExpressionAsString);
            var shouldNotThrowExceptionExpression = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("Assert"),
                "DoesNotThrow",
                new CodeSnippetExpression(lambdaExpr));
            return shouldNotThrowExceptionExpression;
        }

        private CodeExpressionStatement GetAssertNotNullStatement()
        {
            var assertNotNullStatement = new CodeExpressionStatement();
            CodeExpression[] assertNotNullParameters = new CodeExpression[1];
            assertNotNullParameters[0] = new CodeVariableReferenceExpression("result"); // assignMethodInvocatonResult.Left;
            assertNotNullStatement.Expression = new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("Assert"), // targetObject that contains the method to invoke.
                    "NotNull",                                // methodName indicates the method to invoke.
                    assertNotNullParameters);                // parameters array contains the parameters for the method.

            return assertNotNullStatement;
        }

        #region Private Functionality

        private InputParamGenerator _inputParamGenerator;

        private void AddTestTheResultShouldbeTheExpectedOne(CodeTypeDeclaration targetClass, string methodName,
            CodeExpression[] methodParameters, Type targetType)
        {
            // generate test method name
            CodeMemberMethod testMethod = CreateTestMethodSignature(methodName, "ResultShouldHaveExpectedValue");

            // ACT, create the method invocation statement
            CodeExpression invokeMethodExpression = new CodeExpression();

            var targetTypeConstrucor = targetType.GetConstructors()
                .Where(c => c.GetParameters().Length != 0).FirstOrDefault();

            CodeExpression[] ctorParams = _inputParamGenerator.ResolveInputParametersForCtorOrMethod(
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
        #endregion Private Functionality
    }
}
