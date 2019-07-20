using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode.Custom;
using UnityEngine;


namespace UnityEditor.Build.OneBuild
{

    public static class EditorOneBuild
    {
        public static string ConfigDir = "Assets/Config";
        public static bool log = true;


        static Dictionary<string, ConfigValue> configs;

        public static string VersionFileName = "version.txt";
        public static string OneBuildKeyPrefix = "OneBuild.";
        public static string BuildKeyPrefix = "BuildPlayer.";
        public static string VersionNameKey = OneBuildKeyPrefix + "VersionName";
        public static string BuildOutputPathKey = BuildKeyPrefix + "OutputPath";
        public static string BuildScenesKey = BuildKeyPrefix + "Scenes";
        public static string BuildOptionsKey = BuildKeyPrefix + "BuildOptions";
        public static string BuildAssetBundleOptionsKey = BuildKeyPrefix + "BuildAssetBundleOptions";
        public static string BuildShowFolderKey = BuildKeyPrefix + "ShowFolder";


        public const string ConfigNS = "urn:schema-unity-config";
        public const string OneBuildlType = "UnityEditor.Build.OneBuild";
        public const string PlayerSettingsType = "UnityEditor.PlayerSettings";
        //public const string EditorUserBuildSettingsType = "UnityEditor.EditorUserBuildSettings";
        //public const string AdvertisementSettingsType = "UnityEditor.Advertisements.AdvertisementSettings";
        //public const string AnalyticsSettingsType = "UnityEditor.Analytics.AnalyticsSettings";

        public const string DebugVersionName = "Debug";
        public const int VersionMenuPriority = 20;
        public const string UserVersionPrefix = "user-";
        public const string UserVersionMenu = "Build/User/";
        public const int UserVersionMenuPriority = VersionMenuPriority + 20;

        /// <summary>
        /// 默认<see cref="DebugVersionName"/>
        /// </summary>
        public static string VersionName
        {
            get { return EditorPrefs.GetString(VersionNameKey, DebugVersionName) ?? string.Empty; }
            set { EditorPrefs.SetString(VersionNameKey, value); }
        }

        public static bool IsNoUser
        {
            get { return EditorPrefs.GetBool(OneBuildKeyPrefix + "NoUser", false); }
            set
            {
                if (IsNoUser != value)
                {
                    EditorPrefs.SetBool(OneBuildKeyPrefix + "NoUser", value);
                    Menu.SetChecked(BuildVersionNoUserMenu, IsNoUser);
                }
            }
        }

        #region Public Config


        public static string OutputPath
        {
            get { return EditorPrefs.GetString(BuildOutputPathKey, null); }
            set { EditorPrefs.SetString(BuildOutputPathKey, value); }
        }
        public static string[] BuildScenes
        {
            get
            {
                var str = EditorPrefs.GetString(BuildScenesKey, null);
                if (string.IsNullOrEmpty(str))
                    return new string[0];
                return str.Split(';');
            }
            set
            {
                EditorPrefs.SetString(BuildScenesKey, value == null ? string.Empty : string.Join(";", value.Select(o => o.Trim()).ToArray()));
            }
        }
        public static BuildOptions BuildOptions
        {
            get
            {
                var n = EditorPrefs.GetInt(BuildOptionsKey, (int)BuildOptions.None);
                return (BuildOptions)n;
            }
            set { EditorPrefs.SetInt(BuildOptionsKey, (int)value); }
        }
        public static bool ShowFolder
        {
            get { return EditorPrefs.GetBool(BuildShowFolderKey, false); }
            set { EditorPrefs.SetBool(BuildShowFolderKey, value); }
        }
        public static BuildAssetBundleOptions BuildAssetBundleOptions
        {
            get
            {
                var n = EditorPrefs.GetInt(BuildAssetBundleOptionsKey, (int)BuildAssetBundleOptions.None);
                return (BuildAssetBundleOptions)n;
            }
            set { EditorPrefs.SetInt(BuildAssetBundleOptionsKey, (int)value); }
        }


        #endregion


