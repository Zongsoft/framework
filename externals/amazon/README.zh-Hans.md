# Zongsoft.Externals.Amazon 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Amazon)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Amazon)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**E**xternals.**A**mazon](https://github.com/Zongsoft/framework/tree/main/externals/amazon) 将 Amazon Web Services 集成到 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 框架中。当前程序包主要面向 Amazon S3，并通过 Zongsoft 文件系统抽象提供对象存储访问能力。

## S3 文件系统

插件在 `/Workbench/FileSystem` 下注册 `S3FileSystem`，支持对 S3 存储桶执行文件和目录操作，通过 `amazon.s3` 连接设置驱动器读取参数，并使用 `zfs.s3` 方案标识资源。AWS 区域、服务端点、访问密钥和密钥可在随包选项文件或宿主配置中设置。

加载 `Zongsoft.Externals.Amazon.plugin`，并配置 `/Externals/Amazon/ConnectionSettings` 后即可解析该文件系统。指定自定义服务器地址时，驱动器也可连接兼容 S3 的服务；文件系统用法可参考[测试项目](test)。

## 安装与配置

```shell
dotnet add package Zongsoft.Externals.Amazon
```

```xml
<option path="/Externals/Amazon">
	<connectionSettings default="production">
		<connectionSetting connectionSetting.name="production"
		                   driver="amazon.s3"
		                   value="region=us-east-1;accessKey=REPLACE_ME;secretKey=REPLACE_ME" />
	</connectionSettings>
</option>
```

设置包括 `server`/`url`、`region`、`client`、`timeout`、`accessKey`、`secretKey` 与 `accountId`。指定自定义 `server` 时启用路径式寻址，以支持兼容端点。

## 文件系统路径

首个路径段是 Bucket，其余部分是对象键：

```csharp
using Zongsoft.IO;

var path = "zfs.s3:/application-assets/manuals/start.pdf";
await using(var stream = await FileSystem.File.OpenAsync(path, FileMode.OpenOrCreate))
{
	await source.CopyToAsync(stream, cancellationToken);
}

var info = await FileSystem.File.GetInfoAsync(path, cancellationToken);
```

目录操作通过对象键前缀模拟层级；S3 本身没有真实目录。重命名/复制可能需要复制对象，列表由远程服务分页。

> 💡 可用时优先采用工作负载身份或外部凭据提供程序。确需配置密钥时，应在部署期注入，不能提交到选项文件。

🚨 S3 写入和删除会影响远程持久数据。请采用最小权限 Bucket Policy、加密、版本控制与生命周期规则，并验证 Bucket/Key 输入以防跨租户访问。兼容 S3 的服务在元数据、追加、一致性、分段上传和 URL 行为上可能不同。

及时释放打开的数据流。集成测试会进行真实网络调用并修改配置的可丢弃 Bucket，必须保持显式启用。

## 延伸阅读

- [Amazon S3 文档](https://docs.aws.amazon.com/s3/)
- [文件系统测试](test)
- [外部适配器实现协作指南](../SKILL.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

清单挂载 S3 文件系统提供程序。配置 `/Externals/Amazon/ConnectionSettings` 后通过框架文件系统使用 `zfs.s3` 路径；仅引用包不会注册该方案。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Amazon` | [Zongsoft.Externals.Amazon.plugin](src/Zongsoft.Externals.Amazon.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Amazon.deploy](src/Zongsoft.Externals.Amazon.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals amazon]
nuget:Zongsoft.Externals.Amazon
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Amazon.plugin`、`Zongsoft.Externals.Amazon.option`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。
