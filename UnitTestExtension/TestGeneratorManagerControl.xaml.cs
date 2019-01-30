namespace UnitTestExtension
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using UnitTestExtension.Models;
    using UnitTestExtension.ViewModel;

    /// <summary>
    /// Interaction logic for TestGeneratorManagerControl.
    /// </summary>
    public partial class TestGeneratorManagerControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestGeneratorManagerControl"/> class.
        /// </summary>
        public TestGeneratorManagerControl()
        {
            this.InitializeComponent();
            var unitTestManager = new TestGeneratorVM();
            solutionExplorer.ItemsSource = unitTestManager.DiscoverProjectClasses();
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            var unitTestManager = new TestGeneratorVM();
            List<string> selectedClasses = new List<string>();

            foreach(var item in solutionExplorer.Items)
            {
                var project = (ProjectObj)item;
                foreach(ProjectClass projectClass in project.Classes)
                {
                    if (projectClass.IsSelected)
                    {
                        selectedClasses.Add(projectClass.Name);
                    }
                }
            }

            if(selectedClasses.Count() > 0)
            {
                unitTestManager.GenerateUnitTestsLogic(selectedClasses);
            }
        }
    }
}