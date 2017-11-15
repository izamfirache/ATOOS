using ATOOS.Core.Models;
using SolutionAnalyzer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;

namespace DependencyResolver
{
    public class Resolver
    {
        public UnityContainer DiscoverAllSolutionTypes(string solutionPath, string projectName)
        {
            UnityContainer unityContainer = new UnityContainer();

            // discover all solution classes
            var solutionAnalyzer = new ProjectAnalyzer(solutionPath, projectName);
            var discoveredClasses = solutionAnalyzer.AnalyzeProject();

            foreach (Class cls in discoveredClasses)
            {
                Type classType = Type.GetType(cls.Name);

                // having the constructor signature, create a new instance of that object
                var classInstance = CreateNewInstance(cls.Constructor, classType, discoveredClasses);

                // register the type in the unity container
                unityContainer.RegisterInstance(classType, cls.Name, classInstance);
            }

            return unityContainer;
        }

        private object CreateNewInstance(Constructor constructor, Type classType, List<Class> discoveredClasses)
        {
            Object[] parameters = new Object[constructor.Parameters.Count];
            int index = 0;
            foreach(MethodParameter mp in constructor.Parameters)
            {
                switch (mp.Type)
                {
                    case "String":
                        string str = GetRandomString();
                        parameters[index] = str;
                        break;
                    case "int":
                        int nr = GetRandomInteger();
                        parameters[index] = nr;
                        break;
                    default:
                        // find the type in the discoveredClasses
                        Class cls = discoveredClasses.Where(c => c.Name == mp.Name).FirstOrDefault();

                        // if found, resolve the type
                        if (cls != null)
                        {
                            Type clsType = Type.GetType(cls.Name);
                            var customType = CreateNewInstance(cls.Constructor, clsType, discoveredClasses);
                            parameters[index] = customType;
                        }
                        break;
                }
            }
            Object instance = Activator.CreateInstance(classType, parameters);
            return instance;
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
