# Zongsoft.Upgrading 自动升级插件库

[English](README.md) |
[简体中文](README-zh_CN.md)

-----

## 概述

[**Z**ongsoft.**U**pgrading](https://github.com/Zongsoft/framework/tree/main/upgrading) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的自动升级插件库。

### 清单文件示例

`Zongsoft.Hosting.Terminal@1.1.0_win-x64.manifest` 文件内容如下所示：

```xml
<?xml version="1.0" encoding="UTF-8"?>

<release
	name="Zongsoft.Hosting.Terminal"
	kind="Fully"
	edition=""
	version="1.1.0"
	size="123456"
	path="Zongsoft.Hosting.Terminal@1.1.0_win-x64.zip"
	checksum="SHA1:1234567890ABCDEF"
	platform="Windows"
	architecture="X64"
	deprecated="false"
	creation="2026-04-10T10:30:59">

	<title></title>
	<summary></summary>
	<description></description>

	<tags>
		<tag></tag>
	</tags>

	<properties>
		<property name="" type="">
		<![CDATA[
		]]>
		</property>
	</properties>

	<executors>
		<executor event="">
		<![CDATA[
		]]>
		</executor>
	</executors>
</release>
```
