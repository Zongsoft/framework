# Zongsoft.Security.Captcha 人机识别插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Security.Captcha)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Security.Captcha)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**S**ecurity.**C**aptcha](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Security/captcha) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的安全插件库的子插件，提供了 _人机识别_ 相关功能。

内置 `Authencode` 方案会生成 PNG 挑战、把答案保存到 `IDistributedCache`，并在回答正确后换取短期的一次性确认令牌。它与 `ICaptcha`、Security Web 控制器及 `X-Captcha` 响应头协同工作。

## 安装与前置条件

```shell
dotnet add package Zongsoft.Security.Captcha
```

请部署 `Zongsoft.Security.Captcha.plugin`；它依赖主 Security 插件。宿主必须提供 `IDistributedCache`。多实例部署必须使用共享缓存，否则一个节点签发的挑战无法在另一节点核验。

## 浏览器/API 流程

1. 通过 Security 验证码端点请求 `Authencode` 方案。
2. 显示返回的 `image/png`，并从 `X-Captcha` 响应头读取挑战令牌。
3. 以 `token:code` 提交答案（提供程序也接受等号分隔）。
4. 得到确认令牌，并传给受保护的安全流程。
5. 最终核验会消费确认项与原始挑战项。

挑战在 10 分钟后过期；回答正确后产生的确认有效 5 分钟。确认核验会删除两项缓存，因此确认只能使用一次。

> 💡 `AuthencodeImager.Generate(code, width, height)` 可以直接生成图片，但应用流程通常应使用 `ICaptcha`，以保持缓存、格式化与方案匹配一致。

## 安全与无障碍

🚨 图片验证码只是滥用防护的一种信号。请限制端点速率并监控，使用 TLS，不记录答案或令牌，也不要把验证码当作身份证明；还应为无法完成图片挑战的用户提供无障碍替代方案。

图片渲染依赖 ImageSharp 及其字体/绘图库。请验证目标环境有可用字体，并确保图片尺寸、代理响应头与缓存过期行为一致。

## 参考

- CAPTCHA
	- [English](https://en.wikipedia.org/wiki/CAPTCHA)
	- [Chinese](https://zh.wikipedia.org/wiki/CAPTCHA)

- [安全 API 插件](../api/README.zh-Hans.md)
- [ASP.NET Core 分布式缓存](https://learn.microsoft.com/zh-cn/aspnet/core/performance/caching/distributed)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

使用已配置 Security 并注册分布式缓存提供程序的 Web 宿主。服务发现暴露 Authencode 及其格式化器，不要无意注册同方案的第二个挑战实现。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Security.Captcha` | [Zongsoft.Security.Captcha.plugin](Zongsoft.Security.Captcha.plugin) |
| 文件复制及依赖 | [Zongsoft.Security.Captcha.deploy](Zongsoft.Security.Captcha.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft security]
nuget:Zongsoft.Security

[plugins zongsoft security captcha]
nuget:Zongsoft.Security.Captcha
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Security.Captcha.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。
