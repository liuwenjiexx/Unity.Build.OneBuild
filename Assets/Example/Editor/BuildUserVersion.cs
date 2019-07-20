using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.OneBuild;
using UnityEngine;

public class BuildUserVersion
{

    #region Build User Version A

    private const string BuildUserVersionName_A = EditorOneBuild.UserVersionPrefix + "a";
    private const string BuildUserVersionMenuName_A = EditorOneBuild.UserVersionMenu + "A";

    [MenuItem(BuildUserVersionMenuName_A, priority = EditorOneBuild.UserVersionMenuPriority)]
    public static void BuildUserVersionMenu_A()
    {
        EditorOneBuild.SetUserVersion(BuildUserVersionName_A);
    }

    [MenuItem(BuildUserVersionMenuName_A, validate = true)]
    public static bool BuildUserVersionMenu_Validate_A()
    {
        Menu.SetChecked(BuildUserVersionMenuName_A, EditorOneBuild.ContainsVersion(BuildUserVersionName_A));
        return !EditorOneBuild.IsNoUser;
    }

    #endregion

    #region Build User Version B

    private const string BuildUserVersionName_B = EditorOneBuild.UserVersionPrefix + "b";
    private const string BuildUserVersionMenuName_B = EditorOneBuild.UserVersionMenu + "B";

    [MenuItem(BuildUserVersionMenuName_B, priority = EditorOneBuild.UserVersionMenuPriority)]
    public static void BuildUserVersionMenu_B()
    {
        EditorOneBuild.SetUserVersion(BuildUserVersionName_B);
    }

    [MenuItem(BuildUserVersionMenuName_B, validate = true)]
    public static bool BuildUserVersionMenu_Validate_B()
    {
        Menu.SetChecked(BuildUserVersionMenuName_B, EditorOneBuild.ContainsVersion(BuildUserVersionName_B));
        return !EditorOneBuild.IsNoUser;
    }

    #endregion



}
