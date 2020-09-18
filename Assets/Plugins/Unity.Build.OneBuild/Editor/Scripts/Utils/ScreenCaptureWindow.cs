using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using UnityEditor.Experimental.Networking.PlayerConnection;

namespace UnityEditor
{
    public class ScreenCaptureWindow : EditorWindow
    {
        [Range(1, 5)]
        public int scale = 1;

        public string outputPath = "ScreenCapture";
        public string fileNameFormat = "screencapture {0:yyyyMMddHHmmss}";
        string lastPath;
        public Texture2D lastImg;

        [MenuItem("Window/General/Screen Capture")]
        public static void ShowWindow()
        {
            var win = EditorWindow.GetWindow<ScreenCaptureWindow>("Screen Capture");
            win.minSize = new Vector2(400, 300);
            win.maxSize = new Vector2(400, 400);
            win.Show();
        }

        private void OnGUI()
        {
            scale = EditorGUILayout.IntSlider("Scale", scale, 1, 5);
            outputPath = EditorGUILayout.DelayedTextField("Output Path", outputPath);
            fileNameFormat = EditorGUILayout.DelayedTextField("File Name Format", fileNameFormat);
            if (GUILayout.Button("Create"))
            {
                OnWizardCreate();
            }
            GUILayout.Label(lastPath);
            GUILayout.Box(lastImg, GUILayout.Width(200), GUILayout.Height(200));
            if (lastImg)
            {
                GUILayout.Label(string.Format("{0}x{1}", lastImg.width, lastImg.height));
            }
        }
        private int loadCount = 0;
        private void OnWizardCreate()
        {
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            string path = Path.Combine(outputPath, string.Format(fileNameFormat, DateTime.Now) + ".png");
            if (string.IsNullOrEmpty(path))
                return;
            ScreenCapture.CaptureScreenshot(path, scale);
            lastPath = path;
            loadCount = 0;
            LoadImg();

        }

        void LoadImg()
        {
            loadCount++;
            if (loadCount > 10)
                return;
            if (string.IsNullOrEmpty(lastPath))
                return;
            
            if (!File.Exists(lastPath))
            {
                EditorApplication.delayCall += LoadImg;
                return;
            }
            Texture2D img = new Texture2D(1, 1);
            img.LoadImage(File.ReadAllBytes(lastPath));
            lastImg = img;
            Repaint();
        }

    }
}