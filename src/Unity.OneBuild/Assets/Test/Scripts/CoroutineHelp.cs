using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineHelp : MonoBehaviour
{

    private static CoroutineHelp instance;
    public static CoroutineHelp Instance
    {
        get
        {
            if (!instance)
            {
                instance = new GameObject(typeof(CoroutineHelp).Name).AddComponent<CoroutineHelp>();
                DontDestroyOnLoad(instance.gameObject);
            }
            return instance;
        }
    }
}
