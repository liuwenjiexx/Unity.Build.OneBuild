using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using UnityEditor.Build.OneBuild;
using UnityEngine.Build.OneBuild;

[assembly: BuildConfigType(typeof(BuildSettings), "Build")]

namespace UnityEditor.Build.OneBuild
{

    [Serializable]
    public class BuildSettings
    {
        [SerializeField]
        private string outputDir;
        [SerializeField]
        private bool showFolder = true;
        [SerializeField]
        private bool setPodfileModularHeaders;
        [SerializeField]
        private BuildOptions options;
        [SerializeField]
        private string outputFileName;
        [SerializeField]
        private string clearLog;
        [SerializeField]
        private string logEnable;
        [SerializeField]
        private bool autoAddAssetBundleScene = true;
        [SerializeField]
        private string[] scenes;
        [SerializeField]
        private int autoVersionFile = -1;
        [SerializeField]
        private string gitTagVersion;
        [SerializeField]
        private string copyLogFile;

        [SerializeField]
        private bool autoBundleVersionCode = false;

        private static BuildSettings instance;

        #region Provider


        private static UnityEngine.Build.OneBuild.SettingsProvider provider;

        private static UnityEngine.Build.OneBuild.SettingsProvider Provider
        {
            get
            {
                if (provider == null)
                    provider = new UnityEngine.Build.OneBuild.SettingsProvider(typeof(BuildSettings), EditorOneBuild.PackageName, false, true);
                return provider;
            }
        }

        public static BuildSettings Instance { get => (BuildSettings)Provider.Settings; }

        #endregion



        public static BuildTarget BuildTarget { get => EditorUserBuildSettings.activeBuildTarget; }

        public static BuildTargetGroup BuildTargetGroup
        {
            get
            {
                switch (EditorUserBuildSettings.activeBuildTarget)
                {
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                        return BuildTargetGroup.Standalone;
                    case BuildTarget.Android:
                        return BuildTargetGroup.Android;
                    case BuildTarget.iOS:
                        return BuildTargetGroup.iOS;
                    case BuildTarget.WebGL:
                        return BuildTargetGroup.WebGL;
                }
                return EditorUserBuildSettings.selectedBuildTargetGroup;
            }
        }

        public static BuildOptions Options
        {
            get => Instance.options;
            set => Provider.Set(nameof(Options), ref Instance.options, value);
        }

        public static string Version
        {
            get { return PlayerSettings.bundleVersion; }
            set { PlayerSettings.bundleVersion = value; }
        }

        public static int VersionCode
        {
            get
            {
                switch (BuildTargetGroup)
                {
                    case BuildTargetGroup.Android:
                        return PlayerSettings.Android.bundleVersionCode;
                    case BuildTargetGroup.iOS:
                        return int.Parse(PlayerSettings.iOS.buildNumber);
                }
                throw new Exception("not version code");

            }
            set
            {
                switch (BuildTargetGroup)
                {
                    case BuildTargetGroup.Android:
                        PlayerSettings.Android.bundleVersionCode = value;
                        break;
                    case BuildTargetGroup.iOS:
                        PlayerSettings.iOS.buildNumber = value.ToString();
                        break;
                }
            }
        }

        public static string OutputDir
        {
            get => Instance.outputDir;
            set => Provider.Set(nameof(OutputDir), ref Instance.outputDir, value);
        }


        public static string OutputFileName
        {
            get => Instance.outputFileName;
            set => Provider.Set(nameof(OutputFileName), ref Instance.outputFileName, value);
        }
        public static string ClearLog
        {
            get => Instance.clearLog;
            set => Provider.Set(nameof(ClearLog), ref Instance.clearLog, value);
        }
        public static string LogEnable
        {
            get => Instance.logEnable;
            set => Provider.Set(nameof(LogEnable), ref Instance.logEnable, value);
        }


        public static bool ShowFolder
        {
            get => Instance.showFolder;
            set => Provider.Set(nameof(ShowFolder), ref Instance.showFolder, value);
        }

        public static bool SetPodfileModularHeaders
        {
            get => Instance.setPodfileModularHeaders;
            set => Provider.Set(nameof(SetPodfileModularHeaders), ref Instance.setPodfileModularHeaders, value);
        }

        public static bool AutoAddAssetBundleScene
        {
            get => Instance.autoAddAssetBundleScene;
            set => Provider.Set(nameof(AutoAddAssetBundleScene), ref Instance.autoAddAssetBundleScene, value);
        }


        public static string[] Scenes
        {
            get => Instance.scenes;
            set => Provider.Set(nameof(Scenes), ref Instance.scenes, value);
        }

        /// <summary>
        /// v(?<result>\S+)
        /// </summary>
        public static string GitTagVersion
        {
            get => Instance.gitTagVersion;
            set => Provider.Set(nameof(GitTagVersion), ref Instance.gitTagVersion, value);
        }

        public static string CopyLogFile
        {
            get => Instance.copyLogFile;
            set => Provider.Set(nameof(CopyLogFile), ref Instance.copyLogFile, value);
        }


        public static DateTime LocalTime
        {
            get
            {
                if (EditorOneBuild.IsBuilding)
                    return EditorBuildOptions.Instance.LocalTime;
                return DateTime.Now;
            }
        }
        public static DateTime UtcTime
        {
            get
            {
                if (EditorOneBuild.IsBuilding)
                    return EditorBuildOptions.Instance.UtcTime;
                return DateTime.UtcNow;
            }
        }


