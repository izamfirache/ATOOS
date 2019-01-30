using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestExtension.TestGenerationUnits
{
    public class CompilerHelper
    {
        private string _unitTestDirectory;
        public CompilerHelper(string unitTestDirectory)
        {
            _unitTestDirectory = unitTestDirectory;
        }

        public string GenerateCSharpCode(CodeCompileUnit targetUnit, string outputFileName)
        {
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
            CodeGeneratorOptions options = new CodeGeneratorOptions
            {
                BracingStyle = "C"
            };
            
            var path = string.Format(@"{0}\{1}", _unitTestDirectory, outputFileName);
            using (StreamWriter sourceWriter = new StreamWriter(path))
            {
                provider.GenerateCodeFromCompileUnit(
                    targetUnit, sourceWriter, options);

                return path;
            }
        }

        public bool CompileAsDLL(string sourceName, List<string> referencedAssemblies)
        {
            string sourcePath = string.Format(@"{0}\{1}", _unitTestDirectory, sourceName);

            FileInfo sourceFile = new FileInfo(sourcePath);
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
                // Build the output assembly
                // under the root's project folder, create a new folder named UnitTests
                // save the unit tests dlls in it
                String dllName = String.Format(@"{0}\{1}.dll",
                    _unitTestDirectory,
                    sourceFile.Name.Replace(".cs", "_Tests"));

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

                foreach (string assembly in referencedAssemblies)
                {
                    cp.ReferencedAssemblies.Add(assembly);
                }

                // Invoke compilation of the source file.
                CompilerResults cr = provider.CompileAssemblyFromFile(cp, sourcePath);

                if (cr.Errors.Count > 0)
                {
                    // Display compilation errors.
                    Console.WriteLine("Errors building {0} into {1}",
                        sourcePath, cr.PathToAssembly);
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
                        sourcePath, cr.PathToAssembly);
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
