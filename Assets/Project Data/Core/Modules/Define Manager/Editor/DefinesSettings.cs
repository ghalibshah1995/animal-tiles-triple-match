using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    public static class DefinesSettings
    {
        public static readonly string[] STATIC_DEFINES = Array.Empty<string>();

        public static readonly RegisteredDefine[] STATIC_REGISTERED_DEFINES = Array.Empty<RegisteredDefine>();

        public static List<RegisteredDefine> GetDynamicDefines()
        {
            List<Type> gameTypes = TypeCache.GetTypesWithAttribute<DefineAttribute>().ToList();

            List<RegisteredDefine> registeredDefines = new List<RegisteredDefine>();
            registeredDefines.AddRange(STATIC_REGISTERED_DEFINES);

            foreach (Type type in gameTypes)
            {
                //Get attribute
                DefineAttribute[] defineAttributes = (DefineAttribute[])Attribute.GetCustomAttributes(type, typeof(DefineAttribute));

                for (int i = 0; i < defineAttributes.Length; i++)
                {
                    if (!string.IsNullOrEmpty(defineAttributes[i].AssemblyType))
                    {
                        int methodId = registeredDefines.FindIndex(x => x.Define == defineAttributes[i].Define);
                        if (methodId == -1)
                        {
                            registeredDefines.Add(new RegisteredDefine(defineAttributes[i]));
                        }
                    }
                }
            }

            return registeredDefines;
        }
    }
}
