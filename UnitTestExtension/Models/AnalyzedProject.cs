using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestExtension.Models
{
    public class AnalyzedProject
    {
        public AnalyzedProject()
        {
            Classes = new List<Class>();
        }

        public List<Class> Classes { get; set; }
        public string OutputFilePath { get; set; }
        public string Name { get; set; }
    }
}
