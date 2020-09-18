using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEditor.Build.OneBuild
{
    public class EditorFacebookSettings
    {
        static Type FacebookSettingsType = Type.GetType("Facebook.Unity.Settings.FacebookSettings,Facebook.Unity.Settings", true);

        static Type ManifestModType = Type.GetType("Facebook.Unity.Editor.ManifestMod,Facebook.Unity.Editor", true);

        public static string AppId
        {
            get
            {
                return FacebookSettingsType.GetProperty("AppId").GetValue(null) as string;
            }
            set
            {
                if (AppId != value)
                {
                    FacebookSettingsType.GetProperty("AppIds").SetValue(null, new List<string>() { value });
                    GenerateManifest();
                    AssetDatabase.SaveAssets();
                }
            }
        }


        static void GenerateManifest()
        {
            ManifestModType.GetMethod("GenerateManifest").Invoke(null, null);
        }


    }
}