        /// <summary>
        /// 路径包含.manifest, 开启Strip Engine Code 必须设置资源路径
        /// PlayerPrefs Key: [UnityEditor.BuildPlayer.AssetBundleManifestPath]
        /// </summary>
        public static string AssetBundleManifestPath
        {
            get { return PlayerPrefs.GetString(EditorOneBuild.BuildKeyPrefix + "AssetBundleManifestPath", null); }
            set
            {
                if (AssetBundleManifestPath != value)
                {
                    PlayerPrefs.SetString(EditorOneBuild.BuildKeyPrefix + "AssetBundleManifestPath", value);
                }
            }
        }

        #region Version 

        //public static string VersionFile
        //{
        //    get
        //    {
        //        string line;
        //        EditorOneBuild.GetVersionFile(0, out line);
        //        return line;
        //    }
        //}

        ///// <summary>
        ///// <see cref="VersionFileName"/> index: 0
        ///// </summary>
        //public static string VersionFileMajor { get => GetVersionFileStr(0); }
        ///// <summary>
        ///// <see cref="VersionFileName"/> index: 1
        ///// </summary>
        //public static string VersionFileMinor { get => GetVersionFileStr(1); }
        ///// <summary>
        ///// <see cref="VersionFileName"/> index: 2
        ///// </summary>
        //public static string VersionFileBuild { get => GetVersionFileStr(2); }
        ///// <summary>
        ///// <see cref="VersionFileName"/> index: 3
        ///// </summary>
        //public static string VersionFileRevision { get => GetVersionFileStr(3); }



        ///// <summary>
        ///// <see cref="VersionFileName"/> index: 0
        ///// </summary>
        //public static string VersionFile0 { get => GetVersionFileStr(0); }
        ///// <summary>
        ///// <see cref="VersionFileName"/> index: 1
        ///// </summary>
        //public static string VersionFile1 { get => GetVersionFileStr(1); }
        ///// <summary>
        ///// <see cref="VersionFileName"/> index: 2
        ///// </summary>
        //public static string VersionFile2 { get => GetVersionFileStr(2); }
        ///// <summary>
        ///// <see cref="VersionFileName"/> index: 3
        ///// </summary>
        //public static string VersionFile3 { get => GetVersionFileStr(3); }
        ///// <summary>
        ///// <see cref="VersionFileName"/> index: 4
        ///// </summary>
        //public static string VersionFile4 { get => GetVersionFileStr(4); }

        //internal static string GetVersionFileStr(int index)
        //{
        //    string line;
        //    return EditorOneBuild.GetVersionFile(index, out line);
        //}

        /// <summary>
        /// 索引从0开始, -1表示禁用
        /// </summary>
        //public static int AutoVersionFile
        //{
        //    get => Instance.autoVersionFile;
        //    set
        //    {
        //        if (Instance.autoVersionFile != value)
        //        {
        //            Instance.autoVersionFile = value;
        //            Save();
        //        }
        //    }
        //}


        /// <summary>
        /// <see cref="Version"/> index: 0
        /// </summary>
        public static string Version0 { get => GetVersionStr(0); }
        /// <summary>
        /// <see cref="Version"/> index: 1
        /// </summary>
        public static string Version1 { get => GetVersionStr(1); }
        /// <summary>
        /// <see cref="Version"/> index: 2
        /// </summary>
        public static string Version2 { get => GetVersionStr(2); }
        /// <summary>
        /// <see cref="Version"/> index: 3
        /// </summary>
        public static string Version3 { get => GetVersionStr(3); }

        internal static string GetVersionStr(int index)
        {
            string[] parts = Version.Split('.');
            return parts[index];
        }
        #endregion

        private int incrementVersion = -1;
        public static int IncrementVersion
        {
            get => Instance.incrementVersion;
            set { Provider.Set(nameof(IncrementVersion), ref Instance.incrementVersion, value); }
        }

        private bool incrementVersionCode;
        public static bool IncrementVersionCode
        {
            get => Instance.incrementVersionCode;
            set { Provider.Set(nameof(DisableGPUInstancing), ref Instance.incrementVersionCode, value); }
        }

        private string channel;
        public static string Channel
        {
            get => Instance.channel;
            set { Provider.Set(nameof(Channel), ref Instance.channel, value); }
        }

        //public static string keystoreName
        //{
        //    get { return PlayerSettings.Android.keystoreName; }
        //    set
        //    {
        //        if (!string.IsNullOrEmpty(value))
        //        {
        //            PlayerSettings.Android.keystoreName = Path.GetFullPath(value);
        //        }
        //        else
        //        {
        //            PlayerSettings.Android.keystoreName = null;
        //        }
        //        Debug.Log("Set PlayerSettings.Android.keystoreName=" + PlayerSettings.Android.keystoreName);
        //    }
        //}

        private bool disableGPUInstancing;
        public static bool DisableGPUInstancing
        {
            get => Instance.disableGPUInstancing;
            set { Provider.Set(nameof(DisableGPUInstancing), ref Instance.disableGPUInstancing, value); }
        }

        private string shaderMaxTargetLevel;
        public static string ShaderMaxTargetLevel
        {
            get => Instance.shaderMaxTargetLevel;
            set { Provider.Set(nameof(ShaderMaxTargetLevel), ref Instance.shaderMaxTargetLevel, value); }
        }


        public override string ToString()
        {
            return JsonUtility.ToJson(this, true);
        }
    }
}