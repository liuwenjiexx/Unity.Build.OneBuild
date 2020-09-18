echo off
if not defined projectPath (echo not defined projectPath
pause
exit
)

if not defined BUILD_OPTIONS_versionName set BUILD_OPTIONS_versionName=
if not defined logFile set logFile=%projectPath%/Logs/build.log
echo 参数开始
echo UNITY_HOME=%UNITY_HOME%
echo projectPath=%projectPath%
echo logFile=%logFile%
echo BUILD_OPTIONS_versionName=%BUILD_OPTIONS_versionName%
echo 参数结束
echo 正在生成...
"%UNITY_HOME%/Editor/Unity.exe" -batchmode -nographics -quit -executeMethod UnityEditor.Build.OneBuild.EditorOneBuild.Build -projectPath "%projectPath%" -logFile "%logFile%"
echo 生成结束

pause