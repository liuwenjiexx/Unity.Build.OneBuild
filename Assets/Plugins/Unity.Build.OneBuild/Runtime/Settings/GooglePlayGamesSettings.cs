using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Build.OneBuild;


[assembly: BuildConfigType(typeof(GooglePlayGamesSettings), "GooglePlayGames")]

namespace UnityEngine.Build.OneBuild
{
    public class GooglePlayGamesSettings
    {


        #region Provider


        private static SettingsProvider provider;

        private static SettingsProvider Provider
        {
            get
            {
                if (provider == null)
                    provider = new SettingsProvider(typeof(GooglePlayGamesSettings), InternalExtensions.PackageName, true, true);
                return provider;
            }
        }

        public static GooglePlayGamesSettings Settings { get => (GooglePlayGamesSettings)Provider.Settings; }

        #endregion


        [SerializeField]
        private bool enabled;
        public static bool Enabled
        {
            get => Settings.enabled;
            set => Provider.SetProperty(nameof(Enabled), ref Settings.enabled, value);
        }

        [SerializeField]
        private bool debugLogEnabled = false;
        public static bool DebugLogEnabled
        {
            get => Settings.debugLogEnabled;
            set => Provider.SetProperty(nameof(DebugLogEnabled), ref Settings.debugLogEnabled, value);
        }

    }
}