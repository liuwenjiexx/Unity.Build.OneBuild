using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Build.OneBuild;

[assembly: BuildConfigType(typeof(SDKFacebookSettings), "Facebook")]

namespace UnityEngine.Build.OneBuild
{
    [Serializable]
    public class SDKFacebookSettings

    {

        #region Provider


        private static SettingsProvider provider;

        private static SettingsProvider Provider
        {
            get
            {
                if (provider == null)
                    provider = new SettingsProvider(typeof(SDKFacebookSettings), InternalExtensions.PackageName, true, true);

                return provider;
            }
        }

        public static SDKFacebookSettings Settings { get => (SDKFacebookSettings)Provider.Settings; }

        #endregion


        [SerializeField]
        private bool enabled;
        public static bool Enabled
        {
            get => Settings.enabled;
            set => Provider.SetProperty(nameof(Enabled), ref Settings.enabled, value);
        }



        [SerializeField]
        private string appName;
        public static string AppName
        {
            get => Settings.appName;
            set => Provider.SetProperty(nameof(AppName), ref Settings.appName, value);
        }

        [SerializeField]
        private bool eventEnabled;
        public static bool EventEnabled
        {
            get => Settings.eventEnabled;
            set => Provider.SetProperty(nameof(EventEnabled), ref Settings.eventEnabled, value);
        }
    }

}