using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;
using System.IO;
using UnityEditor.GUIExtensions;
using System;
using System.Reflection;
using UnityEngine.Localizations;
using UnityEngine.Build.OneBuild;

namespace UnityEditor.Build.OneBuild
{


    class OneBuildConfigEditorWindow : EditorWindow
    {

        public List<string> configPaths;

        public string selectedPath;
        private XmlDocument doc;
        private XmlElement elemConfig;
        XmlNamespaceManager nsMgr;
        private GUIContent[] displayPaths;
        private static Dictionary<string, CachedMemberInfo> cachedMembers;
        Vector2 scrollPos;
        static string versionFile;
        static Dictionary<Type, string> configTypes;


        class CachedMemberInfo
        {
            public Type type;
            public Dictionary<string, MemberInfo> members = new Dictionary<string, MemberInfo>();

        }
        static Type FindType(string typeName)
        {
            var cached = _FindCachedType(typeName);
            if (cached != null)
                return cached.type;
            return null;
        }

        static CachedMemberInfo _FindCachedType(string typeName)
        {
            if (cachedMembers == null)
            {
                cachedMembers = new Dictionary<string, CachedMemberInfo>(StringComparer.InvariantCultureIgnoreCase);
            }
            CachedMemberInfo result;

            if (!cachedMembers.TryGetValue(typeName, out result))
            {
                Type type = EditorOneBuild.FindType(typeName);
                var cached = new CachedMemberInfo() { type = type };
                cachedMembers[typeName] = cached;

                if (type != null)
                {
                    cachedMembers[type.AssemblyQualifiedName] = cached;
                }
            }

            return result;
        }



        static MemberInfo FindMember(Type type, string memberName)
        {
            var mInfo = _FindCachedType(type.AssemblyQualifiedName);
            if (mInfo == null || mInfo.type == null)
                return null;
            MemberInfo member;
            if (!mInfo.members.TryGetValue(memberName, out member))
            {
                member = EditorOneBuild.FindSetMember(mInfo.type, memberName);
                mInfo.members[memberName] = member;
            }
            return member;
        }


