# Zongsoft.Externals.Velopack 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Velopack.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Velopack.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.Velopack.Web` 从 Zongsoft Web 宿主发布 Velopack Asset Feed 元数据。`VelopackFileScanner` 递归读取应用 `releases` 目录中的 `releases.*.json`，`VelopackController` 把其中 Asset 汇总为 `VelopackAssetFeed` 返回。

## 安装与 Feed 布局

```shell
dotnet add package Zongsoft.Externals.Velopack.Web
```

请部署 Web 插件，并把 Feed JSON 及其引用的包文件放入宿主管理的发布位置。元数据端点为：

```http
GET /Velopack/Releases?id=MyApplication
Accept: application/json
```

可选 `id` 用于筛选 `PackageId`。当前 `os`、`arch` 查询分支不会过滤结果，可选路由 `name` 也未被 Scanner 使用；实现变更前不要记录或依赖这些参数。

> 💡 Scanner 会递归合并每个匹配 JSON Feed。应按宿主或 Package ID 隔离发布目录，避免意外混合 Channel。

## 发布与安全

请使用兼容 Velopack 工具链生成 Feed 和包，以原子方式发布文件，并从客户端视角验证每个 URL。包二进制需要另行配置静态文件或对象存储服务。

🚨 更新 Feed 是软件供应链边界。必须使用 HTTPS、严格发布者访问控制、签名包、不可变版本产物、审计日志和安全缓存规则；绝不能允许匿名上传到扫描目录。

每次请求会扫描目录树并把 JSON 读入内存。请限制文件数量/大小、在适当层缓存，并监控畸形 Feed 与重复 Asset。

## 参考资料

《Velopack 文档》
> https://docs.velopack.io

《Velopack 参考》
> https://docs.velopack.io/reference

《Velopack 源码》
> https://github.com/velopack/velopack

- [客户端升级器](../../README.zh-Hans.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../../../Zongsoft.Plugins/README.zh-Hans.md)。

使用 Web 宿主并配置专用 releases 目录。发布列表端点读取文件，不会创建、签名或发布升级包。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Velopack.Web` | [Zongsoft.Externals.Velopack.Web.plugin](Zongsoft.Externals.Velopack.Web.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Velopack.Web.deploy](Zongsoft.Externals.Velopack.Web.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals velopack web]
nuget:Zongsoft.Externals.Velopack.Web
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Velopack.Web.option`、`Zongsoft.Externals.Velopack.Web.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。
