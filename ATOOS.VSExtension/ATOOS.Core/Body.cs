﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATOOS.VSExtension.ATOOS.Core
{
    public class Body
    {
        public Body()
        {
            Statements = new List<Statement>();
        }
        public List<Statement> Statements { get; set; }
        public int NumberOfLines { get; set; }
    }
}
