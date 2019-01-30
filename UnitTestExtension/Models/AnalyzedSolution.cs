﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestExtension.Models
{
    public class AnalyzedSolution
    {
        public AnalyzedSolution()
        {
            Projects = new List<AnalyzedProject>();
        }

        public List<AnalyzedProject> Projects { get; set; }
        public string Name { get; set; }
    }
}
