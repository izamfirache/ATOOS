using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATOOS.Models
{
    public class Class
    {
        public Class()
        {
            Methods = new List<Method>();
            Attributes = new List<Atribute>();
        }

        public Constructor Constructor { get; set; }
        public string Name { get; set; }
        public List<Method> Methods { get; set; }
        public List<Atribute> Attributes { get; set; }
    }
}
