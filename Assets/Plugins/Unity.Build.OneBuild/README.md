# Unity Build
---
Unity3D 一键打包工具

管理多个平台设置，将多个设置文件根据文件优先级合并为一个设置，在Build时会执行一次加载设置



## 预览

![](doc\images\ui_release.PNG)



## 菜单

### Build

- Build

  生成

- Settings

  打开生成配置编辑器

- Release

  发布版本，移除[debug]版本

- Debug

  开发版本，添加[debug]版本

- Channel - <Channel Name>

  渠道名称

- No User

  若勾选则禁用所有已选中的 User/* 版本, 移除[user]版本

- User/<User Name >

  开发人员可定制的配置，添加[user]版本



## 编辑配置

### 新建配置

1. 点击菜单 `Build/Settings` 打开配置面板

2. 点击 `New Config` 按钮打开 `Create Build Config` 窗口

3. 输入参数后点击 `Create` 按钮创建配置文件

   - Platform

     平台

   - Channel Name

     渠道名称

   - User Name

     用户名

   - Debug

     是否为 debug 配置

4. 点击`新建配置` 按钮创建配置文件

5. 默认配置包含两个默认配置类型，可添加自定义类型[添加配置类型](#添加配置类型) 

   - UnityEditor.Build.OneBuild.BuildSettings

      别名 Build

   - UnityEditor.PlayerSettings

      别名 PlayerSettings
      
      

### 添加配置类型
1. 已有的 [一般设置类型](#一般设置类型)

   注册新的类型，通过 `BuildConfigTypeAttribute` 特性

   语法：
   
   ```c#
   [assembly: BuildConfigType(configType [, Name])]
   ```
   
   样例：
   
   ```C#
   [assembly: BuildConfigType(typeof(BuildSettings), "Build")]
   [assembly: BuildConfigType(typeof(UnityEditor.PlayerSettings))]
   ```
   
2. 点击 `Add Type...` 按钮选择类型




### 添加配置属性

1. 点击配置类型右边 `+` 菜单

2. 选择一个配置项点击添加，配置项可以是字段，属性，方法

3. 编辑配置值

 

**配置项菜单**

点击配置属性名称弹出菜单

- 文本

  强制为文本格式编辑，用于[参数化值](#参数化值)

  ```
{$Build:@Version}
  {$Build:@BuildTargetGroup}
  ```
  
- 键

  对需要多次设置的属性通过参数生成唯一配置键

  比如：`SetStackTraceLogType` 根据 `logType` 生成 `key`

- 合并

  [值合并](#值合并)

  - 枚举值

    分隔符 `,`

  - 其它值

    比如：`SetScriptingDefineSymbolsForGroup` 设置 `defines` 参数为合并值，分隔符为 `;`

- 删除

  删除该配置项





## 一般设置类型

- **UnityEditor.Build.OneBuild.BuildSettings**

  基础 Build 设置

- **UnityEditor.PlayerSettings**

  通用设置

- **UnityEditor.PlayerSettings.Android**

  Android 平台设置

- **UnityEditor.PlayerSettings.iOS**

  iOS 平台设置

- **UnityEditor.Advertisements.AdvertisementSettings**

  Unity 广告设置

- **UnityEditor.Analytics.AnalyticsSettings**

  Unity 事件设置
  
- **UnityEditor.CrashReporting.CrashReportingSettings**

  Unity 错误日志报告设置

- **UnityEditor.Purchasing.PurchasingSettings**

  Unity 内购设置





## 参数化值

格式

```
{$<TypeName>:@@<MethodName>}
```

- TypeName

  类型名称或者类型别名

- MethodName

  静态方法名称



### 常用参数

`targetGroup`

```
{$Build:@BuildTargetGroup}
```

`Build.OutputDir` Release

```
Build/Release/{$Build:@BuildTargetGroup}
```

`Build.OutputDir` Debug

```
Build/Debug/{$Build:@BuildTargetGroup}
```

`Build.OutputFileName` Android Release

```
{$PlayerSettings:@productName}_v{$Build:@Version}_v{$Build:@VersionCode}.apk
```








## 字符串格式化

[字符串格式化](../../../../System.StringFormat/Assets/Plugins/System.StringFormat/README.md)





## 值合并

### 合并分隔符
### 合并选项 

- None

  默认追加

- Replace

  清除之前所有的值

- Remove

  移除指定的值

- Distinct

  去除重复值

### 枚举标志位值，分隔符 `,` 

``` xml
value1, value2, value2 ...
```


### `SetScriptingDefineSymbolsForGroup` 值, 分隔符 `;`
``` xml
value1;value2;value2 ...
```



## 配置文件结构

**配置目录Assets/Build**



### build.xml

文件后缀为 `build.xml`

**build.xml**

正式版
**debug.build.xml**

开发版
**android.build.xml**

Android正式版
**android.debug.build.xml**

Android开发版



- 平台名称

  平台版本，复合值之一，BuildTargetGroup值，如：(standalon, android, ios, ...) 

- debug

  开发版本，区分正式版

- 用户名称

  格式：user-< 名称 >

- 渠道名称

  格式：channel-< 渠道名称 >



### 优先级

配置文件将按优先级排序加载

1. <empty>

   空，默认


2. platform

   平台

3. channel-

   渠道


3. debug

   debug 标志


4. user-

   用户




## 添加用户配置

1. 配置文件名称格式

   ```
   user-<name>
   ```

   例如：user-a.build.xml

2. 自动生成菜单代码

   Assets/Plugins/gen/Editor/EditorOneBuildMenu.cs

3. 查看菜单 `Build/User/< name >`





## 版本号控制

#### BuildSettings.IncrementVersion

默认值 -1，Build时自动递增版本号，递增的索引，索引值从0开始

例如：0.0.[1] ，该索引值为 2

#### BuildSettings.IncrementVersionCode

默认值 false，Build时自动递增 VersionCode

#### 获取版本号值

```
{$Build:@@Version0}.{$Build:@@Version1}.{$Build:@@Version2}.{$Build:@@Version3}.{$Build:@@Version4}
```



### 获取Git标签版本号

设置后会读取Git标签设置版本号文件

比如Git标签格式：v版本号

```
v0.0.1
```

设置 `Build.GitTagVersion`, 正则表达式格式

```
v(?<result>\S+)
```







## 生成

### 自动生成

点击 `Build/Build` 菜单，自动运行构型

生成过程中会多次中断和编译，确保编辑器窗口保持活动窗口



### [命令行生成](doc/Command Build.md)





### [安装](doc/install.md)





## 生成管线

管线顺序:

- **BuildStarted** [unity.build.onebuild IBuildPipeline]

  生成开始，如：生成版本号


- **PreProcessBuildAttribute** [unity.build.onebuild]

  生成之前

- **IPreprocessBuildAssetBundle** [unity.assetbundles]

  生成资源包之前

- **IPostprocessBuildAssetBundle** [unity.assetbundles]

  生成资源包之后

- **BuildPipeline.BuildPlayer** [UnityEditor]

  生成

- **PostProcessBuildAttribute** [UnityEditor]

  生成之后
  
- **BuildEnded** [unity.build.onebuild IBuildPipeline]

  生成结束



## 生成对比

空项目, Unity版本2019.1.10, Android, IL2CPP, Strip Engine Code, StripingLevel Heigh

| Development Build | C++ Compiler Configuration | Size | 百分比差异 | 时间 |
| ----------------- | -------------------------- | ---- | ---------- | ---- |
| 否                | Release                    | 11.3 |            |      |
| 否                | Debug                      | 18.4 | 63%        | 20%  |
| 是                | Release                    | 25.3 | 38%        |      |
| 是                | Debug                      | 33.4 | 32%        | 20%  |

