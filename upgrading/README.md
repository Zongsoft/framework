# Zongsoft.Upgrading 自动升级插件库

[English](README.md) |
[简体中文](README-zh_CN.md)

-----

## 概述

[**Z**ongsoft.**U**pgrading](https://github.com/Zongsoft/framework/tree/main/upgrading) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的自动升级插件库。

💡 如果将本插件库部署到 [_宿主_](https://github.com/Zongsoft/hosting) 程序中，即可享有自动升级更新之功能。

### 清单文件示例

`Zongsoft.Hosting.Terminal@1.1.0_win-x64.manifest` 文件内容如下所示：

```xml
<?xml version="1.0" encoding="UTF-8"?>

<Release
	Name="Zongsoft.Hosting.Terminal"
	Kind="Fully"
	Edition=""
	Version="1.1.0"
	Size="123456"
	Path="Zongsoft.Hosting.Terminal@1.1.0_win-x64"
	Checksum="SHA1:1234567890ABCDEF"
	Platform="Windows"
	Architecture="X64"
	Deprecated="false"
	Creation="2026-04-10T10:30:59">

	<Title></Title>
	<Summary></Summary>
	<Description></Description>

	<Tags>
		<Tag></Tag>
	</Tags>

	<Properties>
		<Property name="" type="">
		<![CDATA[
		]]>
		</Property>
	</Properties>

	<Executors>
		<Executor event="">
		<![CDATA[
		]]>
		</Executor>
	</Executors>
</Release>
```
