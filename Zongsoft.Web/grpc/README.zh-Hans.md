# Zongsoft.Web.Grpc Grpc插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Web.Grpc)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Web.Grpc)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**W**eb](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Web/grpc) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架中的 [_**g**RPC_](https://grpc.io) 插件库，为宿主程序运行提供了 [_**g**RPC_](https://grpc.io) 环境初始化功能。

它为 Zongsoft Web 宿主增加服务器注册和端点发现，用于承载生成的 gRPC 服务基类；本包不定义应用协议，也不生成 `.proto` 文件。

## 安装与部署

```shell
dotnet add package Zongsoft.Web.Grpc
```

请将 `Zongsoft.Web.Grpc.plugin` 与程序集一起部署。该包会注册 `Grpc.AspNetCore.Server` 和服务器反射。

## 服务发现

实现由 `Grpc.Tools` 生成的服务基类，通过框架服务系统注册具体类型，并赋予 `gRPC` 标签。启动时，程序集扫描发现私有注册器 `GrpcInitializer.Registration`，由它调用 `AddGrpcWebEndpoints`、`AddGrpc` 与 `AddGrpcReflection`；`GrpcInitializer` 读取带标签的类型，调用 `MapGrpcService<T>`，并映射反射服务。

> 💡 `gRPC` 标签是发现契约。只有 DI 注册而没有该标签的服务不会被此初始化器映射。

## 宿主注意事项

gRPC 通常使用 HTTP/2 与 Protocol Buffers。请按照 [ASP.NET Core gRPC 指南](https://learn.microsoft.com/zh-cn/aspnet/core/grpc/)配置 Kestrel、TLS、代理、消息大小、截止时间和压缩，并把 `ServerCallContext.CancellationToken` 传递给下游操作。

🚨 本包会映射服务器反射，它会公开服务元数据。如果部署环境不应暴露协议面，请在网络和授权层限制访问。

## 源码与测试

[src 项目](src/Zongsoft.Web.Grpc.csproj)包含实现和插件构件；[test 项目](test/Zongsoft.Web.Grpc.Tests.csproj)验证协议注册及请求范围。两者均纳入 [Zongsoft.Web.slnx](../Zongsoft.Web.slnx)。

## 延伸阅读

- [Web 端点与请求范围](../docs/endpoints.zh-Hans.md)：通用监听、协议策略及宿主接入。
- [Zongsoft.Web](../README.zh-Hans.md)
- [插件化宿主](../../Zongsoft.Plugins.Web/README.zh-Hans.md)
- [Protocol Buffers 指南](https://protobuf.dev/programming-guides/proto3/)
- [gRPC 状态码](https://grpc.io/docs/guides/status-codes/)

## 插件化接入

Zongsoft.Core、Zongsoft.Web、Zongsoft.Plugins、Zongsoft.Plugins.Web 由宿主统一提供，部署清单不引用这四个基础库；编译依赖保留在项目文件中。

本包提供 Web 端点的 gRPC 协议策略：裸 `grpc` 默认 `grpc+http2`，请求按原生 gRPC 媒体类型分类，并执行方法、HTTP 版本、固定方法路径与无范围响应规则。该策略为私有实现，通过 `GrpcEndpointExtension.AddGrpcWebEndpoints` 注册；Web 主库不含这些协议判断。

显式宿主在 `AddWebEndpoints` 创建快照之前调用 `services.AddGrpcWebEndpoints()`；插件服务注册已自动完成这一步。缺少扩展时，建立 gRPC 请求范围会失败，不能作为普通 HTTP 启动。KestrelUtility 单独转换 grpc+http2 等显式承载不需要这项运行规则；转换裸 grpc 时须传入对应规范化规则。Plugins.Web 的服务注册器根据 Web 宿主端点声明自动调用 AddWebEndpoints，延迟到最终服务提供器创建快照；基于 Handler 的服务挂载仍按实施顺序进行，当前协议策略注册不自动改变现有根级服务映射。

`WebEndpointMetadata("/opentelemetry", "grpc")` 可表达路由的范围及请求类别 `Kind`；实际服务必须挂载在同一路径。独立 [协议测试项目](test/Zongsoft.Web.Grpc.Tests.csproj) 验证规则及隔离 HTTP/2 管线，尚不代表区域反射、完整 RPC/流或 HTTP/3 已验收。

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

使用 Plugins.Web 宿主。初始化器注册 gRPC 并映射带 `gRPC` 标签的服务类型，还需部署实际协议/服务插件。HTTP/2 端点及反射暴露需由宿主明确决定。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Web.Grpc` | [Zongsoft.Web.Grpc.plugin](src/Zongsoft.Web.Grpc.plugin) |
| 文件复制及依赖 | [Zongsoft.Web.Grpc.deploy](src/Zongsoft.Web.Grpc.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft web grpc]
nuget:Zongsoft.Web.Grpc
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Web.Grpc.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。
