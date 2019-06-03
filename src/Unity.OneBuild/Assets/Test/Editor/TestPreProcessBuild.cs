using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor;

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
    private const string MyVersionName = "myversion";
    [MenuItem("Build/MyVersion", priority = 31)]
    public static void VersionName_Debug()
    {
        if (OneBuild.ContainsVersion(MyVersionName))
            OneBuild.RemoveVersion(MyVersionName);
        else
            OneBuild.AddVersion(MyVersionName);
    }

    [MenuItem("Build/MyVersion", priority = 31, validate = true)]
    public static bool VersionName_Debug_Validate()
    {
        Menu.SetChecked("Build/MyVersion", OneBuild.ContainsVersion(MyVersionName));
        return true;
    }

}
