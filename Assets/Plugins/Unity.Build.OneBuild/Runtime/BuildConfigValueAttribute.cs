using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Build.OneBuild
{
    public class BuildConfigValueAttribute : Attribute
    {
        public BuildConfigValueAttribute(string value, string displayName)
        {
            this.DisplayName = displayName;
            this.Value = value;
        }
        public string DisplayName { get; set; }
        public string Value { get; set; }


        static Dictionary<string, string> cachedValues;

        public static Dictionary<string, string> FindAllConfigValues()
        {
            if (cachedValues != null)
                return cachedValues;

            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies()
                .Referenced(typeof(BuildConfigValueAttribute).Assembly))
            {
                foreach (BuildConfigValueAttribute attr in ass.GetCustomAttributes(typeof(BuildConfigValueAttribute), true))
                {
                    if (attr.Value != null)
                    {
                        values[attr.Value] = attr.DisplayName;
                    }
                }
            }
            cachedValues = values;
            return values;
        }

    }
}