        static string VersionPath
        {
            get { return ConfigDir + "/version.txt"; }
        }
        private const string BuildVersionKey = "OneBuild.BuildVersion";
        public static string BuildVersion
        {
            get { return PlayerPrefs.GetString(BuildVersionKey, string.Empty); }
            set
            {
                PlayerPrefs.SetString(BuildVersionKey, value);
                PlayerPrefs.Save();
            }
        }
        public static Dictionary<string, string[]> Configs
        {
            get { return configs.ToDictionary(o => o.Key, o => o.Value.values); }
        }



        public static string Version
        {
            get { return PlayerSettings.bundleVersion; }
            set { PlayerSettings.bundleVersion = value; }
        }
        public static string VersionCode
        {
            get
            {
                switch (BuildTargetGroup)
                {
                    case BuildTargetGroup.Android:
                        return PlayerSettings.Android.bundleVersionCode.ToString();
                    case BuildTargetGroup.iOS:
                        return PlayerSettings.iOS.buildNumber;
                }
                return "0";
            }
            set
            {
                switch (BuildTargetGroup)
                {
                    case BuildTargetGroup.Android:
                        PlayerSettings.Android.bundleVersionCode = int.Parse(value);
                        break;
                    case BuildTargetGroup.iOS:
                        PlayerSettings.iOS.buildNumber = value;
                        break;
                }
            }
        }
        public static string OutputDir { get; set; }
        public static string OutputFileName { get; set; }
        public static bool ClearLog { get; set; }
        public static bool LogEnable { get; set; }

        public static DateTime LocalTime
        {
            get; set;
        }
        public static DateTime UtcTime
        {
            get; set;
        }

        public static BuildTargetGroup BuildTargetGroup
        {
            get { return EditorUserBuildSettings.selectedBuildTargetGroup; }
            set { EditorUserBuildSettings.selectedBuildTargetGroup = value; }
        }


        public static Dictionary<string, string> append = new Dictionary<string, string>()
        {
            { "ScriptingDefineSymbols",";" }
        };

        [MenuItem("Build/Build", priority = 1)]
        public static void BuildMenu()
        {
            Build(GetVersion(VersionName));
        }
        [MenuItem("Build/Update Config", priority = 2)]
        public static void UpdateConfig1()
        {
            UpdateConfig(GetVersion(VersionName));
        }

        //[MenuItem("Build/Build Assets", priority = 3)]
        public static void BuildAssetsMenu()
        {
            Build(GetVersion("assets"));
        }
        public const string VersionNameSeparator = ",";



        [MenuItem("Build/Release", priority = VersionMenuPriority)]
        public static void VersionName_Release()
        {
            RemoveVersion(DebugVersionName);
        }
        [MenuItem("Build/Release", priority = VersionMenuPriority, validate = true)]
        public static bool VersionName_Release_Validate()
        {
            Menu.SetChecked("Build/Release", !ContainsVersion(DebugVersionName));
            return true;
        }
        [MenuItem("Build/Debug", priority = VersionMenuPriority)]
        public static void VersionName_Debug()
        {
            RemoveVersion(DebugVersionName);
            AddVersion(DebugVersionName);
        }


        [MenuItem("Build/Debug", priority = VersionMenuPriority, validate = true)]
        public static bool VersionName_Debug_Validate()
        {
            Menu.SetChecked("Build/Debug", ContainsVersion(DebugVersionName));
            return true;
        }

        #region UserVersion

        private const string BuildVersionNoUserMenu = "Build/No User";



        [MenuItem(BuildVersionNoUserMenu, priority = UserVersionMenuPriority - 1)]
        public static void BuildVersionName_NoUser()
        {
            IsNoUser = !IsNoUser;
        }

        [MenuItem(BuildVersionNoUserMenu, priority = UserVersionMenuPriority, validate = true)]
        public static bool BuildVersionName_None_Validate()
        {
            Menu.SetChecked(BuildVersionNoUserMenu, IsNoUser);
            return true;
        }

        #endregion

        static string[] GetAllVersions()
        {
            return GetAllVersions(VersionName);
        }

