using ATOOS.VSExtension.InputGenerators;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATOOS.VSExtension.TestGenerator
{
    public class CUT_AddTestMethod
    {
        public CUT_AddTestMethod(InputParamGenerator inputParamGenerator)
        {
            _inputParamGenerator = inputParamGenerator;
        }

        public void AddTestMethod_ShouldNotThrowException_ResultShouldNotBeNull
            (CodeTypeDeclaration targetClass, 
            string methodName,
            CodeExpression[] methodParameters, 
            Type targetType)
        {
            // generate test method name
            CodeMemberMethod testMethod = CreateTestMethodSignature(methodName, "CallShouldNotThrowAnyException");

            // ACT, create the method invocation statement
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

            // ASSERT
            invokeMethodExpression = new CodeMethodInvokeExpression(
                    // targetObject that contains the method to invoke.
                    new CodeVariableReferenceExpression(targetType.Name.ToLower()),
                    methodName,              // methodName indicates the method to invoke.
                    methodParameters);      // parameters array contains the parameters for the method.

            // declare result variable
            CodeVariableDeclarationStatement assignMethodInvocatonResultForNotNull = new CodeVariableDeclarationStatement(
                    typeof(object), "result", invokeMethodExpression);

            testMethod.Statements.Add(assignMethodInvocatonResultForNotNull);

            // create the result not null assertion statement
            CodeExpressionStatement assertNotNullStatement = new CodeExpressionStatement();
            CodeExpression[] assertNotNullParameters = new CodeExpression[1];
            assertNotNullParameters[0] = new CodeVariableReferenceExpression("result"); // assignMethodInvocatonResult.Left;
            assertNotNullStatement.Expression = new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("Assert"), // targetObject that contains the method to invoke.
                    "NotNull",                                // methodName indicates the method to invoke.
                    assertNotNullParameters);                // parameters array contains the parameters for the method.

            testMethod.Statements.Add(assertNotNullStatement);

            // create the assertion which will verify that no exception is thrown during execution
            var lambdaExpr = string.Format("() => {0}", invokeMethodExpressionAsString);
            var shouldNotThrowExceptionExpression = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("Assert"),
                "DoesNotThrow",
                new CodeSnippetExpression(lambdaExpr));

            // add the above created expressions to the testMethod
            testMethod.Statements.Add(shouldNotThrowExceptionExpression);

            targetClass.Members.Add(testMethod);
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
