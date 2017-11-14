using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ATOOS.Core.Models
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
