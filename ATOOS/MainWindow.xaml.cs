using ATOOS.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using CodeAnalyzer;
using ObjectFactory;
using DynamicInvoke.Helpers;
using UnitTestGenerator.Core;
using System.IO;

namespace ATOOS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Factory _factory = new Factory();
        private SolutionAnalyzer solutionAnalyzer;
        private AnalyzedSolution analyzedSolution;

        public MainWindow()
        {
            InitializeComponent();
            solutionPath.Text = @"c:\dev\TestProject1\TestProject1.sln";
            solutionAnalyzer = new SolutionAnalyzer(solutionPath.Text);
        }

        private void AnalyzeSolution_Click(object sender, RoutedEventArgs e)
        {
            var watch = new Stopwatch();
            watch.Start();

            List<Class> discoveredClasses = new List<Class>();

            if (!string.IsNullOrEmpty(solutionPath.Text))
            {
                analyzedSolution = solutionAnalyzer.AnalyzeSolution();
                foreach (AnalyzedProject proj in analyzedSolution.Projects)
                {
                    resultBox.AppendText(string.Format("Project OutputPath: {0}\r", proj.OutputFilePath));
                    resultBox.AppendText(string.Format("Project name: {0}\r {1}", proj.Name, "{"));
                    foreach (var c in proj.Classes)
                    {
                        resultBox.AppendText(Environment.NewLine);
                        resultBox.AppendText(string.Format("\tpublic class {0} {1}\r", c.Name, '{'));

                        // constructor
                        var constructorSignature = "(";
                        if (c.Constructor != null && c.Constructor.Parameters.Count != 0)
                        {
                            var index = 0;
                            foreach (var cp in c.Constructor.Parameters)
                            {
                                index++;
                                constructorSignature += cp.Type + ' ' + cp.Name;
                                if (index != c.Constructor.Parameters.Count)
                                {
                                    constructorSignature += ',';
                                }
                                else
                                {
                                    constructorSignature += ')';
                                }
                            }
                        }
                        else
                        {
                            constructorSignature += ")";
                        }
                        resultBox.AppendText(string.Format("\t\t public {0}{1};\r", c.Name, constructorSignature));

                        // methods
                        foreach (var m in c.Methods)
                        {
                            var methodSignature = "(";
                            if (m.Parameters.Count != 0)
                            {
                                var index = 0;
                                foreach (var param in m.Parameters)
                                {
                                    index++;
                                    methodSignature += param.Type + ' ' + param.Name;
                                    if (index != m.Parameters.Count)
                                    {
                                        methodSignature += ',';
                                    }
                                    else
                                    {
                                        methodSignature += ')';
                                    }
                                }
                            }
                            else
                            {
                                methodSignature += ')';
                            }
                            resultBox.AppendText(string.Format("\t\t {0} {1} {2}{3}; \r", m.Accessor, m.ReturnType, m.Name, methodSignature));
                        }

                        // attributes
                        foreach (var a in c.Attributes)
                        {
                            resultBox.AppendText(string.Format("\t\t {0} {1} {2}; \r", a.Accessor, a.Type, a.Name));
                        }
                        resultBox.AppendText("\t}\r");
                    }
                    resultBox.AppendText("}\r");
                    resultBox.AppendText("------------------------------------\r");
                }
            }
            else
            {
                MessageBox.Show("Please provide the path to solution and the project name you want to analyze.");
            }

            watch.Stop();
            var time = watch.Elapsed.TotalSeconds;

            resultBox.AppendText(Environment.NewLine);
            resultBox.AppendText(string.Format("Analyze time: {0} seconds.", time.ToString()));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            resultBox.Document.Blocks.Clear();
            resolveTypeResult.Document.Blocks.Clear();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // discover all solution type and register them in the unity container
            _factory.DiscoverAllSolutionTypes(solutionPath.Text);
            MessageBox.Show("Done. The instances factory is created!");
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (_factory._instances.Count != 0)
            {
                // resolve type -- it will be shown in json format
                _factory._instances.TryGetValue(resolveTypeName.Text, out object instance);
                var jsonObj = JsonConvert.SerializeObject(instance, Formatting.None);
                resolveTypeResult.AppendText(jsonObj);
            }
            else
            {
                MessageBox.Show("Press the Discover solution types button first.");
            }
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            // invoke the specified method and display the result
            if (_factory._instances.Count != 0)
            {
                var invokeFunctionHelper = new InvokeFunctionHelper(_factory);
                var result = invokeFunctionHelper.DynamicallyInvokeFunction(solutionPath.Text,
                    invokeFuntionTypeName.Text, invokeFunctionMethodName.Text);

                invokeFunctionResult.AppendText(JsonConvert.SerializeObject(result, Formatting.None));
            }
            else
            {
                MessageBox.Show("Press the Discover solution types button first.");
            }
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            invokeFunctionResult.Document.Blocks.Clear();
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            var unitTestDirectory = @"";
            DirectoryInfo dir = new DirectoryInfo(unitTestDirectory);
            foreach (FileInfo file in dir.GetFiles())
            {
                if (!file.Name.Contains("nunit"))
                {
                    file.Delete();
                }
            }
            bool areUnitTestsGenerated = false;

            if (_factory._instances.Count != 0)
            {
                var unitTestGenerator = new UnitTestGenerator.Core.UnitTestGenerator(unitTestDirectory, _factory);
                unitTestGenerator.GenerateUnitTestsForClass(solutionPath.Text);
                areUnitTestsGenerated = true;
                MessageBox.Show("Done. The unit tests was generated.");
            }
            else
            {
                MessageBox.Show("Press the Discover solution types button first.");
            }

            // copy the project assembly into UnitTestDirectory
            // in order for the tested typesto be visible
            if (analyzeSolution == null)
            {
                solutionAnalyzer.AnalyzeSolution();
            }
            solutionAnalyzer.CopyAllProjAssembliesIntoUnitTestsFolder(unitTestDirectory);

            // run the generated unit tests and display the result in resultBox
            if (areUnitTestsGenerated)
            {
                // execute the unit tests and display the results
                DirectoryInfo di = new DirectoryInfo(unitTestDirectory);
                List<string> testedDlls = new List<string>();
                foreach (FileInfo file in di.GetFiles())
                {
                    if (file.Name.Contains(".dll") && file.Name.Contains("_Tests"))
                    {
                        testedDlls.Add(file.Name);
                    }
                }
                RunUnitTestsForDll(testedDlls, unitTestDirectory);
            }
        }

        private void RunUnitTestsForDll(List<string> testedDlls, string unitTestDirectory)
        {
            var testedDllsArguments = "";
            foreach(string dll in testedDlls)
            {
                testedDllsArguments += string.Format(@"{0}\{1}", unitTestDirectory, dll) + " ";
            }

            var runUnitTestsProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = string.Format(@"{0}\nunit-console\{1}", unitTestDirectory, "nunit3-console.exe"),
                    Arguments = testedDllsArguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            try
            {
                // Start the process with the info we specified and display the result.
                runUnitTestsProcess.Start();
                runUnitTestsProcess.WaitForExit();

                string resultText = runUnitTestsProcess.StandardOutput.ReadToEnd();
                resultBox.AppendText(resultText);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                resultBox.AppendText(e.Message);
            }
        }
    }
}
