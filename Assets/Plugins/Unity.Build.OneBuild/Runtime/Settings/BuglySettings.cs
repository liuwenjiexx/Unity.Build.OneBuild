using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Build.OneBuild;


[assembly: BuildConfigType(typeof(BuglySettings), "Bugly")]

namespace UnityEngine.Build.OneBuild
{

    [Serializable]
    public class BuglySettings
    {

        #region Provider


        private static SettingsProvider provider;

        private static SettingsProvider Provider
        {
            get
            {
                if (provider == null)
                    provider = new SettingsProvider(typeof(BuglySettings), InternalExtensions.PackageName, true, true);
                return provider;
            }
        }

        public static BuglySettings Settings { get => (BuglySettings)Provider.Settings; }

        #endregion


        [SerializeField]
        private bool enabled;
        public static bool Enabled
        {
            get => Settings.enabled;
            set => Provider.SetProperty(nameof(Enabled), ref Settings.enabled, value);
        }

        [SerializeField]
        private string appId;
        public static string AppId
        {
            get => Settings.appId;
            set => Provider.SetProperty(nameof(AppId), ref Settings.appId, value);
        }


        [SerializeField]
        private bool enableExceptionHandler = true;
        public static bool EnableExceptionHandler
        {
            get => Settings.enableExceptionHandler;
            set => Provider.SetProperty(nameof(EnableExceptionHandler), ref Settings.enableExceptionHandler, value);
        }

        [SerializeField]
        private bool debugMode = false;
        public static bool DebugMode
        {
            get => Settings.debugMode;
            set => Provider.SetProperty(nameof(DebugMode), ref Settings.debugMode, value);
        }
   

    }

}