        static string[] GetAllVersions(string version)
        {
            return version.Split(new string[] { VersionNameSeparator }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static bool ContainsVersion(string version)
        {
            return GetAllVersions()
                .Where(o => string.Equals(o, version, StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(ConvertPlatformName(o), version, StringComparison.InvariantCultureIgnoreCase))
                .Count() > 0;
        }

        public static void AddVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return;
            List<string> vers;
            vers = GetAllVersions()
                 .ToList();
            if (!vers.Contains(version.ToLower()))
            {
                vers.Add(version);
                VersionName = string.Join(VersionNameSeparator, vers.ToArray());
            }
        }

        public static void RemoveVersion(string version)
        {
            string ver;
            ver = string.Join(VersionNameSeparator, GetAllVersions()
                 .Where(o => !version.Equals(o, StringComparison.InvariantCultureIgnoreCase))
                 .ToArray());
            VersionName = ver;
        }


        public static void SetUserVersion(string userVersion)
        {
            if (string.IsNullOrEmpty(userVersion))
            {
                RemoveUserVersion();
                return;
            }

            if (!userVersion.StartsWith(UserVersionPrefix))
                throw new Exception("user version not starts with :" + UserVersionPrefix);

            RemoveUserVersion();
            AddVersion(userVersion);
        }

        static void RemoveUserVersion()
        {
            VersionName = RemoveUserVersion(VersionName);
        }

        static string RemoveUserVersion(string version)
        {
            var list = GetAllVersions(version).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].StartsWith(UserVersionPrefix))
                {
                    list.RemoveAt(i);
                    i--;
                }
            }
            return string.Join(VersionNameSeparator, list.ToArray());
        }


        public static void Build(string version)
        {
            Debug.Log("Build Version:" + version);
            BuildVersion = version;
            BuildPlayer();
        }

        /// <summary>
        /// 检查<see cref="IsNoUser"/>
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        static string GetVersion(string version)
        {
              
            if (IsNoUser)
            {
                version = RemoveUserVersion(version);
            }

            return version;
        }



