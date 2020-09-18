using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System.Text;
using Debug = UnityEngine.Debug;
using System.Linq;
using System;

namespace UnityEditor.Build.OneBuild
{

    public class InstallWindow : EditorWindow
    {

        private string[] devices;
        private bool loaded;
        public string selectedAndroidDevice;
        public bool uninstallKeepData = false;
        private string[] apkFiles;
        public string selectedApkFile;
        string outputDir;



        string[] connectDevices = new string[]
        {
                "蓝叠|connect 127.0.0.1:5555" ,
                "夜神|connect 127.0.0.1:62001" ,
                "Mumu|connect 127.0.0.1:7555",
        };

        [MenuItem("Build/Install")]
        public static void ShowWindow()
        {
            GetWindow<InstallWindow>().Show();
        }
        void RefreshDevices()
        {
            string result = RunADB("devices");
            string[] array = result.Split('\n')
                .Select(o => o.Trim())
                .Where(o => !string.IsNullOrEmpty(o))
                .Skip(1)
                .ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                string[] parts = array[i].Split('\t');
                array[i] = parts[0];
            }

            devices = array
                .OrderBy(o => o)
                .ToArray();

        }

        void FindAllApkFiles()
        {

            if (string.IsNullOrEmpty(outputDir))
            {
                apkFiles = new string[0];
                return;
            }
            if (!Directory.Exists(outputDir))
            {
                apkFiles = new string[0];
                return;
            }
            apkFiles = Directory.GetFiles(outputDir, "*.apk", SearchOption.AllDirectories)
                .OrderByDescending(o => File.GetLastWriteTime(o))
                .ToArray();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Install");

            outputDir = OneBuild.BuildSettings.OutputDir;
            if (!string.IsNullOrEmpty(outputDir))
            {
                int index = outputDir.IndexOfAny(new char[] { '/', '\\' });
                if (index >= 0)
                    outputDir = outputDir.Substring(0, index);
            }

            if (!loaded)
            {
                loaded = true;
                FindAllApkFiles();
                RefreshDevices();
            }
        }

        public string SelectedDevice
        {
            get
            {
                int selectedIndex;
                selectedIndex = Array.FindIndex(devices, (o) => o == selectedAndroidDevice);
                if (selectedIndex == -1)
                    return null;
                return selectedAndroidDevice;
            }
        }

