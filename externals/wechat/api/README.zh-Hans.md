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

- 小程序应用会话登录及结构化手机号获取；
- 公众号用户、消息模板、身份验证及 Postmark 操作；
- 银行和支行查询，包括银行卡识别；
- 微信验签材料元数据获取及临时媒体文件上传。

请同时加载 `Zongsoft.Externals.Wechat.Web.plugin` 和 `Zongsoft.Externals.Wechat`。这些控制器使用 `Externals/Wechat` 区域，并从核心插件解析已配置的小程序、公众号、支付机构及相关服务；对公网仅应开放宿主实际需要的端点。

## 安装与配置边界

```shell
dotnet add package Zongsoft.Externals.Wechat.Web
```

本包只包含控制器。账号标识、小程序/公众号凭据、商户/支付 Authority、证书、HTTP Client 和 Token 缓存均由[核心适配器](../README.zh-Hans.md)配置。请从宿主生成 OpenAPI，以查看应用框架约定后的准确路由、动词和载荷。

## 典型小程序流程

1. 客户端从微信获取临时登录 Code。
2. 宿主通过 `Applet.LoginAsync` 交换 Code，使用 OpenID 和可能返回的 UnionID 识别应用用户。
3. 宿主签发自己的应用会话，并将微信会话密钥保留在服务器。
4. 访问凭据的读取与刷新仅供所属服务的服务器端使用。

登录端点为 `POST Applets/{id}/Login?scenario={scenario}`，正文为登录 Code；通过宿主鉴证器、质询器和凭据提供者签发应用会话。手机号端点为 `POST Applets/{id}/PhoneNumber`，正文为 `{ "code": "手机号授权码", "openId": "可选身份" }`，返回完整号码、国内号码和国家区号。

两者要求宿主配置 `Wechat.Accounts` 授权策略，以请求的 `Account` 为资源验证调用者与租户归属；策略缺失返回 403。手机号入口还要求已认证的应用身份。没有小程序用户目录和公众号/小程序 Credential HTTP 端点。协议密钥不参与序列化，详见[小程序身份契约](../docs/implementation.zh-Hans.md#applet-identity)。

公众号操作同样先解析具名 Channel，再读取用户、发送模板消息或身份验证；银行/证书/文件控制器委托给已配置支付与平台服务。

`Certificates/{code?}` 查询默认商户中指定的公钥 ID 或平台证书序列号，省略时选择当前加密材料。结果为 `VerificationKey` 的标识、模式及有效期，不提供商户私钥或证书文件。材料加载和证书模式的刷新条件见[商户与密钥契约](../docs/implementation.zh-Hans.md#merchant-credentials)。宿主应统一维护凭据和证书刷新。

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
| `Zongsoft.Externals.Wechat.Web` | [Zongsoft.Externals.Wechat.Web.plugin](src/Zongsoft.Externals.Wechat.Web.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Wechat.Web.deploy](src/Zongsoft.Externals.Wechat.Web.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals wechat]
nuget:Zongsoft.Externals.Wechat@7.10.0

[plugins zongsoft externals wechat web]
nuget:Zongsoft.Externals.Wechat.Web@7.10.0
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Wechat.Web.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

公众号 `Channels/{id}/Login` 同样经宿主认证链返回应用会话。Postmark 另外要求 `Wechat.JSSDK` 站点策略，返回十六进制 Signature。用户与消息接口要求已认证身份及账号策略，路由中显式绑定 id。具体输入、state 校验职责及路由见[公众号服务契约](../docs/implementation.zh-Hans.md#channel-services)。

## 第三方平台预授权

已登录调用者可使用 `POST Components/{componentAppId}/Authorizations`，正文为 `Context`、`RedirectUri`、`AccountType` 和可选 `Mobile`。宿主须配置 `Wechat.Components` 策略检查业务范围、组件、账号类型及回跳站点，并注册可靠的 `IComponentStore`。服务端保存所属用户与 state 后才返回授权链接及期限；此操作不代表租户关联已生效，不返回组件或授权者凭据。详见[组件契约](../docs/implementation.zh-Hans.md#component-platform)。
