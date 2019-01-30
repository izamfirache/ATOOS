using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnitTestExtension.CodeAnalyzer;
using UnitTestExtension.Models;
using UnitTestExtension.ObjectFactory;

namespace DynamicInvoke.Helpers
{
    public class InvokeFunctionHelper
    {
        private Factory _objectFactory;
        public InvokeFunctionHelper(Factory objectFactory)
        {
            _objectFactory = objectFactory;
        }

        public object DynamicallyInvokeFunction(string solutionPath, string typeName, string methodName)
        {
            // discover all solution projects
            var solutionAnalyzer = new SolutionAnalyzer(solutionPath);
            var analyedSolution = solutionAnalyzer.AnalyzeSolution();

            foreach (AnalyzedProject proj in analyedSolution.Projects)
            {
                var assembly = Assembly.LoadFile(proj.OutputFilePath);
                var assemblyExportedTypes = assembly.GetExportedTypes();
                
                foreach (Type type in assemblyExportedTypes)
                {
                    if (type.Name == typeName)
                    {
                        var methods = type.GetMethods();
                        foreach(MethodInfo m in methods)
                        {
                            if(m.Name == methodName)
                            {
                                // generate method parameters
                                var methodParameters = m.GetParameters();
                                var parameters = new List<object>();
                                foreach(ParameterInfo p in methodParameters)
                                {
                                    var instance = ResolveParameter(p.ParameterType.Name);
                                    parameters.Add(instance);
                                }

                                // invoke the function
                                _objectFactory.Instances.TryGetValue(typeName, out object objectInstance);
                                var result = m.Invoke(objectInstance, parameters.ToArray());
                                return result;
                            }
                        }
                    }
                }
            }

            return "Method not found";
        }

        private object ResolveParameter(string typeToResolve)
        {
            switch (typeToResolve)
            {
                case "String":
                    return GetRandomString();
                case "Int32":
                    return GetRandomInteger();
                default: return null;
            }
        }

        private int GetRandomInteger()
        {
            Random rnd = new Random();
            return rnd.Next(0, 10000);
        }

        private string GetRandomString()
        {
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
            var builder = new StringBuilder();
            Random rnd = new Random();

            for (var i = 0; i < 10; i++) // harcoded length for now
            {
                var c = pool[rnd.Next(0, pool.Length)];
                builder.Append(c);
            }
            return builder.ToString();
        }
    }
}
