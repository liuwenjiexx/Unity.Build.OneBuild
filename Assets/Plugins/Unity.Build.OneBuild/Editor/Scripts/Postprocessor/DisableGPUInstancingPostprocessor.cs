using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.OneBuild;
using UnityEngine;


namespace UnityEditor.Build.OneBuild
{

    /// <summary>
    /// <see cref="BuildSettings.DisableGPUInstancing"/>
    /// </summary>
    class DisableGPUInstancingPostprocessor : AssetPostprocessor
    {
        //static string[] paths = new string[]
        //{
        //};

        public static void OnPostprocessAllAssets(string[] importedAsset, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!BuildSettings.DisableGPUInstancing)
                return;

            List<GameObject> gos = null;
            foreach (var assetPath in importedAsset)
            {
                if (!assetPath.EndsWith(".prefab", StringComparison.InvariantCultureIgnoreCase))
                    continue;
                //if (paths.FirstOrDefault(o => assetPath.ToLower().StartsWith(o, StringComparison.InvariantCultureIgnoreCase)) == null)
                //    continue;
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (!go)
                    continue;
                if (gos == null)
                    gos = new List<GameObject>();
                gos.Add(go);
            }
            if (gos != null)
            {
                DisableGPUInstancing(gos.ToArray());
            }
        }

        public static void DisableGPUInstancing(GameObject[] selectedGameObjects)
        {
            bool hasChanged = false;

            for (int i = 0; i < selectedGameObjects.Length; i++)
            {
                bool changed = false;
                ParticleSystemRenderer[] psRenderer = selectedGameObjects[i].GetComponentsInChildren<ParticleSystemRenderer>();
                for (int j = 0; j < psRenderer.Length; j++)
                {
                    if (psRenderer[j].enableGPUInstancing)
                    {
                        if (!hasChanged)
                        {
                            hasChanged = true;
                            AssetDatabase.StartAssetEditing();
                        }
                        psRenderer[j].enableGPUInstancing = false;
                        changed = true;
                    }
                }
                if (changed)
                {
                    Debug.LogWarning("DisableGPUInstancing: " + selectedGameObjects[i].name);
                }
            }

            if (hasChanged)
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }
        }
    }
}