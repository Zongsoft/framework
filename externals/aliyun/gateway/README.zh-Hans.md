# Zongsoft.Externals.Aliyun.Gateway 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Aliyun.Gateway)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Aliyun.Gateway)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.Aliyun.Gateway` 是面向阿里云服务的 ASP.NET Core 回调网关。它接收服务回调，并将请求正文及参数分派给具名的 Zongsoft `IHandler`。

## 路由与处理器

网关公开 `POST /Externals/Aliyun/Fallback/{name}/{key?}` 路由。`{name}` 用于选择 `FallbackExecutor.Instance.Handlers` 中的处理器，可选的键及全部请求参数通过执行上下文传入。处理器返回值作为响应内容，空返回值对应 `204 No Content`。

请在 Zongsoft Web 宿主中加载 `Zongsoft.Externals.Aliyun.Gateway.plugin` 并注册所需的回调处理器，同时按照各阿里云服务的要求验证回调签名并限制端点访问。

## 安装与处理器注册

```shell
dotnet add package Zongsoft.Externals.Aliyun.Gateway
```

在 `FallbackExecutor` 使用的插件树中注册稳定处理器名称；`{name}` 必须与其中之一匹配。处理器以请求流作为参数，并从 `Parameters` 获取路由/查询值。正文只能读取一次，应传递取消，并返回符合回调协议的值。

注意当前网关清单把名为 `Handlers` 的节点绑定到了执行器本身，而不是其字典。因此消费插件应先暴露执行器的 `Handlers` 属性，再把处理器追加到该集合；消费插件的 manifest 需声明对 `Zongsoft.Externals.Aliyun.Gateway` 的依赖，并列出实现处理器的应用程序集。

```xml
<extension path="/Workbench/Externals/Aliyun/Fallback/Handlers">
	<expose name="Items" value="{path:../@Handlers}" />
</extension>
<extension path="/Workbench/Externals/Aliyun/Fallback/Handlers/Items">
	<object name="Notification" type="MyCompany.Aliyun.NotificationHandler, MyCompany.Aliyun" />
</extension>
```

> 💡 如果提供商在超时后重试，应尽快应答。完成签名验证后，把长耗时且幂等的工作放入内部可靠队列。

🚨 访问该路由本身不能证明调用方来自阿里云。处理载荷前必须验证服务专属签名、时间戳/随机数和重放窗口；限制载荷、使用 TLS、避免记录秘密或个人数据，并安全处理重复投递。

## 验证

使用提供商回调验证工具或本地签名 Fixture，覆盖合法、无效签名、过期、重复、畸形、空结果与处理器失败情形。测试不可指向生产回调 URL。

## 延伸阅读

- [阿里云集成](../README.zh-Hans.md)
- [ASP.NET Core 请求正文指南](https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/use-http-context)
- [外部适配器实现协作指南](../../SKILL.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../../Zongsoft.Plugins/README.zh-Hans.md)。

使用 Plugins.Web 宿主，并部署下列依赖主适配器。服务/控制器发现发生在宿主初始化阶段，无需把业务逻辑放进 Program.cs；接收请求前验证端点及授权。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Aliyun.Gateway` | [Zongsoft.Externals.Aliyun.Gateway.plugin](Zongsoft.Externals.Aliyun.Gateway.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Aliyun.Gateway.deploy](Zongsoft.Externals.Aliyun.Gateway.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals aliyun]
nuget:Zongsoft.Externals.Aliyun

[plugins zongsoft externals aliyun gateway]
nuget:Zongsoft.Externals.Aliyun.Gateway
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Aliyun.Gateway.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。
