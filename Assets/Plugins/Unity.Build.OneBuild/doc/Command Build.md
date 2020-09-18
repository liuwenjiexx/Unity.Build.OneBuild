# 命令行生成

### Release.bat

```bat
echo off
set projectPath=%~dp0..
set BUILD_OPTIONS_versionName=<版本名称>
call "%projectPath%/Packages/Unity.Build.OneBuild/Editor/Build.bat"
pause
```

### Debug.bat

```bat
echo off
set projectPath=%~dp0..
set BUILD_OPTIONS_versionName=<版本名称>
call "%projectPath%/Packages/Unity.Build.OneBuild/Editor/Build_Debug.bat"
pause
```

- BUILD_OPTIONS_< property >

  生成选项参数，对应 `EditorBuildOptions` 属性

- BUILD_SETTINGS_< property >

  生成设置参数，对应 `BuildSettings` 属性

- BUILD_OPTIONS_versionName

  必选，指定生成的版本名称

- projectPath

  必选，Unity工程路径

- logFile

  输出日志路径，默认 < project Path>/Logs/build.log