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
        public static void LoadConfigDebug()
        {
            string currentPath = VersionPath;
            string version = "";
            if (File.Exists(currentPath))
            {
                version = File.ReadAllText(currentPath);
            }
            version += ",debug";
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
                        matchs[part.Trim().ToLower()] = 10;
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

        [MenuItem("LWJ/OneBuild/Build")]
        public static void Build()
        {
            SetConfig();
            //start build
            EditorPrefs.SetBool(typeof(OneBuild).Name + ".startedbuild", true);

            Buld();
        }
        static object ParseEnum(Type enumType, string str)
        {
            if (!enumType.IsEnum)
                throw new Exception("Not Enum Type: " + enumType.FullName);
            if (enumType.IsDefined(typeof(FlagsAttribute), true))
            {
                //uint n = 0;
                //Debug.Log(typeof(uint).IsAssignableFrom(enumType));
                //Debug.Log(enumType.IsAssignableFrom(typeof(uint)));
                //Debug.Log(enumType.IsSubclassOf(typeof(uint)));
                //Debug.Log(typeof(uint).IsSubclassOf(enumType));
                //foreach (var part in str.Split(','))
                //{
                //    if (string.IsNullOrEmpty(part))
                //        continue;
                //    n |= (uint)Enum.Parse(enumType, part);
                //}
                //return Convert.ChangeType(n, enumType);
                return Enum.Parse(enumType, str.Replace(' ', ','));
            }
            else
            {
                return Enum.Parse(enumType, str);
            }
        }
        [MenuItem("LWJ/OneBuild/Build (Debug)")]
        public static void BuildDebug()
        {

            LoadConfigDebug();
            SetConfig(false);
            //start build
            EditorPrefs.SetBool(typeof(OneBuild).Name + ".startedbuild", true);

            Buld();
        }
        [MenuItem("LWJ/OneBuild/Update Config")]
        public static void SetConfig()
        {
            SetConfig(true);
        }
        [MenuItem("LWJ/OneBuild/ Update Config (Debug)")]
        public static void SetConfigDebug()
        {
            LoadConfigDebug();
            SetConfig(false);
        }
        public static void SetConfig(bool loadConfig)
        {
            if (loadConfig)
                LoadConfig();
            BuildTargetGroup buildGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (Contains("CompanyName"))
                PlayerSettings.companyName = Get("CompanyName");
            if (Contains("ProductName"))
                PlayerSettings.productName = Get("ProductName");
            if (Contains("ApplicationIdentifier"))
                PlayerSettings.applicationIdentifier = Get("ApplicationIdentifier");
            if (buildGroup == BuildTargetGroup.Android || buildGroup == BuildTargetGroup.iOS)
            {
                if (Contains("Version"))
                    PlayerSettings.bundleVersion = Get("Version");
            }
            if (Contains("ApiCompatibilityLevel"))
                PlayerSettings.SetApiCompatibilityLevel(buildGroup, Get<ApiCompatibilityLevel>("ApiCompatibilityLevel"));



            if (Contains("ScriptingBackend"))
                PlayerSettings.SetScriptingBackend(buildGroup, Get<ScriptingImplementation>("ScriptingBackend"));

            if (Contains("Architecture"))
                PlayerSettings.SetArchitecture(buildGroup, (int)Get<Architecture>("Architecture"));
            if (Contains("StripEngineCode"))
                PlayerSettings.stripEngineCode = Get("StripEngineCode", true);
            if (Contains("StrippingLevel"))
                PlayerSettings.strippingLevel = Get("StrippingLevel", StrippingLevel.Disabled);

            if (Contains("Il2CppCompilerConfiguration"))
                PlayerSettings.SetIl2CppCompilerConfiguration(buildGroup, Get<Il2CppCompilerConfiguration>("Il2CppCompilerConfiguration"));


            if (Contains("LoggingError"))
                PlayerSettings.SetStackTraceLogType(LogType.Error, Get("LoggingError", StackTraceLogType.ScriptOnly));
            if (Contains("LoggingAssert"))
                PlayerSettings.SetStackTraceLogType(LogType.Assert, Get("LoggingAssert", StackTraceLogType.ScriptOnly));
            if (Contains("LoggingWarning"))
                PlayerSettings.SetStackTraceLogType(LogType.Warning, Get("LoggingWarning", StackTraceLogType.ScriptOnly));
            if (Contains("LoggingLog"))
                PlayerSettings.SetStackTraceLogType(LogType.Log, Get("LoggingLog", StackTraceLogType.ScriptOnly));
            if (Contains("LoggingException"))
                PlayerSettings.SetStackTraceLogType(LogType.Exception, Get("LoggingException", StackTraceLogType.ScriptOnly));

            //Development Build
            if (Contains("DevelomentBuild"))
                EditorUserBuildSettings.development = Get("DevelomentBuild", false);
            if (Contains("AutoconnectProfiler"))
                EditorUserBuildSettings.connectProfiler = Get("AutoconnectProfiler", false);
            if (Contains("ScriptDebugging"))
                EditorUserBuildSettings.allowDebugging = Get("ScriptDebugging", false);
            if (Contains("ScriptsOnlyBuild"))
                EditorUserBuildSettings.buildScriptsOnly = Get("ScriptsOnlyBuild", false);

            switch (buildGroup)
            {
                case BuildTargetGroup.Android:
                    if (Contains("VersionCode"))
                        PlayerSettings.Android.bundleVersionCode = Get("VersionCode", 1);

                    if (Contains("Android.TargetArchitectures"))
                        PlayerSettings.Android.targetArchitectures = Get<AndroidArchitecture>("Android.TargetArchitectures");
                    if (Contains("Android.KeystoreName"))
                        PlayerSettings.Android.keystoreName = Get("Android.KeystoreName");
                    if (Contains("Android.KeystorePass"))
                        PlayerSettings.Android.keystorePass = Get("Android.KeystorePass");
                    if (Contains("Android.KeyaliasName"))
                        PlayerSettings.Android.keyaliasName = Get("Android.KeyaliasName");
                    if (Contains("Android.KeyaliasPass"))
                        PlayerSettings.Android.keyaliasPass = Get("Android.KeyaliasPass");

                    break;
                case BuildTargetGroup.iOS:
                    if (Contains("VersionCode"))
                        PlayerSettings.iOS.buildNumber = Get("VersionCode");
                    break;
            }

            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();

        }

        [DidReloadScripts]
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

        public static T Get<T>(string name)
        {
            return Get<T>(name, default(T));
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
                return (T)ParseEnum(type, (string)obj);
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