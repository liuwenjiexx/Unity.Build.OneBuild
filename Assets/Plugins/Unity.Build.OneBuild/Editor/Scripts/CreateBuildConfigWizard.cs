using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.ComponentModel;
using System;

namespace UnityEditor.Build.OneBuild
{
    public class CreateBuildConfigWizard : ScriptableWizard
    {
        public BuildTargetGroup platform;
        public string channelName;
        public string userName;
        public bool debug;
        [Header("Output")]
        public string filename;
        public Action<string> callback;
        void OnEnable()
        {
            platform = 0;
            OnValidate();
        }

        private void OnValidate()
        {
            filename = EditorOneBuild.GetConfigFileName(platform, channelName, userName, debug);
            isValid = !File.Exists(Path.Combine(EditorOneBuild.ConfigDir, filename));
        }

        private void OnWizardCreate()
        {
            if (string.IsNullOrEmpty(filename))
                return;
            string path= EditorOneBuild.CreateConfigFile(filename);
            callback?.Invoke(path);
        }

    }
}