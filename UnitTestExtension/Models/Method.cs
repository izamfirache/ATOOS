﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestExtension.Models
{
    public class Method
    {
        public Method()
        {
            Parameters = new List<MethodParameter>();
        }

        public string Name { get; set; }
        public string ReturnType { get; set; }
        public string Accessor { get; set; }
        public List<MethodParameter> Parameters { get; set; }
        public Body MethodBody { get; set; }
    }
}
