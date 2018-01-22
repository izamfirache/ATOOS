using System.Collections.Generic;

namespace ATOOS.Core.Models
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