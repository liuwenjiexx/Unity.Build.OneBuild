#if SDK_IRONSOURCE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using System.IO;
using UnityEditor.Callbacks;

namespace UnityEditor.Build.OneBuild
{
    public class IronSourcePostProcessBuild
    {
        [PostProcessBuild(999)]
        static void PostProcessBuild(BuildTarget target, string pathToBuildProject)
        {

            if (target == BuildTarget.iOS)
            {
#if UNITY_IOS
                //Xcode
                Debug.Log("PostProcessBuild IronSource Xcode");
                string projPath = PBXProject.GetPBXProjectPath(pathToBuildProject);
                PBXProject pbxProj = new PBXProject();
                pbxProj.ReadFromFile(projPath);
                string targetGuid = pbxProj.TargetGuidByName(PBXProject.GetUnityTargetName());
                pbxProj.AddFrameworkToProject(targetGuid, "AdSupport.framework", false);

                //Plist
                Debug.Log("PostProcessBuild IronSource Plist");
                string plistPath = pathToBuildProject + "/Info.plist";
                PlistDocument plist = new PlistDocument();
                plist.ReadFromString(File.ReadAllText(plistPath));
                PlistElementDict rootDict = plist.root;
                var dic = rootDict.CreateDict("NSAppTransportSecurity");
                dic.SetBoolean("NSAllowsArbitraryLoads", true);

                plist.WriteToFile(plistPath);
#endif
            }

        }
    }

}

#endif