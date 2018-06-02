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
using System.Text.RegularExpressions;
using UnityEditor.Purchasing;
using UnityEditor.CrashReporting;
using UnityEditor.Advertisements;
using UnityEditor.Analytics;

namespace LWJ.Unity.Editor
{

    using LogType = UnityEngine.LogType;

    public static class OneBuild
    {
        public static string ConfigDir = "Assets/Config";
        public static bool log = true;

        static Dictionary<string, string> configs;

        public static string VersionFileName = "version.txt";

        static string VersionPath
        {
            get { return ConfigDir + "/version.txt"; }
        }
        private const string LastBuildVersionKey = "OneBuild.LastBuildVersion";
        public static string LastBuildVersion
        {
            get { return PlayerPrefs.GetString(LastBuildVersionKey, string.Empty); }
            set
            {
                PlayerPrefs.SetString(LastBuildVersionKey, value);
                PlayerPrefs.Save();
            }
        }
        public static Dictionary<string, string> Configs
        {
            get { return configs; }
        }

        public static Dictionary<string, string> LoadConfig(out string version, StringBuilder log = null)
        {
            string currentPath = VersionPath;
            version = "";
            if (File.Exists(currentPath))
            {
                version = File.ReadAllText(currentPath);
            }
            return LoadConfig(version, log);
        }

