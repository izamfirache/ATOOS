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
        public MainWindow()
        {
            InitializeComponent();
            solutionPath.Text = @"";
        }

        private void AnalyzeSolution_Click(object sender, RoutedEventArgs e)
        {
            var watch = new Stopwatch();
            watch.Start();

            List<Class> discoveredClasses = new List<Class>();
            var projectAnalyzer = new CodeAnalyzer.SolutionAnalyzer(solutionPath.Text);

            if (!string.IsNullOrEmpty(solutionPath.Text))
            {
                var analyzedSolution = projectAnalyzer.AnalyzeSolution();
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
            MessageBox.Show("Done!");
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
            
            if (_factory._instances.Count != 0)
            {
                var unitTestGenerator = new UnitTestGenerator.Core.UnitTestGenerator(unitTestDirectory, _factory);
                unitTestGenerator.GenerateUnitTestsForClass(solutionPath.Text);

                MessageBox.Show("Done. Check the UnitTests folder.");
            }
            else
            {
                MessageBox.Show("Press the Discover solution types button first.");
            }

            // runt the generated unit tests and display the result in resultBox
        }
    }
}
