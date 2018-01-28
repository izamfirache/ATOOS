using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ATOOS.VSExtension.InputGenerators
{
    public class InputParamGenerator
    {
        private Type[] assemblyExportedTypes;

        public InputParamGenerator(Type[] assemblyExportedTypes)
        {
            this.assemblyExportedTypes = assemblyExportedTypes;
        }

        public object ResolveParameter(string typeToResolve)
        {
            switch (typeToResolve)
            {
                case "String":
                    return GetRandomString();
                case "Int32":
                    return GetRandomInteger();
                default: throw new Exception(string.Format("Can not resolve type : ", typeToResolve));
            }
        }

        public CodeExpression[] ResolveInputParametersForCtorOrMethod(int paramsArrayLength,
            ParameterInfo[] parameters, CodeMemberMethod testMethod)
        {
            CodeExpression[] ctorParams = new CodeExpression[paramsArrayLength];
            var j = 0;
            foreach (ParameterInfo pi in parameters)
            {
                if (pi.ParameterType.Name == "String" || pi.ParameterType.Name == "Int32")
                {
                    ctorParams[j] = new CodePrimitiveExpression(ResolveParameter(pi.ParameterType.Name));
                }
                else
                {
                    CodeObjectCreateExpression createObjectExpression = CreateCustomType(pi.ParameterType.Name);

                    // declare the resolved parameter
                    CodeVariableDeclarationStatement assignResolveCustomParamToVariable =
                        new CodeVariableDeclarationStatement(
                            pi.ParameterType, pi.ParameterType.Name.ToLower(), createObjectExpression);

                    testMethod.Statements.Add(assignResolveCustomParamToVariable);

                    ctorParams[j] = new CodeVariableReferenceExpression(pi.ParameterType.Name.ToLower());
                }
                j++;
            }

            return ctorParams;
        }

        public CodeObjectCreateExpression CreateCustomType(string typeToResolve)
        {
            Type typeToResolveInfo = assemblyExportedTypes
                .Where(aet => aet.Name == typeToResolve).FirstOrDefault();

            if (typeToResolveInfo != null)
            {
                if (typeToResolveInfo.IsClass)
                {
                    var typeToResolveConstructor = typeToResolveInfo.GetConstructors()
                        .Where(c => c.GetParameters().Length != 0).FirstOrDefault();

                    CodeExpression[] ctorParams = new CodeExpression[typeToResolveConstructor.GetParameters().Length];
                    var j = 0;
                    foreach (ParameterInfo pi in typeToResolveConstructor.GetParameters())
                    {
                        if (pi.ParameterType.Name == "String" || pi.ParameterType.Name == "Int32")
                        {
                            ctorParams[j] = new CodePrimitiveExpression(ResolveParameter(pi.ParameterType.Name));
                        }
                        else
                        {
                            CodeObjectCreateExpression createObjectExpression = CreateCustomType(pi.ParameterType.Name);
                            ctorParams[j] = createObjectExpression;
                        }
                        //ctorParams[j] = new CodePrimitiveExpression(ResolveParameter(pi.ParameterType.Name));
                        j++;
                    }

                    CodeObjectCreateExpression objectCreationExpression =
                        new CodeObjectCreateExpression(typeToResolveInfo.FullName, ctorParams);

                    return objectCreationExpression;
                }
                else //if(typeToResolveInfo.IsInterface)
                {
                    // interface -- find all exported types that implement that interface
                    List<Type> interfaceTypes = (from t in assemblyExportedTypes
                                                 where !t.IsInterface && !t.IsAbstract
                                                 where typeToResolveInfo.IsAssignableFrom(t)
                                                 select t).ToList();

                    if (interfaceTypes.Count != 0)
                    {
                        return CreateCustomType(interfaceTypes.FirstOrDefault().Name);
                    }
                    else
                    {
                        throw new Exception(string.Format("Can not find a type that implements : ", typeToResolve));
                    }
                }
            }
            else
            {
                throw new Exception(string.Format("Can not resolve type : ", typeToResolve));
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
