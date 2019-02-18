# UnityOneBuild
---
Unity3D One Key Config

将多个配置文件根据文件优先级合并为一个配置



### 配置样例

PlayerSettings.productName
``` xml
<companyName>MyCompanyName</companyName>
```
  
PlayerSettings.SetScriptingBackend
```
  <scriptingBackend>
    <BuildTargetGroup>{$BuildTargetGroup}</BuildTargetGroup>
    <ScriptingImplementation>IL2CPP</ScriptingImplementation>
  </scriptingBackend>
```

### 文件名格式
##### 关键字优先级顺序
* 平台：android，ios
* 开发版：debug

build.config (发布版)
build.debug.config (开发版)
build.android.config (Android发布版)
build.android.debug.config (Android开发版)




## Project reference:
* [My Space](https://play.google.com/store/apps/details?id=com.lwj.model3d)
* [Crystal Defense](https://play.google.com/store/apps/details?id=com.lwj.crystaldefense)
* [Easy Color](https://play.google.com/store/apps/details?id=com.lwj.easycolor)
* [2048](https://play.google.com/store/apps/details?id=com.lwj.game2048)
* [Guess Word](https://play.google.com/store/apps/details?id=com.lwj.guessword)
