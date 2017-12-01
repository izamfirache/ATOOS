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

namespace CodeGenerationTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //InstallNeededNugetPackages();

            try
            {
                // GENERATE A BasicUnitTest.cs
                var basicUnitTest = new BasicUnitTest();
                basicUnitTest.GenerateBasicUnitTestDll();

                // GENERATE A HelloWorld.cs and compile it as DLL
                //var helloWorld = new HelloWorld();
                //helloWorld.GenerateHelloWorldDll();

                // GENERATE A SampleCode.cs and compile it as DLL
                // var sampleClass = new Sample();
                // sampleClass.GenerateSampleDll();

                Console.Write("\nPress any key to exit.");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void InstallNeededNugetPackages()
        {
            try
            {
                // Install the xunit Nuget pkg into the target project
                string packageID = "NUnit";
                IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");
                
                string installPath = @"";
                PackageManager packageManager = new PackageManager(repo, installPath);
                packageManager.InstallPackage(packageID);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
