# Unity OneBuild
---
Unity3D 一键打包工具

管理多个平台配置，将多个配置文件根据文件优先级合并为一个配置



#### 菜单

###### Build

- Build

  BuildPipeline.BuildPlayer

- Update Config

  加载配置文件并运行，在Build前自动执行

- Release

  发布版本，移除[debug]版本

- Debug

  开发版本，添加[debug]版本

- No User

  若勾选则禁用所有已选中的 User/* 版本, 移除[user]版本

- User/UserName

  开发人员可定制的配置，添加[user]版本

**样例：**

```c#
#region Build User Version A

private const string BuildUserVersionName_A = OneBuild.UserVersionPrefix + "a";
private const string BuildUserVersionMenuName_A = OneBuild.UserVersionMenu + "A";

[MenuItem(BuildUserVersionMenuName_A, priority = OneBuild.UserVersionMenuPriority)]
public static void BuildUserVersionMenu_A()
{
    OneBuild.SetUserVersion(BuildUserVersionName_A);
}

[MenuItem(BuildUserVersionMenuName_A, validate = true)]
public static bool BuildUserVersionMenu_Validate_A()
{
    Menu.SetChecked(BuildUserVersionMenuName_A, OneBuild.ContainsVersion(BuildUserVersionName_A));
    return !OneBuild.IsNoUser;
}

#endregion
```


### 配置样例

```xml
<Type type="UnityEditor.Build.OneBuild" name="Build"></Type>
<Type type="UnityEditor.PlayerSettings" name="PlayerSettings"></Type>
```

- type

  指定类型名称

- name

  对type起一个别名，可在其它地方引用{$name:Property}

  

#### 属性
PlayerSettings.productName
``` xml
<Type type="UnityEditor.PlayerSettings">
  <productName>MyProduct</productName>
</Type>
```

#### 方法

PlayerSettings.SetScriptingBackend
``` xml
<SetScriptingBackend>
  <BuildTargetGroup>{$Build:BuildTargetGroup}</BuildTargetGroup>
  <ScriptingImplementation>IL2CPP</ScriptingImplementation>
</SetScriptingBackend>
```

#### Flags
格式:value1,value2... 多个值[,]分隔
``` xml
<Type type="UnityEditor.PlayerSettings.Android">
  <targetArchitectures>
    <AndroidArchitecture>ARMv7,ARM64</AndroidArchitecture>
  </targetArchitectures>
</Type>
```

#### 引用属性
格式:{$TypeName:Name[,Format]}

``` xml
<Type type="UnityEditor.PlayerSettings" name="PlayerSettings">
</Type>
<Type type="UnityEditor.Build.OneBuild" name="Build">
  <OutputFileName>{$PlayerSettings:productName}_{$Build:Version}_v{$Build:VersionCode}.apk</OutputFileName>
</Type>
```

#### 值合并
#### combin: 合并分隔符
#### combineOptions: 合并选项 

- None

  默认追加

- Clear 

  清除之前所有的值

- Remove

  移除指定的值

- Distinct

  去除重复值

  


#### 字符串值合并 a;b;c...
``` xml
<SetScriptingDefineSymbolsForGroup combin=";" combinOptions="Distinct">
  <BuildTargetGroup>{$Build:BuildTargetGroup}</BuildTargetGroup>
  <defines>DEBUG</defines>
</SetScriptingDefineSymbolsForGroup>
```

#### Flags枚举值合并 value1,value2...
``` xml
<BuildOptions combin="," combinOptions="Remove">AutoRunPlayer</BuildOptions>
```



#### 配置文件目录

**Assets/Config**

**version.txt**

配置版本优先级，指定配置文件加载顺序

默认的配置：

```
build;platform;debug;user
```

- build

默认版本

- platform

平台版本，复合值之一，BuildTargetGroup值，值为 (standalon, android, ios, ...) 

- debug

开发版本，值为(debug)

- user

用户版本，复合值之一，值为(user-xxx) 



#### 样例

**build.xml**

正式版
**build.debug.xml**

开发版
**build.android.xml**

Android正式版
**build.android.debug.xml**

Android开发版



### 支持的特性

- PreProcessBuildAttribute

  Build 管线，在 BuildPipeline.BuildPlayer 之前执行
  
  执行顺序:
  
  ```
  PreProcessBuildAttribute
  BuildPipeline.BuildPlayer
  PostProcessBuildAttribute
  ```
  
  