        private static Dictionary<string, ConfigValue> LoadConfig(string version, StringBuilder log = null)
        {
            var configs = new Dictionary<string, ConfigValue>(StringComparer.InvariantCultureIgnoreCase);

            Regex nsRegex = new Regex("type:([^ $]+)", RegexOptions.IgnoreCase);

            BuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            LocalTime = DateTime.Now;
            UtcTime = DateTime.UtcNow;

            string[] versionParts = version.Trim().ToLower().Split(new string[] { VersionNameSeparator }, StringSplitOptions.RemoveEmptyEntries);
            if (!versionParts.Contains("build"))
            {
                var tmp = new List<string>(versionParts);
                tmp.Add("build");
                versionParts = tmp.ToArray();
            }
            List<string> files = new List<string>();

            Dictionary<string, int> orderValues;
            orderValues = ParseOrderValue(File.ReadAllLines(VersionPath, Encoding.UTF8)[0]);

            if (log != null)
                log.AppendLine("LoadConfig");

            foreach (var file in Directory.GetFiles(ConfigDir))
            {
                string[] tmp = Path.GetFileNameWithoutExtension(file).Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                tmp = tmp.Select(o => o.Trim().ToLower()).Distinct().ToArray();
                if (tmp.Where(o => BuildTargetGroup.ToString().ToLower() == o ||
                versionParts.Contains(o, StringComparer.InvariantCultureIgnoreCase))
                    .Count() == tmp.Length)
                {
                    files.Add(file);
                }
            }

            files = Order(files, orderValues, (file, b) =>
             {
                 return Path.GetFileNameWithoutExtension(file).ToLower().Split('.').Where(o => ConvertPlatformName(o) == b).Count() > 0;
             }).ToList();


            Dictionary<string, Type> fileTypes = new Dictionary<string, Type>();
            List<ConfigValue> fileValues = new List<ConfigValue>();
            string filePath = null;


            Action<XmlNode, Type> parseNode = (node, type) =>
             {

                 ConfigValue configValue = new ConfigValue()
                 {
                     memberName = node.LocalName,
                 };
                 configValue.type = type;
                 configValue.combin = GetAttributeValue(node, "combin", "");
                 string combinOptionsStr = GetAttributeValue(node, "combinOptions", null);
                 if (!string.IsNullOrEmpty(combinOptionsStr))
                     configValue.combinOptions = (CombineOptions)ParseEnum(typeof(CombineOptions), combinOptionsStr);

                 var valueNodes = node.SelectNodes("*");
                 List<string> argsKeys = new List<string>();
                 if (valueNodes.Count > 0)
                 {
                     configValue.values = new string[valueNodes.Count];
                     for (int i = 0; i < valueNodes.Count; i++)
                     {
                         var valueNode = valueNodes[i];
                         configValue.values[i] = valueNode.InnerText;
                         if (valueNode.Attributes["key"] != null)
                         {
                             var keyAttr = valueNode.Attributes["key"];
                             bool b;
                             if (bool.TryParse(keyAttr.Value, out b))
                             {
                                 argsKeys.Add(configValue.values[i]);
                             }
                         }
                     }
                 }
                 else
                 {
                     configValue.values = new string[] { node.InnerText };
                 }


                 configValue.key = GetKey(type, configValue.memberName, argsKeys.ToArray());

                 configValue.member = FindSetMember(configValue);
                 if (configValue.member == null)
                     Debug.LogError("not found member. " + configValue + ", file:" + filePath);


                 fileValues.Add(configValue);


             };


            foreach (var file in files)
            {
                fileTypes.Clear();
                fileValues.Clear();
                filePath = file;
                XmlDocument doc;
                doc = new XmlDocument();
                doc.Load(file);

                foreach (XmlNode node in doc.DocumentElement.SelectNodes("*"))
                {
                    switch (node.LocalName.ToLower())
                    {
                        case "type":
                            var typeAttr = node.Attributes["type"];

                            if (typeAttr == null)
                                throw new Exception("node not name attribute. " + node.LocalName + " file:" + filePath);

                            string typeName = typeAttr.Value;
                            var type = FindType(typeName);

                            if (type == null)
                                throw new Exception("not found type:" + typeName + ", file:" + file);

                            fileTypes[typeName] = type;

                            string name = GetAttributeValue(node, "name", null);
                            if (!string.IsNullOrEmpty(name))
                                fileTypes[name] = type;

                            foreach (XmlNode itemNode in node.SelectNodes("*"))
                            {
                                parseNode(itemNode, type);
                            }
                            break;
                        default:
                            Debug.LogErrorFormat("Unknown node: {0}", " node:" + node.LocalName + " , file: " + filePath);
                            break;
                    }
                }


                foreach (var value in fileValues)
                {
                    for (int i = 0; i < value.values.Length; i++)
                    {
                        string val = value.values[i];
                        if (string.IsNullOrEmpty(val))
                            continue;
                        val = tplRegex.Replace(val, (m) =>
                          {
                              var g = m.Groups[1];
                              string name = g.Value;
                              string[] parts = name.Split(':');

                              if (parts.Length > 1)
                              {
                                  string typeName = parts[0];
                                  Type type;
                                  if (!fileTypes.TryGetValue(typeName, out type))
                                  {
                                      throw new Exception("not found type:" + val + ", file:" + file);
                                  }
                                  return m.Value.Substring(0, g.Index - m.Index) + GetKey(type, parts[1]) + m.Value.Substring((g.Index - m.Index) + g.Length);
                              }
                              else
                              {
                                  if (FindGetMember(name) == null)
                                      throw new Exception("not found get member:" + name + ", file:" + file + ", item:" + value.memberName);
                              }

                              return m.Value;
                          });

                        value.values[i] = val;
                    }
                }

                foreach (var value in fileValues)
                {
                    if (!string.IsNullOrEmpty(value.combin))
                    {
                        ConfigValue oldValue = null;
                        if (configs.ContainsKey(value.key))
                        {
                            oldValue = configs[value.key];
                        }
                        if (oldValue == null)
                        {
                            if ((value.combinOptions & CombineOptions.Remove) == CombineOptions.Remove)
                            {
                                continue;
                            }

                            configs[value.key] = value;
                        }
                        else
                        {
                            oldValue.Combin(value.values, value.combin, value.combinOptions);
                        }
                    }
                    else
                    {
                        configs[value.key] = value;
                    }
                }

                if (log != null)
                {
                    log.AppendFormat("file:{0}", file)
                        .AppendLine();
                }
                //Debug.Log("file:" + file + "\n" + ToString(fileValues));
            }
            ReplaceTemplate(configs);

            return configs;
        }


