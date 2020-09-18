using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Build.OneBuild;

namespace UnityEditor.Build.OneBuild
{

    [Serializable]
    public class UserBuildSettings
    {

        [SerializeField]
        private bool isNoUser;
        [SerializeField]
        private string versionName;


        public const string VersionNameSeparator = ",";

        #region Provider


        private static UnityEngine.Build.OneBuild.SettingsProvider provider;

        private static UnityEngine.Build.OneBuild.SettingsProvider Provider
        {
            get
            {
                if (provider == null)
                    provider = new UnityEngine.Build.OneBuild.SettingsProvider(typeof(UserBuildSettings), EditorOneBuild.PackageName, false, false);
                return provider;
            }
        }

        public static UserBuildSettings Instance { get => (UserBuildSettings)Provider.Settings; }

        #endregion

        /// <summary>
        /// 默认<see cref="DebugVersionName"/>
        /// </summary>
        public static string VersionName
        {
            get => Instance.versionName ?? EditorOneBuild.DebugVersionName;
            set
            {
                if (Provider.SetProperty(nameof(VersionName), ref Instance.versionName, value))
                {
                    BuildSettings.Channel = GetVersionWithPrefix(EditorOneBuild.ChannelVersionPrefix);
                    EditorOneBuild.UpdateConfig();
                }
            }
        }

        public static string AvalibleVersionName
        {
            get => GetAvalibleVersionName(VersionName);
        }

        public static bool IsDebug
        {
            get => ContainsVersion(EditorOneBuild.DebugVersionName);
            set
            {
                if (IsDebug != value)
                {
                    RemoveVersion(EditorOneBuild.DebugVersionName);
                    if (value)
                        AddVersion(EditorOneBuild.DebugVersionName);
                }
            }
        }

        /// <summary>
        /// 禁止 User 版本名
        /// </summary>
        public static bool IsNoUser
        {
            get => Instance.isNoUser;
            set
            {
                if (Provider.SetProperty(nameof(IsNoUser), ref Instance.isNoUser, value))
                {
                    Menu.SetChecked(EditorOneBuild.BuildVersionNoUserMenu, IsNoUser);
                }
            }
        }

        //public static string Channel
        //{
        //    get
        //    {
        //        string channel = GetVersionWithPrefix(ChannelVersionPrefix);
        //        if (!string.IsNullOrEmpty(channel))
        //        {
        //            channel.Substring(ChannelVersionPrefix.Length);
        //        }
        //        return channel;
        //    }
        //    set
        //    {
        //        SetChannelVersion(value);
        //    }
        //}

        //public static string User
        //{
        //    get
        //    {
        //        string user = GetVersionWithPrefix(UserVersionPrefix);
        //        if (!string.IsNullOrEmpty(user))
        //        {
        //            user.Substring(UserVersionPrefix.Length);
        //        }
        //        return user;
        //    }
        //    set
        //    {
        //        SetUserVersion(value);
        //    }
        //}

        #region Version Name

        static string[] GetAllVersions(string version)
        {
            return version.Split(new string[] { VersionNameSeparator }, StringSplitOptions.RemoveEmptyEntries);
        }
        static string[] GetAllVersions()
        {
            return GetAllVersions(VersionName);
        }


        public static bool ContainsVersion(string version)
        {
            return GetAllVersions()
                .Where(o => string.Equals(o, version, StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(ConvertToShortKeyword(o), version, StringComparison.InvariantCultureIgnoreCase))
                .Count() > 0;
        }

        public static void AddVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return;
            List<string> vers;
            vers = GetAllVersions()
                 .ToList();
            if (!vers.Contains(version.ToLower()))
            {
                vers.Add(version);
                VersionName = string.Join(VersionNameSeparator, vers.ToArray());
            }
        }

        public static void RemoveVersion(string version)
        {
            string ver;
            ver = string.Join(VersionNameSeparator, GetAllVersions()
                 .Where(o => !version.Equals(o, StringComparison.InvariantCultureIgnoreCase))
                 .ToArray());
            VersionName = ver;
        }

        public static string ConvertToShortKeyword(string name)
        {
            if (name.StartsWith(EditorOneBuild.UserVersionPrefix, StringComparison.InvariantCultureIgnoreCase))
                return "user";

            if (name.StartsWith(EditorOneBuild.ChannelVersionPrefix, StringComparison.InvariantCultureIgnoreCase))
                return "channel";

            if (string.Equals(name, "standalone"))
                return "platform";

            if (Enum.GetNames(typeof(BuildTargetGroup)).Contains(name, StringComparer.InvariantCultureIgnoreCase))
                return "platform";

            return name;
        }


        public static void SetVersionWithPrefix(string versionPrefix, string version)
        {
            if (!version.StartsWith(versionPrefix))
                throw new Exception(string.Format("version [{0}] not starts with [{1}]", version, versionPrefix));

            if (!string.IsNullOrEmpty(version))
            {
                if (ContainsVersion(version))
                    return;
            }

            RemoveVersionWithPrefix(versionPrefix);

            if (string.IsNullOrEmpty(versionPrefix))
            {
                return;
            }
            if (!string.IsNullOrEmpty(version))
            {
                AddVersion(version);
            }
        }

        public static void RemoveVersionWithPrefix(string versionPrefix)
        {
            VersionName = RemoveVersionWithPrefix(versionPrefix, VersionName);
        }

        public static string RemoveVersionWithPrefix(string versionPrefix, string version)
        {
            bool changed = false;
            var list = GetAllVersions(version).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].StartsWith(versionPrefix))
                {
                    list.RemoveAt(i);
                    i--;
                    changed = true;
                }
            }
            if (changed)
            {
                version = string.Join(VersionNameSeparator, list.ToArray());
            }
            return version;
        }
        public static string GetVersionWithPrefix(string versionPrefix)
        {
            return GetVersionWithPrefix(versionPrefix, VersionName);
        }
        public static string GetVersionWithPrefix(string versionPrefix, string version)
        {
            var list = GetAllVersions(version);
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].StartsWith(versionPrefix))
                {
                    return list[i].Substring(versionPrefix.Length);
                }
            }
            return null;
        }


        public static void SetUserVersion(string userVersion)
        {
            SetVersionWithPrefix(EditorOneBuild.UserVersionPrefix, userVersion);
        }

        internal static string RemoveUserVersion(string version)
        {
            return RemoveVersionWithPrefix(EditorOneBuild.UserVersionPrefix, version);
        }

        public static void SetChannelVersion(string channelVersion)
        {
            SetVersionWithPrefix(EditorOneBuild.ChannelVersionPrefix, channelVersion);
        }

        internal static string RemoveChannelVersion(string version)
        {
            return RemoveVersionWithPrefix(EditorOneBuild.ChannelVersionPrefix, version);
        }

        /// <summary>
        /// 检查<see cref="IsNoUser"/>
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public static string GetAvalibleVersionName(string version)
        {

            if (IsNoUser)
            {
                version = RemoveUserVersion(version);
            }

            return version;
        }


        #endregion



    }
}