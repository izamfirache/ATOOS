using ATOOS.Core.Models;
using DependencyResolver;
using Newtonsoft.Json;
using SolutionAnalyzer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace ATOOS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Resolver _resolver = new Resolver();
        public MainWindow()
        {
            InitializeComponent();
            solutionPath.Text = @"c:\dev\TestProject\TestProject.sln";
            projectName.Text = "TestProject";
        }

        private void AnalyzeSolution_Click(object sender, RoutedEventArgs e)
        {
            var watch = new Stopwatch();
            watch.Start();

            List<Class> discoveredClasses = new List<Class>();
            var projectAnalyzer = new ProjectAnalyzer(solutionPath.Text, projectName.Text);

            if (!string.IsNullOrEmpty(solutionPath.Text) && !string.IsNullOrEmpty(projectName.Text))
            {
                var classes = projectAnalyzer.AnalyzeProject();
                foreach (var c in classes)
                {
                    resultBox.AppendText(Environment.NewLine);
                    resultBox.AppendText(string.Format("public class {0} {1}\r", c.Name, '{'));

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
                    resultBox.AppendText(string.Format("\t public {0}{1};\r", c.Name, constructorSignature));

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
                        resultBox.AppendText(string.Format("\t {0} {1} {2}{3}; \r", m.Accessor, m.ReturnType, m.Name, methodSignature));
                    }

                    // attributes
                    foreach (var a in c.Attributes)
                    {
                        resultBox.AppendText(string.Format("\t {0} {1} {2}; \r", a.Accessor, a.Type, a.Name));
                    }
                    resultBox.AppendText("}");
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
            _resolver.DiscoverAllSolutionTypes(solutionPath.Text, projectName.Text);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            // resolve type -- it will be shown in json format
            _resolver._instances.TryGetValue(resolveTypeName.Text, out object instance);
            resolveTypeResult.AppendText(JsonConvert.SerializeObject(instance, Formatting.Indented));
        }
    }
}
