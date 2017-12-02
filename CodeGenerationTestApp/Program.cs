using NuGet;
using System;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Runtime.InteropServices;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CodeGenerationTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var unitTestDirectory = @"";
            DirectoryInfo di = new DirectoryInfo(unitTestDirectory);
            foreach (FileInfo file in di.GetFiles())
            {
                if (file.Name != "nunit.framework.dll")
                {
                    file.Delete();
                }
            }

            //var nugetHelper = new NugetHelper();
            //nugetHelper.InstallNUnitNugetPackages();

            try
            {
                // GENERATE A BasicUnitTest.cs
                var basicUnitTest = new BasicUnitTest();
                var isBasicTestDllCreated = basicUnitTest.GenerateBasicUnitTestDll();

                if (isBasicTestDllCreated)
                {
                    // execute the unit tests and display the results
                    // Use ProcessStartInfo class
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        CreateNoWindow = false,
                        UseShellExecute = false,
                        FileName = string.Format(@"{0}\nunit-console\{1}", unitTestDirectory, "nunit3-console.exe"),
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Arguments = string.Format(@"{0}\{1}", unitTestDirectory, "BasicUnitTest_Tests.dll")
                    };

                    try
                    {
                        // Start the process with the info we specified.
                        // Call WaitForExit and then the using statement will close.
                        using (Process exeProcess = Process.Start(startInfo))
                        {
                            exeProcess.WaitForExit();
                        }
                    }
                    catch(Exception e)
                    {
                        var str = e.Message;
                    }
                }

                // GENERATE A HelloWorld.cs and compile it as DLL
                //var helloWorld = new HelloWorld();
                //helloWorld.GenerateHelloWorldDll();

                // GENERATE A SampleCode.cs and compile it as DLL
                //var sampleClass = new Sample();
                //sampleClass.GenerateSampleDll();

                Console.Write("\nPress any key to exit.");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
