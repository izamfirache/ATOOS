using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestExtension.Models
{
    public class MethodParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public int Position { get; set; } // parameter position in method's signature
    }
}
