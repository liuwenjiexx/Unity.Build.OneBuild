using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using UnityEngine;
using System.StringFormats;
using System.Runtime.CompilerServices;
using UnityEngine.Localizations;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEditor.Build.OneBuild;
using UnityEngine.Build.OneBuild;

[assembly: BuildConfigType(typeof(UnityEditor.PlayerSettings))]
[assembly: BuildConfigType(typeof(UnityEditor.PlayerSettings.Android))]
[assembly: BuildConfigType(typeof(UnityEditor.PlayerSettings.iOS), "IOS")]
[assembly: BuildConfigType(typeof(UnityEditor.Advertisements.AdvertisementSettings))]
[assembly: BuildConfigType(typeof(UnityEditor.Analytics.AnalyticsSettings))]
[assembly: BuildConfigType(typeof(UnityEditor.CrashReporting.CrashReportingSettings))]
[assembly: BuildConfigType(typeof(UnityEditor.Purchasing.PurchasingSettings))]

[assembly: BuildConfigValue("{$Build:@BuildTargetGroup}", "BuildTargetGroup")]

namespace UnityEditor.Build.OneBuild
{

    public static class EditorOneBuild
    {
        public const string PackageName = "unity.build.onebuild";
        public static string ConfigDir = $"ProjectSettings/Packages/{PackageName}/Build";
        public static bool log = true;
        public const string BuildLogPrefix = "[Build] ";

        static Dictionary<string, ConfigValue> configs;


        /// <summary>
        /// 版本号作为文本文件独立出去，可以方便让其它工具生成
        /// </summary>
        public static string VersionFileName = "version.txt";
        public static string DefaultBuildPriority = "platform;channel;debug;user";
        public const string BuildPriorityFileName = "priority.txt";
        public const string OneBuildKeyPrefix = "OneBuild.";
        public const string BuildKeyPrefix = "UnityEditor.BuildPlayer.";

        public const string ExtensionName = "build.xml";


        public const string XMLNS = "urn:schema-unity-config";
        public const string OneBuildlType = "UnityEditor.Build.OneBuild";
        public const string PlayerSettingsType = "UnityEditor.PlayerSettings";
        //public const string EditorUserBuildSettingsType = "UnityEditor.EditorUserBuildSettings";
        //public const string AdvertisementSettingsType = "UnityEditor.Advertisements.AdvertisementSettings";
        //public const string AnalyticsSettingsType = "UnityEditor.Analytics.AnalyticsSettings";

        public const int BuildMenuPriority = 0;

        public const int VersionMenuPriority = BuildMenuPriority + 50;
        public const string MenuPrefix = "Build/";

        public const string UserVersionMenu = MenuPrefix + "User/";
        public const int UserVersionMenuPriority = VersionMenuPriority + 100;

        public const string ChannelMenuPrefix = MenuPrefix + "Channel - ";
        public const int ChannelMenuPriority = VersionMenuPriority + 1;
        public const int PreprocessBuildOrder_Config = -1000;
        public const int PreprocessBuildOrder_BuildPlayer = 10;

        private static string packageDir;
        private static LocalizationValues editorLocalizationValues;

        public const string DebugVersionName = "debug";
        public const string UserVersionPrefix = "user-";
        public const string ChannelVersionPrefix = "channel-";

        public static string PackageDir
        {
            get
            {
                if (string.IsNullOrEmpty(packageDir))
                    packageDir = GetPackageDirectory(PackageName);
                return packageDir;
            }
        }


        public static bool IsBuilding
        {
            get;
            internal set;
        }



        public static string VersionFilePath
        {
            get => "Assets/Build/" + VersionFileName;
        }


        public static Dictionary<string, string[]> Configs
        {
            get { return configs.ToDictionary(o => o.Key, o => o.Value.values); }
        }




        public static LocalizationValues EditorLocalizationValues
        {
            get
            {
                if (editorLocalizationValues == null)
                    editorLocalizationValues = new DirectoryLocalizationValues(Path.Combine(PackageDir, "Editor/Localization"));
                return editorLocalizationValues;
            }
        }


        [MenuItem("Build/Build", priority = BuildMenuPriority)]
        public static void Build()
        {
            if (!EnsureConfig())
                return;
            var options = CreateBuildOptions();
            Build(options);
        }

        [MenuItem("Build/Build And Run", priority = BuildMenuPriority)]
        public static void BuildAndRun()
        {
            if (!EnsureConfig())
                return;
            var options = CreateBuildOptions();
            options.isRun = true;
            Build(options);
        }

        //[MenuItem("Build/Load Settings", priority = BuildMenuPriority + 1)]
        public static void UpdateConfig()
        {
            UpdateConfig(UserBuildSettings.AvalibleVersionName);
        }

        //[MenuItem("Build/Build Assets", priority = 3)]
        public static void BuildAssetsMenu()
        {
            Build(UserBuildSettings.GetAvalibleVersionName("assets"));
        }



        [MenuItem("Build/Release", priority = VersionMenuPriority)]
        public static void VersionName_Release()
        {
            UserBuildSettings.IsDebug = false;
        }
        [MenuItem("Build/Release", priority = VersionMenuPriority, validate = true)]
        public static bool VersionName_Release_Validate()
        {
            Menu.SetChecked("Build/Release", !UserBuildSettings.IsDebug);
            return true;
        }
        [MenuItem("Build/Debug", priority = VersionMenuPriority)]
        public static void VersionName_Debug()
        {
            UserBuildSettings.IsDebug = true;
        }


        [MenuItem("Build/Debug", priority = VersionMenuPriority, validate = true)]
        public static bool VersionName_Debug_Validate()
        {
            Menu.SetChecked("Build/Debug", UserBuildSettings.IsDebug);
            return true;
        }


        #region UserVersion

        internal const string BuildVersionNoUserMenu = "Build/No User";



        [MenuItem(BuildVersionNoUserMenu, priority = UserVersionMenuPriority - 1)]
        public static void BuildVersionName_NoUser()
        {
            UserBuildSettings.IsNoUser = !UserBuildSettings.IsNoUser;
        }

        [MenuItem(BuildVersionNoUserMenu, priority = UserVersionMenuPriority, validate = true)]
        public static bool BuildVersionName_None_Validate()
        {
            Menu.SetChecked(BuildVersionNoUserMenu, UserBuildSettings.IsNoUser);
            return true;
        }

        #endregion

