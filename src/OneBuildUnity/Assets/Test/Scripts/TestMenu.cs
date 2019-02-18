﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TestMenu : MonoBehaviour
{
    public Toggle steamAssets;

    class Utils
    {
        public static string StreamingAssetsUrl =
#if UNITY_ANDROID
"jar:file://" + Application.dataPath + "!/assets/";  
#elif UNITY_IPHONE
Application.dataPath + "/Raw/";  
#else
"file://" + Application.streamingAssetsPath + "/";
#endif

    }
    // Use this for initialization
    void Start()
    {
        string path = "Build/1.0.0/StreamingAssets/Standalone";
        path = path.Trim();
        if (!(path.EndsWith("/") || path.EndsWith("\\")))
            path += "/";
        Utils.StreamingAssetsUrl = "file://" + System.IO.Path.GetFullPath(path);

        //Debug.Log(Application.streamingAssetsPath);
        Debug.Log("StreamingAssetsUrl:" + Utils.StreamingAssetsUrl);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Menu_Click(string sceneName)
    {
        if (sceneName == "main")
        {

            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
        else
        {
            CoroutineHelp.Instance.StartCoroutine(StartLoadScene(sceneName, steamAssets.isOn));
        }
    }




    IEnumerator StartLoadScene(string sceneName, bool useSteamAssets)
    {
        if (useSteamAssets)
        {
            string url = Utils.StreamingAssetsUrl + sceneName;
            Debug.Log("load assetbundle\n" + url);
            WWW www = new WWW(url);
            yield return www;
            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.LogError("load scene error\n" + url);
                yield break;
            }
            AssetBundle assetBundle = www.assetBundle;
            if (!assetBundle)
            {
                Debug.LogError("asset bundle null\n" + url);
                yield break;
            }
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            assetBundle.Unload(false);
        }
        else
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }

}