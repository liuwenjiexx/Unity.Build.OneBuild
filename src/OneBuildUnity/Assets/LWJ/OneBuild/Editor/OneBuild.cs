using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor.iOS.Xcode.Custom;
using UnityEditor.Callbacks;
using System.Linq;
using System.Xml;
using System.Text;

namespace LWJ.Unity.Editor
{

    public static class OneBuild
    {
        public static string ConfigDir = "Assets/Config";
        public static bool log = true;

        static string VersionPath
        {
            get { return ConfigDir + "/version.txt"; }
        }

        public static string VersionFileName = "version.txt";
        static StringBuilder sb = new StringBuilder();

        public static void LoadConfig()
        {
            string currentPath = VersionPath;
            string version = null;
            if (File.Exists(currentPath))
            {
                version = File.ReadAllText(currentPath);
            }
            LoadConfig(version);
        }

        public static void LoadConfig(string version)
        {
            configs = new Dictionary<string, string>();
            Dictionary<string, int> matchs = new Dictionary<string, int>();


            if (!string.IsNullOrEmpty(version))
            {
                foreach (var part in version.Split(',', '\r', '\n'))
                {
                    if (!string.IsNullOrEmpty(part))
                    {
                        matchs.Add(part.Trim().ToLower(), 10);
                    }
                }
            }

            Dictionary<string, int> files = new Dictionary<string, int>();

            matchs.Add("app", 0);
            matchs.Add(EditorUserBuildSettings.selectedBuildTargetGroup.ToString().ToLower(), 1);

            foreach (var file in Directory.GetFiles(ConfigDir))
            {
                string[] tmp = Path.GetFileNameWithoutExtension(file).Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                tmp = tmp.Distinct().ToArray();
                if (tmp.Where(o => matchs.ContainsKey(o.Trim().ToLower()))
                   .Count() == tmp.Length)
                {
                    files.Add(file, tmp.Sum(o => matchs[o]));
                }
            }

            sb.Append("*** config file ***").AppendLine();
            foreach (var file in files.OrderBy(o => o.Value).Select(o => o.Key))
            {

                sb.Append(file).AppendLine();
                XmlDocument doc;
                doc = new XmlDocument();
                doc.Load(file);
                foreach (XmlNode node in doc.DocumentElement.SelectNodes("*"))
                {
                    string name, value;

                    if (node.Name == "item")
                    {
                        name = node.Attributes["name"].Value;
                    }
                    else
                    {
                        name = node.LocalName;
                    }
                    value = node.InnerText;

                    configs[name] = value;
                }


            }
            sb.Append("*** config file ***").AppendLine();

            sb.Append("config data:").AppendLine();

            sb.Append(JsonUtility.ToJson(new Serialization<string, string>(configs), true)).AppendLine();
            //Debug.Log(sb.ToString());

        }


        [MenuItem("LWJ/One Build")]
        public static void Build()
        {
            LoadConfig();


            BuildTargetGroup buildGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            PlayerSettings.companyName = Get("CompanyName", "");
            PlayerSettings.productName = Get("ProductName", "");
            PlayerSettings.applicationIdentifier = Get("ApplicationIdentifier", "");
            if (buildGroup == BuildTargetGroup.Android || buildGroup == BuildTargetGroup.iOS)
            {
                PlayerSettings.bundleVersion = Get("Version", "");
            }

            PlayerSettings.SetApiCompatibilityLevel(buildGroup, Get("ApiCompatibilityLevel", ApiCompatibilityLevel.NET_2_0_Subset));


            if (Contains("Android.KeystoreName"))
                PlayerSettings.Android.keystoreName = Get("Android.KeystoreName");
            if (Contains("Android.KeystorePass"))
                PlayerSettings.Android.keystorePass = Get("Android.KeystorePass");
            if (Contains("Android.KeyaliasName"))
                PlayerSettings.Android.keyaliasName = Get("Android.KeyaliasName");
            if (Contains("Android.KeyaliasPass"))
                PlayerSettings.Android.keyaliasPass = Get("Android.KeyaliasPass");


            PlayerSettings.SetScriptingBackend(buildGroup, Get("ScriptingBackend", ScriptingImplementation.Mono2x));

            PlayerSettings.SetArchitecture(buildGroup, (int)Get("Architecture", Architecture.Universal));
            PlayerSettings.stripEngineCode = Get("StripEngineCode", true);
            PlayerSettings.strippingLevel = Get("StrippingLevel", StrippingLevel.Disabled);

            PlayerSettings.SetStackTraceLogType(LogType.Error, Get("LoggingError", StackTraceLogType.ScriptOnly));
            PlayerSettings.SetStackTraceLogType(LogType.Assert, Get("LoggingAssert", StackTraceLogType.ScriptOnly));
            PlayerSettings.SetStackTraceLogType(LogType.Warning, Get("LoggingWarning", StackTraceLogType.ScriptOnly));
            PlayerSettings.SetStackTraceLogType(LogType.Log, Get("LoggingLog", StackTraceLogType.ScriptOnly));
            PlayerSettings.SetStackTraceLogType(LogType.Exception, Get("LoggingException", StackTraceLogType.ScriptOnly));

            //Development Build
            EditorUserBuildSettings.development = Get("DevelomentBuild", false);
            EditorUserBuildSettings.connectProfiler = Get("AutoconnectProfiler", false);
            EditorUserBuildSettings.allowDebugging = Get("ScriptDebugging", false);
            EditorUserBuildSettings.buildScriptsOnly = Get("ScriptsOnlyBuild", false);

            switch (buildGroup)
            {
                case BuildTargetGroup.Android:
                    PlayerSettings.Android.bundleVersionCode = Get("VersionCode", 1);
                    break;
                case BuildTargetGroup.iOS:
                    PlayerSettings.iOS.buildNumber = Get("VersionCode");
                    break;
            }

            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();
            //start build
            EditorPrefs.SetBool(typeof(OneBuild).Name + ".startedbuild", true);

            Buld();
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnReloadScripts()
        {
            if (!EditorPrefs.GetBool(typeof(OneBuild).Name + ".startedbuild"))
                return;
            EditorPrefs.SetBool(typeof(OneBuild).Name + ".startedbuild", false);
            Buld();
        }

        static void Buld()
        {
            EditorPrefs.SetBool(typeof(OneBuild).Name + ".startedbuild", false);

            var buildGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            string outputPath = Get("Output.Path", "");
            string outputDir = Path.GetDirectoryName(outputPath);

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            BuildOptions options = BuildOptions.None;

            if (Get("BuildOptions.AutoRunPlayer", false))
                options |= BuildOptions.AutoRunPlayer;

            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, outputPath, EditorUserBuildSettings.activeBuildTarget, options);

        }



