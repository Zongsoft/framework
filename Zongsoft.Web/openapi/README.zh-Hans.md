# Zongsoft.Web.OpenApi Open-API 插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Web.OpenApi)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Web.OpenApi)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

---

## 概述

[**Z**ongsoft.**W**eb.**O**pen**A**pi](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Web/openapi) 是 [***Z**ongsoft*](https://github.com/Zongsoft/framework) 开源框架中的 [***O**pen **API***](https://www.openapis.org) 插件库，为 [*ASP.NET*](https://learn.microsoft.com/zh-cn/aspnet) 应用提供了开箱即用的 [***O**pen **API***](https://www.openapis.org) 相关功能。

插件自动发现应用中的控制器和操作，提供 JSON 和 YAML 格式的 **OpenAPI 3.2.1** 文档。内置的 [Scalar](https://github.com/scalar/scalar) 界面读取同一份文档，显示多级 API 目录。插件支持 `net10.0` 和 `net11.0`。

## 功能特性

- **零代码接入**：以 Zongsoft 插件方式部署，宿主启动后自动注册文档端点与 Scalar 界面。
- **自动发现**：应用中的全部控制器与操作被收集为 `ControllerServiceDescriptorCollection` 并生成文档。
- **属性路由**：基于 `ControllerModel`/`ActionModel` 生成路径、HTTP 方法、参数（路径/查询/请求头/请求体）与 Schema。
- **约定式路由支持**：通过约定式路由（如 `MapControllerRoute("default", "{controller}/{action}/{id?}")`）暴露的接口，会从运行时路由表（`EndpointDataSource`）反查真实路径，而不是被跳过。
- **安全方案**：通过配置声明认证方案（如 `Credential`、`Bearer`、`Basic`、`OAuth2`、`OpenID`）。
- **服务器与环境**：以变量化模板定义服务器地址与环境列表。
- **公共请求头**：通过 `IHeaderProvider` 为每个操作注入公共请求头。
- **Scalar 界面**：内置交互式 API 浏览器（基于 `Scalar.AspNetCore`）。
- **插件化部署**：以 `.plugin`/`.option` 构件发布，可按宿主启用或禁用。

## 快速开始

### 1. 部署插件

将 `Zongsoft.Web.OpenApi` 插件拷贝到 Zongsoft 宿主应用的 `plugins` 目录下，例如 `plugins/zongsoft/openapi/`：

```
plugins/
└── zongsoft/
    └── openapi/
        ├── Zongsoft.Web.OpenApi.plugin
        ├── Zongsoft.Web.OpenApi.option
        ├── Zongsoft.Web.OpenApi.dll
        └── Microsoft.OpenApi.dll
```

插件中的 `WebInitializer` 会在宿主启动时自动调用 `app.UseOpenApi()` 与 `app.MapScalarApiReference()`，**应用无需任何代码改动**。

### 2. 访问文档

宿主启动后，可访问以下端点：

| 端点                 | 说明                          |
| ------------------ | --------------------------- |
| `/openapi/v1.yaml` | YAML 格式的 OpenAPI 文档         |
| `/openapi/v1.json` | JSON 格式的 OpenAPI 文档         |
| `/openapi/v1.yml`  | YAML 格式的别名                  |
| `/scalar/`         | Scalar 交互式界面，可选择手动模式和已配置的环境 |

路由模板为 `/openapi/{documentName}.{extension}`，默认文档名为 `v1`。如需自定义路由，可手动注册端点：

```csharp
app.UseOpenApi("/docs/{documentName}.{extension}");
```

## API 目录

目录按应用模块、服务命名空间的各级和服务组织。嵌套控制器对应多级服务命名空间。Scalar 将各级目录显示为可展开的栏目，一个服务可以同时包含自己的操作和子目录。

自动化工具可以沿 `tags[].parent` 遍历目录，并使用 `tags[].name` 匹配操作的 Tag。`tags[].summary` 用于显示标题。操作引用的 Tag 名称为服务限定名。JSON 和 YAML 包含相同的目录和操作。

同一操作公开多个路由或 HTTP 方法时，文档为每个入口生成唯一的 `operationId`。按路径和 HTTP 方法作序数排序，首个入口沿用原标识，其余入口附加方法和转义后的路径。响应状态码和响应体需要显式契约。缺少响应元数据时，文档提供通用的 `default` 响应说明。

### 宿主兼容性

插件在两个目标框架上均使用 `Microsoft.OpenApi` 3.x。`Microsoft.AspNetCore.OpenApi` 10.x 依赖 `Microsoft.OpenApi` 2.x。若 .NET 10 应用需要该生成器，可以通过不同宿主提供两份文档。 [OpenAPI.NET 迁移指南](https://github.com/microsoft/OpenAPI.NET/blob/main/docs/upgrade-guide-3.md#integrations-with-aspnet-core)

## 路由与参数

### 路径解析

JSON 和 YAML 中 `paths` 下的路径键均以 `/` 开头。

1. **属性路由**：从控制器/操作的 Selector 路由模板（`AttributeRouteModel`/`IRouteTemplateProvider`）解析路径。空或 `null` 的模板（如不带模板的 `[HttpGet]`）会被忽略——它只表达 HTTP 方法谓词，而非路由模板。
2. **约定式路由**：对于没有属性路由模板的操作，生成器查询运行时路由表（从请求服务中解析的 `EndpointDataSource`），按 `ControllerActionDescriptor`（控制器名/操作名）与 `HttpMethodMetadata` 匹配对应的 `RouteEndpoint`；随后用模板文本（`RoutePattern.RawText`，如 `/quartz/{controller}/{action}`）结合 `ControllerActionDescriptor.RouteValues` 中的实际路由值实例化（如 `/quartz/Jobs/Edit`）。这样约定式路由接口就能以真实 URL 进入文档，而不再被跳过。

### 参数推断

无显式绑定源（`[From*]`）的参数按以下规则推断：

- 参数名存在于路由模板中 → `in: path`；
- 参数类型为标量类型 → `in: query`；
- 其余情况按请求体参数处理（`requestBody` 及 `application/json` Schema）。

## 配置说明

所有选项均在 `/web/openapi` 配置路径下（默认值见 `Zongsoft.Web.OpenApi.option`）：

```xml
<options>
	<option path="/web/openapi">
		<authentication persisted="true">
			<authenticator authenticator.name="credential" kind="http" scheme="Credential" />
			<authenticator authenticator.name="bearer" kind="http" scheme="Bearer" />
			<authenticator authenticator.name="basic" kind="http" scheme="Basic">
				<username>{{username}}</username>
				<password>{{password}}</password>
			</authenticator>
		</authentication>

		<headers>
			<header header.name="X-Json-Behaviors" method="GET,POST" value="ignores:null,empty;casing:camel" />
		</headers>

		<servers>
			<server server.name="current" />
			<server server.name="ports" description="Host and port" url="{scheme}://{host}:{port}">
				<variable variable.name="scheme" default="http">
					<value>http</value>
					<value>https</value>
				</variable>
				<variable variable.name="host" default="localhost" />
				<variable variable.name="port" default="8069" />
			</server>
			<server server.name="sites" description="Site, host and port" url="{scheme}://{site}.{host}:{port}">
				<variable variable.name="scheme" default="http">
					<value>http</value>
					<value>https</value>
				</variable>
				<variable variable.name="site" default="api.b">
					<value>api.a</value>
					<value>api.b</value>
					<value>api.c</value>
					<value>api.iot</value>
					<value>api.gateway</value>
				</variable>
				<variable variable.name="host" default="zongsoft.com" />
				<variable variable.name="port" default="80" />
			</server>
		</servers>

		<environments>
			<environment environment.name="development">
				<variable variable.name="username" default="" />
				<variable variable.name="password" default="" />
			</environment>
		</environments>
	</option>
</options>
```

### 认证方案

设置 `authentication persisted="true"` 可将认证设置保存在浏览器存储中。随插件提供的配置启用此选项，未配置时不持久化。调用受保护接口前，请在 Scalar 中输入有效凭据；插件不会预填随机生成的凭据。

要复用 Zongsoft 登录凭证，请先调用登录接口，然后打开 **Test Request → Auth**，选择 `credential`，并在 **Value** 中填入 `Credential <编号>`。Scalar 不会从登录响应中自动提取编号。开启持久化后，Scalar 会按文档记住认证值，刷新页面也能保留。在同一文档中改用其他服务器地址时，请确认已保存的凭证属于目标服务器。

- `authenticator` — 声明一个安全方案。认证下拉框按 XML 声明顺序显示：
  - `name` — 方案标识；
  - `kind` — `http` | `custom` | `oauth2` | `openid`；
  - `scheme` — HTTP 认证方案名（`kind="http"` 时）；
  - `location` — `header` | `cookie` | `query` | `path`（`kind="custom"` 时）；
  - `username`/`password` — `Basic` 认证的凭据。

### 公共请求头

每个 `<header>` 声明一个公共请求头（`name`/`value`），应用于指定的 HTTP 方法（`method`，逗号分隔）。内置的 `HeaderProvider` 实现了 `IHeaderProvider`，可通过服务容器替换或追加提供者以定制请求头注入。

### 服务器与环境

服务器列表始终将 `current` 放在首位。没有显式 URL 时，使用文档所在站点的地址和请求的 `PathBase`，自动跟随实际协议、主机和端口。根路径部署输出 `url: /`，挂载在 `/gateway/app` 的应用输出 `url: /gateway/app`。选择 **Current site（当前站点）** 无需填写端口。

服务器下拉提供 **current（当前站点）**、**ports（主机和端口）** 和 **sites（站点）**。主机模式使用 `{scheme}://{host}:{port}`，站点模式使用 `{scheme}://{site}.{host}:{port}`。

选择 **sites** 后，在 `site` 下拉框中选择 `api.a`、`api.b`、`api.c`、`api.iot` 或 `api.gateway`，默认为 `api.b`。可通过编辑 `site` 下的 `<value>` 子项增减候选项。主机和端口可直接填写。访问 IP 地址时使用 **ports**，IPv6 主机需包含方括号，例如 `[::1]`。

默认选择器不提供完整地址输入。如需一次性粘贴完整 URL（包括基路径），可以按需添加以下服务器：

```xml
<server server.name="full-url" description="完整服务地址" url="{url}">
	<variable variable.name="url" default="http://localhost:8069" />
</server>
```

每个服务器的 `server.name` 对应稳定的 OpenAPI `name`，`description` 用作说明，省略时使用名称。Scalar 的选择器可能显示服务器名称或 URL。保留名称 `current` 忽略大小写，始终排在首位；其余服务器按 XML 声明顺序排列。如果代理无法正确设置 `Request.PathBase`，可以显式配置当前站点的公开基地址：

```xml
<server server.name="current" description="当前站点" url="/gateway/app" />
<server server.name="staging" description="预发布环境" url="https://staging.example.com/api" />
```

服务器 URL 支持 `{name}` 占位符。变量具有 `default` 默认值和可选的 `description`。非空的 `<value>` 列表按 XML 声明顺序提供下拉选项，默认值必须与其中一项完全匹配。省略列表即可自由输入。变量按 URL 模板中首次出现的顺序输出。

宿主应在暴露文档端点前配置代理转发和应用基路径。相对服务器 URL 使用文档来源地址，不依赖内部监听地址。下载文档后，离线客户端和自动化测试需要显式指定基地址。跨域浏览器请求要求目标服务器的 CORS 策略允许文档站点；HTTPS 页面还要求使用 HTTPS API 地址。

打开 `/scalar/` 默认进入 **none**，可通过服务器选择器手动指定地址。要添加固定的部署目标，在 `/web/openapi` 下为相应环境设置 `url`。

```xml
<environments>
	<environment environment.name="development-a" url="http://localhost:5801">
		<variable variable.name="site" default="a" />
	</environment>
	<environment environment.name="development-b" url="http://localhost:5802/api">
		<variable variable.name="site" default="b" />
	</environment>
</environments>
```

在 `/scalar/` 左上角的文档下拉框中，**none** 始终排在第一项，随后按 XML 声明顺序列出配置了 `url` 的环境。下拉框、浏览器链接和文档地址都使用同一个环境名，例如 `/scalar/#development-a` 和 `/openapi/environments/development-a.json`。`description` 可用于说明该环境的服务器。没有 URL 的环境仍可为手动文档提供变量。从接口页面切换环境时，Scalar 可能保留网址片段中的接口路径；直接打开 `/scalar/#staging-a` 这样的短链接可进入目标环境的文档首页。

每个环境可以定义自己的 `<variable>`。例如，在请求字段中填写 `{{site}}`，即可引用当前环境的站点值。`url` 仍用于配置完整的服务器地址。

环境名由小写 ASCII 字母和数字组成，可用单个连字符分隔各段，最长 255 个字符。`none` 保留给手动模式。URL 必须是 HTTP 或 HTTPS 绝对地址，可包含端口和基路径，不含用户信息、查询字符串或片段。浏览器跨域调用时，目标环境须通过 CORS 允许文档页面的来源。认证信息在客户端填写，公开的配置和文档只放空占位值。

## 扩展点

| 接口                         | 用途                  |
| -------------------------- | ------------------- |
| `Services.IHeaderProvider` | 按操作与 HTTP 方法提供公共请求头 |

## 许可证

**Zongsoft.Web.OpenApi** 库基于 [MIT 许可证](https://github.com/Zongsoft/framework/blob/main/LICENSE) 发布。

## 插件化接入

Zongsoft.Core、Zongsoft.Web、Zongsoft.Plugins、Zongsoft.Plugins.Web 由宿主统一提供，部署清单不引用这四个基础库；编译依赖保留在项目文件中。

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

使用 Plugins.Web 宿主，其初始化流程发现本包服务注册，并按部署选项映射 OpenAPI/Scalar。公开文档端点前确认暴露范围与身份验证。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Web.OpenApi` | [Zongsoft.Web.OpenApi.plugin](src/Zongsoft.Web.OpenApi.plugin) |
| 文件复制及依赖 | [Zongsoft.Web.OpenApi.deploy](src/Zongsoft.Web.OpenApi.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft web openapi]
nuget:Zongsoft.Web.OpenApi
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Web.OpenApi.option`、`Zongsoft.Web.OpenApi.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。
