using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using System;


public class TestMenu : MonoBehaviour
{
    public Toggle steamAssets;

    class Utils
    {
        public static string StreamingAssetsUrl =
#if UNITY_EDITOR
            "fil1e://" + Application.streamingAssetsPath + "/";
#elif UNITY_ANDROID
"jar:file://" + Application.dataPath + "!/assets/";  
#elif UNITY_IPHONE
Application.dataPath + "/Raw/";  
#else
"fil1e://" + Application.streamingAssetsPath + "/";
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

        string str = "build,0;debug,10;platform,1;google,3;googlead,4";
        var order = ParseOrderValue(str);

        foreach (var name in order.OrderBy(o => o.Value))
        {
            Debug.Log(name.Key + " =" + name.Value);
        }
        Debug.Log("----");
        string[] names2;
        names2 = new string[] {
            "build.android.debug",
            "build",
            "build.android.google.googlead",
            "build.android.google",
            "build.android",
            "build.android.google.debug",
            "build.debug",
        };

        Func<string, string, bool> equalName = (a, b) =>
        {
            return a.Split('.').Where(o => string.Equals(ConvertPlatformName(o), b, StringComparison.InvariantCultureIgnoreCase)).Count() > 0;
        };

        foreach (var name in Order(names2, order, equalName))
        {
            Debug.Log(name);
        }
    }

    static string ConvertPlatformName(string name)
    {
        switch (name.ToLower())
        {
            case "ios":
            case "android":
                return "platform";
        }
        return name;
    }

 
static    IEnumerable<string> Order(IEnumerable<string> names, Dictionary<string, int> order, Func<string, string, bool> equalName)
    {
        if (equalName == null)
            throw new ArgumentNullException("equalName");

        //list.Sort(new OrderComparer() { equal = equalName));

        Debug.Log("order:" + " , " + string.Join("---", names.ToArray()));
        foreach (var orderItem in order.OrderByDescending(o => o.Value))
        {

            names = names.OrderBy(o => equalName(o, orderItem.Key) ? 1 : 0);
            Debug.Log("order:" + orderItem.Key + " , " + string.Join("---", names.ToArray()));
        }
        return names;
    }

    static Dictionary<string, int> ParseOrderValue(string str)
    {
        Dictionary<string, int> order = new Dictionary<string, int>();

        foreach (var item in str.Split(';'))
        {
            if (item.Length == 0)
                continue;
            string[] parts = item.Split(',');
            string name = parts[0].ToLower().Trim();
            int n = 0;
            if (parts.Length > 1 && !int.TryParse(parts[1], out n))
            {
                n = 0;
            }
            order[name] = n;
        }
        return order;
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
