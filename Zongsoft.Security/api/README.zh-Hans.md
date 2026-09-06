# Zongsoft.Security.Web 安全插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Security.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Security.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**S**ecurity.**W**eb](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Security/api) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的安全插件库的子插件，提供了安全功能集的 _**W**eb_ 插件化支持。

它把框架安全服务发布为 ASP.NET Core 控制器：登录/退出/续期、秘密验证、验证码签发/核验、用户、角色、成员、密码和权限。本包只是 [Zongsoft.Security](../README.zh-Hans.md) 的 HTTP 投射，不是独立身份存储。

## 安装与宿主

```shell
dotnet add package Zongsoft.Security.Web
```

请把 `Zongsoft.Security.Web.plugin` 部署到插件化 Web 宿主。清单依赖 `Zongsoft.Security`；核心安全插件、数据映射、数据库结构、凭据身份验证与授权服务必须先可用。

## 端点分组

| 控制器 | 职责 |
| --- | --- |
| `AuthenticationController` | 按方案/键登录、退出、续期凭据、签发和核验秘密 |
| `CaptchaController` | 签发并核验某个 `ICaptcha` 方案 |
| `UserController` | 用户生命周期、联系方式核验、密码和成员关系 |
| `RoleController` | 角色生命周期、成员、父级与继承关系 |
| 嵌套权限控制器 | 用户或角色的直接权限与过滤权限 |
| `AuthorizationController` | 查看授权方案与已计算权限 |

路由由框架 MVC 约定决定。请使用随库维护的 [HTTP 请求示例](../docs/http/)或生成的 OpenAPI 元数据，确认当前部署版本的准确路由与载荷。

## 典型流程

1. 当策略要求时签发验证码或带外秘密。
2. 通过身份验证器方案登录，并保存返回凭据。
3. 经配置的身份验证处理器发送该凭据。
4. 对每项操作授权，并按凭据期限续期或退出。

> 💡 `scheme`、`scenario` 与验证渠道都是由已注册服务解析的领域值；未核对宿主配置前，不要在客户端自行杜撰。

🚨 这些控制器会修改身份与权限状态。必须使用 TLS、限制请求速率、约束管理操作、防止跨租户标识符，并按浏览器凭据类型处理防伪需求；日志中不得出现原始密码或秘密载荷。

## 响应与取消

控制器使用标准 HTTP 状态码、框架分页和取消令牌。批量或重置操作可能替换成员关系或权限，客户端必须区分追加操作与 `reset=true`。请求取消并不证明服务端变更已经回滚；可重试流程应使用幂等设计并重新读取状态。

## 延伸阅读

- [安全模型与配置](../README.zh-Hans.md)
- [API 参考](../docs/Zongsoft.Security-api.md)
- [HTTP 示例](../docs/http/)
- [Zongsoft.Web 约定](../../Zongsoft.Web/README.zh-Hans.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

使用 Plugins.Web 宿主，并部署下列依赖主适配器。服务/控制器发现发生在宿主初始化阶段，无需把业务逻辑放进 Program.cs；接收请求前验证端点及授权。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Security.Web` | [Zongsoft.Security.Web.plugin](Zongsoft.Security.Web.plugin) |
| 文件复制及依赖 | [Zongsoft.Security.Web.deploy](Zongsoft.Security.Web.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft security]
nuget:Zongsoft.Security

[plugins zongsoft security web]
nuget:Zongsoft.Security.Web
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Security.Web.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。
