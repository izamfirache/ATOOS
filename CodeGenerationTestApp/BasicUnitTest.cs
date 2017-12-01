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

        public void GenerateBasicUnitTestDll()
        {
            Initialize();
            AddMethod();
            GenerateCSharpCode();
            CompileAsDLL(OutputFileName);
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
        }

        private void AddMethod()
        {
            // Declaring a BasicUnitTest1 method
            CodeMemberMethod basicUnitTest1 = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Public,
                Name = "BasicUnitTest1",
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
            CodePrimitiveExpression intPrimitive10 = new CodePrimitiveExpression(10);
            CodePrimitiveExpression intPrimitive20 = new CodePrimitiveExpression(20);

            parameters[0] = intPrimitive10;
            parameters[1] = intPrimitive20;

            codeStatement.Expression =
                new CodeMethodInvokeExpression(
                    // targetObject that contains the method to invoke.
                    new CodeTypeReferenceExpression("Assert"),
                    // methodName indicates the method to invoke.
                    "AreEqual",
                    // parameters array contains the parameters for the method.
                    parameters);

            basicUnitTest1.Statements.Add(codeStatement);
            targetClass.Members.Add(basicUnitTest1);
        }

        private void GenerateCSharpCode()
        {
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
            CodeGeneratorOptions options = new CodeGeneratorOptions
            {
                BracingStyle = "C"
            };
            using (StreamWriter sourceWriter = new StreamWriter(OutputFileName))
            {
                provider.GenerateCodeFromCompileUnit(
                    targetUnit, sourceWriter, options);
            }
        }

        private bool CompileAsDLL(string sourceName)
        {
            FileInfo sourceFile = new FileInfo(sourceName);
            CodeDomProvider provider = null;
            bool compileOk = false;

            // Select the code provider based on the input file extension.
            if (sourceFile.Extension.ToUpper(CultureInfo.InvariantCulture) == ".CS")
            {
                provider = CodeDomProvider.CreateProvider("CSharp");
            }
            else if (sourceFile.Extension.ToUpper(CultureInfo.InvariantCulture) == ".VB")
            {
                provider = CodeDomProvider.CreateProvider("VisualBasic");
            }
            else
            {
                Console.WriteLine("Source file must have a .cs or .vb extension");
            }

            if (provider != null)
            {
                // Format the executable file name.
                // Build the output assembly path using the current directory
                // and <source>_cs.dll or <source>_vb.dll.

                String dllName = String.Format(@"{0}\{1}.dll",
                    System.Environment.CurrentDirectory,
                    sourceFile.Name.Replace(".", "_"));

                CompilerParameters cp = new CompilerParameters
                {
                    // Generate an executable instead of 
                    // a class library.
                    GenerateExecutable = false,

                    // Specify the assembly file name to generate.
                    OutputAssembly = dllName,

                    // Save the assembly as a physical file.
                    GenerateInMemory = false,

                    // Set whether to treat all warnings as errors.
                    TreatWarningsAsErrors = false
                };
                cp.ReferencedAssemblies.Add("nunit.framework.dll");

                // Invoke compilation of the source file.
                CompilerResults cr = provider.CompileAssemblyFromFile(cp, sourceName);

                if (cr.Errors.Count > 0)
                {
                    // Display compilation errors.
                    Console.WriteLine("Errors building {0} into {1}",
                        sourceName, cr.PathToAssembly);
                    foreach (CompilerError ce in cr.Errors)
                    {
                        Console.WriteLine("  {0}", ce.ToString());
                        Console.WriteLine();
                    }
                }
                else
                {
                    // Display a successful compilation message.
                    Console.WriteLine("Source {0} built into {1} successfully.",
                        sourceName, cr.PathToAssembly);
                }

                // Return the results of the compilation.
                if (cr.Errors.Count > 0)
                {
                    compileOk = false;
                }
                else
                {
                    compileOk = true;
                }
            }
            return compileOk;
        }
    }                   
}
