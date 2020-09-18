using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Build.OneBuild;

[assembly: BuildConfigType(typeof(IronSourceSettings), "IronSource")]

namespace UnityEngine.Build.OneBuild
{
    [Serializable]
    public class IronSourceSettings
    {


        #region Provider


        private static SettingsProvider provider;

        private static SettingsProvider Provider
        {
            get
            {
                if (provider == null)
                    provider = new SettingsProvider(typeof(IronSourceSettings), InternalExtensions.PackageName, true, true);
                return provider;
            }
        }

        public static IronSourceSettings Settings { get => (IronSourceSettings)Provider.Settings; }

        #endregion

        [SerializeField]
        private bool enabled;
        public static bool Enabled
        {
            get => Settings.enabled;
            set => Provider.SetProperty(nameof(Enabled), ref Settings.enabled, value);
        }


        [SerializeField]
        private string appKey;
        public static string AppKey
        {
            get => Settings.appKey;
            set => Provider.SetProperty(nameof(AppKey), ref Settings.appKey, value);
        }

    }
}