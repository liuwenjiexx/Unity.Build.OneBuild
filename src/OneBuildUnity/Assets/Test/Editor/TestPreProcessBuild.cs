using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Build;

public class TestPreProcessBuild
{
    [PreProcessBuild]
    public static void PreProcessBuild()
    {
        Debug.Log("Test PreProcessBuildAttribute");
    }

    public static void PreProcessBuild_Calbback()
    {
        Debug.Log("Test PreProcessBuild_Calbback");
    }
}
