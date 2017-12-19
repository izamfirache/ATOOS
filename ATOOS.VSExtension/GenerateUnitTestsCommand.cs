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

namespace ATOOS.VSExtension
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class GenerateUnitTestsCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("4210003b-22d4-4404-bca8-38abaa597829");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateUnitTestsCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
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

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static GenerateUnitTestsCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new GenerateUnitTestsCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var dte = (DTE2)ServiceProvider.GetService(typeof(DTE));
            var unitTestProjectName = string.Format("{0}{1}", "NewProject_Tests", new Random().Next(0, 10000));

            // discover solution/project types and create an object factory
            Factory objectFactory = new Factory();
            var solutionFullPath = GetSolutionFullPath(dte);
            objectFactory.DiscoverAllSolutionTypes(solutionFullPath);

            // generate unit tests for the analyzed project
            if (objectFactory.Instances.Count != 0)
            {
                // create an empty Unit Test project and return a path to the created project
                var generatedTestClassesDirectory = CreateUnitTestsProject(dte, unitTestProjectName);
                string packagesPath = GetSolutionPackagesFolder(dte);

                var unitTestGenerator = new UnitTestGenerator(generatedTestClassesDirectory, objectFactory, packagesPath);
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

                MessageBox.Show("Done. The unit tests was generated.",
                        "Unit tests generated", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
            }
        }

        public string CreateUnitTestsProject(DTE2 dte, string unitTestProjectName)
        {
            try
            {
                CreateUnitTestProject(dte, unitTestProjectName);
                
                string packagesPath = GetSolutionPackagesFolder(dte);
                InstallNUnitNugetPackages(packagesPath);
                AddNeededReferencesToProject(dte, unitTestProjectName, packagesPath);

                MessageBox.Show("Unit test project created. Please install the NUnit3TestAdapter nuget " + 
                        "package manually on the created project and run the generated unit tests.",
                        "Setup done", MessageBoxButtons.OK, MessageBoxIcon.Information);

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
                        "NUnit.3.9.0\\lib\\net45\\nunit.framework.dll"));
                    foreach (Project project in currentSolution.Projects)
                    {
                        if (project.Name != projectName)
                        {
                            var projectOutputPath = string.Format("{0}\\{1}\\bin\\Debug\\{2}{3}",
                                GetSolutionPath(dte), project.Name, project.Name, ".dll");
                            vsProject.References.Add(projectOutputPath);
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

            // TODO: find this based on runtime context !!!!
            string csTemplatePath = @"";

            // create a new C# console project using the template obtained above.
            currentSolution.AddFromTemplate(csTemplatePath, currentSolutionPath, projectName, false);
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

        private void InstallNUnitNugetPackages(string installPath)
        {
            try
            {
                string NunitPackageID = "NUnit";

                IPackageRepository repo = PackageRepositoryFactory.Default
                    .CreateRepository("https://packages.nuget.org/api/v2");

                PackageManager packageManager = new PackageManager(repo, installPath);
                packageManager.InstallPackage(NunitPackageID);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
