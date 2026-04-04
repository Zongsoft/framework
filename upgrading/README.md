# Zongsoft.Upgrading 自动升级插件库

[English](README.md) |
[简体中文](README-zh_CN.md)

-----

## 概述

[**Z**ongsoft.**U**pgrading](https://github.com/Zongsoft/framework/tree/main/upgrading) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的自动升级插件库。

💡 如果将本插件库部署到 [_宿主_](https://github.com/Zongsoft/hosting) 程序中，即可享有自动升级更新之功能。

### 清单文件示例

`Zongsoft.Hosting.Terminal@1.1.0_win-x64.manifest` 文件内容如下所示：

```json
{
	"name": "Zongsoft.Hosting.Terminal",
	"kind": "Fully",
	"title": "Zongsoft Terminal Application",
	"edition": null,
	"version": "1.1.0",
	"size": 123456,
	"path": "Zongsoft.Hosting.Terminal@1.1.0_win-x64.zip",
	"checksum": null,
	"platform": "windows",
	"architecture": "x64",
	"deprecated": false,
	"summary": null,
	"creation": "2026-04-01T10:30:59",
	"description": null
}
```
