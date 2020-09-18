using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Build.OneBuild
{

    /// <summary>
    /// <see cref="BuildSettings.ShaderMaxTargetLevel"/>
    /// </summary>
    class ShaderMaxTargetLevelPostprocessor : AssetPostprocessor
    {
        public static string MaxTargetLevel
        {
            get => BuildSettings.ShaderMaxTargetLevel;
        }

        private static Regex targetLevelRegex;
        static Regex TargetLevelRegex
        {
            get
            {
                if (targetLevelRegex == null)
                {
                    string pattern = "(#pragma\\s+target\\s+)(?<version>[\\d\\.]+)";
                    targetLevelRegex = new Regex(pattern);
                }
                return targetLevelRegex;
            }
        }

        public static void OnPostprocessAllAssets(string[] importedAsset, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            string maxTargetLevel = MaxTargetLevel;

            if (string.IsNullOrEmpty(maxTargetLevel))
                return;

            foreach (var assetPath in importedAsset)
            {
                if (!assetPath.StartsWith("Assets"))
                    continue;
                //排除文件夹
                if (Path.GetExtension(assetPath).Equals(".shader"))
                {
                    SetShaderMaxTargetLevel(assetPath, maxTargetLevel);
                }
            }
        }

        public static void SetShaderMaxTargetLevel(string assetPath, string maxTargetLevel)
        {

            float maxVersion = float.Parse(maxTargetLevel);

            string text = File.ReadAllText(assetPath, Encoding.UTF8);
            bool changed = false;
            string newText = TargetLevelRegex.Replace(text, (m) =>
            {
                string strVersion = m.Groups["version"].Value;
                float n;
                if (float.TryParse(strVersion, out n))
                {
                    if (n > maxVersion)
                    {
                        changed = true;

                        Debug.LogWarning($"fix shader target level: {strVersion} > {maxTargetLevel} path: {assetPath}");
                        return m.Groups[1].Value + maxTargetLevel;
                    }
                }
                else
                {
                    Debug.LogError("error target level number: " + strVersion + ", " + assetPath);
                }
                return m.Value;
            });
            if (changed)
            {
                File.WriteAllText(assetPath, newText, Encoding.UTF8);
            }
        }

    }
}