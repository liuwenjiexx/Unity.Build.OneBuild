using System;

namespace UnityEditor.Build.OneBuild
{

    [Flags]
    public enum CombineOptions
    {
        None,
        Replace = 0x2,
        Remove = 0x4,
        Distinct = 0x8,
    }

}