        static Dictionary<string, int> ParseOrderValue(string str)
        {
            Dictionary<string, int> order = new Dictionary<string, int>();
            int n = 0;
            foreach (var item in str.Split(';'))
            {
                if (item.Length == 0)
                    continue;
                //string[] parts = item.Split('=');
                //string name = parts[0].ToLower().Trim();
                string name;
                name = item;
                //int n = 0;
                //if (parts.Length > 1 && !int.TryParse(parts[1], out n))
                //{
                //    n = 0;
                //}
                order[name] = n;
                n++;
            }
            return order;
        }
        static IEnumerable<string> Order(IEnumerable<string> names, Dictionary<string, int> order, Func<string, string, bool> equalName)
        {
            if (equalName == null)
                throw new ArgumentNullException("equalName");

            foreach (var orderItem in order.OrderByDescending(o => o.Value))
            {
                names = names.OrderBy(o => equalName(o, orderItem.Key) ? 1 : 0);
            }
            return names;
        }
        static string ConvertPlatformName(string name)
        {
            if (name.StartsWith(UserVersionPrefix, StringComparison.InvariantCultureIgnoreCase))
                return "user";

            if (string.Equals(name, "standalone"))
                return "platform";

            if (Enum.GetNames(typeof(BuildTargetGroup)).Contains(name, StringComparer.InvariantCultureIgnoreCase))
                return "platform";

            return name;
        }

        static string GetAttributeValue(XmlNode node, string name, string defaultValue)
        {
            var attr = node.Attributes[name];
            if (attr == null)
                return defaultValue;
            return attr.Value;
        }

        private static string ToString(IEnumerable<ConfigValue> values)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            foreach (var value in values)
            {
                sb.AppendFormat("    \"{0}\": [\"{1}\"]", value.key, string.Join(",", value.values))
                    .AppendLine();
            }
            sb.AppendLine("}");

            return sb.ToString();
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



        static MemberInfo FindSetMember(ConfigValue value)
        {
            string[] parts = value.memberName.Split('.');
            MemberInfo member = null;
            Type type = value.type;
            string lowerMemberName = value.memberName.ToLower();

            foreach (var mInfo in type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.SetField | BindingFlags.SetProperty | BindingFlags.InvokeMethod))
            {
                if (mInfo.Name.ToLower() == lowerMemberName)
                {
                    member = mInfo;
                    break;
                }
            }

