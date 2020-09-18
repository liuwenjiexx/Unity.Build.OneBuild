using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Build.OneBuild;

[assembly: BuildConfigType(typeof(AppsFlyerSettings), "AppsFlyer")]

namespace UnityEngine.Build.OneBuild
{
    [Serializable]
    public class AppsFlyerSettings
    {
        #region Provider


        private static SettingsProvider provider;

        private static SettingsProvider Provider
        {
            get
            {
                if (provider == null) 
                    provider = new SettingsProvider(typeof(AppsFlyerSettings), "unity.sdk.appsflyer", true, true);
                return provider;
            }
        }

        public static AppsFlyerSettings Settings { get => (AppsFlyerSettings)Provider.Settings; }

        #endregion

        [SerializeField]
        private bool enabled;
        public static bool Enabled
        {
            get => Settings.enabled;
            set => Provider.SetProperty(nameof(Enabled), ref Settings.enabled, value);
        }

        [SerializeField]
        private string devKey;
        public static string DevKey
        {
            get => Settings.devKey;
            set => Provider.SetProperty(nameof(DevKey), ref Settings.devKey, value);
        }

        [SerializeField]
        private bool isDebug = false;
        public static bool IsDebug
        {
            get => Settings.isDebug;
            set => Provider.SetProperty(nameof(IsDebug), ref Settings.isDebug, value);
        }

        [SerializeField]
        private bool isSandbox = false;
        public static bool IsSandbox
        {
            get => Settings.isSandbox;
            set => Provider.SetProperty(nameof(IsSandbox), ref Settings.isSandbox, value);
        }



        #region Initalize




        #endregion

    }
}