        private void OnEnable()
        {
            using (EditorOneBuild.EditorLocalizationValues.BeginScope())
            {
                titleContent = new GUIContent("Build Config".Localization());
            }

            if (configTypes == null)
            {
                configTypes = BuildConfigTypeAttribute.FindAllConfigTypes();

                //configTypes = allConfigTypes.Select(o => o.Key).ToArray();
                //configTypeNames = allConfigTypes.Select(o => o.Value).ToArray();

                //configTypeContents = new GUIContent[] { new GUIContent("Add Type...") }.Concat(configTypes.Select(o => new GUIContent(o.FullName))).ToArray();
            }

            Refresh();
            if (string.IsNullOrEmpty(selectedPath) && configPaths.Count > 0)
            {
                int index = 0;
                for (int i = 0; i < configPaths.Count; i++)
                {
                    if (Path.GetFileName(configPaths[i]).Equals(EditorOneBuild.ExtensionName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }
                selectedPath = configPaths[index];
            }
            Select(selectedPath);
        }

        void Refresh()
        {
            if (configPaths == null)
                configPaths = new List<string>();
            configPaths.Clear();

            if (Directory.Exists(EditorOneBuild.ConfigDir))
            {
                foreach (var filePath in Directory.GetFiles(EditorOneBuild.ConfigDir, "*"+EditorOneBuild.ExtensionName))
                {
                    string filename = Path.GetFileName(filePath);
                    if (!EditorOneBuild.IsBuildFIle(filename))
                        continue;
                    configPaths.Add(filePath.ReplacePathSeparator());
                }
            }
            displayPaths = configPaths.Select(o => new GUIContent(o, o)).ToArray();

            Select(selectedPath);

            UpdateBuildOptions();

        }

        public static void UpdateBuildOptions()
        {
            versionFile = EditorOneBuild.GetVersionFile();
        }



        public void Select(string configPath)
        {
            selectedPath = configPath;
            if (selectedPath != null)
                selectedPath = selectedPath.ReplacePathSeparator();

            doc = null;
            if (!string.IsNullOrEmpty(selectedPath))
            {
                Load(selectedPath);
            }
        }

        void Load(string path)
        {
            doc = new XmlDocument();
            doc.Load(path);
            nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("ns", EditorOneBuild.XMLNS);
            elemConfig = doc.SelectSingleNode("ns:config", nsMgr) as XmlElement;
        }
        private bool isConfigDirted;

        void DirtyConfig()
        {
            isConfigDirted = true;
            EditorApplication.delayCall += () =>
            {
                if (isConfigDirted)
                {
                    Save();
                }
            };
        }

        void Save()
        {
            if (doc == null)
                return;
            doc.Save(selectedPath);
            AssetDatabase.ImportAsset(selectedPath, ImportAssetOptions.ForceUpdate);
            isConfigDirted = false;
        }




        [MenuItem(EditorOneBuild.MenuPrefix + "Settings", priority = EditorOneBuild.BuildMenuPriority + 1)]
        static void Show_Menu()
        {
            EditorOneBuild.EnsureConfig();

            GetWindow<OneBuildConfigEditorWindow>().Show();
        }

        bool ContainsType(string typeName)
        {
            return doc.SelectSingleNode($"//ns:Type[@type='{typeName }']", nsMgr) != null;
        }

        void AddType(Type type, string name)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            string typeName = type.FullName;

            if (ContainsType(typeName))
            {
                EditorUtility.DisplayDialog("error".Localization(), string.Format("msg_type_exists".Localization(), typeName), "ok".Localization());
            }
            XmlNode typeNode = null;

            typeNode = doc.CreateElement("Type", EditorOneBuild.XMLNS);
            typeNode.SetOrAddAttributeValue("type", typeName);
            typeNode.SetOrAddAttributeValue("name", name ?? string.Empty);

            elemConfig.AppendChild(typeNode);
            GUIUtility.keyboardControl = -1;
            DirtyConfig();
        }


        private void OnGUI()
        {
            using (EditorOneBuild.EditorLocalizationValues.BeginScope())
            {

                GUIBuildSettigns();

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Open Project Settings".Localization()))
                    {
                        //EditorApplication.ExecuteMenuItem("Edit/Project Settings.../Player");
                        SettingsService.OpenProjectSettings("Project/Player");
                        // Selection.activeObject = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
                    }
                    if (GUILayout.Button("Load Settings".Localization()))
                    {
                        EditorOneBuild.UpdateConfig();
                    }
                    if (GUILayout.Button("Build".Localization()))
                    {
                        if (Event.current.shift)
                        {
                            var options = EditorOneBuild.CreateBuildOptions();
                            options.versionName = UserBuildSettings.AvalibleVersionName;
                            options.isPreBuild = true;
                            EditorOneBuild.Build(options);
                        }
                        else
                        {
                            EditorOneBuild.Build(UserBuildSettings.AvalibleVersionName);
                        }
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    var style = new GUIStyle(EditorStyles.largeLabel);
                    style.fontSize += 4;
                    style.padding = new RectOffset(5, 0, 1, 0);
                    style.margin = new RectOffset();
                    if (GUILayout.Button("↻", style, GUILayout.ExpandWidth(false)))
                    {
                        Refresh();
                    }

                    int selectedIndex = configPaths.IndexOf(selectedPath);
                    int newIndex = EditorGUILayout.Popup(selectedIndex, displayPaths);
                    if (newIndex != selectedIndex)
                    {
                        Select(configPaths[newIndex]);
                    }

                    EditorGUILayoutx.PingButton(selectedPath);

                    if (GUILayout.Button("New Config".Localization(), GUILayout.ExpandWidth(false)))
                    {
                        var wizard = ScriptableWizard.DisplayWizard<CreateBuildConfigWizard>("Create Build Config");
                        wizard.callback = (path) =>
                        {
                            if (path != null)
                            {
                                path = path.ReplacePathSeparator();
                                GUIUtility.keyboardControl = -1;
                                Refresh();

                                string newPath = null;

                                for (int i = 0; i < configPaths.Count; i++)
                                {
                                    if (configPaths[i].Equals(path, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        newPath = configPaths[i];
                                        break;
                                    }
                                }
                                Select(path);
                            }
                        };
                    }

                }


                if (doc == null)
                    return;

                using (var sv = new GUILayout.ScrollViewScope(scrollPos))
                {
                    scrollPos = sv.scrollPosition;

                    using (var checker = new EditorGUI.ChangeCheckScope())
                    {
                        float oldWidth = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = Mathf.Max(EditorGUIUtility.labelWidth, Screen.width * 0.3f);

                        foreach (XmlElement typeNode in doc.DocumentElement.SelectNodes("ns:Type", nsMgr))
                        {
                            DrawTypeNode(typeNode);
                        }
                        EditorGUIUtility.labelWidth = oldWidth;

                        if (checker.changed)
                        {
                            DirtyConfig();
                        }
                    }




                    using (new GUILayout.HorizontalScope())
                    {

                        if (doc != null)
                        {
                            if (GUILayout.Button("Add Type...", "popup"))
                            {
                                GenericMenu menu = new GenericMenu();
                                configTypes.Where(item =>
                                {
                                    Type configType = item.Key;
                                    if (!ContainsType(configType.FullName))
                                    {
                                        menu.AddItem(new GUIContent(configType.FullName), false, () =>
                                          {
                                              AddType(configType, item.Value);
                                          });
                                    }
                                    return false;
                                }).ToArray();
                                menu.ShowAsContext();
                            }

                        }
                    }
                }
            }
        }





        void DrawTypeNode(XmlElement typeNode)
        {
            XmlAttribute attr;

            string typeName;
            typeName = typeNode.GetAttributeValue("type", string.Empty);
            attr = typeNode.GetOrAddAttribute("name", string.Empty);

            Type type = null;

            if (!string.IsNullOrEmpty(typeName))
            {
                type = FindType(typeName);
            }
            object obj = null;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayoutx.MenuButton(new GUIContent(typeName), "label", () =>
                 {
                     GenericMenu menu = new GenericMenu();
                     menu.AddItem(new GUIContent("Delete".Localization()), false, () =>
                     {
                         using (EditorOneBuild.EditorLocalizationValues.BeginScope())
                         {
                             if (EditorUtility.DisplayDialog("Delete".Localization(), string.Format("msg_del_type_content".Localization(), typeName), "ok", "cancel"))
                             {
                                 typeNode.ParentNode.RemoveChild(typeNode);
                                 DirtyConfig();
                             }
                         }
                     });
                     return menu;
                 });

                attr.Value = EditorGUILayoutx.DelayedPlaceholderField(attr.Value ?? string.Empty, new GUIContent("Alias".Localization()), GUILayout.Width(120));


                EditorGUILayoutx.MenuButton(new GUIContent("+"), "label", () =>
                {
                    GenericMenu menu = new GenericMenu();
                    var members = new Dictionary<string, MemberInfo>(EditorOneBuild.GetSetMembers(type));

                    foreach (XmlNode itemNode in typeNode.SelectNodes("ns:*", nsMgr))
                    {
                        if (itemNode.NodeType == XmlNodeType.Element)
                        {
                            members.Remove(itemNode.Name);
                        }
                    }

                    foreach (var member in members.Values.OrderBy(o => o.Name))
                    {
                        if (member.IsDefined(typeof(System.ObsoleteAttribute), true))
                            continue;

                        MethodInfo method1 = member as MethodInfo;
                        if (method1 != null)
                        {
                            if (member.Name.StartsWith("Get") && method1.ReturnType != typeof(void))
                                continue;
                        }

                        menu.AddItem(new GUIContent(member.Name), false, (data) =>
                           {
                               MemberInfo mInfo = (MemberInfo)data;
                               XmlNode memberNode = typeNode.OwnerDocument.CreateElement(mInfo.Name, EditorOneBuild.XMLNS);
                               if (mInfo.MemberType == MemberTypes.Field || mInfo.MemberType == MemberTypes.Property)
                               {
                                   Type valueType;
                                   if (mInfo.MemberType == MemberTypes.Field)
                                   {
                                       FieldInfo fInfo = (FieldInfo)mInfo;
                                       valueType = fInfo.FieldType;
                                   }
                                   else
                                   {
                                       PropertyInfo pInfo = (PropertyInfo)mInfo;
                                       valueType = pInfo.PropertyType;
                                   }
                                   memberNode.InnerText = valueType.GetDefaultValue().ToStringOrEmpty();
                               }
                               else if (mInfo.MemberType == MemberTypes.Method)
                               {
                                   MethodInfo method = (MethodInfo)mInfo;
                                   foreach (var p in method.GetParameters())
                                   {
                                       XmlNode paramNode = memberNode.OwnerDocument.CreateElement(p.Name, EditorOneBuild.XMLNS);
                                       paramNode.InnerText = p.ParameterType.GetDefaultValue().ToStringOrEmpty();
                                       memberNode.AppendChild(paramNode);
                                   }
                               }
                               typeNode.AppendChild(memberNode);
                               DirtyConfig();
                           }, member);
                    }


                    return menu;
                }, GUILayout.ExpandWidth(false));

            }

            if (string.IsNullOrEmpty(typeName))
            {
                //EditorGUILayout.HelpBox("type name empty", MessageType.Error);
                // GUIUtility.ExitGUI();
            }


            if (type == null)
            {
                //if (Event.current.type == EventType.Repaint)
                //EditorGUILayout.HelpBox($"type null, <{typeName}>", MessageType.Error);
                return;
            }


            using (new EditorGUILayoutx.Scopes.IndentLevelVerticalScope())
            {
                foreach (var itemNode in ToEnumerable(typeNode.SelectNodes("*")))
                {
                    DrawConfigItem(type, itemNode);
                }
            }

        }


        bool IsTextValue(XmlNode valueNode)
        {
            return valueNode.GetAttributeValue<bool>("text", false);
        }
        void SetIsTextValue(XmlNode valueNode, bool isText)
        {
            if (isText)
                valueNode.SetOrAddAttributeValue("text", isText.ToString());
            else
                valueNode.RemoveAttribute("text");
        }

        bool IsKeyValue(XmlNode valueNode)
        {
            return valueNode.GetAttributeValue<bool>("key", false);
        }

        void SetIsKeyValue(XmlNode node, XmlNode valueNode, bool isKey)
        {
            foreach (XmlNode item in node.SelectNodes("ns:*", nsMgr))
                item.RemoveAttribute("key");
            valueNode.RemoveAttribute("key");

            if (isKey)
            {
                valueNode.SetOrAddAttributeValue("key", isKey.ToString());
            }
        }

        bool GetCombine(XmlNode itemNode, XmlNode valueNode, out string separator, out CombineOptions options, out bool isCombineValue)
        {
            separator = itemNode.GetAttributeValue<string>("combine", null);
            options = itemNode.GetAttributeValue<CombineOptions>("combineOptions", CombineOptions.None);

            isCombineValue = IsCombineValue(valueNode);
            if (itemNode.HasAttribute("combine"))
                return true;
            return false;
        }

        bool IsCombineValue(XmlNode valueNode)
        {
            return valueNode.HasAttribute("combineValue");
        }

        void SetCombine(XmlNode itemNode, XmlNode valueNode, string separator, CombineOptions options)
        {
            itemNode.SetOrAddAttributeValue("combine", separator);
            itemNode.SetOrAddAttributeValue("combineOptions", options.ToString());
            valueNode.SetOrAddAttributeValue("combineValue", true.ToString());
        }

        void RemoveCombine(XmlNode itemNode, XmlNode valueNode)
        {
            itemNode.RemoveAttribute("combine");
            itemNode.RemoveAttribute("combineOptions");
            valueNode.RemoveAttribute("combineValue");
            foreach (XmlNode item in itemNode.SelectNodes("ns:*", nsMgr))
            {
                item.RemoveAttribute("combineValue");
            }
        }

        void DrawLabelOptions(XmlNode valueNode)
        {

            if (IsTextValue(valueNode))
            {
                GUILayout.Label("text".Localization());
            }
            if (IsKeyValue(valueNode))
            {
                GUILayout.Label("key".Localization());
            }
        }

        void GUIBuildSettigns()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Version Name".Localization(), GUILayout.ExpandWidth(false));
                string versionName = UserBuildSettings.AvalibleVersionName;

                if (!UserBuildSettings.IsDebug)
                {
                    if (string.IsNullOrEmpty(versionName))
                        versionName = "Release";
                    else
                        versionName = "Release," + versionName;
                }

                GUILayout.Label("[" + versionName + "]", GUILayout.ExpandWidth(false));

                GUILayout.Label("Version".Localization(), GUILayout.ExpandWidth(false));
                using (var checker = new EditorGUI.ChangeCheckScope())
                {
                    if (versionFile == null)
                        versionFile = string.Empty;
                    //string[] parts = versionFile.Split('.');
                    //for (int i = 0; i < parts.Length; i++)
                    //{
                    //    string label = null;
                    //    switch (i)
                    //    {
                    //        case 0:
                    //            label = "Major";
                    //            break;
                    //        case 1:
                    //            label = "Minor";
                    //            break;
                    //        case 2:
                    //            label = "Build";
                    //            break;
                    //        case 3:
                    //            label = "Revision";
                    //            break;
                    //    }
                    //    if (i > 0)
                    //        GUILayout.Label(".", GUILayout.Width(10));
                    //    //if (label != null)
                    //    //    GUILayout.Label(label.Localization(), GUILayout.ExpandWidth(false));

                    //    parts[i] = EditorGUILayoutx.DelayedEditableLabel(parts[i], GUILayout.Width(20));
                    //}
                    versionFile = EditorGUILayoutx.DelayedEditableLabel(versionFile, GUILayout.ExpandWidth(false));
                    if (checker.changed)
                    {
                        //versionFile = string.Join(".", parts);
                        EditorOneBuild.SaveVersionFile(versionFile);
                    }
                }

            }
        }