        public static Dictionary<string, string> LoadConfigDebug(out string version, StringBuilder log = null)
        {
            string currentPath = VersionPath;
            version = "";
            if (File.Exists(currentPath))
            {
                version = File.ReadAllText(currentPath);
            }
            version += ",debug";
            return LoadConfig(version, log);
        }
        public static Dictionary<string, string> LoadConfig(string version, StringBuilder log = null)
        {
            var configs = new Dictionary<string, string>();
            Dictionary<string, int> matchs = new Dictionary<string, int>();

            LastBuildVersion = version;
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
            if (log != null)
                log.Append("*** config file ***").AppendLine();
            Dictionary<string, string> append = new Dictionary<string, string>()
            {
                { "ScriptingDefineSymbols",";" }
            };

            foreach (var file in files.OrderBy(o => o.Value).Select(o => o.Key))
            {
                if (log != null)
                    log.Append(file).AppendLine();
                XmlDocument doc;
                doc = new XmlDocument();
                doc.Load(file);
                foreach (XmlNode node in doc.DocumentElement.SelectNodes("*"))
                {
                    string name, value;

                    if (node.Name == "Item")
                    {
                        name = node.Attributes["Name"].Value;
                    }
                    else
                    {
                        name = node.LocalName;
                    }
                    value = node.InnerText;
                    if (append.ContainsKey(name))
                    {
                        string oldValue = null;
                        if (configs.ContainsKey(name))
                        {
                            oldValue = configs[name];
                            if (oldValue != null)
                                oldValue = oldValue.TrimEnd();
                        }
                        if (string.IsNullOrEmpty(oldValue))
                        {
                            configs[name] = value;
                        }
                        else
                        {
                            if (oldValue.EndsWith(append[name]))
                            {
                                configs[name] = oldValue + value;
                            }
                            else
                            {
                                configs[name] = oldValue + append[name] + value;
                            }
                        }
                    }
                    else
                    {
                        configs[name] = value;
                    }
                }


            }
            ReplaceTemplate(configs);

            if (log != null)
            {
                log.Append("*** config file ***").AppendLine();

                log.Append("config data:").AppendLine();

                log.Append(JsonUtility.ToJson(new Serialization<string, string>(configs), true)).AppendLine();
                //Debug.Log(sb.ToString());
            }

            return configs;
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

        [MenuItem("LWJ/OneBuild/Update Config", priority = 1)]
        public static void UpdateConfig1()
        {
            StringBuilder sb = new StringBuilder();
            configs = LoadConfig(null, sb);

            UpdateConfig();
            Debug.Log("Update Config\n" + sb.ToString());

        }
        [MenuItem("LWJ/OneBuild/Update Config (Debug)", priority = 1)]
        public static void UpdateConfigDebug()
        {
            string ver;
            StringBuilder sb = new StringBuilder();
            configs = LoadConfigDebug(out ver, sb);
            UpdateConfig();
            Debug.Log("Update Config\n" + sb.ToString());

        }
        public static void UpdateConfig()
        {

            BuildTargetGroup buildGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (Contains("CompanyName"))
                PlayerSettings.companyName = Get("CompanyName");
            if (Contains("ProductName"))
                PlayerSettings.productName = Get("ProductName");
            if (Contains("ApplicationIdentifier"))
                PlayerSettings.SetApplicationIdentifier(buildGroup, Get("ApplicationIdentifier"));
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
            if (Contains("AotOptions"))
                PlayerSettings.aotOptions = Get("AotOptions");
#if !(UNITY_2017)
            if (Contains("Il2CppCompilerConfiguration"))
                PlayerSettings.SetIl2CppCompilerConfiguration(buildGroup, Get<Il2CppCompilerConfiguration>("Il2CppCompilerConfiguration"));
#endif
            if (Contains("ScriptingDefineSymbols"))
            {
                string str = Get("ScriptingDefineSymbols");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildGroup, str);
            }

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

            if (Contains("DefaultOrientation"))
                PlayerSettings.defaultInterfaceOrientation = Get<UIOrientation>("DefaultOrientation");

            //Development Build
            if (Contains("DevelomentBuild"))
                EditorUserBuildSettings.development = Get("DevelomentBuild", false);
            if (Contains("AutoconnectProfiler"))
                EditorUserBuildSettings.connectProfiler = Get("AutoconnectProfiler", false);
            if (Contains("ScriptDebugging"))
                EditorUserBuildSettings.allowDebugging = Get("ScriptDebugging", false);
            if (Contains("ScriptsOnlyBuild"))
                EditorUserBuildSettings.buildScriptsOnly = Get("ScriptsOnlyBuild", false);

            if (Contains("Analytics.Enabled"))
                AnalyticsSettings.enabled = Get<bool>("Analytics.Enabled");
            if (Contains("Analytics.TestMode"))
                AnalyticsSettings.testMode = Get<bool>("Analytics.TestMode");

            if (Contains("Advertisement.Enabled"))
                AdvertisementSettings.enabled = Get<bool>("Advertisement.Enabled");
            if (Contains("Advertisement.TestMode"))
                AdvertisementSettings.testMode = Get<bool>("Advertisement.TestMode");
            if (Contains("Advertisement.InitializeOnStartup"))
                AdvertisementSettings.initializeOnStartup = Get<bool>("Advertisement.InitializeOnStartup");

            if (Contains("CrashReporting.Enabled"))
                CrashReportingSettings.enabled = Get<bool>("CrashReporting.Enabled");
            if (Contains("CrashReporting.CaptureEditorExceptions"))
                CrashReportingSettings.captureEditorExceptions = Get<bool>("CrashReporting.CaptureEditorExceptions");

            if (Contains("Purchasing.Enabled"))
                PurchasingSettings.enabled = Get<bool>("Purchasing.Enabled");

            switch (buildGroup)
            {
                case BuildTargetGroup.Android:
                    if (Contains("VersionCode"))
                        PlayerSettings.Android.bundleVersionCode = Get("VersionCode", 1);

                    if (Contains("Android.KeystoreName"))
                        PlayerSettings.Android.keystoreName = Get("Android.KeystoreName");
                    if (Contains("Android.KeystorePass"))
                        PlayerSettings.Android.keystorePass = Get("Android.KeystorePass");
                    if (Contains("Android.KeyaliasName"))
                        PlayerSettings.Android.keyaliasName = Get("Android.KeyaliasName");
                    if (Contains("Android.KeyaliasPass"))
                        PlayerSettings.Android.keyaliasPass = Get("Android.KeyaliasPass");
#if !(UNITY_2017)
                    if (Contains("Android.TargetArchitectures"))
                        PlayerSettings.Android.targetArchitectures = Get<AndroidArchitecture>("Android.TargetArchitectures");
#endif
                    break;
                case BuildTargetGroup.iOS:
                    if (Contains("VersionCode"))
                        PlayerSettings.iOS.buildNumber = Get("VersionCode");
                    if (Contains("iOS.HideHomeButton"))
                        PlayerSettings.iOS.hideHomeButton = Get<bool>("iOS.HideHomeButton");
                    if (Contains("iOS.ForceHardShadowsOnMetal"))
                        PlayerSettings.iOS.forceHardShadowsOnMetal = Get<bool>("iOS.ForceHardShadowsOnMetal");
                    if (Contains("iOS.AllowHTTPDownload"))
                        PlayerSettings.iOS.allowHTTPDownload = Get<bool>("iOS.AllowHTTPDownload");
                    if (Contains("iOS.ManualProvisioningProfileID"))
                        PlayerSettings.iOS.iOSManualProvisioningProfileID = Get("iOS.ManualProvisioningProfileID");
                    if (Contains("iOS.AppleDeveloperTeamID"))
                        PlayerSettings.iOS.appleDeveloperTeamID = Get("iOS.AppleDeveloperTeamID");
                    if (Contains("iOS.AppleEnableAutomaticSigning"))
                        PlayerSettings.iOS.appleEnableAutomaticSigning = Get<bool>("iOS.AppleEnableAutomaticSigning");
             

                    if (Contains("iOS.TargetDevice"))
                        PlayerSettings.iOS.targetDevice = Get<iOSTargetDevice>("iOS.TargetDevice");
                    if (Contains("iOS.SdkVersion"))
                        PlayerSettings.iOS.sdkVersion = Get<iOSSdkVersion>("iOS.SdkVersion");
                    if (Contains("iOS.TargetOSVersionString"))
                        PlayerSettings.iOS.targetOSVersionString = Get("iOS.TargetOSVersionString");
                    if (Contains("ScriptCallOptimization"))
                        PlayerSettings.iOS.scriptCallOptimization = Get<ScriptCallOptimizationLevel>("ScriptCallOptimization");
                    if (Contains("iOS.UseOnDemandResources"))
                        PlayerSettings.iOS.useOnDemandResources = Get<bool>("iOS.UseOnDemandResources");
                
#if !(UNITY_2017)
                           if (Contains("iOS.ManualProvisioningProfileType"))
                        PlayerSettings.iOS.iOSManualProvisioningProfileType = Get<ProvisioningProfileType>("iOS.ManualProvisioningProfileType");
#endif
                    break;
            }

            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();

        }

