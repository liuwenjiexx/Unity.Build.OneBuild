﻿using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace UnityEngine
{
    internal static class InternalExtensions
    {
        public const string PackageName = "unity.build.onebuild";

        public static IEnumerable<Assembly> Referenced(this IEnumerable<Assembly> assemblies, Assembly referenced)
        {
            string fullName = referenced.FullName;

            foreach (var ass in assemblies)
            {
                if (referenced == ass)
                {
                    yield return ass;
                }
                else
                {
                    foreach (var refAss in ass.GetReferencedAssemblies())
                    {
                        if (fullName == refAss.FullName)
                        {
                            yield return ass;
                            break;
                        }
                    }
                }
            }
        }
    }
}