using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;

namespace DependencyResolver
{
    public class UnityDependencyResolver
    {
        private UnityContainer _unityContainer;
        public UnityDependencyResolver(UnityContainer unityContainer)
        {
            _unityContainer = unityContainer;
        }

        public object ResolveCustomType(string typeToResolve)
        {
            return _unityContainer.Resolve(Type.GetType(typeToResolve));
        }
    }
}
