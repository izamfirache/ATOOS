using NuGet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerationTestApp
{
    public class NugetHelper
    {
        public void InstallNUnitNugetPackages()
        {
            try
            {
                string NunitPackageID = "NUnit";
                string NunitConsolePackageID = "NUnit.Console";
                string NunitAdapterPackageID = "NUnit3TestAdapter";

                IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");

                string installPath = @"";
                PackageManager packageManager = new PackageManager(repo, installPath);
                packageManager.InstallPackage(NunitPackageID);
                packageManager.InstallPackage(NunitConsolePackageID);
                packageManager.InstallPackage(NunitAdapterPackageID);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