        [MenuItem(MenuPrefix + "Help", priority = UserVersionMenuPriority + 20)]
        static void OpenREADME_Menu()
        {
            string assetPath = Path.Combine(PackageDir, "README.md");
            Application.OpenURL(Path.GetFullPath(assetPath));
        }
        //2020/9/1
        private static string GetPackageDirectory(string packageName)
        {
            string path = Path.Combine("Packages", packageName);
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "package.json")))
                return path;

            foreach (var dir in Directory.GetDirectories("Assets", "*", SearchOption.AllDirectories))
            {
                if (string.Equals(Path.GetFileName(dir), packageName, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (File.Exists(Path.Combine(dir, "package.json")))
                        return dir;
                }
            }

            foreach (var pkgPath in Directory.GetFiles("Assets", "package.json", SearchOption.AllDirectories))
            {
                try
                {
                    if (JsonUtility.FromJson<UnityPackage>(File.ReadAllText(pkgPath, System.Text.Encoding.UTF8)).name == packageName)
                    {
                        return Path.GetDirectoryName(pkgPath);
                    }
                }
                catch { }
            }

            return null;
        }
        [Serializable]
        class UnityPackage
        {
            public string name;
        }


        public static EditorBuildOptions CreateBuildOptions()
        {
            EditorBuildOptions options = new EditorBuildOptions();
            options.isBatchMode = Application.isBatchMode;
            options.isDebug = UserBuildSettings.IsDebug;

            options.channel = BuildSettings.Channel;

            options.scenes = EditorBuildSettings.scenes.Where(o => o.enabled && !string.IsNullOrEmpty(o.path) && string.IsNullOrEmpty(AssetDatabase.GetImplicitAssetBundleName(o.path))).Select(o => o.path).ToArray();

            options.UtcTime = DateTime.UtcNow;

            if (options.isBatchMode)
            {
                HandleCmdLineArgs(options);
            }
            else
            {
                options.versionName = UserBuildSettings.AvalibleVersionName;
            }

            if (string.IsNullOrEmpty(options.logFile))
            {
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    string path = Environment.GetEnvironmentVariable("LOCALAPPDATA");
                    path = Path.Combine(path, "Unity/Editor/Editor.log");
                    if (File.Exists(path))
                        options.logFile = path;
                }
                else
                {

                }
            }

            return options;
        }

        public static void UpdateBuildOptions()
        {
            EditorBuildOptions options = EditorBuildOptions.Instance;
            EditorBuildOptions.Instance.version = BuildSettings.Version;
            string outputPath = Path.Combine(BuildSettings.OutputDir ?? "", BuildSettings.OutputFileName ?? "");
            options.outputPath = outputPath;
            options.showFolder = BuildSettings.ShowFolder;
            options.setPodfileModularHeaders = BuildSettings.SetPodfileModularHeaders;
            options.options = BuildSettings.Options;
            EditorBuildOptions.Save();
            OneBuildConfigEditorWindow.UpdateBuildOptions();
        }

        static void HandleCmdLineArgs(EditorBuildOptions options)
        {
            var cmdLineArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < cmdLineArgs.Length; i++)
            {
                if (cmdLineArgs[i] == "-logFile")
                {
                    if (i < cmdLineArgs.Length - 1)
                        options.logFile = cmdLineArgs[i + 1];
                }
            }


            var variables = Environment.GetEnvironmentVariables();
            foreach (string key in variables.Keys)
            {
                Type type = null;
                object obj = null;
                string memberName = null;
                MemberInfo member = null;
                if (key.StartsWith("BUILD_OPTIONS_"))
                {
                    type = typeof(EditorBuildOptions);
                    memberName = key.Substring("BUILD_OPTIONS_".Length);
                    obj = options;
                }
                else if (key.StartsWith("BUILD_SETTINGS_"))
                {
                    type = typeof(BuildSettings);
                    memberName = key.Substring("BUILD_SETTINGS_".Length);
                }
                if (type != null && !string.IsNullOrEmpty(memberName))
                {
                    member = type.GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.SetField | BindingFlags.SetProperty).FirstOrDefault();
                    if (member != null)
                    {
                        PropertyInfo pInfo = member as PropertyInfo;
                        if (pInfo != null)
                        {
                            object value = Convert.ChangeType(variables[key], pInfo.PropertyType);
                            if (pInfo.GetSetMethod().IsStatic)
                                pInfo.SetValue(null, value);
                            else
                                pInfo.SetValue(obj, value);
                        }
                        else
                        {
                            FieldInfo fInfo = member as FieldInfo;
                            if (fInfo != null)
                            {
                                object value = Convert.ChangeType(variables[key], fInfo.FieldType);
                                if (fInfo.IsStatic)
                                    fInfo.SetValue(null, value);
                                else
                                    fInfo.SetValue(obj, value);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("not found member: " + key);
                    }
                }
            }
        }

        public static void Build(string versionName)
        {
            var options = CreateBuildOptions();
            options.versionName = versionName;
            Build(options);
        }

        public static void Build(EditorBuildOptions buildOptions)
        {
            if (buildOptions == null)
                buildOptions = CreateBuildOptions();

            EditorBuildOptions.Instance = buildOptions;
            OneBuildConfigEditorWindow.UpdateBuildOptions();

            BuildPlayer(buildOptions);
        }



        public static bool IsBuildFIle(string path)
        {
            string fileName = Path.GetFileName(path);
            if (!(string.Equals(Path.GetFileName(fileName), ExtensionName, StringComparison.InvariantCultureIgnoreCase) ||
                fileName.EndsWith("." + ExtensionName, StringComparison.InvariantCultureIgnoreCase)))
                return false;
            return true;
        }

        public static string[] GetFileKeywords(string path)
        {
            string fileName = Path.GetFileName(path);
            string[] tmp;
            if (fileName.Equals(ExtensionName))
                tmp = new string[0];
            else
                tmp = fileName.Substring(0, fileName.Length - (ExtensionName.Length + 1)).Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            tmp = tmp.Select(o => o.Trim().ToLower()).Distinct().ToArray();
            return tmp;
        }

        public static bool ContainsKeyword(string path, string keyword)
        {
            string[] keywords = GetFileKeywords(path);
            return keywords.Where(o => UserBuildSettings.ConvertToShortKeyword(o).Equals(keyword, StringComparison.InvariantCultureIgnoreCase)).Count() > 0;
        }

        /// <summary>
        /// 排序
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static string[] SortFilePaths(string[] paths)
        {
            string[] keywords = null;
            string priorityFile = ConfigDir + "/" + BuildPriorityFileName;
            if (File.Exists(priorityFile))
            {
                keywords = File.ReadAllLines(priorityFile, Encoding.UTF8)[0].Split(';');
            }
            if (keywords == null || keywords.Length == 0)
            {
                keywords = DefaultBuildPriority.Split(';');
            }
            return SortFilePaths(paths, keywords);
        }
        public static string[] SortFilePaths(string[] paths, string[] keywords)
        {
            for (int i = keywords.Length - 1; i >= 0; i--)
            {
                string keyword = keywords[i];
                paths = paths.OrderBy(file =>
                {
                    return ContainsKeyword(file, keyword);
                }).ToArray();
            }
            return paths;
        }

        private static Dictionary<string, ConfigValue> LoadConfig(string version, StringBuilder log = null)
        {
            var configs = new Dictionary<string, ConfigValue>(StringComparer.InvariantCultureIgnoreCase);

            Regex nsRegex = new Regex("type:([^ $]+)", RegexOptions.IgnoreCase);

            string[] versionParts = version.Trim().ToLower().Split(new string[] { UserBuildSettings.VersionNameSeparator }, StringSplitOptions.RemoveEmptyEntries);
            if (!versionParts.Contains("build"))
            {
                var tmp = new List<string>(versionParts);
                tmp.Add("build");
                versionParts = tmp.ToArray();
            }
            List<string> files = new List<string>();

            if (log != null)
                log.AppendLine(BuildLogPrefix + "Load Settings");

            foreach (var file in Directory.GetFiles(ConfigDir))
            {
                if (!IsBuildFIle(file))
                    continue;
                string[] tmp = GetFileKeywords(file);
                if (tmp.Where(o => BuildTargetGroupToPlatformName(BuildSettings.BuildTargetGroup) == o ||
                versionParts.Contains(o, StringComparer.InvariantCultureIgnoreCase))
                    .Count() == tmp.Length)
                {
                    files.Add(file);
                }
            }

            files = SortFilePaths(files.ToArray()).ToList();


            //Debug.Log("load settings files:\n" + string.Join("\n", files.ToArray()));

            Dictionary<string, Type> fileTypes = new Dictionary<string, Type>();
            List<ConfigValue> fileValues = new List<ConfigValue>();
            string filePath = null;


            Action<XmlNode, Type, bool, string> parseNode = (node, type, isData, dataTypeName) =>
               {

                   ConfigValue configValue = new ConfigValue()
                   {
                       memberName = node.LocalName,
                       isData = isData,
                   };
                   configValue.type = type;
                   configValue.combine = GetAttributeValue(node, "combine", null);
                   string combinOptionsStr = GetAttributeValue(node, "combineOptions", null);
                   if (!string.IsNullOrEmpty(combinOptionsStr))
                       configValue.combineOptions = (CombineOptions)ParseEnum(typeof(CombineOptions), combinOptionsStr);

                   var valueNodes = node.SelectNodes("*");
                   List<string> argsKeys = new List<string>();
                   if (valueNodes.Count > 0)
                   {
                       configValue.values = new string[valueNodes.Count];
                       for (int i = 0; i < valueNodes.Count; i++)
                       {
                           var valueNode = valueNodes[i];
                           configValue.values[i] = valueNode.InnerText;
                           //key true
                           if (valueNode.Attributes["key"] != null)
                           {
                               var keyAttr = valueNode.Attributes["key"];
                               bool b;
                               if (bool.TryParse(keyAttr.Value, out b))
                               {
                                   argsKeys.Add(configValue.values[i]);
                               }
                           }
                           //combinValue index
                           if (valueNode.Attributes["combineValue"] != null)
                           {
                               configValue.combineValueIndex = i;
                           }
                       }
                   }
                   else
                   {
                       configValue.values = new string[] { node.InnerText };
                   }

                   if (isData)
                   {
                       //if (node.Attributes["key"] == null)
                       //    throw new Exception("key not found. " + node.OuterXml);
                       //configValue.key = node.Attributes["key"].InnerText;
                       if (argsKeys.Count > 0)
                       {
                           configValue.key = argsKeys[0];
                           configValue.uniqueKey = GetKey(dataTypeName, null, argsKeys.ToArray());
                       }
                       else
                       {
                           configValue.key = configValue.memberName;
                           configValue.uniqueKey = GetKey(dataTypeName, configValue.key, argsKeys.ToArray());
                       }
                   }
                   else
                   {
                       configValue.uniqueKey = GetKey(type, configValue.memberName, argsKeys.ToArray());
                       configValue.member = FindSetMember(configValue.type, configValue.memberName);
                       if (configValue.member == null)
                           Debug.LogError("not found member. " + configValue + ", file:" + filePath);
                   }

                   fileValues.Add(configValue);


               };


            foreach (var file in files)
            {
                if (log != null)
                {
                    log.AppendFormat("{0}", file)
                        .AppendLine();
                }
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
                            {
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
                                    parseNode(itemNode, type, false, null);
                                }
                            }
                            break;
                        case "data":
                            {
                                var typeAttr = node.Attributes["type"];

                                if (typeAttr == null)
                                    throw new Exception("node not name attribute. " + node.LocalName + " file:" + filePath);
                                string typeName = typeAttr.Value;
                                foreach (XmlNode itemNode in node.SelectNodes("*"))
                                {
                                    parseNode(itemNode, null, true, typeName);
                                }
                            }
                            break;
                        default:
                            Debug.LogErrorFormat(BuildLogPrefix + "Unknown node: {0}", " node:" + node.LocalName + " , file: " + filePath);
                            break;
                    }
                }

                Dictionary<string, object> scope = new Dictionary<string, object>();
                foreach (var item in fileTypes)
                {
                    scope.Add(item.Key, item.Value);
                }


                foreach (var value in fileValues)
                {
                    value.scope = scope;
                    if (!string.IsNullOrEmpty(value.combine))
                    {
                        ConfigValue oldValue = null;
                        if (configs.ContainsKey(value.uniqueKey))
                        {
                            oldValue = configs[value.uniqueKey];
                        }
                        if (oldValue == null)
                        {
                            if ((value.combineOptions & CombineOptions.Remove) == CombineOptions.Remove)
                            {
                                continue;
                            }

                            configs[value.uniqueKey] = value;
                        }
                        else
                        {
                            oldValue.Combine(value.values, value.combine, value.combineOptions);
                        }
                    }
                    else
                    {
                        configs[value.uniqueKey] = value;
                    }
                }

                //Debug.Log("file:" + file + "\n" + ToString(fileValues));
            }
            ReplaceTemplate(configs);

            return configs;
        }


        static string[] ParsePriorityOrder(string str)
        {
            return str.Split(';');

            //Dictionary<string, int> order = new Dictionary<string, int>();
            //int n = 0;
            //foreach (var item in str.Split(';'))
            //{
            //    if (item.Length == 0)
            //        continue;
            //    //string[] parts = item.Split('=');
            //    //string name = parts[0].ToLower().Trim();
            //    string name;
            //    name = item;
            //    //int n = 0;
            //    //if (parts.Length > 1 && !int.TryParse(parts[1], out n))
            //    //{
            //    //    n = 0;
            //    //}
            //    order[name] = n;
            //    n++;
            //}
            //return order;
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


        public static string BuildTargetGroupToPlatformName(BuildTargetGroup buildTargetGrounp)
        {
            //old BuildTargetGroup.iPhone;
            if ((int)buildTargetGrounp == (int)BuildTargetGroup.iOS)
                return "ios";
            return buildTargetGrounp.ToString().ToLower();
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
                sb.AppendFormat("    \"{0}\": [\"{1}\"]", value.uniqueKey, string.Join(",", value.values))
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
                return Enum.Parse(enumType, str);
            }
            else
            {
                return Enum.Parse(enumType, str);
            }
        }

        static Dictionary<Type, Dictionary<string, MemberInfo>> cachedMembers;


        public static Dictionary<string, MemberInfo> GetSetMembers(Type type)
        {
            if (cachedMembers == null)
                cachedMembers = new Dictionary<Type, Dictionary<string, MemberInfo>>();
            Dictionary<string, MemberInfo> members;
            if (!cachedMembers.TryGetValue(type, out members))
            {
                members = new Dictionary<string, MemberInfo>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var mInfo in type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.SetField | BindingFlags.SetProperty | BindingFlags.InvokeMethod))
                {
                    if (!(mInfo.MemberType == MemberTypes.Property || mInfo.MemberType == MemberTypes.Field || mInfo.MemberType == MemberTypes.Method))
                        continue;
                    if (mInfo.IsDefined(typeof(CompilerGeneratedAttribute), true))
                        continue;
                    FieldInfo fInfo = mInfo as FieldInfo;
                    if (fInfo != null)
                    {
                        continue;
                    }
                    members[mInfo.Name] = mInfo;
                }

                foreach (var member in members.Values.ToArray())
                {
                    if (member.MemberType == MemberTypes.Method)
                    {
                        if (member.Name.StartsWith("set_") || member.Name.StartsWith("get_"))
                        {
                            MemberInfo tmp;
                            if (members.TryGetValue(member.Name.Substring(4), out tmp))
                            {
                                if (tmp.MemberType == MemberTypes.Property)
                                    members.Remove(member.Name);
                            }
                        }
                    }
                }


                cachedMembers[type] = members;
            }
            return members;
        }

        public static MemberInfo FindSetMember(Type type, string memberName) //ConfigValue value)
        {
            string[] parts = memberName.Split('.');
            MemberInfo member = null;
            // Type type = value.type;
            string lowerMemberName = memberName.ToLower();

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
        public static Type FindType(string typeName)
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
            configs = LoadConfig(version);
            foreach (var item in configs.Values)
            {
                item.SetValue();
            }
            PlayerPrefs.Save();
            configs = LoadConfig(version, log);
            foreach (var item in configs.Values)
            {
                if (item.isTemplateValue)
                {
                    item.SetValue();
                }
            }
            PlayerPrefs.Save();
            AssetDatabase.SaveAssets();
            //if (ClearLog)
            //{
            //_ClearLog();
            //}

            log.AppendLine(ToString(configs.Values));

            // if (LogEnable)
            {
                Debug.Log(log.ToString());
            }


            GenerateMenuCode();

            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();
        }


        static void BuildAssetBundles()
        {

            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            string outputDir = BuildSettings.OutputDir;
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

            configs = LoadConfig(EditorBuildOptions.Instance.versionName);


            if (target == BuildTarget.iOS)
            {/*
                iOSPostProcessBuild(pathToBuiltProject);

                string projPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";

                PBXProject pbxProj = new PBXProject();

                pbxProj.ReadFromFile(projPath);
                string targetGuid = pbxProj.TargetGuidByName("Unity-iPhone");
                //pbxProj.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");


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

                plist.WriteToFile(plistPath);*/
            }
            else if (target == BuildTarget.Android)
            {
                AndroidPostProcessBuild(pathToBuiltProject);
            }


            string outputPath = pathToBuiltProject;

            //File.WriteAllText(outputPath + ".txt", sb.ToString());
            Debug.Log(BuildLogPrefix + "Output Path: " + pathToBuiltProject);
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
            string key;

            if (memberName != null)
                key = typeName + "." + memberName;
            else
                key = typeName;
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
        static Regex tplRegex = new Regex("\\{\\$(?<name>[^:\\}]+)(:(?<format>[^\\}]*?))?\\}");

        /// <summary>
        /// Template: {$Key,FormatString}
        /// </summary>
        /// <param name="input"></param>
        private static void ReplaceTemplate(Dictionary<string, ConfigValue> input)
        {
            string[] values;
            ConfigValue configValue;
            foreach (var key in input.Keys.ToArray())
            {
                configValue = input[key];
                values = configValue.values;
                if (values == null)
                    continue;
                var formatValueProvider = new ConfigKeyFormatValueProvider(input, configValue);
                for (int i = 0; i < values.Length; i++)
                {
                    string value = values[i];
                    try
                    {
                        value = value.FormatStringWithKey(formatValueProvider);
                        if (value != values[i])
                        {
                            configValue.isTemplateValue = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("template error. key: <" + key + ">, value: <" + value + ">");
                        throw ex;
                    }
                    values[i] = value;
                }
            }
        }


        class ConfigKeyFormatValueProvider : IKeyFormatStringValueProvider
        {
            public Dictionary<string, ConfigValue> values;
            public ConfigValue configValue;

            public ConfigKeyFormatValueProvider(Dictionary<string, ConfigValue> values, ConfigValue value)
            {
                this.values = values;
                this.configValue = value;
            }

            public object GetFormatValue(string key)
            {
                if (values.ContainsKey(key))
                {
                    string value;
                    var v = values[key];
                    if (v.values != null && v.values.Length > 0)
                        value = v.values[0];
                    else
                        value = null;
                    return value;
                }
                else
                {
                    object value;
                    if (configValue.scope != null && configValue.scope.TryGetValue(key, out value))
                        return value;

                    Type type = Type.GetType(key);
                    if (type != null)
                        return type;

                    throw new Exception("not found key:" + key);
                }
            }
        }

        static void ValidateScene()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            if (scenes.Length == 0)
                throw new Exception("build scene count 0");

            for (int i = 0; i < scenes.Length; i++)
            {
                string assetPath = scenes[i].path;
                if (string.IsNullOrEmpty(assetPath))
                    throw new Exception("missing scene, index: " + i);
            }
        }

        static void BuildPlayer(EditorBuildOptions buildOptions)
        {

            ValidateScene();

            var task = new EditorTask();
            task.progressBarEnabled = true;
            task.AddOnStarted(BuildCallbackStarted);
            task.AddOnTaskBefore(BuildCallbackBefore);
            task.AddOnTaskAfter(BuildCallbackAfter);
            task.AddOnEnded(BuildCallbackEnded);

            Debug.Log(BuildLogPrefix + "Task List Begin");

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

                //{typeof(PreProcessBuildAttribute).Name}: 
                task.AddAction((Action)Delegate.CreateDelegate(typeof(Action), mInfo), $"{mInfo.Name}");

                Debug.LogFormat(BuildLogPrefix + task.tasks.Count + ", [{0}.{1}]", mInfo.DeclaringType.FullName, mInfo.Name);
            }

            foreach (var obj in GetBuildPipelines())
            {
                var mInfo = obj.GetType().GetMethod("PreBuild");
                if (mInfo != null)
                {
                    task.AddAction((Action)Delegate.CreateDelegate(typeof(Action), obj, mInfo), $"{mInfo.Name}");
                }
            }

            Debug.Log(BuildLogPrefix + "Task List End");
            task.Run();

        }

        static List<object> buildPipelines;
        static List<object> GetBuildPipelines()
        {
            if (buildPipelines == null)
            {
                buildPipelines = new List<object>();
                foreach (var type in FindBaseNameTypes(typeof(IBuildPipeline).FullName))
                {
                    if (type.IsAbstract)
                        continue;
                    var obj = Activator.CreateInstance(type);
                    buildPipelines.Add(obj);
                }
            }
            return buildPipelines;
        }

        public static IEnumerable<Type> FindBaseNameTypes(string baseTypeName)
        {

            HashSet<Type> baseTypes = new HashSet<Type>();
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = ass.GetType(baseTypeName, false);
                if (type != null)
                {
                    baseTypes.Add(type);
                }
            }
            HashSet<Type> types = new HashSet<Type>();
            foreach (var baseType in baseTypes)
            {
                foreach (var type in AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(o => PreProcessBuildAttribute.IsDependent(o, new Assembly[] { baseType.Assembly }))
                    .SelectMany(o => o.GetTypes()))
                {
                    if (baseType.IsAssignableFrom(type))
                    {
                        types.Add(type);
                    }
                }
            }
            return types;
        }

        static void BuildCallbackStarted()
        {
            Debug.Log(BuildLogPrefix + "build start");
            IsBuilding = true;


            if (!string.IsNullOrEmpty(BuildSettings.GitTagVersion))
            {
                SaveGitTagVersion(BuildSettings.GitTagVersion);
            }

            if (BuildSettings.IncrementVersion >= 0)
            {
                IncrementVersion(BuildSettings.IncrementVersion);
                Debug.Log(BuildLogPrefix + "Increment Version: " + BuildSettings.Version);
            }

            if (BuildSettings.IncrementVersionCode)
            {
                BuildSettings.VersionCode = BuildSettings.VersionCode + 1;
                Debug.Log(BuildLogPrefix + "Increment Version Code: " + BuildSettings.VersionCode);
            }

            foreach (var obj in GetBuildPipelines())
            {
                var method = obj.GetType().GetMethod("BuildStarted");
                if (method != null)
                    method.Invoke(obj, null);
            }

        }

        static void BuildCallbackBefore()
        {
            IsBuilding = true;
        }
        static void BuildCallbackAfter()
        {
            IsBuilding = false;
        }
        static void BuildCallbackEnded()
        {
            //确保设置为false            
            IsBuilding = false;
            try
            {
                foreach (var obj in GetBuildPipelines())
                {
                    var method = obj.GetType().GetMethod("BuildEnded");
                    if (method != null)
                        method.Invoke(obj, null);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
            }

            Debug.Log(BuildLogPrefix + $"build completed. ({(DateTime.UtcNow - EditorBuildOptions.Instance.UtcTime).TotalSeconds.ToString("0.#")}s) ");

            //复制日志文件
            EditorBuildOptions options = EditorBuildOptions.Instance;
            if (!string.IsNullOrEmpty(options.logFile))
            {
                if (File.Exists(options.logFile))
                {
                    string dstFile = options.outputPath + ".log";
                    if (!Directory.Exists(Path.GetDirectoryName(dstFile)))
                        Directory.CreateDirectory(Path.GetDirectoryName(dstFile));
                    File.Copy(options.logFile, dstFile, true);
                }
            }

            RunBuildEndCmd();

            if (options.isRun)
            {
                //   BuildDeviceWindow.Uninstall(null, true);
                InstallWindow.InstallAndRun(null, options.outputPath);
            }

        }

        const string ProgressBarTitle = "Build Player";

        #region PreProcessBuild

        [PreProcessBuild(PreProcessBuildAttribute.Order_Config)]
        static void PreProcessBuild_LoadSettings()
        {
            using (var progressBar = new EditorProgressBar("Load Settings"))
            {
                progressBar.OnProgress("update config", 0.1f);
                UpdateConfig(EditorBuildOptions.Instance.versionName);
                UpdateBuildOptions();
                Debug.Log(BuildLogPrefix + "Version: " + EditorBuildOptions.Instance.version);
            }

            using (var progressBar = new EditorProgressBar("Run Start cmd"))
            {
                RunBuildStartCmd();
            }
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

            EditorBuildOptions editorBuildOptions = EditorBuildOptions.Instance;
            if (configs == null)
            {
                configs = LoadConfig(editorBuildOptions.versionName);
            }

            if (Contains(GetKey(OneBuildlType, "BuildAssetBundles")))
            {
                BuildAssetBundles();
                return;
            }

            Debug.Log(BuildLogPrefix + "Options: " + JsonUtility.ToJson(EditorBuildOptions.Instance, true));

            if (editorBuildOptions.isPreBuild)
                return;

            string outputPath = editorBuildOptions.outputPath;
            if (string.IsNullOrEmpty(outputPath))
                throw new Exception("output path empty");


            string[] scenes = editorBuildOptions.scenes;
            BuildOptions buildOptions = editorBuildOptions.options;

            //if (File.Exists(outputPath))
            //{
            //    DeleteDirectoryFiles(Path.GetDirectoryName(outputPath));
            //}
            //else if (Directory.Exists(outputPath))
            //{
            //    DeleteDirectoryFiles(outputPath);
            //}

            if (EditorUserBuildSettings.development)
                buildOptions |= BuildOptions.Development;
            if (EditorUserBuildSettings.connectProfiler)
                buildOptions |= BuildOptions.ConnectWithProfiler;
            if (EditorUserBuildSettings.buildScriptsOnly)
                buildOptions |= BuildOptions.BuildScriptsOnly;
            if (EditorUserBuildSettings.allowDebugging)
                buildOptions |= BuildOptions.AllowDebugging;

            if (scenes == null || scenes.Length == 0)
                throw new Exception("build player scenes empty");
            BuildPlayerOptions opts = new BuildPlayerOptions();
            opts.scenes = scenes;
            opts.locationPathName = outputPath;
            opts.target = EditorUserBuildSettings.activeBuildTarget;
            opts.options = buildOptions;
            opts.assetBundleManifestPath = editorBuildOptions.assetBundleManifestPath;

            if (string.IsNullOrEmpty(opts.assetBundleManifestPath) && !string.IsNullOrEmpty(BuildSettings.AssetBundleManifestPath))
            {
                opts.assetBundleManifestPath = BuildSettings.AssetBundleManifestPath;
            }
            if (!string.IsNullOrEmpty(opts.assetBundleManifestPath))
            {
                if (!File.Exists(opts.assetBundleManifestPath))
                    throw new Exception($"asset bundle manifest file not exists <{opts.assetBundleManifestPath}>");
            }

            using (var progressBar = new EditorProgressBar(ProgressBarTitle))
            {
                Debug.Log($@"BuildPlayerOptions:
                locationPathName: {opts.locationPathName}
                target: {opts.target}
                options: {opts.options}
                assetBundleManifestPath: {opts.assetBundleManifestPath}
                scenes: [{string.Join("\n", opts.scenes)}]");

                var report = BuildPipeline.BuildPlayer(opts);

                if (report.summary.result != BuildResult.Succeeded)
                    throw new Exception("" + report.summary.result);

                //if (ShowFolder)
                EditorUtility.RevealInFinder(outputPath);
            }

        }

        #endregion


        #region 命令行

        static IEnumerable<string> EnumateCmdFiles(string action)
        {
            foreach (var file in Directory.GetFiles(Path.GetFullPath(ConfigDir), "*.build.cmd", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(file);
                string[] parts = fileName.Split('.');
                string actionName = "";
                if (parts.Length >= 3)
                {
                    actionName = parts[parts.Length - 3];
                }
                if (actionName.Equals(action, StringComparison.InvariantCultureIgnoreCase))
                    yield return Path.GetFullPath(file);
            }
        }

        static IDictionary<string, string> BuildCmdArgs()
        {
            IDictionary<string, string> dic;

            dic = new Dictionary<string, string>();

            if (EditorBuildOptions.Instance.isDebug)
            {
                dic["DEBUG"] = "true";
            }
            else
            {
                dic["DEBUG"] = "false";
            }

            dic["PLATFORM"] = BuildSettings.BuildTargetGroup.ToString().ToLower();
            var buildOptions = EditorBuildOptions.Instance;
            dic["VERSION"] = buildOptions.version;
            dic["VERSION_CODE"] = BuildSettings.VersionCode.ToString();
            dic["CHANNEL"] = buildOptions.channel;
            dic["OUTPUT_PATH"] = Path.GetFullPath(buildOptions.outputPath);
            dic["OUTPUT_DIR"] = Path.GetFullPath(BuildSettings.OutputDir);
            dic["OUTPUT_FILENAME"] = BuildSettings.OutputFileName;
            dic["BUILD_TIMESTAMP"] = buildOptions.timestamp.ToString();
            dic["BUILD_TIME_YEAR"] = buildOptions.LocalTime.Year.ToString();
            dic["BUILD_TIME_MONTH"] = buildOptions.LocalTime.Month.ToString();
            dic["BUILD_TIME_DAY"] = buildOptions.LocalTime.Day.ToString();
            dic["BUILD_TIME_HOUR"] = buildOptions.LocalTime.Hour.ToString();
            dic["BUILD_TIME_MINUTE"] = buildOptions.LocalTime.Minute.ToString();
            dic["BUILD_TIME_SECOND"] = buildOptions.LocalTime.Second.ToString();


            return dic;
        }

        static void RunBuildStartCmd()
        {
            StringBuilder sb = new StringBuilder();
            var env = BuildCmdArgs();

            string args = sb.ToString();
            foreach (var file in EnumateCmdFiles("start"))
            {
                Debug.Log("execute start cmd: " + file);
                StartProcess(file, args, workingDirectory: Path.GetFullPath("."), env: env);
            }
        }

        static void RunBuildEndCmd()
        {
            StringBuilder sb = new StringBuilder();
            var env = BuildCmdArgs();

            string args = sb.ToString();
            foreach (var file in EnumateCmdFiles("end"))
            {
                Debug.Log("execute end cmd: " + file);
                StartProcess(file, args, workingDirectory: Path.GetFullPath("."), env: env);
            }
        }

        static string StartProcess(string filePath, string args, string workingDirectory = null, IDictionary<string, string> env = null)
        {

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = filePath;
            startInfo.Arguments = args;
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                startInfo.WorkingDirectory = Path.GetFullPath(workingDirectory);
            }
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            //startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.RedirectStandardError = true;
            //startInfo.StandardErrorEncoding = Encoding.UTF8;
            if (env != null)
            {
                foreach (var item in env)
                {
                    startInfo.EnvironmentVariables[item.Key] = item.Value;
                }

            }

            string result;
            using (var p = Process.Start(startInfo))
            {
                p.WaitForExit(10000);
                if (!p.HasExited)
                    throw new Exception("cmd timeout: " + filePath);
                result = p.StandardOutput.ReadToEnd();

                if (p.ExitCode != 0)
                {
                    if (!string.IsNullOrEmpty(result))
                    {
                        throw new Exception(result + "\n" + args);
                    }
                    else
                    {
                        throw new Exception("error code: " + p.ExitCode + "\n" + args);
                    }
                }
            }
            Debug.Log(BuildLogPrefix + "run cmd\n" + filePath + " " + args + "\n" + result);

            return result;
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
                if ((options & CombineOptions.Replace) != CombineOptions.Replace)
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
            public string uniqueKey;
            public Type type;
            public string memberName;
            public MemberInfo member;
            public string[] values;
            public string combine;
            public CombineOptions combineOptions;
            public int combineValueIndex;
            public bool isData;
            public Dictionary<string, object> scope;
            public bool isTemplateValue;
            public void Combine(string[] newValues, string separator, CombineOptions options)
            {
                //for (int i = 0; i < newValues.Length && i < newValues.Length; i++)
                {
                    this.values[combineValueIndex] = CombinString(this.values[combineValueIndex], newValues[combineValueIndex], separator, options);
                }
            }

            public void SetValue()
            {
                if (isData)
                    return;
                object value = null, oldValue = null;
                if (member == null)
                {
                    Debug.LogError("Not Find Member: " + uniqueKey);
                }
                try
                {
                    if (member is PropertyInfo)
                    {
                        PropertyInfo pInfo = (PropertyInfo)member;
                        value = ChangeType(values, pInfo.PropertyType);
                        if (pInfo.GetMethod != null)
                        {
                            oldValue = pInfo.GetValue(null, null);
                            if (object.Equals(value, oldValue))
                                return;
                        }
                        pInfo.SetValue(null, value, null);
                        //Debug.Log("Set " + pInfo.Name + " " + oldValue + "=>" + value);
                    }
                    else if (member is FieldInfo)
                    {
                        FieldInfo fInfo = (FieldInfo)member;
                        value = ChangeType(values, fInfo.FieldType);
                        oldValue = fInfo.GetValue(null);
                        if (object.Equals(value, oldValue))
                            return;
                        fInfo.SetValue(null, value);
                        //Debug.Log("Set " + fInfo.Name + " " + oldValue + "=>" + value);
                    }
                    else if (member is MethodInfo)
                    {
                        MethodInfo mInfo = (MethodInfo)member;
                        object[] args = mInfo.GetParameters()
                            .Select((o, i) => ChangeType(values[i], o.ParameterType))
                            .ToArray();
                        if (args.Length == 2 && args[0] != null && args[0] is BuildTargetGroup && mInfo.Name.StartsWith("Set"))
                        {
                            var getMethod = mInfo.DeclaringType.GetMethod("Get" + mInfo.Name.Substring(3), new Type[] { typeof(BuildTargetGroup) });
                            if (getMethod != null)
                            {
                                value = args[1];
                                oldValue = getMethod.Invoke(null, new object[] { args[0] });
                                if (object.Equals(value, oldValue))
                                    return;
                            }
                            //Debug.Log("Set " + mInfo + " " + oldValue + "=>" + value);
                        }

                        mInfo.Invoke(null, args);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Set Member Error <{this}>  = {string.Join(",", values)}");
                    throw ex;
                }
            }

            public override string ToString()
            {
                return string.Format("item: {0} ", uniqueKey);
            }
        }



        static void GenerateMenuCode()
        {

            HashSet<string> dic = new HashSet<string>();
            foreach (var filePath in Directory.GetFiles(ConfigDir, "*.xml"))
            {
                string name = Path.GetFileNameWithoutExtension(filePath);
                string[] parts = name.ToLower().Split('.');
                foreach (var item in parts)
                {
                    dic.Add(item);
                }
            }


            StringBuilder sb = new StringBuilder();

            sb.AppendLine("//*** auto generated ***")
            .AppendLine("namespace UnityEditor.Build.OneBuild")
            .AppendLine("{")
            .AppendLine("    class _EditorBuildMenu")
            .AppendLine("    {");

            int lengthStart = sb.Length;

            Action<string, string, string, string, int, bool> add = (type, name, versionNamePrifx, menuName, menuPriority, allowMulti) =>
              {
                  string versionName = versionNamePrifx + name;

                  sb.AppendLine($"        [MenuItem(\"{menuName }\", priority = {menuPriority})]")
                  .AppendLine($"        static void Build{type}VersionMenu_{name}()")
                  .AppendLine("        {");
                  if (allowMulti)
                  {
                      sb.AppendLine($"            if (UserBuildSettings.ContainsVersion(\"{versionName}\"))")
                      .AppendLine($"                UserBuildSettings.RemoveVersion(\"{versionName}\");")
                      .AppendLine("            else")
                      .AppendLine("            {")
                      .AppendLine($"                UserBuildSettings.AddVersion(\"{versionName}\");")
                      .AppendLine("            }");
                  }
                  else
                  {
                      sb.AppendLine($"            if (UserBuildSettings.ContainsVersion(\"{versionName}\"))")
                      .AppendLine($"                UserBuildSettings.RemoveVersion(\"{versionName}\");")
                      .AppendLine("            else")
                      .AppendLine("            {")
                      .AppendLine($"                UserBuildSettings.RemoveVersionWithPrefix(\"{versionNamePrifx}\");")
                      .AppendLine($"                UserBuildSettings.AddVersion(\"{versionName}\");")
                      .AppendLine("            }");
                  }

                  sb.AppendLine("        }")
                  .AppendLine($"        [MenuItem(\"{menuName}\", validate = true)]")
                  .AppendLine($"        static bool Build{type}VersionMenu_Validate_{name}()")
                  .AppendLine("        {")
                  .AppendLine($"            Menu.SetChecked(\"{menuName}\", UserBuildSettings.ContainsVersion(\"{versionName}\"));");

                  if (type == "User")
                      sb.AppendLine("            return !UserBuildSettings.IsNoUser;");
                  else
                      sb.AppendLine("            return true;");

                  sb.AppendLine("        }")
                  .AppendLine();
              };

            foreach (var item in dic.OrderBy(o => o))
            {
                string name;
                if (item.StartsWith(UserVersionPrefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    name = item.Substring(UserVersionPrefix.Length);
                    add("User", name, UserVersionPrefix, UserVersionMenu + name, UserVersionMenuPriority, false);
                }
                if (item.StartsWith(ChannelVersionPrefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    name = item.Substring(ChannelVersionPrefix.Length);
                    add("Channel", name, ChannelVersionPrefix, ChannelMenuPrefix + name, ChannelMenuPriority, true);
                }
            }

            string csPath = "Assets/Plugins/gen/Editor/EditorOneBuildMenu.cs";


            if (sb.Length == lengthStart)
            {
                if (FileUtil.DeleteFileOrDirectory(csPath))
                {
                    AssetDatabase.Refresh();
                }
                return;
            }

            sb.AppendLine("    }")
                .AppendLine("}");

            if (!Directory.Exists(Path.GetDirectoryName(csPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(csPath));
            string codeString = sb.ToString();
            if (File.Exists(csPath))
            {
                if (File.ReadAllText(csPath, Encoding.UTF8) == codeString)
                    return;
            }

            File.WriteAllText(csPath, codeString, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(csPath);
        }


        class PreprocessBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
        {
            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildReport report)
            {

                //BuildVersion = GetVersion(VersionName);
                //Debug.Log("Build Config: " + BuildVersion);
                //UpdateConfig(BuildVersion);
                if (BuildSettings.AutoAddAssetBundleScene)
                {
                    EditorBuildSettingsRemoveAssetBundleSence();
                }
            }


            public void OnPostprocessBuild(BuildReport report)
            {
                if (BuildSettings.AutoAddAssetBundleScene)
                {
                    EditorBuildSettingsAddAssetBundleSence();
                }
                //Debug.Log("interface OnPostprocessBuild ");

                if (configs == null)
                {
                    configs = LoadConfig(EditorBuildOptions.Instance.versionName);
                }

                switch (report.summary.platformGroup)
                {
                    case BuildTargetGroup.iOS:
#if UNITY_IOS
                        PostprocessIOS(report);
#endif
                        break;
                }

            }
#if UNITY_IOS
            void PostprocessIOS(BuildReport report)
            {

                //.xcodeproj
                PBXProject pbxProj = null;
                string pbxProjPath = null;
                string targetGuid = null;
                string outputPath = report.summary.outputPath;

                foreach (var item in configs.Select(o => o.Value).Where(o => o.uniqueKey.StartsWith("UnityEditor.iOS.Xcode.PBXProject")))
                {
                    if (item.isData)
                    {
                        if (pbxProj == null)
                        {
                            //修改xcode文件
                            pbxProjPath = Path.Combine(outputPath, "Unity-iPhone.xcodeproj/project.pbxproj");
                            pbxProj = new PBXProject();
                            pbxProj.ReadFromFile(pbxProjPath);
                            targetGuid = pbxProj.TargetGuidByName("Unity-iPhone");
                        }
                        switch (item.memberName)
                        {
                            case "SetBuildProperty":
                                pbxProj.SetBuildProperty(targetGuid, item.values[0], item.values[1]);
                                break;
                            case "AddBuildProperty":
                                foreach (var val in item.values.Skip(1))
                                {
                                    pbxProj.AddBuildProperty(targetGuid, item.values[0], val);
                                }
                                break;
                            case "AddFrameworkToProject":
                                string framework = item.values[0];
                                bool weak = true;
                                if (item.values.Length > 1)
                                    weak = bool.Parse(item.values[1]);
                                pbxProj.AddFrameworkToProject(targetGuid, framework, weak);
                                break;
                            case "AddCapability":
                                PBXCapabilityType capability = typeof(PBXCapabilityType).GetField(item.values[0]).GetValue(null) as PBXCapabilityType;
                                string entitlementsFilePath = null;
                                bool addOptionalFramework = false;
                                if (item.values.Length > 1)
                                    entitlementsFilePath = item.values[1];
                                if (item.values.Length > 2)
                                    addOptionalFramework = bool.Parse(item.values[2]);
                                pbxProj.AddCapability(targetGuid, capability, entitlementsFilePath, addOptionalFramework);
                                break;
                            default:
                                throw new Exception("unknow member:" + item.memberName);
                        }
                    }
                }

                if (pbxProj != null)
                {
                    pbxProj.WriteToFile(pbxProjPath);
                }

                //Info.plist
                PlistDocument plist = null;
                string plistPath = null;
                PlistElementDict rootDict = null;

                foreach (var item in configs.Select(o => o.Value).Where(o => o.uniqueKey.StartsWith("UnityEditor.iOS.Xcode.PlistDocument")))
                {
                    if (item.isData)
                    {
                        if (plist == null)
                        {
                            plistPath = Path.Combine(outputPath, "Info.plist");
                            plist = new PlistDocument();
                            plist.ReadFromString(File.ReadAllText(plistPath));
                            rootDict = plist.root;
                        }

                        switch (item.memberName)
                        {
                            case "String":
                                rootDict.SetString(item.values[0], item.values[1]);
                                break;
                            case "Array":
                                {
                                    var array = rootDict.CreateArray(item.values[0]);
                                    foreach (var val in item.values.Skip(1))
                                        array.AddString(val);

                                }
                                break;
                        }

                    }
                }
                if (plist != null)
                {
                    plist.WriteToFile(plistPath);
                }

                //修改Podfile,刚build完Podfile不存在，延迟设置
                if (BuildSettings.SetPodfileModularHeaders)
                {
                    EditorApplication.delayCall += () =>
                    {
                        _SetPodfileModularHeaders(outputPath);
                    };
                }

            }

            static void _SetPodfileModularHeaders(string outputPath)
            {
                string podfilePath = Path.Combine(outputPath, "Podfile");
                if (!File.Exists(podfilePath))
                {
                    //Debug.LogError("Podfile not exits");
                    return;
                }
                string text = File.ReadAllText(podfilePath);
                Regex regex = new Regex("(pod [^,]+,)[^\r]*", RegexOptions.IgnoreCase | RegexOptions.Multiline);

                string newText = regex.Replace(text, m => m.Groups[1].Value + " :modular_headers => true");
                if (text != newText)
                {
                    File.WriteAllText(podfilePath, newText);
                }
            }
#endif

        }


        #region VersionFile

        public static string GetVersionFile()
        {
            string path = VersionFilePath;
            if (!File.Exists(path))
                return "";
            //throw new Exception("Version File not exists. " + path);
            string line = File.ReadAllLines(path, Encoding.UTF8)[0];
            return line;
        }

        internal static string GetVersionFile(int index, out string line)
        {
            line = GetVersionFile();
            string[] parts = line.Trim().Split('.');
            if (index >= parts.Length)
                throw new Exception("version index over. " + index + ", " + line);
            return parts[index];
        }

        public static void SaveVersionFile(string version)
        {
            string path = VersionFilePath;
            string[] lines = File.ReadAllLines(path, new UTF8Encoding(false));
            lines[0] = version;
            File.WriteAllLines(path, lines);
        }

        public static void IncrementVersionFile(int index)
        {
            string path = VersionFilePath;
            if (!File.Exists(path))
                throw new Exception("Version File not exists. " + path);
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            string[] parts = lines[0].Trim().Split('.');
            if (index >= parts.Length)
                throw new Exception("version index over. " + index + ", " + lines[0]);
            int n;
            if (!int.TryParse(parts[index], out n))
                throw new Exception("parse version num " + parts[index]);
            n++;
            parts[index] = n.ToString();
            string version = string.Join(".", parts);
            SaveVersionFile(version);
        }

        public static void IncrementVersion(int index)
        {
            string[] parts = BuildSettings.Version.Split('.');
            if (index >= parts.Length)
                throw new Exception("version index over. " + index);
            int n;
            if (!int.TryParse(parts[index], out n))
                throw new Exception("parse version num " + parts[index]);
            n++;
            parts[index] = n.ToString();
            string version = string.Join(".", parts);
            BuildSettings.Version = version;
        }


        public static void SaveGitTagVersion(string pattern)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                string version = System.VersionControl.Git.ParseTag(pattern);
                SaveVersionFile(version);
            }
        }

        #endregion



        [InitializeOnLoadMethod]
        public static void EditorBuildSettingsAddAssetBundleSence()
        {
            foreach (var assetPath in AssetDatabase.FindAssets("t:Scene").Select(o => AssetDatabase.GUIDToAssetPath(o)))
            {

                string abName = AssetDatabase.GetImplicitAssetBundleName(assetPath);
                if (!string.IsNullOrEmpty(abName))
                {
                    if (EditorBuildSettings.scenes.Where(o => o.path == assetPath).Count() == 0)
                    {
                        var scenes = EditorBuildSettings.scenes;
                        ArrayUtility.Add(ref scenes, new EditorBuildSettingsScene(assetPath, true));
                        EditorBuildSettings.scenes = scenes;
                    }
                }
            }

            //输出日志到控制台
            if (Application.isBatchMode)
            {

                //if (Application.platform == RuntimePlatform.WindowsEditor)
                //{
                //    try
                //    {
                //        Debug.Log("Initialize Console output");
                //        new ConsoleWindow().Initialize();
                //    }
                //    catch (Exception ex)
                //    {
                //        Debug.LogException(ex);
                //    }                    
                //}

                //Application.logMessageReceivedThreaded += Application_logMessageReceivedThreaded;
                //Debug.Log("LogPath:" + Application.consoleLogPath);
                //LogToConsole(Application.consoleLogPath);
            }
        }
        // if ( Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix )
        //unityPath = $"/Applications/Unity/Hub/Editor/{unityVersion}/Unity.app/Contents/MacOS/Unity";
        //    else
        //        unityPath = $"C:/Program Files/Unity/Hub/Editor/{unityVersion}/Editor/Unity.exe";
        //private static void Application_logMessageReceivedThreaded(string condition, string stackTrace, LogType type)
        //{
        //    System.IO.TextWriter writer = Console.Out;
        //    writer.WriteLine(condition);
        //    writer.WriteLine(stackTrace);
        //}

        //private static void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        //{
        //    Console.WriteLine($"{type} {condition} {stackTrace}");
        //}
        //static FileStream stream;
        //static void LogToConsole(string logPath)
        //{
        //    try
        //    {
        //        using (FileStream stream = File.Open(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        //        {
        //            using (StreamReader reader = new StreamReader(stream))
        //            {
        //                while (!process.HasExited)
        //                {
        //                    PrintFromLog(reader);
        //                    System.Threading.Thread.Sleep(500);
        //                }

        //                System.Threading.Thread.Sleep(500);
        //                PrintFromLog(reader);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //}

        //private static void PrintFromLog(StreamReader logStream)
        //{
        //    var txt = logStream.ReadToEnd();
        //    if (string.IsNullOrEmpty(txt)) return;

        //    Console.Write(txt);
        //}

        public static void EditorBuildSettingsRemoveAssetBundleSence()
        {
            EditorBuildSettings.sceneListChanged += EditorBuildSettings_sceneListChanged;
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            bool changed = false;
            for (int i = 0; i < scenes.Count; i++)
            {

                string assetPath = scenes[i].path;
                string abName = AssetDatabase.GetImplicitAssetBundleName(assetPath);
                if (!string.IsNullOrEmpty(abName))
                {
                    scenes.RemoveAt(i);
                    i--;
                    changed = true;
                }
            }
            if (changed)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                //AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
            }
        }
        private static void EditorBuildSettings_sceneListChanged()
        {

        }
        public static string GetConfigFileName(BuildTargetGroup platform, string channelName, string userName, bool debug)
        {
            string filename = "";
            if (platform != 0)
            {
                string platformName = EditorOneBuild.BuildTargetGroupToPlatformName(platform);
                if (!string.IsNullOrEmpty(platformName))
                {
                    filename += platformName;
                }
            }

            if (!string.IsNullOrEmpty(channelName))
            {
                if (!string.IsNullOrEmpty(filename))
                    filename += ".";
                filename += EditorOneBuild.ChannelVersionPrefix + channelName;
            }

            if (debug)
            {
                if (!string.IsNullOrEmpty(filename))
                    filename += ".";
                filename += EditorOneBuild.DebugVersionName;
            }

            if (!string.IsNullOrEmpty(userName))
            {
                if (!string.IsNullOrEmpty(filename))
                    filename += ".";
                filename += EditorOneBuild.UserVersionPrefix + userName;
            }

            if (!string.IsNullOrEmpty(filename))
                filename += ".";
            filename += EditorOneBuild.ExtensionName;
            return filename;
        }

        public static string CreateConfigFile(string fileName)
        {
            string assetPath = Path.Combine(ConfigDir, fileName);
            if (!Directory.Exists(Path.GetDirectoryName(assetPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath));

            if (File.Exists(assetPath))
            {
                EditorUtility.DisplayDialog("error".Localization(), string.Format("msg_file_exists".Localization(), assetPath), "ok".Localization());
                return null;
            }

            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlElement elemConfig = doc.CreateElement("config");
            elemConfig.SetOrAddAttributeValue("xmlns", EditorOneBuild.XMLNS);

            XmlElement elemType;
            elemType = doc.CreateElement("Type");
            elemType.SetOrAddAttributeValue("type", "UnityEditor.Build.OneBuild.BuildSettings");
            elemType.SetOrAddAttributeValue("name", "Build");
            elemConfig.AppendChild(elemType);

            elemType = doc.CreateElement("Type");
            elemType.SetOrAddAttributeValue("type", "UnityEditor.PlayerSettings");
            elemType.SetOrAddAttributeValue("name", "PlayerSettings");
            elemConfig.AppendChild(elemType);

            doc.AppendChild(elemConfig);
            doc.Save(assetPath);
            AssetDatabase.ImportAsset(assetPath);

            return assetPath;
        }


        public static bool HasConfig()
        {
            string dir = ConfigDir;
            if (!Directory.Exists(dir))
                return false;
            string path = Path.Combine(dir, ExtensionName);
            if (File.Exists(path))
                return true;
            if (Directory.GetFiles(dir, "*." + ExtensionName, SearchOption.TopDirectoryOnly).Length > 0)
                return true;
            return false;
        }

        public static bool EnsureConfig()
        {
            if (HasConfig())
                return true;

            if (!EditorUtility.DisplayDialog("Empty Config", $"Initialize default config [*.{ExtensionName}] file", "yes", "cancel"))
                return false;


            string dstDir = ConfigDir;
            if (!Directory.Exists(dstDir))
                Directory.CreateDirectory(dstDir);
            string path = Path.Combine(dstDir, ExtensionName);
            string srcDir = Path.Combine(PackageDir, "Default");
            Action<string, string[]> copy = (fileName, values) =>
             {
                 string dstPath = Path.Combine(dstDir, fileName);
                 if (File.Exists(dstPath))
                     return;
                 string srcPath = Path.Combine(srcDir, fileName);
                 File.Copy(srcPath, dstPath, false);

                 if (values != null && values.Length > 1)
                 {
                     XmlDocument doc = new XmlDocument();
                     doc.Load(dstPath);
                     XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                     nsmgr.AddNamespace("config", XMLNS);

                     for (int i = 0; i < values.Length; i += 2)
                     {
                         XmlNode node = doc.SelectSingleNode(values[i], nsmgr);
                         if (node == null)
                         {
                             Debug.LogError("missing node: " + values[i] + ", file: " + fileName);
                             continue;
                         }

                         if (node.NodeType == XmlNodeType.Attribute)
                             node.Value = values[i + 1];
                         else
                             node.InnerText = values[i + 1];
                     }
                     doc.Save(dstPath);
                 }

             };

            string playerSettingsType = "/*/config:Type[@type='UnityEditor.PlayerSettings']";
            copy(ExtensionName, new string[] {
                  playerSettingsType+ "/config:productName", Application.productName ,
                 playerSettingsType+ "/config:SetApplicationIdentifier/config:string", Application.identifier ,
            });
            copy("debug." + ExtensionName, new string[]{
                playerSettingsType+ "/config:productName", Application.productName +"Dev",
            });
            copy("android." + ExtensionName, null);
            copy("android.debug." + ExtensionName, null);
            copy("android.channel-googleplay." + ExtensionName, null);
            copy("ios." + ExtensionName, null);
            copy("ios.debug." + ExtensionName, null);
            AssetDatabase.Refresh();
            return true;
        }

    }


}

namespace Windows
{
    /// <summary>
    /// Creates a console window that actually works in Unity
    /// You should add a script that redirects output using Console.Write to write to it.
    /// </summary>
    [System.Security.SuppressUnmanagedCodeSecurity]
    public class ConsoleWindow
    {
        TextWriter oldOutput;

        public void Initialize()
        {
            //
            // Attach to any existing consoles we have
            // failing that, create a new one.
            //
            if (!AttachConsole(0x0ffffffff))
            {
                AllocConsole();
            }

            oldOutput = Console.Out;

            try
            {
                System.Text.Encoding encoding = System.Text.Encoding.UTF8;

                System.Console.OutputEncoding = encoding;


                IntPtr stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);
                Microsoft.Win32.SafeHandles.SafeFileHandle safeFileHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(stdHandle, true);
                FileStream fileStream = new FileStream(safeFileHandle, FileAccess.Write);

                StreamWriter standardOutput = new StreamWriter(fileStream, encoding);
                standardOutput.AutoFlush = true;
                Console.SetOut(standardOutput);
            }
            catch (System.Exception e)
            {
                Debug.Log("Couldn't redirect output: " + e.Message);
            }
        }
        ~ConsoleWindow()
        {
            Shutdown();
        }

        public void Shutdown()
        {
            Console.SetOut(oldOutput);
            FreeConsole();
        }

        public void SetTitle(string strName)
        {
            SetConsoleTitleA(strName);
        }

        private const int STD_INPUT_HANDLE = -10;
        private const int STD_OUTPUT_HANDLE = -11;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();

        [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleTitleA(string lpConsoleTitle);
    }
}