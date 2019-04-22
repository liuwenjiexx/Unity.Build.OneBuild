# UnityOneBuild
---
Unity3D One Key Config

将多个配置文件根据文件优先级合并为一个配置



### 配置样例

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

None 默认追加
Clear 清除之前所有的值
Remove 移除指定的值
Distinct 值不重复


#### 字符串值合并 a;b;c...
``` xml
<SetScriptingDefineSymbolsForGroup combin=";" combinOptions="Distinct">
  <BuildTargetGroup>{$Build:BuildTargetGroup}</BuildTargetGroup>
  <Il2CppCompilerConfiguration>DEBUG</Il2CppCompilerConfiguration>
</SetScriptingDefineSymbolsForGroup>
```

#### Flags枚举值合并 value1,value2...
``` xml
<BuildOptions combin="," combinOptions="Remove">AutoRunPlayer</BuildOptions>
```

### 文件名格式
##### 关键字优先级顺序
* 平台：android，ios
* 开发版：debug

build.config (发布版)
build.debug.config (开发版)
build.android.config (Android发布版)
build.android.debug.config (Android开发版)
