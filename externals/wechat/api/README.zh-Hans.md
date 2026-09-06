# Zongsoft.Externals.Wechat.Web 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Wechat.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Wechat.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.Wechat.Web` 将 [Zongsoft 微信集成](..)中的服务公开为 ASP.NET Core Web API，适用于需要以统一 HTTP 接口访问微信小程序、公众号、支付和证书能力的 Zongsoft Web 宿主。

## API 范围

- 小程序登录、手机号获取、用户查询及访问凭证刷新；
- 公众号凭证、用户、消息模板、身份验证及 Postmark 操作；
- 银行和支行查询，包括银行卡识别；
- 平台证书获取及临时媒体文件上传。

请同时加载 `Zongsoft.Externals.Wechat.Web.plugin` 和 `Zongsoft.Externals.Wechat`。这些控制器使用 `Externals/Wechat` 区域，并从核心插件解析已配置的小程序、公众号、支付机构及相关服务；对公网仅应开放宿主实际需要的端点。

## 安装与配置边界

```shell
dotnet add package Zongsoft.Externals.Wechat.Web
```

本包只包含控制器。账号标识、小程序/公众号凭据、商户/支付 Authority、证书、HTTP Client 和 Token 缓存均由[核心适配器](../README.zh-Hans.md)配置。请从宿主生成 OpenAPI，以查看应用框架约定后的准确路由、动词和载荷。

## 典型小程序流程

1. 客户端从微信获取临时登录 Code。
2. 调用已配置小程序端点，经 `Applet`/`UserProvider` 交换 Code。
3. 按宿主身份验证设计使用返回的应用身份。
4. 只有所属服务需要时才经 Credential 端点刷新访问凭据。

公众号操作同样先解析具名 Channel，再读取用户、发送模板消息或身份验证；银行/证书/文件控制器委托给已配置支付与平台服务。

> 💡 微信 Access Token 与平台证书都有自身过期和刷新规则。请使用核心 Manager 与缓存，不要让每个控制器调用方各自实现刷新循环。

## 安全与副作用

🚨 登录 Code、手机号、OpenID/UnionID、支付证书和媒体文件都很敏感。必须使用 TLS、验证应用自身调用者、隔离账号/租户名、验证上传类型和大小，并从日志中清除秘密与个人数据。

部分操作会调用微信外部 API，可能发送消息或改变远端状态。应限制速率，并在提供商支持时使用幂等；重试前区分提供商拒绝与暂时传输失败。

## 延伸阅读

- [微信核心集成](../README.zh-Hans.md)
- [回调网关](../gateway/README.zh-Hans.md)
- [微信开放文档](https://developers.weixin.qq.com/doc/)
- [外部适配器实现协作指南](../../SKILL.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../../Zongsoft.Plugins/README.zh-Hans.md)。

使用 Plugins.Web 宿主，并部署下列依赖主适配器。服务/控制器发现发生在宿主初始化阶段，无需把业务逻辑放进 Program.cs；接收请求前验证端点及授权。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Wechat.Web` | [Zongsoft.Externals.Wechat.Web.plugin](Zongsoft.Externals.Wechat.Web.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Wechat.Web.deploy](Zongsoft.Externals.Wechat.Web.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals wechat]
nuget:Zongsoft.Externals.Wechat

[plugins zongsoft externals wechat web]
nuget:Zongsoft.Externals.Wechat.Web
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Wechat.Web.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。
