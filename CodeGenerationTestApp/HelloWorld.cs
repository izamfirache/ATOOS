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
    public class HelloWorld
    {
        public void GenerateHelloWorldDll()
        {
            // GENERATE A TEST HELLO WORLD CLASS USING C#
            CodeCompileUnit codeUnit = new CodeCompileUnit();
            CodeNamespace codeNameSpace = new CodeNamespace("CodeGenerationTestApp");

            CodeNamespaceImport usingSystemDll = new CodeNamespaceImport("System");
            CodeNamespaceImport usingNewtonsoftJsonDll = new CodeNamespaceImport("Newtonsoft.Json");
            CodeNamespaceImport usingNunitDll = new CodeNamespaceImport("NUnit");

            CodeTypeDeclaration programClass = new CodeTypeDeclaration("Program")
            { IsClass = true, TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed };

            codeNameSpace.Imports.Add(usingSystemDll);
            codeNameSpace.Imports.Add(usingNewtonsoftJsonDll);
            codeNameSpace.Imports.Add(usingNunitDll);

            codeUnit.Namespaces.Add(codeNameSpace);
            codeNameSpace.Types.Add(programClass);

            CodeEntryPointMethod entryPointMethod = new CodeEntryPointMethod();
            CodeTypeReferenceExpression systemConsoleReference = new CodeTypeReferenceExpression("System.Console");
            CodeMethodInvokeExpression helloWorldStatement = new CodeMethodInvokeExpression(systemConsoleReference, "WriteLine", new CodePrimitiveExpression("Hello World!"));
            entryPointMethod.Statements.Add(helloWorldStatement);
            programClass.Members.Add(entryPointMethod);

            CodeDomProvider codeDomProvider = CodeDomProvider.CreateProvider("CSharp");
            CodeGeneratorOptions generatorOptions = new CodeGeneratorOptions { BracingStyle = "C" };
            using (StreamWriter sourceWriter = new StreamWriter("HelloWorld.cs"))
            {
                codeDomProvider.GenerateCodeFromCompileUnit(codeUnit, sourceWriter, generatorOptions);
            }

            Console.WriteLine(File.ReadAllText("HelloWorld.cs"));

            // COMPILE THAT CLASS AND GENERATE A DLL
            var compileHelper = new CompileHelper();
            //var isDllCompiled = compileHelper.CompileAsDLL("HelloWorld.cs");
            CompileAsDLL("HelloWorld.cs");
        }

        public bool CompileAsDLL(string sourceName)
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
                cp.ReferencedAssemblies.Add("Newtonsoft.Json.dll");
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
