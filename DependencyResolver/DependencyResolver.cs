using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;

namespace DependencyResolver
{
    public class DependencyResolver
    {
        public UnityContainer DiscoverAllSolutionTypes()
        {
            var unityContainer = new UnityContainer();

            // after SolutionAnalyzer process is done
            // get all the type signatures from the database
            // using UnityContainer, register all types in order to be resolved in the future

            return unityContainer;
        }
    }
}
