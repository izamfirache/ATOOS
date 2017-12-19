using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATOOS.VSExtension.ATOOS.Core
{
    public class Constructor
    {
        public Constructor()
        {
            Parameters = new List<MethodParameter>();
        }

        public List<MethodParameter> Parameters { get; set; }
    }
}
