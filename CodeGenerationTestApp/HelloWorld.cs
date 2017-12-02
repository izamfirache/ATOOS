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
            var compileHelper = new CompileHelper();

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

            // GENERATE THE CODE
            compileHelper.GenerateCSharpCode(codeUnit, "HelloWorld.cs");

            // COMPILE THE CODE AND GENERATE A DLL
            var isDllCompiled = compileHelper.CompileAsDLL("HelloWorld.cs",
                new List<string>()
                {
                    "Newtonsoft.Json.dll",
                    "nunit.framework.dll" });
        }
    }
}
