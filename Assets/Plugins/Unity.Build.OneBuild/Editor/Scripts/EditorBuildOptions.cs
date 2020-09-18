using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


namespace UnityEditor.Build.OneBuild
{

    [Serializable]
    public class EditorBuildOptions
    {
        public bool isPreBuild;
        public bool isDebug;
        public string version;
        public string versionName;
        public string outputPath;
        public bool showFolder = false;
        public bool setPodfileModularHeaders = false;
        public BuildOptions options;

        public int timestamp;
        public string[] scenes;
        public string assetBundleManifestPath;
        public bool isBatchMode;
        public string logFile;
        public bool isRun;
        public string channel;

        private static EditorBuildOptions instance;

        public EditorBuildOptions()
        {

        }

        public static EditorBuildOptions Instance
        {
            get
            {
                if (instance == null)
                {
                    string json = PlayerPrefs.GetString(EditorOneBuild.BuildKeyPrefix + "BuildOptions");
                    if (!string.IsNullOrEmpty(json))
                    {
                        try
                        {
                            instance = JsonUtility.FromJson<EditorBuildOptions>(json);
                        }
                        catch { }
                    }
                    if (instance == null)
                        instance = new EditorBuildOptions();
                }
                return instance;
            }
            set
            {
                instance = value;
                Save();
            }
        }

        public DateTime UtcTime
        {
            get => timestamp.FromUtcSeconds();
            set => timestamp = value.ToUtcSeconds();
        }
        public DateTime LocalTime
        {
            get => UtcTime.ToLocalTime();
        }



        public static void Save()
        {
            string json = JsonUtility.ToJson(Instance);
            PlayerPrefs.SetString(EditorOneBuild.BuildKeyPrefix + "BuildOptions", json);
            PlayerPrefs.Save();
        }



        public override string ToString()
        {
            return "BuildOptions: " + JsonUtility.ToJson(this, true);
        }

    }
}