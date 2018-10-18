using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode.Custom;
using UnityEngine;


namespace LWJ.Unity.Editor
{

    public static class OneBuild
    {
        public static string ConfigDir = "Assets/Config";
        public static bool log = true;

        static Dictionary<string, string[]> configs;

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
        public static Dictionary<string, string[]> Configs
        {
            get { return configs; }
        }

        public static Dictionary<string, object> GlobalVars;

        public static HashSet<string> CustomMembers = new HashSet<string>()
        {
            "version",
            "versionCode",
            "output.dir",
            "output.filename",
            "loggingError",
            "loggingAssert",
            "loggingWarning",
            "loggingLog",
            "loggingException",
            "BuildOptions"
        };
        public static Dictionary<string, string> append = new Dictionary<string, string>()
        {
            { "ScriptingDefineSymbols",";" }
        };

        [MenuItem("Build/Build", priority = 1)]
        public static void BuildMenu()
        {
            string version = GetVersion(null);
            Build(version);

        }
        [MenuItem("Build/Update Config", priority = 1)]
        public static void UpdateConfig1()
        {
            string version = GetVersion(null);
            UpdateConfig(version, true);
        }

        [MenuItem("Build/Build (Debug)", priority = 2)]
        public static void BuildDebug()
        {
            string version = GetVersion("debug");
            Build(version);
        }


        [MenuItem("Build/Update Config (Debug)", priority = 2)]
        public static void UpdateConfigDebug()
        {
            string version = GetVersion("debug");
            UpdateConfig(version, true);
        }

        public static void Build(string version)
        {
            UpdateConfig(version, false);
            LastBuildVersion = version;
            DelayBuild();
        }

        public static string GetVersion(string version)
        {
            string currentPath = VersionPath;
            string ver = "";
            if (File.Exists(currentPath))
            {
                ver = File.ReadAllText(currentPath);
                ver = ver.Trim();
            }

            if (!string.IsNullOrEmpty(version))
            {
                version = version.Trim();
                if (string.IsNullOrEmpty(ver))
                {
                    ver = version;
                }
                else
                {
                    if (!ver.EndsWith(","))
                    {
                        ver += ",";
                    }
                    ver += version;
                }
            }

            return ver;
        }




        public static Dictionary<string, string[]> LoadConfig(string version, StringBuilder log = null)
        {
            var configs = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase);
            Dictionary<string, int> matchs = new Dictionary<string, int>();


            GlobalVars = new Dictionary<string, object>()
            {
                {"DateTime",DateTime.Now },
                {"BuildTargetGroup" , EditorUserBuildSettings.selectedBuildTargetGroup}
            };

            if (!string.IsNullOrEmpty(version))
            {
                foreach (var part in version.Split(',', '\r', '\n'))
                {
                    string ver = part.Trim();
                    if (!string.IsNullOrEmpty(ver))
                    {
                        matchs[ver.ToLower()] = 10;
                    }
                }
            }

            Dictionary<string, int> files = new Dictionary<string, int>();

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


            foreach (var file in files.OrderBy(o => o.Value).Select(o => o.Key))
            {
                if (log != null)
                    log.Append(file).AppendLine();
                XmlDocument doc;
                doc = new XmlDocument();
                doc.Load(file);
                foreach (XmlNode node in doc.DocumentElement.SelectNodes("*"))
                {
                    string name;
                    string[] values;
                    name = node.LocalName;
                    var valueNodes = node.SelectNodes("*");
                    if (valueNodes.Count > 0)
                    {
                        values = new string[valueNodes.Count];
                        for (int i = 0; i < valueNodes.Count; i++)
                        {
                            values[i] = valueNodes[i].InnerText;
                        }
                    }
                    else
                    {
                        values = new string[] { node.InnerText };
                    }
                    if (append.ContainsKey(name))
                    {
                        string[] oldValue = null;
                        if (configs.ContainsKey(name))
                        {
                            oldValue = configs[name];
                            if (oldValue[0] != null)
                                oldValue[0] = oldValue[0].TrimEnd();
                        }
                        if (oldValue == null || string.IsNullOrEmpty(oldValue[0]))
                        {
                            configs[name] = values;
                        }
                        else
                        {
                            if (oldValue[0].EndsWith(append[name]))
                            {
                                oldValue[0] = oldValue[0] + values[0];
                            }
                            else
                            {
                                oldValue[0] = oldValue[0] + append[name] + values[0];
                            }
                        }
                    }
                    else
                    {
                        configs[name] = values;
                    }
                }


            }
            ReplaceTemplate(configs);

