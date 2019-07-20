using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// <see cref="BuildPipeline.BuildPlayer"/> pre process. <see cref="PostProcessAttribute"/>
    /// </summary>
    public class PreProcessBuildAttribute : CallbackOrderAttribute
    {
        public PreProcessBuildAttribute()
        {
        }

        public PreProcessBuildAttribute(int callbackOrder)
        {
            base.m_CallbackOrder = callbackOrder;
        }

        public int CallbackOrder
        {
            get { return m_CallbackOrder; }
        }

        public const int Order_Config = -1000;
        public const int Order_BuildPlayer = 10;

        public static IEnumerable<Item> Find<T>(BindingFlags bindingFlags)
            where T : Attribute
        {

            HashSet<Type> attrTypes = new HashSet<Type>();
            attrTypes.Add(typeof(T));
            string typeName = typeof(T).FullName;
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = ass.GetType(typeName, false);
                if (type != null)
                {
                    if (type.IsSubclassOf(typeof(Attribute)))
                    {
                        attrTypes.Add(type);
                    }
                }
            }

            Attribute attr;
            foreach (var member in AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(o => IsDependent(o, attrTypes.Select(o1 => o1.Assembly)))
                .SelectMany(o => o.GetTypes())
                .SelectMany(o => o.GetMembers(bindingFlags)))
            {
                foreach (var attrType in attrTypes)
                {
                    if (member.IsDefined(attrType, false))
                    {
                        attr = member.GetCustomAttributes(attrType, false)[0] as Attribute;
                        yield return new Item() { attribute = attr, member = member };
                        break;
                    }
                }
            }


        }


        public class Item
        {
            public Attribute attribute;
            public MemberInfo member;
        }

        static bool IsDependent(Assembly ass, IEnumerable<Assembly> dependentAssemblies)
        {
            var referencedAssemblies = ass.GetReferencedAssemblies();

            foreach (var depAss in dependentAssemblies)
            {
                if (depAss == ass)
                    return true;
                foreach (var refAss in referencedAssemblies)
                {                    
                    if (refAss.FullName == depAss.FullName)
                    {
                        return true;
                    }
                }
            }
             
            return false;
        }

    }

}