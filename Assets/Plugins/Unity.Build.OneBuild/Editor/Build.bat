echo off
if not defined projectPath (echo not defined projectPath
pause
exit
)

if not defined BUILD_OPTIONS_versionName set BUILD_OPTIONS_versionName=
if not defined logFile set logFile=%projectPath%/Logs/build.log
echo ������ʼ
echo UNITY_HOME=%UNITY_HOME%
echo projectPath=%projectPath%
echo logFile=%logFile%
echo BUILD_OPTIONS_versionName=%BUILD_OPTIONS_versionName%
echo ��������
echo ��������...
"%UNITY_HOME%/Editor/Unity.exe" -batchmode -nographics -quit -executeMethod UnityEditor.Build.OneBuild.EditorOneBuild.Build -projectPath "%projectPath%" -logFile "%logFile%"
echo ���ɽ���

pause