        void DrawConfigItem(Type type, XmlNode configItemNode)
        {
            string memberName = configItemNode.LocalName;


            PropertyInfo property = null;
            FieldInfo field = null;
            MethodInfo method = null;
            MemberInfo member;
            member = FindMember(type, memberName);

            Type valueType = null;

            if (member is PropertyInfo)
            {
                property = (PropertyInfo)member;
                valueType = property.PropertyType;
            }
            else if (member is FieldInfo)
            {
                field = (FieldInfo)member;
                valueType = field.FieldType;
            }
            else if (member is MethodInfo)
            {
                method = (MethodInfo)member;
            }


            bool handle = false;

            using (var scope = new GUILayout.HorizontalScope())
            {
                string displayName;
                XmlNode valueNode = configItemNode;

                if (field != null || property != null)
                {
                    var first = configItemNode.SelectSingleNode("ns:*", nsMgr);
                    if (first != null)
                    {
                        valueNode = first;
                    }
                }

                using (new GUILayout.HorizontalScope(GUILayout.Width(EditorGUIUtility.labelWidth)))
                {
                    displayName = memberName;
                    EditorGUILayoutx.MenuButton<object>(new GUIContent(displayName), "label", (d) =>
                     {
                         GenericMenu menu = new GenericMenu();

                         if (property != null || field != null)
                         {
                             SetValueNodeMenu(configItemNode, configItemNode, menu);
                         }


                         menu.AddItem(new GUIContent("Delete".Localization()), false, () =>
                         {
                             using (EditorOneBuild.EditorLocalizationValues.BeginScope())
                             {
                                 if (EditorUtility.DisplayDialog($"Delete".Localization(), string.Format("msg_del_item_content".Localization(), displayName), "ok", "cancel"))
                                 {
                                     configItemNode.ParentNode.RemoveChild(configItemNode);
                                     DirtyConfig();
                                 }
                             }
                         });
                         return menu;
                     }, null);
                    GUILayout.FlexibleSpace();

                    DrawLabelOptions(valueNode);

                    DrawCombineNodeValue(configItemNode, valueNode);
                }


                if (field != null || property != null)
                {
                    DrawValueNode(valueNode, valueType);
                    handle = true;
                }
            }


            if (member == null)
            {
                EditorGUILayout.HelpBox("member null", MessageType.Error);
                return;
            }

            if (!handle)
            {
                if (method != null)
                {
                    using (new EditorGUILayoutx.Scopes.IndentLevelVerticalScope())
                    {
                        int paramIndex = 0;

                        var paramNodes = configItemNode.SelectNodes("ns:*", nsMgr);

                        foreach (var parameter in method.GetParameters())
                        {
                            if (paramIndex >= paramNodes.Count)
                            {
                                EditorGUILayout.HelpBox("Parameter length error" + method.GetParameters().Length, MessageType.Error, false);
                                break;
                            }
                            XmlNode paramNode = paramNodes[paramIndex];

                            using (new GUILayout.HorizontalScope())
                            {
                                using (new GUILayout.HorizontalScope(GUILayout.Width(EditorGUIUtility.labelWidth)))
                                {
                                    EditorGUILayoutx.MenuButton(new GUIContent(parameter.Name), "label", (d) =>
                                    {
                                        GenericMenu menu = new GenericMenu();
                                        SetValueNodeMenu(configItemNode, d, menu);
                                        return menu;
                                    }, paramNode, GUILayout.ExpandWidth(true));
                                    GUILayout.FlexibleSpace();

                                    DrawLabelOptions(paramNode);
                                    DrawCombineNodeValue(configItemNode, paramNode);
                                }

                                DrawValueNode(paramNode, parameter.ParameterType);
                            }
                            paramIndex++;
                        }
                    }
                }
                else
                {

                }
            }
        }

