using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE80;
using EnvDTE;
using System.Windows.Forms;
using NuGet;

namespace AddNewProjectToSolution.Extension
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class AddNewProjectToSolutionCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("607c9bd0-1fcc-44dd-94f8-862aff287f9e");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddNewProjectToSolutionCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private AddNewProjectToSolutionCommand(Package package)
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
        public static AddNewProjectToSolutionCommand Instance
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
            Instance = new AddNewProjectToSolutionCommand(package);
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
            AddUnitTestProjectToSolution(dte);
        }

        public void AddUnitTestProjectToSolution(DTE2 dte)
        {
            try
            {
                var unitTestProjectName = string.Format("{0}{1}", "NewProject_Tests", new Random().Next(0, 10000));
                
                CreateUnitTestProject(dte, unitTestProjectName);

                AddClassToUnitTestsProject(dte, unitTestProjectName);

                string packagesPath = string.Format(@"{0}\\{1}", GetSolutionPath(dte), "packages");
                InstallNUnitNugetPackages(packagesPath);

                AddNeededReferencesToProject(dte, unitTestProjectName, packagesPath);

                MessageBox.Show("Unit test project created. Please install the NUnit3TestAdapter nuget package manually on the created project and run the generated unit tests.",
                        "Setup done", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex.Message);
            }
        }

        private void AddNeededReferencesToProject(DTE2 dte, string projectName, string packagesPath)
        {
            // get the current solution
            Solution2 currentSolution = (Solution2)dte.Solution;

            foreach(Project proj in currentSolution.Projects)
            {
                if(proj.Name == projectName)
                {
                    var vsProject = proj.Object as VSLangProj.VSProject;
                    vsProject.References.Add(string.Format("{0}\\{1}", 
                        packagesPath, 
                        "NUnit.3.9.0\\lib\\net45\\nunit.framework.dll"));
                }
            }
        }

        private void AddClassToUnitTestsProject(DTE2 dte, string unitTestProjectName)
        {
            // get the current solution
            Solution2 currentSolution = (Solution2)dte.Solution;

            // Point to the first specified project
            Project prj = null;
            foreach (Project project in currentSolution.Projects)
            {
                if(project.Name == unitTestProjectName)
                {
                    prj = project;
                }
            }

            // Retrieve the path to the class template.
            string itemPath = currentSolution.GetProjectItemTemplate("Class.zip", "csproj");

            if(prj != null)
            {
                //Create a new project item based on the template, in this case, a Class.
                ProjectItem prjItem = prj.ProjectItems.AddFromTemplate(itemPath, "NewUnitTestClass.cs");
            }
        }

        private void CreateUnitTestProject(DTE2 dte, string projectName)
        {
            // get the current solution
            Solution2 currentSolution = (Solution2)dte.Solution;
            string currentSolutionPath = string.Format("{0}\\{1}", GetSolutionPath(dte), projectName);
            
            // TODO: find this based on runtime context !!!!
            string csTemplatePath = @"c:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\ProjectTemplatesCache\CSharp\Windows Root\Windows\1033\ClassLibrary\csClassLibrary.vstemplate";
            
            // create a new C# console project using the template obtained above.
            currentSolution.AddFromTemplate(csTemplatePath, currentSolutionPath, projectName, false);
        }

        private string GetSolutionPath(DTE2 dte)
        {
            Solution2 currentSolution = (Solution2)dte.Solution;

            string currentSolutionPath = currentSolution.FileName;
            int index = currentSolution.FileName.LastIndexOf("\\");
            currentSolutionPath = currentSolutionPath.Substring(0, index);

            return currentSolutionPath;
        }

        private void InstallNUnitNugetPackages(string installPath)
        {
            try
            {
                string NunitPackageID = "NUnit";
                //string NunitConsolePackageID = "NUnit.Console";
                //string NunitAdapterPackageID = "NUnit3TestAdapter";

                IPackageRepository repo = PackageRepositoryFactory.Default
                    .CreateRepository("https://packages.nuget.org/api/v2");
                
                PackageManager packageManager = new PackageManager(repo, installPath);
                packageManager.InstallPackage(NunitPackageID);
                //packageManager.InstallPackage(NunitConsolePackageID);
                //packageManager.InstallPackage(NunitAdapterPackageID);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
