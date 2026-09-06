# Zongsoft.Web Web基础库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**W**eb](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Web) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架中的 _**W**eb_ 类库，提供了有关 [_ASP.NET_](https://learn.microsoft.com/zh-cn/aspnet) 应用开发的各项功能。

它是框架的 ASP.NET Core 集成层，提供面向服务的控制器、模型绑定与 JSON 格式化、凭据身份验证、控制器元数据、文件端点、SignalR 发现和多站点配置。应用通过插件清单装配时，应增加 [Zongsoft.Plugins.Web](../Zongsoft.Plugins.Web/README.zh-Hans.md)。

## 基础概念

- **服务控制器**把 `IDataService<TModel>` 投射为 HTTP 操作，同时保留筛选、排序、分页、验证和授权语义。
- **控制器描述符**从 MVC 应用模型得到模块、服务、路由、操作及参数的稳定工具视图。
- **绑定器**把范围、混合值、布尔值、分页和排序等紧凑 HTTP 值转换为框架抽象。
- **站点**描述逻辑网站及其主机，是配置元数据，而不是另一台 ASP.NET 服务器。

底层请求模型可参阅 [ASP.NET Core MVC 概述](https://learn.microsoft.com/zh-cn/aspnet/core/mvc/overview)。

## 安装

```shell
dotnet add package Zongsoft.Web
```

## 最小控制器

```csharp
using Microsoft.AspNetCore.Mvc;
using Zongsoft.Data;
using Zongsoft.Web;

[ApiController]
[Route("api/products")]
public sealed class ProductController(IDataService<Product> service) :
	ServiceController<Product, IDataService<Product>>
{
	protected override IDataService<Product> GetService() => service;
}

public sealed class Product
{
	public int ProductId { get; set; }
	public string Name { get; set; } = string.Empty;
}
```

基类会按数据服务能力暴露计数、存在性、查询、新增、更新、保存和删除流程。需要定制时重写受保护钩子或禁用操作，无需复制整套 Action。

这是消费既有数据服务的控制器骨架：应用需要先注册自己的 `IDataService<Product>` 实现、实体映射和连接，不能仅复制此类就得到可用数据库。插件化应用把控制器放在自己的类库及 manifest 中，由宿主扫描和容器注入；不在控制器内构造数据引擎或数据库驱动。无需数据库的可运行 HTTP 接入例子见 [Plugins.Web 完整示例](../Zongsoft.Plugins.Web/README.zh-Hans.md)。

> 💡 控制器约定与特性都会影响最终路由。应生成 OpenAPI 或检查控制器描述符，不要仅凭类名拼接客户端 URL。

## HTTP 约定

`WebUtility.Paginate` 把分页元数据写入响应头。JSON 格式化器使用框架序列化器。绑定器支持 `Paging`、`Sorting`、`Range<T>`、`Mixture<T>`、布尔值和时间跨度；无效输入通过 MVC 模型状态报告。

通过 ASP.NET 身份验证注册凭据方案：

```csharp
builder.Services
	.AddAuthentication()
	.AddCredentials();
```

`AuthorizationAttribute` 与 `AuthorizationConvention` 将操作关联到框架授权元数据。凭据签发和权限服务位于 [Zongsoft.Security](../Zongsoft.Security/README.zh-Hans.md)。

🚨 身份验证只确认调用者身份，并不会授权所有端点。请显式配置策略，切勿记录原始凭据或把凭据放入查询字符串。

## 文件、SignalR 与扩展点

`WebFileAccessor` 及内置文件/目录控制器通过 HTTP 暴露框架文件系统；启用前请限制根目录、权限、大小和媒体类型。SignalR 发现用于识别插件宿主中的 Hub 实现；主机和站点集合通过 `IWebEnvironment` 提供匹配元数据。

扩展点包括泛型服务控制器、MVC 约定、绑定器、格式化器、筛选器、凭据选项、Web 主机/站点抽象，以及 `ControllerServiceDescriptor` 元数据。

## 延伸阅读

- [插件化 Web 宿主](../Zongsoft.Plugins.Web/README.zh-Hans.md)
- [OpenAPI 集成](openapi/README.zh-Hans.md)
- [gRPC 集成](grpc/README.zh-Hans.md)
- [实现协作指南](SKILL.md)
