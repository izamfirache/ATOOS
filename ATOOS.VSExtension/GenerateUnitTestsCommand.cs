using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE80;
using EnvDTE;
using System.Windows.Forms;
using NuGet;
using ATOOS.VSExtension.ObjectFactory;
using ATOOS.VSExtension.TestGenerator;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ATOOS.VSExtension
{
    internal sealed class GenerateUnitTestsCommand
    {
        public const int CommandId = 0x0100;
        
        public static readonly Guid CommandSet = new Guid("4210003b-22d4-4404-bca8-38abaa597829");
        
        private readonly Package package;
        
        private GenerateUnitTestsCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        public static GenerateUnitTestsCommand Instance
        {
            get;
            private set;
        }

        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }
        
        public static void Initialize(Package package)
        {
            Instance = new GenerateUnitTestsCommand(package);
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            //try
            //{
                var dte = (DTE2)ServiceProvider.GetService(typeof(DTE));
                var selectedProjectName = GetSelectedProjectName(dte);
                var unitTestProjectName = string.Format("{0}_Tests", selectedProjectName);

                // discover solution/project types and create an object factory
                var solutionFullPath = GetSolutionFullPath(dte);

                // generate unit tests for the analyzed project
                var generatedTestClassesDirectory = CreateUnitTestsProject(dte, unitTestProjectName);
                string packagesPath = GetSolutionPackagesFolder(dte);

                var unitTestGenerator = new UnitTestGenerator(generatedTestClassesDirectory,
                    packagesPath, selectedProjectName);
                List<string> testClasses = unitTestGenerator.GenerateUnitTestsForClass(solutionFullPath, unitTestProjectName);

                // add test classes to project
                string csprojPath = string.Format(@"{0}\\{1}\\{2}{3}", GetSolutionPath(dte),
                    unitTestProjectName, unitTestProjectName, ".csproj");

                var p = new Microsoft.Build.Evaluation.Project(csprojPath);
                foreach (string generatedTestClass in testClasses)
                {
                    p.AddItem("Compile", generatedTestClass);
                }
                p.Save();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}

            MessageBox.Show("Unit test project created. Please install the NUnit3TestAdapter nuget " +
                    "package manually on the created project, reload/build solution and run the generated unit tests " +
                    "in the Unit Test Explorer window.", "Unit Test setup done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string GetSelectedProjectName(DTE2 dte)
        {
            var selectedItems = dte.SelectedItems;
            var selectedProjectName = selectedItems.Item(1).Name;

            return selectedProjectName;
        }

        public string CreateUnitTestsProject(DTE2 dte, string unitTestProjectName)
        {
            try
            {
                CreateUnitTestProject(dte, unitTestProjectName);
                
                string packagesPath = GetSolutionPackagesFolder(dte);
                //InstallNeededNugetPackage(packagesPath);

                AddNeededReferencesToProject(dte, unitTestProjectName, packagesPath);
                var projectPath = string.Format(@"{0}\\{1}", GetSolutionPath(dte), unitTestProjectName);
                return projectPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex.Message);
                throw ex;
            }
        }

        private string GetSolutionPackagesFolder(DTE2 dte)
        {
            string packagesPath = string.Format(@"{0}\\{1}", GetSolutionPath(dte), "packages");
            return packagesPath;
        }

        private void AddNeededReferencesToProject(DTE2 dte, string projectName, string packagesPath)
        {
            // get the current solution
            Solution2 currentSolution = (Solution2)dte.Solution;

            foreach (Project proj in currentSolution.Projects)
            {
                if (proj.Name == projectName)
                {
                    var vsProject = proj.Object as VSLangProj.VSProject;
                    vsProject.References.Add(string.Format("{0}\\{1}",
                        packagesPath,
                        "NUnit.3.10.1\\lib\\net45\\nunit.framework.dll"));

                    vsProject.References.Add(string.Format("{0}\\{1}",
                        packagesPath,
                        "Moq.4.10.0\\lib\\net45\\Moq.dll")); // TODO: use regular expressions here for versioning

                    //var systemCoreDllPath = typeof(System.Linq.Enumerable).Assembly.Location;
                    //vsProject.References.Add(systemCoreDllPath);
                    

                    foreach (Project project in currentSolution.Projects)
                    {
                        if (project.Name != projectName)
                        {
                            var projectOutputPath = string.Format("{0}\\{1}\\bin\\Debug\\{2}{3}",
                                GetSolutionPath(dte), project.Name, project.Name, ".dll");

                            if (File.Exists(projectOutputPath))
                            {
                                vsProject.References.Add(projectOutputPath);
                            }
                            else
                            {
                                MessageBox.Show("Please build the selected project and then create the unit test project.", 
                                    "Build selected project", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                // stop execution
                                Environment.Exit(0);
                            }
                        }
                    }
                }
            }
        }

        private void CreateUnitTestProject(DTE2 dte, string projectName)
        {
            // get the current solution
            Solution2 currentSolution = (Solution2)dte.Solution;
            string currentSolutionPath = string.Format("{0}\\{1}", GetSolutionPath(dte), projectName);
            string projTemplate = currentSolution.GetProjectTemplate("csClassLibrary.vstemplate|FrameworkVersion=4.6.1", "CSharp");

            // create a new C# class library project using the template obtained above.
            currentSolution.AddFromTemplate(projTemplate, currentSolutionPath, projectName, false);
            currentSolution.SaveAs(currentSolution.FullName);
        }

        private string GetSolutionPath(DTE2 dte)
        {
            Solution2 currentSolution = (Solution2)dte.Solution;

            string currentSolutionPath = currentSolution.FileName;
            int index = currentSolution.FileName.LastIndexOf("\\");
            currentSolutionPath = currentSolutionPath.Substring(0, index);

            return currentSolutionPath;
        }

        private string GetSolutionFullPath(DTE2 dte)
        {
            Solution2 currentSolution = (Solution2)dte.Solution;
            return currentSolution.FileName;
        }

        private void InstallNeededNugetPackage(string installPath)
        {
            try
            {
                string NunitPackageID = "NUnit";
                //string MoqPackageID = "Moq";

                IPackageRepository repo = PackageRepositoryFactory.Default
                    .CreateRepository("https://packages.nuget.org/api/v2");

                PackageManager packageManager = new PackageManager(repo, installPath);

                var nunitFrameworkDllFilePath = string.Format("{0}\\{1}", installPath,
                    "NUnit.3.9.0\\lib\\net45\\nunit.framework.dll");

                if (!File.Exists(nunitFrameworkDllFilePath))
                {
                    packageManager.InstallPackage(NunitPackageID);
                }

                //var moqFilePath = string.Format("{0}\\{1}", installPath,
                //        "Moq.4.8.1\\lib\\net45\\Moq.dll"); // TODO: use regular expressions for versioning
                //if (!File.Exists(moqFilePath))
                //{
                //    packageManager.InstallPackage(MoqPackageID);
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