            if (log != null)
            {
                log.Append("*** config file ***").AppendLine();

                log.Append("config data:").AppendLine();

                var tmp = configs.ToDictionary(o => o.Key, o => string.Join(",", o.Value));
                log.Append(JsonUtility.ToJson(new Serialization<string, string>(tmp), true))
                    .AppendLine();
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
                return Enum.Parse(enumType, str.Replace(' ', ','));
            }
            else
            {
                return Enum.Parse(enumType, str);
            }
        }



        static MemberInfo FindSetMember(string typeAndMember, string[] args)
        {
            string[] parts = typeAndMember.Split('.');
            MemberInfo member = null;
            Type type = null;
            string memberName;
            if (parts.Length > 1)
            {
                memberName = parts[parts.Length - 1];
                string typeName;
                typeName = typeAndMember.Substring(0, typeAndMember.LastIndexOf('.'));
                type = Type.GetType(typeName);

                if (type == null)
                    type = FindType(typeName);
                if (type == null)
                    type = FindType("UnityEditor." + typeName);

            }
            else
            {
                memberName = parts[0];
            }

            if (type != null)
            {
                member = FindSetMember(type, memberName, args);
            }
            else
            {
                member = FindSetMember(typeof(PlayerSettings), memberName, args);
                if (member == null)
                    member = FindSetMember(typeof(EditorUserBuildSettings), memberName, args);
            }

            return member;
        }