        bool DrawCombineNodeValue(XmlNode itemNode, XmlNode valueNode)
        {
            string separator;
            CombineOptions options;
            bool isCombineValue;
            if (!GetCombine(itemNode, valueNode, out separator, out options, out isCombineValue))
            {
                return false;
            }

            if (!isCombineValue)
                return false;
            using (new GUILayout.HorizontalScope())
            {
                options = (CombineOptions)EditorGUILayout.EnumPopup(options, GUILayout.Width(60));
                separator = EditorGUILayout.DelayedTextField(separator ?? string.Empty, GUILayout.Width(EditorStyles.textField.fontSize + EditorStyles.textField.padding.horizontal));
                if (string.IsNullOrEmpty(separator))
                {
                    EditorGUILayout.HelpBox("Separator empty", MessageType.Error);
                }
                SetCombine(itemNode, valueNode, separator, options);
            }
            return true;
        }





        void SetValueNodeMenu(XmlNode itemNode, XmlNode valueNode, GenericMenu menu)
        {
            bool isText = IsTextValue(valueNode);
            bool isKey = IsKeyValue(valueNode);

            menu.AddItem(new GUIContent("Text".Localization()), isText, () =>
            {
                SetIsTextValue(valueNode, !isText);
                DirtyConfig();
            });

            menu.AddItem(new GUIContent("Key".Localization()), isKey, () =>
            {
                SetIsKeyValue(itemNode, valueNode, !isKey);
                DirtyConfig();
            });



            string s;
            CombineOptions options;
            bool isCombineValue;
            bool isCombine = GetCombine(itemNode, valueNode, out s, out options, out isCombineValue);

            menu.AddItem(new GUIContent("Combine".Localization()), isCombineValue, () =>
            {
                if (isCombineValue)
                {
                    RemoveCombine(itemNode, valueNode);
                }
                else
                {
                    RemoveCombine(itemNode, valueNode);
                    SetCombine(itemNode, valueNode, string.Empty, CombineOptions.Distinct);
                }
                DirtyConfig();
            });
        }

