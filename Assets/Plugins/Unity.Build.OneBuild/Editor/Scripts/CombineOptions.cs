using System;

namespace UnityEditor.Build.OneBuild
{

    [Flags]
    public enum CombineOptions
    {
        None,
        Clear = 0x2,
        Remove = 0x4,
        Distinct = 0x8,
    }

}