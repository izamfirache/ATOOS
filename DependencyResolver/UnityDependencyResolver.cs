using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependencyResolver
{
    public class UnityDependencyResolver
    {
        public object ResolveCustomType(string typeID)
        {
            // get the type definition based on typeID
            // get random values for all the parameters from it's constructor
            // by invoking ResolveCustomParameter() or ResolvePrimitiveParameter()
            // using Unity, create a new instance of that type with the above generated parameters
            // using Unity implies that all the types discovered by SolutionAnalyzer 
            // should be registered somwhere (maybe after Analyze solution phase)

            throw new NotImplementedException();
        }

        private object ResolveCustomParameter(string typeID)
        {
            throw new NotImplementedException();
        }

        private object ResolvePrimitiveParameter()
        {
            throw new NotImplementedException();
        }
    }
}