        static MemberInfo FindSetMember(Type type, string memberName, string[] args)
        {
            string lowerName = memberName.ToLower();
            var members = type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.SetField | BindingFlags.SetProperty | BindingFlags.InvokeMethod);
            MemberInfo member = null;
            foreach (var mInfo in members)
            {
                if (mInfo.MemberType == MemberTypes.Field || mInfo.MemberType == MemberTypes.Property)
                {
                    if (mInfo.Name.ToLower() == lowerName)
                    {
                        member = mInfo;
                        break;
                    }
                }
            }
            if (member == null)
            {
                string setName = "set" + lowerName;
                foreach (var mInfo in members)
                {
                    if (mInfo.MemberType == MemberTypes.Method)
                    {
                        if (mInfo.Name.ToLower() == lowerName || mInfo.Name.ToLower() == setName)
                        {
                            MethodInfo m = (MethodInfo)mInfo;
                            if (m.GetParameters().Length == args.Length)
                            {
                                member = mInfo;
                                break;
                            }
                        }
                    }
                }
            }
            return member;
        }

        static void SetMember(string typeAndMember, string[] values)
        {
            MemberInfo member = FindSetMember(typeAndMember, values);
            if (member == null)
            {
                Debug.LogError("Not Find Member: " + typeAndMember);
            }
            try
            {
                if (member is PropertyInfo)
                {
                    PropertyInfo pInfo = (PropertyInfo)member;
                    pInfo.SetValue(null, ChangeType(values[0], pInfo.PropertyType), null);
                }
                else if (member is FieldInfo)
                {
                    FieldInfo fInfo = (FieldInfo)member;
                    fInfo.SetValue(null, ChangeType(values[0], fInfo.FieldType));
                }
                else if (member is MethodInfo)
                {
                    MethodInfo mInfo = (MethodInfo)member;
                    object[] args = mInfo.GetParameters()
                        .Select((o, i) => ChangeType(values[i], o.ParameterType))
                        .ToArray();
                    mInfo.Invoke(null, args);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Set Member Error: " + typeAndMember + " = " + string.Join(",", values));
                throw ex;
            }
        }

        static object ChangeType(string value, Type type)
        {
            if (type.IsEnum)
                return ParseEnum(type, value);
            return Convert.ChangeType(value, type);
        }

        static Type FindType(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(o => o.GetTypes()).Where(o => o.FullName == typeName || (o.IsNested && o.FullName.Replace('+', '.') == typeName)).FirstOrDefault();
        }



        public static void UpdateConfig(string version, bool log)
        {
            StringBuilder sb = null;
            if (log)
                sb = new StringBuilder();
            configs = LoadConfig(version, sb);

            BuildTargetGroup buildGroup = EditorUserBuildSettings.selectedBuildTargetGroup;


            foreach (var item in configs)
            {
                if (CustomMembers.Contains(item.Key))
                    continue;
                SetMember(item.Key, item.Value);
            }

            if (buildGroup == BuildTargetGroup.Android || buildGroup == BuildTargetGroup.iOS)
            {
                if (Contains("Version"))
                    PlayerSettings.bundleVersion = Get("Version");
            }

            if (Contains("loggingError"))
                PlayerSettings.SetStackTraceLogType(LogType.Error, Get("loggingError", StackTraceLogType.ScriptOnly));
            if (Contains("loggingAssert"))
                PlayerSettings.SetStackTraceLogType(LogType.Assert, Get("loggingAssert", StackTraceLogType.ScriptOnly));
            if (Contains("loggingWarning"))
                PlayerSettings.SetStackTraceLogType(LogType.Warning, Get("loggingWarning", StackTraceLogType.ScriptOnly));
            if (Contains("loggingLog"))
                PlayerSettings.SetStackTraceLogType(LogType.Log, Get("loggingLog", StackTraceLogType.ScriptOnly));
            if (Contains("loggingException"))
                PlayerSettings.SetStackTraceLogType(LogType.Exception, Get("loggingException", StackTraceLogType.ScriptOnly));

            switch (buildGroup)
            {
                case BuildTargetGroup.Android:
                    if (Contains("VersionCode"))
                        PlayerSettings.Android.bundleVersionCode = Get("VersionCode", 1);
                    break;
                case BuildTargetGroup.iOS:
                    if (Contains("VersionCode"))
                        PlayerSettings.iOS.buildNumber = Get("VersionCode");
                    break;
            }
            if (sb != null)
                Debug.Log("Update Config\n" + sb.ToString());

            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();

        }



        [DidReloadScripts]
        static void OnReloadScripts()
        {

            if (!EditorPrefs.GetBool(typeof(OneBuild).Name + ".startedbuild"))
                return;
            EditorPrefs.SetBool(typeof(OneBuild).Name + ".startedbuild", false);
            _Build();
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
                    _Build();
                }
            };
        }

        private static void _Build()
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

            BuildOptions options;

            options = Get<BuildOptions>("BuildOptions", BuildOptions.None);

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
            string[] obj;
            if (!configs.TryGetValue(name, out obj))
                throw new Exception("Not Key:" + name);
            return Get<T>(name, default(T));
        }

        public static T Get<T>(string name, T defaultValue)
        {
            string[] obj;
            if (!configs.TryGetValue(name, out obj))
                return defaultValue;
            if (obj == null)
                return default(T);
            Type type = typeof(T);
            if (type == typeof(string))
            {
                if (obj[0] is string)
                    return (T)(object)obj[0];
                return (T)(object)obj.ToString();
            }
            if (type.IsEnum)
            {
                return (T)ParseEnum(type, obj[0] as string);
            }

            return (T)Convert.ChangeType(obj, type);
        }
        static Regex tplRegex = new Regex("\\{\\$(.*?)(\\,(.*))?\\}");
        /// <summary>
        /// Template: {$Name}
        /// </summary>
        /// <param name="input"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static string ReplaceTemplate(string input, Func<string, string, string> func)
        {
            if (func == null)
                throw new ArgumentNullException("func");
            string ret = tplRegex.Replace(input, (m) =>
             {
                 string name = m.Groups[1].Value;
                 string format = m.Groups[3].Value;
                 return func(name, format);
             });
            return ret;
        }
        /// <summary>
        /// Template: {$Key,FormatString}
        /// </summary>
        /// <param name="input"></param>
        public static void ReplaceTemplate(Dictionary<string, string[]> input)
        {
            string[] values;
            foreach (var key in input.Keys.ToArray())
            {
                values = input[key];
                if (values == null)
                    continue;
                for (int i = 0; i < values.Length; i++)
                {
                    string value = values[i];

                    if (tplRegex.IsMatch(value))
                    {
                        value = FindReplaceString(input, key, value, key);
                        values[i] = value;
                    }
                }
            }
        }

        static string FindReplaceString(Dictionary<string, string[]> input, string key, string value, string startKey)
        {
            value = tplRegex.Replace(value, (m) =>
            {
                string name = m.Groups[1].Value;
                string format = m.Groups[3].Value;


                //value = ReplaceTemplate(value, (name, format) =>
                //{
                if (input.Comparer.Equals(key, name))
                    throw new Exception("reference self. key: [" + name + "], key1:[" + key + "]" + "], key2:[" + startKey + "]");
                if (input.Comparer.Equals(startKey, name))
                    throw new Exception("loop reference key1:[" + name + "], key2:[" + startKey + "]");
                string newValue;
                if (input.ContainsKey(name))
                {
                    newValue = FindReplaceString(input, name, input[name][0], startKey);
                    if (!string.IsNullOrEmpty(format))
                    {
                        newValue = string.Format(format, newValue);
                    }
                }
                else if (GlobalVars.ContainsKey(name))
                {
                    object v = GlobalVars[name];
                    if (v != null && !string.IsNullOrEmpty(format) && v is IFormattable)
                    {
                        newValue = ((IFormattable)v).ToString(format, null);
                    }
                    else
                    {
                        newValue = v.ToString();
                    }

                }
                else
                {
                    throw new Exception("not found key: [" + name + "]");
                }
                return newValue;
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