            return member;
        }

        static MemberInfo FindGetMember(Type type, string memberName)
        {
            string lowerName = memberName.ToLower();

            MemberInfo member = null;
            foreach (var mInfo in type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.InvokeMethod))
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
            return member;
        }
        static MemberInfo FindGetMember(string key)
        {
            int index = key.LastIndexOf('.');
            if (index < 0)
                return null;
            string typeName = key.Substring(0, index);
            string memberName = key.Substring(index + 1);
            Type type = FindType(typeName);
            if (type == null)
                return null;
            MemberInfo member = FindGetMember(type, memberName);

            return member;
        }

        static object GetValueByKey(string key)
        {
            var member = FindGetMember(key);
            if (member == null)
                throw new Exception("not found get member:" + key);
            object value;
            if (member is PropertyInfo)
            {
                PropertyInfo pInfo = (PropertyInfo)member;
                value = pInfo.GetValue(null, null);
            }
            else if (member is FieldInfo)
            {
                FieldInfo fInfo = (FieldInfo)member;
                value = fInfo.GetValue(null);
            }
            else
            {
                MethodInfo mInfo = (MethodInfo)member;
                value = mInfo.Invoke(null, null);
            }
            return value;
        }

        static object ChangeType(string[] value, Type type)
        {
            if (type == typeof(string[]))
                return value;


            return ChangeType(value[0], type);
        }
        static object ChangeType(string value, Type type)
        {
            if (type == typeof(string))
            {
                if (value is string)
                    return value;
                return value.ToString();
            }
            if (type.IsEnum)
            {
                return ParseEnum(type, value as string);
            }
            return Convert.ChangeType(value, type);
        }
        static Type FindType(string typeName)
        {
            Type type;
            type = Type.GetType(typeName);
            if (type == null)
            {
                type = AppDomain.CurrentDomain.GetAssemblies()
                 .SelectMany(o => o.GetTypes())
                 .Where(o => string.Equals(o.FullName, typeName, StringComparison.InvariantCultureIgnoreCase) ||
                     string.Equals(o.Name, typeName, StringComparison.InvariantCultureIgnoreCase) ||
                     (o.IsNested && o.FullName.Replace('+', '.') == typeName))
                 .FirstOrDefault();
            }
            return type;
        }


        public static void UpdateConfig(string version)
        {
            StringBuilder log = new StringBuilder();
            configs = LoadConfig(version, log);


            BuildTargetGroup buildGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            BuildScenes = EditorBuildSettings.scenes.Where(o => o.enabled).Select(o => o.path).ToArray();
            ShowFolder = false;
            BuildOptions = BuildOptions.None;
            OutputPath = null;
            BuildAssetBundleOptions = BuildAssetBundleOptions.None;

            foreach (var item in configs.Values)
            {
                item.SetValue();
            }

            if (ClearLog)
            {
                _ClearLog();
            }

            log.AppendLine("values:")
                .AppendLine(ToString(configs.Values));

            if (LogEnable)
            {
                Debug.Log(log.ToString());
            }

            string outputPath = Path.Combine(OutputDir, OutputFileName);

            OutputPath = outputPath;



            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();
        }


        static void BuildAssetBundles()
        {
            BuildTargetGroup buildGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            string outputDir = OutputDir;
            string[] tmp = Get<string[]>(GetKey(PlayerSettingsType, "Build.Assets"));

            AssetBundleBuild[] assetBundleBuilds = new AssetBundleBuild[tmp.Length];
            for (int i = 0; i < assetBundleBuilds.Length; i++)
            {
                string assetPath = tmp[i];
                AssetBundleBuild assetBundleBuild = new AssetBundleBuild();
                assetBundleBuild.assetNames = new string[] { assetPath };
                assetBundleBuild.assetBundleName = Path.GetFileNameWithoutExtension(assetPath);

                assetBundleBuilds[i] = assetBundleBuild;
            }


            BuildAssetBundleOptions assetBundleOptions = Get<BuildAssetBundleOptions>(GetKey(PlayerSettingsType, "BuildAssetBundleOptions"), BuildAssetBundleOptions.None);
            BuildPipeline.BuildAssetBundles(outputDir, assetBundleBuilds, assetBundleOptions, buildTarget);

            Debug.Log("Build Assets Complete.");
        }


        public static void _ClearLog()
        {
            if (Application.isEditor)
            {
                try
                {
                    var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
                    var type = assembly.GetType("UnityEditor.LogEntries");
                    var method = type.GetMethod("Clear");
                    method.Invoke(new object(), null);
                }
                catch { }
            }
            else
            {
                Debug.ClearDeveloperConsole();
            }
            Debug.Log("Clear Log");
        }


        [PostProcessBuild(0)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {

            configs = LoadConfig(BuildVersion);
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
                if (Get(GetKey(PlayerSettingsType, "iOS.GameCenter"), false))
                {
                    //Game Center
                    pbxProj.AddFrameworkToProject(targetGuid, "GameKit.framework", false);
                    pbxProj.AddCapability(targetGuid, PBXCapabilityType.GameCenter);
                }


                //#if BUGLY_SDK
                //            //Bugly

                //            pbxProj.AddFrameworkToProject(targetGuid, "Security.framework", false);
                //            pbxProj.AddFrameworkToProject(targetGuid, "SystemConfiguration.framework", false);
                //            pbxProj.AddFrameworkToProject(targetGuid, "JavaScriptCore.framework", true);
                //            pbxProj.AddFileToBuild(targetGuid, pbxProj.AddFile("usr/lib/libz.tbd", "Frameworks/libz.tbd", PBXSourceTree.Sdk));
                //            pbxProj.AddFileToBuild(targetGuid, pbxProj.AddFile("usr/lib/libc++.tbd", "Frameworks/libc++.tbd", PBXSourceTree.Sdk));

                //            pbxProj.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-ObjC");
                //            pbxProj.SetBuildProperty(targetGuid, "DEBUG_INFORMATION_FORMAT", "dwarf-with-dsym");
                //            pbxProj.SetBuildProperty(targetGuid, "GENERATE_DEBUG_SYMBOLS", "yes");

                //            //---Bugly
                //#endif
                pbxProj.WriteToFile(projPath);


                string plistPath = pathToBuiltProject + "/Info.plist";
                PlistDocument plist = new PlistDocument();
                plist.ReadFromString(File.ReadAllText(plistPath));
                PlistElementDict rootDict = plist.root;

                if (Get(GetKey(PlayerSettingsType, "iOS.GameCenter"), false))
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

            //File.WriteAllText(outputPath + ".txt", sb.ToString());
            Debug.Log("build path\n" + pathToBuiltProject);
        }


        static void AndroidPostProcessBuild(string pathToBuiltProject)
        {


        }



        static void iOSPostProcessBuild(string pathToBuiltProject)
        {

        }

        private static string GetKey(Type type, string memberName, string[] args = null)
        {
            return GetKey(type.FullName, memberName, args);
        }

        private static string GetKey(string typeName, string memberName, string[] argss = null)
        {
            string key = typeName + "." + memberName;
            if (argss != null && argss.Length > 0)
            {
                key += "+" + string.Join("+", argss);
            }
            return key;
        }

        public static bool Contains(string key)
        {
            return configs.ContainsKey(key);
        }

        public static string Get(string key)
        {
            return Get<string>(key);
        }

        public static T Get<T>(string key)
        {
            if (!configs.ContainsKey(key))
                throw new Exception("Not Key:" + key);
            return Get<T>(key, default(T));
        }

        public static T Get<T>(string key, T defaultValue)
        {
            string[] v;
            try
            {
                ConfigValue value;

                if (!configs.TryGetValue(key, out value))
                    return defaultValue;
                v = value.values;
                if (v == null)
                    return default(T);
                return (T)ChangeType(v, typeof(T));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError("config error name:" + key);
                return defaultValue;
            }

        }
        static Regex tplRegex = new Regex("\\{\\$(.*?)(\\,(.*?))?\\}");
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
        private static void ReplaceTemplate(Dictionary<string, ConfigValue> input)
        {
            string[] values;
            foreach (var key in input.Keys.ToArray())
            {
                values = input[key].values;
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

        static string FindReplaceString(Dictionary<string, ConfigValue> input, string key, string value, string startKey)
        {
            value = tplRegex.Replace(value, (m) =>
            {
                string name = m.Groups[1].Value;
                string format = m.Groups[3].Value;

                if (input.Comparer.Equals(key, name))
                    throw new Exception("reference self. key: [" + name + "], key1:[" + key + "]" + "], key2:[" + startKey + "]");
                if (input.Comparer.Equals(startKey, name))
                    throw new Exception("loop reference key1:[" + name + "], key2:[" + startKey + "]");
                string newValue;
                if (input.ContainsKey(name))
                {
                    newValue = FindReplaceString(input, name, input[name].values[0], startKey);
                    if (!string.IsNullOrEmpty(format))
                    {
                        newValue = string.Format(format, newValue);
                    }
                }
                else
                {
                    object v = GetValueByKey(name);
                    if (v != null && !string.IsNullOrEmpty(format) && v is IFormattable)
                    {
                        newValue = ((IFormattable)v).ToString(format, null);
                    }
                    else
                    {
                        newValue = v.ToString();
                    }
                }
                return newValue;
            });
            return value;
        }

        //enum Architecture
        //{
        //    None = 0,
        //    ARM64 = 1,
        //    /// <summary>
        //    /// Arm7和Arm64
        //    /// </summary>
        //    Universal = 2,
        //}



        static void BuildPlayer()
        {
            var task = new EditorTask();

            StringBuilder log = new StringBuilder();
            log.AppendLine("Build Task");

            foreach (var item in PreProcessBuildAttribute.Find<PreProcessBuildAttribute>(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod)
                .OrderBy(o =>
                {
                    int order = 0;
                    var orderAttr = o.attribute as CallbackOrderAttribute;
                    if (orderAttr != null)
                        order = (int)orderAttr.GetType().GetField("m_CallbackOrder", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField | BindingFlags.Instance).GetValue(orderAttr);
                    return order;
                }
                ))
            {
                var mInfo = item.member as MethodInfo;
                if (mInfo == null)
                    continue;
                var ps = mInfo.GetParameters();
                if (ps.Length != 0)
                    throw new Exception(string.Format("{0}  method:{1}  only empty parameter", mInfo.DeclaringType, mInfo.Name));

                task.Add((Action)Delegate.CreateDelegate(typeof(Action), mInfo));

                log.AppendFormat("Type:{0} Method{1}", mInfo.DeclaringType.FullName, mInfo)
                .AppendLine();
            }

            Debug.Log(log.ToString());
            task.Run();
        }


        #region PreProcessBuild

        [PreProcessBuild(PreProcessBuildAttribute.Order_Config)]
        static void PreProcessBuild_Config()
        {
            UpdateConfig(BuildVersion);
        }


        static void DeleteDirectoryFiles(string path)
        {
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
        }
        [PreProcessBuild(PreProcessBuildAttribute.Order_BuildPlayer)]
        static void PreProcessBuild_BuildPlayer()
        {
            if (configs == null)
            {
                configs = LoadConfig(BuildVersion);
            }

            if (Contains(GetKey(OneBuildlType, "BuildAssetBundles")))
            {
                BuildAssetBundles();
                return;
            }

            string outputPath = OutputPath;
            string[] scenes = BuildScenes;
            BuildOptions buildOptions = BuildOptions;

            if (File.Exists(outputPath))
            {
                DeleteDirectoryFiles(Path.GetDirectoryName(outputPath));
            }
            else if (Directory.Exists(outputPath))
            {
                DeleteDirectoryFiles(outputPath);
            }


            if (scenes == null || scenes.Length == 0)
                throw new Exception("build player scenes empty");
            BuildPlayerOptions opts = new BuildPlayerOptions();
            opts.scenes = scenes;
            opts.locationPathName = outputPath;
            opts.target = EditorUserBuildSettings.activeBuildTarget;
            opts.options = buildOptions;
            // opts.assetBundleManifestPath = "AssetBundles/Windows/Windows.manifest";
            var report = BuildPipeline.BuildPlayer(opts);

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("" + report.summary.result);
            if (ShowFolder)
                EditorUtility.RevealInFinder(outputPath);
        }

        #endregion


        static string CombinString(string oldStr, string newStr, string separator, CombineOptions options)
        {
            if (string.IsNullOrEmpty(newStr))
                return oldStr;

            List<string> oldParts = new List<string>();
            List<string> newParts = new List<string>();

            if (!string.IsNullOrEmpty(oldStr))
            {
                if ((options & CombineOptions.Clear) != CombineOptions.Clear)
                {
                    oldParts.AddRange(oldStr.Split(new string[] { separator }, StringSplitOptions.None));
                }
            }

            if (!string.IsNullOrEmpty(newStr))
                newParts.AddRange(newStr.Split(new string[] { separator }, StringSplitOptions.None));



            if ((options & CombineOptions.Remove) == CombineOptions.Remove)
            {
                foreach (var newPart in newParts)
                    oldParts.Remove(newPart);
            }
            else
            {
                foreach (var newPart in newParts)
                {
                    if ((options & CombineOptions.Distinct) == CombineOptions.Distinct)
                    {
                        if (oldParts.Contains(newPart))
                            continue;
                    }
                    oldParts.Add(newPart);
                }
            }
            return string.Join(separator, oldParts.ToArray());
        }


        class ConfigValue
        {
            public string key;
            public Type type;
            public string memberName;
            public MemberInfo member;
            public string[] values;
            public string combin;
            public CombineOptions combinOptions;

            public void Combin(string[] newValues, string separator, CombineOptions options)
            {
                for (int i = 0; i < newValues.Length && i < newValues.Length; i++)
                {
                    this.values[i] = CombinString(this.values[i], newValues[i], separator, options);
                }
            }

            public void SetValue()
            {

                if (member == null)
                {
                    Debug.LogError("Not Find Member: " + key);
                }
                try
                {
                    if (member is PropertyInfo)
                    {
                        PropertyInfo pInfo = (PropertyInfo)member;
                        pInfo.SetValue(null, ChangeType(values, pInfo.PropertyType), null);
                    }
                    else if (member is FieldInfo)
                    {
                        FieldInfo fInfo = (FieldInfo)member;
                        fInfo.SetValue(null, ChangeType(values, fInfo.FieldType));
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
                    Debug.LogError("Set Member Error: " + this + " = " + string.Join(",", values));
                    throw ex;
                }
            }

            public override string ToString()
            {
                return string.Format("item: {0} ", key);
            }
        }
    }



}