        [MenuItem("LWJ/OneBuild/Build", priority = 2)]
        public static void Build()
        {
            string version;
            configs = LoadConfig(out version);
            LastBuildVersion = version;
            UpdateConfig();
            DelayBuild();
        }

        [MenuItem("LWJ/OneBuild/Build (Debug)", priority = 2)]
        public static void BuildDebug()
        {
            string version;
            configs = LoadConfigDebug(out version);
            LastBuildVersion = version;
            UpdateConfig();

            DelayBuild();
        }

        [DidReloadScripts]
        static void OnReloadScripts()
        {

            if (!EditorPrefs.GetBool(typeof(OneBuild).Name + ".startedbuild"))
                return;
            EditorPrefs.SetBool(typeof(OneBuild).Name + ".startedbuild", false);
            Buld();
        }

        public static void DelayBuild()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("IsPlaying");
                return;
            }

            EditorPrefs.SetBool(typeof(OneBuild).Name + ".startedbuild", true);

            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isCompiling)
                {
                    Buld();
                }
            };
        }

        public static void Buld()
        {
            EditorPrefs.SetBool(typeof(OneBuild).Name + ".startedbuild", false);

            if (EditorApplication.isPlaying)
            {
                Debug.LogError("IsPlaying");
                return;
            }
            if (configs == null)
            {
                configs = LoadConfig(LastBuildVersion);
            }
            //start build
            var buildGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            string outputDir = Get("Output.Dir");
            string fileName = Get("Output.FileName", string.Empty);
            string outputPath = Path.Combine(outputDir, fileName);

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            BuildOptions options = BuildOptions.None;

            if (Get("BuildOptions.AutoRunPlayer", false))
                options |= BuildOptions.AutoRunPlayer;

            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, outputPath, EditorUserBuildSettings.activeBuildTarget, options);

        }





        [PostProcessBuild(0)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            var sb = new StringBuilder();
            configs = LoadConfig(LastBuildVersion, sb);
            BuildTargetGroup buildGroup = EditorUserBuildSettings.selectedBuildTargetGroup;


            if (target == BuildTarget.iOS)
            {
                iOSPostProcessBuild(pathToBuiltProject);

                string projPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";

                PBXProject pbxProj = new PBXProject();

                pbxProj.ReadFromFile(projPath);
                string targetGuid = pbxProj.TargetGuidByName("Unity-iPhone");
                pbxProj.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");


                //pbxProj.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-all_load");
                //pbxProj.AddFrameworkToProject(targetGuid, "WebKit.framework", true);
                //pbxProj.AddFrameworkToProject(targetGuid, "StoreKit.framework", false);
                //pbxProj.AddCapability(targetGuid, PBXCapabilityType.InAppPurchase);
                if (Get("iOS.GameCenter", false))
                {
                    //Game Center
                    pbxProj.AddFrameworkToProject(targetGuid, "GameKit.framework", false);
                    pbxProj.AddCapability(targetGuid, PBXCapabilityType.GameCenter);
                }


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

                if (Get("iOS.GameCenter", false))
                {
                    //Game Center
                    var caps = rootDict.CreateArray("UIRequiredDeviceCapabilities");
                    caps.AddString("armv7");
                    caps.AddString("gamekit");
                }

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
            return Get<string>(name);
        }

        public static T Get<T>(string name)
        {
            string obj;
            if (!configs.TryGetValue(name, out obj))
                throw new Exception("Not Key:" + name);
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
        static Regex tplRegex = new Regex("\\{\\$(.*?)\\}");
        /// <summary>
        /// Template: {$Name}
        /// </summary>
        /// <param name="input"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static string ReplaceTemplate(string input, Func<string, string> func)
        {
            if (func == null)
                throw new ArgumentNullException("func");
            string ret = tplRegex.Replace(input, (m) =>
             {
                 string name = m.Groups[1].Value;
                 return func(name);
             });
            return ret;
        }
        /// <summary>
        /// Template: {$Key}
        /// </summary>
        /// <param name="input"></param>
        public static void ReplaceTemplate(Dictionary<string, string> input)
        {
            string value;
            foreach (var key in input.Keys.ToArray())
            {
                value = input[key];
                if (value == null)
                    continue;
                if (tplRegex.IsMatch(value))
                {
                    value = FindReplaceString(input, key, key);
                    input[key] = value;
                }
            }
        }

        static string FindReplaceString(Dictionary<string, string> input, string key, string startKey)
        {
            string value = input[key];

            value = ReplaceTemplate(value, (name) =>
            {
                if (input.Comparer.Equals(key, name))
                    throw new Exception("reference self. key: [" + name + "]");
                if (input.Comparer.Equals(startKey, name))
                    throw new Exception("loop reference key1:[" + name + "], key2:[" + key + "]");
                if (!input.ContainsKey(name))
                    throw new Exception("not found key: [" + name + "]");

                return FindReplaceString(input, name, startKey);
            });
            return value;
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
        private class Serialization<TKey, TValue> : ISerializationCallbackReceiver
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