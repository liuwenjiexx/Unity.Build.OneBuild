#if SDK_BUGLY

using System.Collections;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEditor;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

using System.IO;
namespace UnityEditor.Build.OneBuild
{
    public class BuglyPostProcessBuild
    {

        [PostProcessBuild(999)]
        static void PostProcessBuild(BuildTarget target, string pathToBuildProject)
        {

            if (target == BuildTarget.iOS)
            {
#if UNITY_IOS
                //Xcode
                Debug.Log("PostProcessBuild Bugly Xcode");
                string projPath = PBXProject.GetPBXProjectPath(pathToBuildProject);
                if (!File.Exists(projPath))
                {
                    Debug.LogError("path not exists: " + projPath);
                    return;
                }
                PBXProject pbxProj = new PBXProject();
                pbxProj.ReadFromFile(projPath);
                string targetGuid = pbxProj.TargetGuidByName(PBXProject.GetUnityTargetName());

                //在 libBuglyBridge.a 上设置 framework 
                //default
                //pbxProj.AddFrameworkToProject(targetGuid, "Security.framework", false);
                //pbxProj.AddFrameworkToProject(targetGuid, "SystemConfiguration.framework", false);
                //pbxProj.AddFrameworkToProject(targetGuid, "JavaScriptCore.framework", true);

                pbxProj.AddFileToBuild(targetGuid, pbxProj.AddFile("usr/lib/libz.tbd", "Frameworks/libz.tbd", PBXSourceTree.Sdk));
                pbxProj.AddFileToBuild(targetGuid, pbxProj.AddFile("usr/lib/libc++.tbd", "Frameworks/libc++.tbd", PBXSourceTree.Sdk));

                pbxProj.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-ObjC");
                //pbxProj.SetBuildProperty(targetGuid, "DEBUG_INFORMATION_FORMAT", "dwarf-with-dsym");
                //pbxProj.SetBuildProperty(targetGuid, "GENERATE_DEBUG_SYMBOLS", "yes");
                pbxProj.WriteToFile(projPath);
#endif
            }
        }
    }
}

#endif