        void DrawValueNode(XmlNode node, Type valueType)
        {
            if (valueType == null)
                valueType = typeof(string);

            if (IsTextValue(node) || IsCombineValue(node))
            {
                TextField(node);
                return;
            }

            string error = null;
            if (valueType.IsEnum)
            {

                try
                {
                    Enum value = (Enum)Enum.Parse(valueType, node.InnerText, true);
                    if (valueType.IsDefined(typeof(FlagsAttribute)))
                    {
                        node.InnerText = EditorGUILayout.EnumFlagsField(value).ToString();
                    }
                    else
                    {
                        node.InnerText = EditorGUILayout.EnumPopup(value).ToString();
                    }
                    return;
                }
                catch (Exception ex)
                {
                    error = $"parse [enum] [{valueType.GetType().Name}] error";
                }
            }

            if (valueType == typeof(bool))
            {
                bool value;
                if (bool.TryParse(node.InnerText, out value))
                {
                    node.InnerText = EditorGUILayout.Toggle(value).ToString();
                    return;
                }
                else
                {
                    error = "parse [bool] error";
                }
            }
            else if (valueType == typeof(float))
            {
                float value;
                if (float.TryParse(node.InnerText, out value))
                {
                    node.InnerText = EditorGUILayout.DelayedFloatField(value).ToString();
                    return;
                }
                else
                {
                    error = "parse [float] error";
                }
            }
            else if (valueType == typeof(double))
            {
                double value;
                if (double.TryParse(node.InnerText, out value))
                {
                    node.InnerText = EditorGUILayout.DelayedDoubleField(value).ToString();
                    return;
                }
                else
                {
                    error = "parse [double] error";
                }
            }
            else if (valueType == typeof(int))
            {
                int value;
                if (int.TryParse(node.InnerText, out value))
                {
                    node.InnerText = EditorGUILayout.DelayedFloatField(value).ToString();
                    return;
                }
                else
                {
                    error = "parse [int32] error";
                }
            }

            TextField(node);

            if (!string.IsNullOrEmpty(error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }
        }
        static GUIStyle popupButtonStyle;
        void TextField(XmlNode node)
        {
            int ctrlId = GUIUtility.GetControlID(FocusType.Passive);
            TemplateValueState state = (TemplateValueState)GUIUtility.GetStateObject(typeof(TemplateValueState), ctrlId);
            string text = node.InnerText;
            if (state.newValue != null)
            {
                if (text != state.newValue)
                {
                    GUI.changed = true;
                    text = state.newValue;
                }
                state.newValue = null;
            }

            text = EditorGUILayout.DelayedTextField(text);
            if (popupButtonStyle == null)
            {
                popupButtonStyle = new GUIStyle("popup");

                popupButtonStyle.margin = new RectOffset(2, 2, 0, 0);
                popupButtonStyle.padding = new RectOffset(4, 0, 0, 0);
                //popupButtonStyle.border = new RectOffset(0,0,10,10);
                popupButtonStyle.overflow = new RectOffset(2, 0, -1, 2);
                //popupButtonStyle.contentOffset = new Vector2(10, 0);

            }
            if (GUILayout.Button("", popupButtonStyle, GUILayout.Width(16)))
            {

                if (tplValueMenu == null)
                {
                    tplValueMenu = new GenericMenu();

                    foreach (var item in BuildConfigValueAttribute.FindAllConfigValues().OrderBy(o => o.Value))
                    {
                        tplValueMenu.AddItem(new GUIContent(item.Value), false, (s) =>
                        {
                            string str = (string)s;
                            TemplateValueState.active.newValue = str;
                        }, item.Key);
                    }
                }
                GUIUtility.keyboardControl = -1;
                TemplateValueState.active = state;
                tplValueMenu.ShowAsContext();
            }
            node.InnerText = text;
        }
        class TemplateValueState
        {
            public string newValue;
            public static TemplateValueState active;
        }

        GenericMenu tplValueMenu;


        void DrawEnumValueNode(XmlNode node, Type valueType)
        {
            string textValue = node.InnerText;
            Enum enumValue = (Enum)Enum.Parse(valueType, textValue);
            if (valueType.IsDefined(typeof(FlagsAttribute), false))
                enumValue = (Enum)EditorGUILayout.EnumFlagsField(enumValue);
            else
                enumValue = (Enum)EditorGUILayout.EnumPopup(enumValue);
            node.InnerText = enumValue.ToString();
        }


        static IEnumerable<XmlNode> ToEnumerable(XmlNodeList nodeList)
        {
            List<XmlNode> list = new List<XmlNode>(nodeList.Count);
            foreach (XmlNode item in nodeList)
            {
                list.Add(item);
            }
            return list;
        }


        [UnityEditor.Callbacks.OnOpenAsset(-1)]
        static bool OnOpenAsset(int instanceID, int line)
        {
            string assetPath;
            assetPath = AssetDatabase.GetAssetPath(instanceID);
            if (!string.IsNullOrEmpty(assetPath))
            {
                if (EditorOneBuild.IsBuildFIle(assetPath))
                {
                    Show_Menu();
                    var win = GetWindow<OneBuildConfigEditorWindow>();
                    win.Select(assetPath);
                    return true;
                }
            }
            return false;
        }

    }
}