        static Dictionary<string, string> configs;


        [PostProcessBuild(0)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            sb = new StringBuilder();
            LoadConfig();
            BuildTargetGroup buildGroup = EditorUserBuildSettings.selectedBuildTargetGroup;


            if (target == BuildTarget.iOS)
            {
                iOSPostProcessBuild(pathToBuiltProject);

                string projPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";

                PBXProject pbxProj = new PBXProject();

                pbxProj.ReadFromFile(projPath);
                string targetGuid = pbxProj.TargetGuidByName("Unity-iPhone");
                pbxProj.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");

#if BUGLY_SDK
            //Bugly

            pbxProj.AddFrameworkToProject(targetGuid, "Security.framework", false);
            pbxProj.AddFrameworkToProject(targetGuid, "SystemConfiguration.framework", false);
            pbxProj.AddFrameworkToProject(targetGuid, "JavaScriptCore.framework", true);
            pbxProj.AddFileToBuild(targetGuid, pbxProj.AddFile("usr/lib/libz.tbd", "Frameworks/libz.tbd", PBXSourceTree.Sdk));
            pbxProj.AddFileToBuild(targetGuid, pbxProj.AddFile("usr/lib/libc++.tbd", "Frameworks/libc++.tbd", PBXSourceTree.Sdk));

            pbxProj.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-ObjC");
            pbxProj.SetBuildProperty(targetGuid, "DEBUG_INFORMATION_FORMAT", "dwarf-with-dsym");
            pbxProj.SetBuildProperty(targetGuid, "GENERATE_DEBUG_SYMBOLS", "yes");

            //---Bugly
#endif
                pbxProj.WriteToFile(projPath);


                string plistPath = pathToBuiltProject + "/Info.plist";
                PlistDocument plist = new PlistDocument();
                plist.ReadFromString(File.ReadAllText(plistPath));
                PlistElementDict rootDict = plist.root;

                //Game Center
                var caps = rootDict.CreateArray("UIRequiredDeviceCapabilities");
                caps.AddString("armv7");
                caps.AddString("gamekit");


                plist.WriteToFile(plistPath);
            }
            else if (target == BuildTarget.Android)
            {
                AndroidPostProcessBuild(pathToBuiltProject);
            }


            string outputPath = pathToBuiltProject;

            File.WriteAllText(outputPath + ".txt", sb.ToString());
            Debug.Log("build path\n" + pathToBuiltProject);
        }


        static void AndroidPostProcessBuild(string pathToBuiltProject)
        {


        }



        static void iOSPostProcessBuild(string pathToBuiltProject)
        {

        }





        public static bool Contains(string name)
        {
            return configs.ContainsKey(name);
        }

        public static string Get(string name)
        {
            return Get<string>(name, null);
        }

        public static T Get<T>(string name, T defaultValue)
        {
            string obj;
            if (!configs.TryGetValue(name, out obj))
                return defaultValue;
            if (obj == null)
                return default(T);
            Type type = typeof(T);
            if (type == typeof(string))
            {
                if (obj is string)
                    return (T)(object)obj;
                return (T)(object)obj.ToString();
            }
            if (type.IsEnum)
            {
                return (T)Enum.Parse(type, (string)obj);
            }

            return (T)Convert.ChangeType(obj, type);
        }


        enum Architecture
        {
            None = 0,
            ARM64 = 1,
            /// <summary>
            /// Arm7和Arm64
            /// </summary>
            Universal = 2,
        }

        [Serializable]
        public class Serialization<TKey, TValue> : ISerializationCallbackReceiver
        {
            [SerializeField]
            List<TKey> keys;
            [SerializeField]
            List<TValue> values;

            Dictionary<TKey, TValue> target;
            public Dictionary<TKey, TValue> ToDictionary() { return target; }

            public Serialization(Dictionary<TKey, TValue> target)
            {
                this.target = target;
            }

            public void OnBeforeSerialize()
            {
                keys = new List<TKey>(target.Keys);
                values = new List<TValue>(target.Values);
            }

            public void OnAfterDeserialize()
            {
                var count = Math.Min(keys.Count, values.Count);
                target = new Dictionary<TKey, TValue>(count);
                for (var i = 0; i < count; ++i)
                {
                    target.Add(keys[i], values[i]);
                }
            }
        }


    }
}