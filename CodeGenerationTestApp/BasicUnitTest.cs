using NUnit.Framework;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerationTestApp
{
    public class BasicUnitTest
    {
        private CodeCompileUnit targetUnit;
        private CodeTypeDeclaration targetClass;
        private string OutputFileName = "BasicUnitTest.cs";
        private CompileHelper _compileHelper;

        public bool GenerateBasicUnitTestDll()
        {
            Initialize();

            AddTestMethod("TestMethod1", 10, 20);
            AddTestMethod("TestMethod2", "str", "str1");
            AddTestMethod("TestMethod3", 20, 20);
            AddTestMethod("TestMethod4", 1, 1);

            _compileHelper.GenerateCSharpCode(targetUnit, OutputFileName);
            var isCompiled = _compileHelper.CompileAsDLL(OutputFileName, new List<string>()
            {
                "nunit.framework.dll"
            });
            return isCompiled;
        }

        private void Initialize()
        {
            targetUnit = new CodeCompileUnit();

            CodeNamespace samples = new CodeNamespace("BasicUnitTestNamespace");
            samples.Imports.Add(new CodeNamespaceImport("System"));
            samples.Imports.Add(new CodeNamespaceImport("NUnit.Framework"));

            targetClass = new CodeTypeDeclaration("BasicUnitTestClass")
            {
                IsClass = true,
                TypeAttributes =
                TypeAttributes.Public
            };

            samples.Types.Add(targetClass);
            targetUnit.Namespaces.Add(samples);

            _compileHelper = new CompileHelper();
        }

        private void AddTestMethod(string methodName, object o1, object o2)
        {
            CodeMemberMethod testMethod = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Public,
                Name = methodName,
                ReturnType = new CodeTypeReference(typeof(void)),
                CustomAttributes =
                {
                    new CodeAttributeDeclaration
                    {
                        Name = "TestCase"
                    }
                }
            };

            // Declaring an assert statement for method BasicUnitTest1.
            CodeExpressionStatement codeStatement =
                new CodeExpressionStatement();
            

            CodeExpression[] parameters = new CodeExpression[2];
            parameters[0] = new CodePrimitiveExpression(o1);
            parameters[1] = new CodePrimitiveExpression(o2);

            codeStatement.Expression =
                new CodeMethodInvokeExpression(
                    // targetObject that contains the method to invoke.
                    new CodeTypeReferenceExpression("Assert"),
                    // methodName indicates the method to invoke.
                    "AreEqual",
                    // parameters array contains the parameters for the method.
                    parameters);

            testMethod.Statements.Add(codeStatement);
            targetClass.Members.Add(testMethod);
        }
    }                   
}
