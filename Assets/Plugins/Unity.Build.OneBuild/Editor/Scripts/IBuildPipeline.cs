using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEditor.Build
{

    public interface IBuildPipeline
    {
        /// <summary>
        /// 生成版本号
        /// </summary>
        void BuildStarted();

        void PreBuild();

        void PostBuild();

        void BuildEnded();
    }

}