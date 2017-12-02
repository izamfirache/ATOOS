using ATOOS.Core.Models;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ObjectFactory
{
    public class Factory
    {
        public Dictionary<string, object> _instances = new Dictionary<string, object>();
        private Type[] _assemblyExportedTypes = new Type[100];

        public void DiscoverAllSolutionTypes(string solutionPath)
        {
            // discover all solution projects
            var solutionAnalyzer = new CodeAnalyzer.SolutionAnalyzer(solutionPath);
            var analyedSolution = solutionAnalyzer.AnalyzeSolution();

            foreach (AnalyzedProject proj in analyedSolution.Projects)
            {
                var assembly = Assembly.LoadFile(proj.OutputFilePath);
                _assemblyExportedTypes = assembly.GetExportedTypes();

                foreach (Class cls in proj.Classes)
                {
                    Type classType = null;
                    foreach (Type type in _assemblyExportedTypes)
                    {
                        if (type.Name == cls.Name)
                        {
                            classType = type;
                            break;
                        }
                    }

                    if (classType != null)
                    {
                        if (cls.Constructor != null)
                        {
                            // having the constructor signature, create a new instance of that object
                            var classInstance = CreateNewInstance(cls.Constructor, classType, proj.Classes);
                            _instances.Add(cls.Name, classInstance);
                        }
                        else
                        {
                            // no constructor
                            var classInstance = CreateDefaultInstance(classType);
                            _instances.Add(cls.Name, classInstance);
                        }
                    }
                }
            }
        }
        private object CreateDefaultInstance(Type classType)
        {
            Object instance = Activator.CreateInstance(classType);
            return instance;
        }
        private object CreateNewInstance(Constructor constructor, Type classType, List<Class> discoveredClasses)
        {
            List<object> parameters = new List<object>();
            int index = 0;
            foreach(MethodParameter mp in constructor.Parameters)
            {
                switch (mp.Type)
                {
                    case "string":
                        string str = GetRandomString();
                        parameters.Add(str);
                        break;
                    case "int":
                        int nr = GetRandomInteger();
                        parameters.Add(nr);
                        break;
                    default:
                        // find the type in the discoveredClasses
                        Class cls = discoveredClasses.Where(c => c.Name == mp.Type).FirstOrDefault();

                        // if found, resolve the type
                        if (cls != null)
                        {
                            Type clsType = _assemblyExportedTypes.Where(t => t.Name == cls.Name).FirstOrDefault();
                            if (cls.Constructor != null)
                            {
                                var customType = CreateNewInstance(cls.Constructor, clsType, discoveredClasses);
                                parameters.Add(customType);
                            }
                            else
                            {
                                var customType = CreateDefaultInstance(clsType);
                                parameters.Add(customType);
                            }
                        }
                        break;
                }
                index++;
            }
            object instance = Activator.CreateInstance(classType, parameters.ToArray());
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