        private void OnGUI()
        {
            GUIStyle style;
            GUIAndroidDeviceList();

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("reset adb"))
                {
                    RunADB("kill-server");
                    RunADB("start-server");
                }
                //if (GUILayout.Button("AndroidDeviceMonitor"))
                //{
                //    OpenAndroidDeviceMonitor();
                //}
            }


            using (new GUILayout.HorizontalScope())
            {
                uninstallKeepData = EditorGUILayout.ToggleLeft("Keep Data", uninstallKeepData);

                if (GUILayout.Button("uninstall"))
                {
                    Uninstall(SelectedDevice, uninstallKeepData);
                }

            }
            EditorGUILayout.LabelField("Build Path", outputDir);


            style = new GUIStyle(EditorStyles.largeLabel);
            style.fontSize += 4;
            style.padding = new RectOffset(5, 0, 1, 0);
            style.margin = new RectOffset();
            if (GUILayout.Button("↻", style, GUILayout.ExpandWidth(false)))
            {
                FindAllApkFiles();
            }

            using (var sv = new GUILayout.ScrollViewScope(scrollPos))
            {
                scrollPos = sv.scrollPosition;

                for (int i = 0; i < apkFiles.Length; i++)
                {
                    DateTime dt = File.GetLastWriteTime(apkFiles[i]);

                    if (selectedApkFile == apkFiles[i])
                        GUI.color = Color.yellow;
                    using (new GUILayout.HorizontalScope())
                    {

                        if (GUILayout.Button(Path.GetFileName(apkFiles[i]), "label", GUILayout.ExpandWidth(true)))
                        {
                            selectedApkFile = apkFiles[i];
                        }

                        GUILayout.Label(dt.ToString("MM-dd HH:mm"), GUILayout.ExpandWidth(false));

                        if (GUILayout.Button("install", GUILayout.ExpandWidth(false)))
                        {
                            InstallAndRun(SelectedDevice, apkFiles[i]);
                        }
                    }
                    GUI.color = Color.white;
                }
            }

        }


        void GUIAndroidDeviceList()
        {
            GUIStyle style;

            using (new GUILayout.HorizontalScope())
            {

                int selectedIndex;
                selectedIndex = Array.FindIndex(devices, (o) => o == selectedAndroidDevice);
                GUILayout.Label("Device", GUILayout.ExpandWidth(false));
                int index = EditorGUILayout.Popup(selectedIndex, devices);
                if (selectedIndex != index)
                {
                    if (index == -1)
                    {
                        selectedAndroidDevice = null;
                    }
                    else
                    {
                        selectedAndroidDevice = devices[index];
                    }

                }

                style = new GUIStyle(EditorStyles.largeLabel);
                style.fontSize += 4;
                style.padding = new RectOffset(5, 0, 1, 0);
                style.margin = new RectOffset();
                if (GUILayout.Button("↻", style, GUILayout.ExpandWidth(false)))
                {
                    RefreshDevices();
                }

                //int selectedIndex = configPaths.IndexOf(selectedPath);
                //int newIndex = EditorGUILayout.Popup(selectedIndex, displayPaths);
                //if (newIndex != selectedIndex)
                //{
                //    Select(configPaths[newIndex]);
                //}

                //EditorGUILayoutx.PingButton(selectedPath);

                //using (new GUILayout.HorizontalScope())
                //{
                //    string[] names = connectDevices.Select(o => o.Split('|')[0]).ToArray();
                //    ArrayUtility.Insert(ref names, 0, "Connect");
                //    index = EditorGUILayout.Popup(0, names, GUILayout.ExpandWidth(false));
                //    if (index > 0)
                //    {
                //        string cmd = connectDevices[index - 1].Split('|')[1];
                //        RunADB(cmd);
                //    }
                //}
            }
        }

        public static void InstallAndRun(string device, string installFile)
        {
            string cmdText;
            cmdText = GetCmdTextPrefix(device) + $"install -r \"{Path.GetFullPath(installFile)}\"";
            EditorUtility.DisplayProgressBar("Install", Path.GetFileName(installFile), 0);
            try
            {
                RunADB(cmdText);
            }
            catch
            {
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            LaunchApp(device);
        }

        public static void Uninstall(string device, bool keepData)
        {

            string cmdText;
            cmdText = GetCmdTextPrefix(device);
            cmdText += "uninstall";
            if (keepData)
            {
                cmdText += " -k";
            }
            cmdText += $" {Application.identifier}";
            RunADB(cmdText);
        }


        [MenuItem("Help/Android/Android Device Monitor")]
        public static void OpenAndroidDeviceMonitor()
        {
            string androidSdkPath = null;
            Process p;
            ProcessStartInfo startInfo;
            p = Process.GetProcessesByName("adb").FirstOrDefault();
            if (p != null)
            {
                androidSdkPath = Path.GetDirectoryName(Path.GetDirectoryName(p.MainModule.FileName));
            }
            if (string.IsNullOrEmpty(androidSdkPath))
            {
                androidSdkPath = Environment.GetEnvironmentVariable("ANDROID_SDK_HOME");
            }
            if (string.IsNullOrEmpty(androidSdkPath))
            {
                startInfo = new ProcessStartInfo();
                startInfo.FileName = "adb";
                startInfo.Arguments = "start-server";
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p = Process.Start(startInfo);
                p.WaitForExit();
                if (p.ExitCode == 0)
                {
                    p = Process.GetProcessesByName("adb").FirstOrDefault();
                    if (p != null)
                    {
                        androidSdkPath = Path.GetDirectoryName(Path.GetDirectoryName(p.MainModule.FileName));
                    }
                }
            }

            if (string.IsNullOrEmpty(androidSdkPath))
                throw new Exception("require run adb.exe");

            startInfo = new ProcessStartInfo();
            startInfo.FileName = androidSdkPath + "/tools/monitor.bat";
            startInfo.Arguments = "start-server";
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            Process.Start(startInfo);
        }


        [MenuItem("Help/Android/Connect/蓝叠_5555")]
        public static void AndroidConnectLanDie()
        {
            StartProcess("adb", "connect 127.0.0.1:5555");
        }
        [MenuItem("Help/Android/Connect/夜神_62001")]
        public static void AndroidConnectYeShen()
        {
            StartProcess("adb", "connect 127.0.0.1:62001");

        }
        [MenuItem("Help/Android/Connect/Mumu_7555")]
        public static void AndroidConnectMumu()
        {
            StartProcess("adb", "connect 127.0.0.1:7555");
        }

        [MenuItem("Help/Android/Device Log")]
        public static void ConnectDeviceLog()
        {
            StartProcess("adb", "forward tcp:55000 localabstract:Unity-" + Application.identifier);
        }


        public static void LaunchApp(string device)
        {
            StartProcess("adb", GetCmdTextPrefix(device) + $"shell am start -n {Application.identifier}/com.unity3d.player.UnityPlayerActivity");
        }

        string GetCmdTextPrefix()
        {
            return GetCmdTextPrefix(SelectedDevice);
        }

        static string GetCmdTextPrefix(string device)
        {

            string cmdText = "";
            if (!string.IsNullOrEmpty(device))
            {
                cmdText = "-s " + device + " ";
            }
            return cmdText;
        }

        Vector2 scrollPos;


        static string RunADB(string args)
        {
            return StartProcess("adb", args, null);
        }

        static string StartProcess(string filePath, string args, string workingDirectory = null)
        {

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = filePath;
            startInfo.Arguments = args;
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                startInfo.WorkingDirectory = Path.GetFullPath(workingDirectory);
            }
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            //startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.RedirectStandardError = true;
            //startInfo.StandardErrorEncoding = Encoding.UTF8;

            string result;
            using (var p = Process.Start(startInfo))
            {
                result = p.StandardOutput.ReadToEnd();

                if (p.ExitCode != 0)
                {
                    if (!string.IsNullOrEmpty(result))
                    {
                        throw new Exception(result + "\n" + args);
                    }
                    else
                    {
                        throw new Exception("error code: " + p.ExitCode + "\n" + args);
                    }
                }
            }
            Debug.Log("run cmd\n" + filePath + " " + args + "\n" + result);

            return result;
        }


    }

}