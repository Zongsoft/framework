# Zongsoft.Security 安全插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Security)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Security)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**S**ecurity](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Security) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的安全插件库，提供了用户角色、身份验证、授权管理等安全领域的基础功能。

本包提供默认的持久化用户、角色、成员、权限、身份验证和授权服务。它实现 `Zongsoft.Core` 中的安全契约，并通过 `Zongsoft.Data` 持久化模型；只有需要 HTTP 端点时才增加 [Zongsoft.Security.Web](api/README.zh-Hans.md)。

## 安全模型

- **身份验证（Authentication）**通过 `Identity`、`Secretor` 等验证器方案证明身份，并签发有期限的凭据。
- **授权（Authorization）**判断主体能否执行操作；角色、直接权限、继承成员关系和过滤权限共同参与决策。
- **凭据（Credential）**是后续请求携带的已签发证明，在过期或撤销前应像密码一样保护。
- **质询/秘密（Challenge/Secret）**是登录、找回密码和变更联系方式使用的带外验证步骤，本身不是长期凭据。

## 安装与数据初始化

```shell
dotnet add package Zongsoft.Security
```

请部署 `Zongsoft.Security.plugin`、`Zongsoft.Security.option` 与 `Zongsoft.Security.mapping`，并在启动服务前使用 [database](database/) 中匹配数据库的脚本初始化结构；映射与数据库表是同一契约，必须同步演进。

插件依赖 `Zongsoft.Data`，并把 Security 模块、身份验证器、授权器以及角色/用户/成员/权限服务挂载到工作台。

## 配置

```xml
<option path="/Security">
	<identity verification="none" passwordLength="0" passwordStrength="None" />
	<authentication period="8:0:0">
		<attempter limit="5" window="00:01:00" period="00:05:00" />
		<expiration>
			<scenario scenario.name="api" period="1.00:00:00" />
		</expiration>
	</authentication>
	<authorization roles="security,securities" />
</option>
```

`identity` 控制身份核验与密码策略；`authentication.period` 控制凭据期限，`attempter` 限制连续失败，场景过期时间约束验证流程；`authorization.roles` 指定管理角色。

🚨 随包值只是框架默认值，并非生产安全基线。请明确选择密码和身份策略，使用 TLS，妥善保护签名/加密材料，并确保日志不包含凭据与验证秘密。

## 应用工作流

应用通常解析 Core 中的身份验证/授权契约，而不直接实例化 `CredentialProvider`、`UserService` 或 `PrivilegeService`。插件负责装配具体服务、通过 `Module.Events` 发布身份验证事件并公开模块诊断。请经服务 API 管理用户/角色生命周期，以维持成员和权限不变量。

授权过滤与数据范围直接相关：应保留取消信号和调用者上下文，绝不能把“无授权结果”替换成无限制查询。

## 延伸阅读

- [HTTP API 插件](api/README.zh-Hans.md)
- [验证码提供程序](captcha/README.zh-Hans.md)
- [数据库表说明](database/Zongsoft.Security-tables.md)
- [API 参考](docs/Zongsoft.Security-api.md)
- [实现协作指南](SKILL.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../Zongsoft.Plugins/README.zh-Hans.md)。

还需部署一个 Data 驱动并准备对应安全表。清单挂载 Security 模块、认证和授权服务，映射必须与插件一起部署；接收登录请求前配置缓存/凭据依赖。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Security` | [Zongsoft.Security.plugin](src/Zongsoft.Security.plugin) |
| 文件复制及依赖 | [Zongsoft.Security.deploy](src/Zongsoft.Security.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft security]
nuget:Zongsoft.Security
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Security.plugin`、`Zongsoft.Security.option`、`Zongsoft.Security.mapping`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。
