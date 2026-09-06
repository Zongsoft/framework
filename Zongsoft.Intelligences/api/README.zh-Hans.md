# Zongsoft.Intelligences.Web 插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Intelligences.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Intelligences.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**I**ntelligences.**W**eb](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Intelligences/api) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的 _**AI**_ **W**eb 插件库，提供了对 _**AI**_ 功能的 _**W**eb **API**_ 插件化支持。

它把 [Zongsoft.Intelligences](../README.zh-Hans.md) 的助手、模型、对话会话、历史与流式对话公开为框架 MVC 端点。本包不会增加 AI 提供程序；父插件中必须至少配置一个助手及其驱动/连接。

## 安装与部署

```shell
dotnet add package Zongsoft.Intelligences.Web
```

请把 `Zongsoft.Intelligences.Web.plugin` 部署到 Zongsoft Web 宿主。其清单依赖 `Zongsoft.Intelligences`，路由使用 `AI` Area。

## API 形状

| 资源 | 操作 |
| --- | --- |
| assistants | 列出助手或按名称查看一个助手 |
| models | 列出/获取提供程序模型并激活模型 |
| chats | 列出、打开、放弃会话并提交消息 |
| history | 读取或清除会话中的角色/文本记录 |

流式对话响应由 `WebUtility.EnumerableAsync` 通过 `System.Net.ServerSentEvents` 写出。客户端应增量处理[服务器发送事件](https://html.spec.whatwg.org/multipage/server-sent-events.html)，并通过关闭响应取消生成。

```http
POST /AI/ollama/Chats/my-session/Chat?role=user
Content-Type: text/plain; charset=utf-8

请用一段话解释有界通道。
```

上述路由来自显式控制器模板；请把 `ollama` 与会话标识替换成配置值。依赖历史之前，应先创建或找到对应会话。

> 💡 请从运行宿主生成 OpenAPI，以确认完整路由和响应形状。AI 模型载荷由提供程序定义，可能独立于本传输层变化。

## 安全与运行

🚨 提示词、历史、模型标识与生成文本都可能包含敏感或不可信内容。请验证和授权端点、隔离租户、限制请求/历史/输出大小与持续时间、限制速率，并且绝不能把模型输出当作可信 HTML 或命令。

取消会传递到助手操作。会话状态由配置的助手实现拥有；除非该实现明确保证，否则不要假设它持久化或能在宿主实例间共享。

## 延伸阅读

- [AI 提供程序与配置指南](../README.zh-Hans.md)
- [Zongsoft.Web 约定](../../Zongsoft.Web/README.zh-Hans.md)
- [服务器发送事件规范](https://html.spec.whatwg.org/multipage/server-sent-events.html)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

使用 Plugins.Web 宿主，并部署下列依赖主适配器。服务/控制器发现发生在宿主初始化阶段，无需把业务逻辑放进 Program.cs；接收请求前验证端点及授权。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Intelligences.Web` | [Zongsoft.Intelligences.Web.plugin](Zongsoft.Intelligences.Web.plugin) |
| 文件复制及依赖 | [Zongsoft.Intelligences.Web.deploy](Zongsoft.Intelligences.Web.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft intelligences]
nuget:Zongsoft.Intelligences

[plugins zongsoft intelligences web]
nuget:Zongsoft.Intelligences.Web
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Intelligences.Web.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。
