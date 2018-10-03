using ATOOS.VSExtension.ATOOS.Core;
using ATOOS.VSExtension.TestGenerator;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using NuGet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace ATOOS.VSExtension
{
    public class GenerateUnitTestsBLL
    {
        public void GenerateUnitTestsLogic(List<string> projClasses)
        {
            //List<string> projClasses = DiscoverProjectClasses();
            //try
            //{
            var dte = (DTE2)Microsoft.VisualStudio.Shell.ServiceProvider
                    .GlobalProvider.GetService(typeof(EnvDTE.DTE));
            var selectedProjectName = GetSelectedProjectName(dte);
            var unitTestProjectName = string.Format("{0}_Tests", selectedProjectName);

            // discover solution/project types and create an object factory
            var solutionFullPath = GetSolutionFullPath(dte);

            // generate unit tests for the analyzed project
            var generatedTestClassesDirectory = CreateUnitTestsProject(dte, unitTestProjectName);
            string packagesPath = GetSolutionPackagesFolder(dte);

            var unitTestGenerator = new UnitTestGenerator(generatedTestClassesDirectory,
                packagesPath, selectedProjectName);
            List<string> testClasses = 
                unitTestGenerator.GenerateUnitTestsForClass(solutionFullPath, 
                unitTestProjectName, projClasses);

            // add test classes to project
            string csprojPath = string.Format(@"{0}\\{1}\\{2}{3}", GetSolutionPath(dte),
                unitTestProjectName, unitTestProjectName, ".csproj");

            var p = new Microsoft.Build.Evaluation.Project(csprojPath);
            foreach (string generatedTestClass in testClasses)
            {
                p.AddItem("Compile", generatedTestClass);
            }
            p.Save();
        }

        public List<ProjectObj> DiscoverProjectClasses()
        {
            var projects = new List<ProjectObj>();

            // get the DTE reference...
            var dte = (DTE2)Microsoft.VisualStudio.Shell.ServiceProvider
                    .GlobalProvider.GetService(typeof(EnvDTE.DTE));

            // get the solution
            Solution solution = dte.Solution;
            Console.WriteLine(solution.FullName);

            // get all the projects
            foreach (Project project in solution.Projects)
            {
                var proj = new ProjectObj()
                {
                    Name = project.Name
                };

                // get all the items in each project
                foreach (ProjectItem item in project.ProjectItems)
                {
                    FileCodeModel2 model = (FileCodeModel2)item.FileCodeModel;
                    if (model != null)
                    {
                        foreach (CodeElement codeElement in model.CodeElements)
                        {
                            if (codeElement.Kind == vsCMElement.vsCMElementNamespace)
                            {
                                foreach (CodeElement ce in codeElement.Children)
                                {
                                    if (ce.Kind == vsCMElement.vsCMElementClass)
                                    {
                                        proj.Classes.Add(new ProjectClass() { Name = item.Name.Replace(".cs", "") });
                                    }
                                }
                            }
                        }
                    }
                }
                projects.Add(proj);
            }

            return projects;
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
                System.Windows.Forms.MessageBox.Show("ERROR: " + ex.Message);
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
                                System.Windows.Forms.MessageBox.Show("Please build the selected project and then create the unit test project.",
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
                System.Windows.Forms.MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
