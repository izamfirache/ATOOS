using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATOOS.VSExtension.ATOOS.Core
{
    public class Class
    {
        public Class()
        {
            Methods = new List<Method>();
            Attributes = new List<Attribute>();
        }

        public Constructor Constructor { get; set; }
        public string Name { get; set; }
        public List<Method> Methods { get; set; }
        public List<Attribute> Attributes { get; set; }
    }
}
