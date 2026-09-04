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

该库自动发现当前应用中的控制器与操作，生成 **OpenAPI 3.1** 规范文档（`JSON` 或 `YAML` 格式），并通过独立端点对外提供，同时内置 [**Scalar**](https://github.com/scalar/scalar) 交互式界面——无需改动任何业务代码。

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
| `/scalar`          | Scalar 交互式界面（类 Swagger 浏览器） |

路由模板为 `/openapi/{documentName}.{extension}`，默认文档名为 `v1`。如需自定义路由，可手动注册端点：

```csharp
app.UseOpenApi("/docs/{documentName}.{extension}");
```

## 工作原理

### 文档生成

`DocumentGenerator.Generate(context)` 基于存放在 `ApplicationContext.Current.Properties` 中的 `ControllerServiceDescriptorCollection` 构建 `OpenApiDocument`：

- `GenerateEnvironments` — 环境列表（`x-environment` 扩展、Scalar 活动环境等）；
- `GenerateServers` — 带变量化模板的服务器列表；
- `GenerateSecuritySchemes` — 依据 `<authentication>` 配置生成安全方案；
- `GeneratePaths` — 路径、操作与参数；
- `GenerateSchemas` — 操作引用的组件 Schema。

### 路径解析

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
			<server server.name="ports" url="http://{host}:{port}">
				<variable variable.name="host" default="localhost">
					<value>localhost</value>
					<value>127.0.0.1</value>
				</variable>
			</server>
		</servers>

		<environments>
			<environment environment.name="development">
				<variable variable.name="host" default="192.168.0.101" />
			</environment>
		</environments>
	</option>
</options>
```

### 认证方案

- `persisted` — 是否在 Scalar 界面中持久化所选方案；
- `authenticator` — 声明一个安全方案：
  - `name` — 方案标识；
  - `kind` — `http` | `custom` | `oauth2` | `openid`；
  - `scheme` — HTTP 认证方案名（`kind="http"` 时）；
  - `location` — `header` | `cookie` | `query` | `path`（`kind="custom"` 时）；
  - `username`/`password` — `Basic` 认证的凭据。

### 公共请求头

每个 `<header>` 声明一个公共请求头（`name`/`value`），应用于指定的 HTTP 方法（`method`，逗号分隔）。内置的 `HeaderProvider` 实现了 `IHeaderProvider`，可通过服务容器替换或追加提供者以定制请求头注入。

### 服务器与环境

服务器与环境均支持带 `<variable>` 占位符（`{name}`）的模板化 `url`。每个变量声明 `default` 默认值及可选的 `<value>` 候选列表，Scalar 界面会将其渲染为下拉选择项。

## 扩展点

| 接口                         | 用途                  |
| -------------------------- | ------------------- |
| `Services.IHeaderProvider` | 按操作与 HTTP 方法提供公共请求头 |

## 目录结构

```
openapi/
├── DocumentGenerator.cs            # 文档生成入口
├── DocumentGenerator.*.cs          # 环境 / 服务器 / 安全 / 路径 / Schema
├── DocumentContext.cs              # 生成上下文（格式、服务、路由表等）
├── DocumentFormat.cs               # json / yaml 格式
├── Tags.cs                         # 标签生成（x-parent、x-displayName 等）
├── Utility.cs                      # 参数位置与类型工具
├── WebExtension.cs                 # UseOpenApi() 端点扩展
├── WebExtension.Writer.cs          # 响应输出的 UTF-8 缓冲写入器
├── WebInitializer.cs               # 端点与 Scalar 界面的自动注册
├── Configuration/                  # 认证 / 请求头 / 服务器 / 环境 / 变量选项
├── Services/                       # IHeaderProvider 及其实现
└── Extensions/                     # OpenAPI 扩展（x-*）构建器
```

## 许可证

**Zongsoft.Web.OpenApi** 库基于 [GNU 宽通用公共许可证 v3.0](../../LICENSE) 发布。
