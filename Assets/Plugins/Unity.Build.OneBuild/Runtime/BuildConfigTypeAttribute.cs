using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

namespace UnityEngine.Build.OneBuild
{

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class BuildConfigTypeAttribute : Attribute
    {
        public BuildConfigTypeAttribute(Type configType, string name=null)
        {
            this.ConfigType = configType;
            this.Name = name;
        }


        public Type ConfigType { get; set; }
        public string Name { get; set; }

        public static Dictionary<Type, string> FindAllConfigTypes()
        {
            Dictionary<Type, string> types = new Dictionary<Type, string>();
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies()
                .Referenced(typeof(BuildConfigTypeAttribute).Assembly))
            {
                foreach (BuildConfigTypeAttribute attr in ass.GetCustomAttributes(typeof(BuildConfigTypeAttribute), true))
                {
                    if (attr.ConfigType != null)
                    {
                        types[attr.ConfigType] = attr.Name;
                    }
                }
            }
            return types;
        }

    }

}