﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestExtension.Models
{
    public class ProjectObj
    {
        public ProjectObj()
        {
            Classes = new List<ProjectClass>();
        }
        public string Name { get; set; }
        public List<ProjectClass> Classes { get; set; }
    }
}
