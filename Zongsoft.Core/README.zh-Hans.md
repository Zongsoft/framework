# Zongsoft Core

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Core)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Core)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) | [简体中文](README.zh-Hans.md)

-----

## 概述

_**Z**ongsoft.**C**ore_ 是 [_**Z**ongsoft Framework_](https://github.com/Zongsoft/framework) 的基础类库。它提供框架其它包共同依赖的抽象接口、基类、工具 API 和基础设施组件。

当前项目目标框架为 `net8.0`、`net9.0` 和 `net10.0`，根命名空间为 `Zongsoft`，并以 `Zongsoft.Core` NuGet 包发布。项目集成了 `Microsoft.Extensions.*` 体系中的依赖注入、配置、选项、宿主抽象、对象池和内存缓存等能力。

## 安装

```powershell
dotnet add package Zongsoft.Core
```

## 包含内容

该包覆盖面较广，是 _**Z**ongsoft_ 其它上层包的公共基础层。主要能力如下：

- **通用工具** _(`Zongsoft.Common`)_
  > 类型转换、随机数、序列、断言谓词、计时器、位向量、字符串/类型/URI 扩展、时间戳、异步锁和验证接口。
- **集合** _(`Zongsoft.Collections`)_
  > 层次节点、分类树、参数包、同步集合、队列、对象池，以及集合和字典扩展。
- **组件模型** _(`Zongsoft.Components`)_
  > 命令基础设施、命令行解析、事件交换、特性管道、重试/回退/熔断/限流/超时特性、状态机、工作者、监视器、处理器、执行器、过滤器、转换器和标识符。
- **配置** _(`Zongsoft.Configuration`)_
  > 设置和连接设置、配置绑定和识别、XML 配置提供程序、模型配置、Profile/INI 解析，以及与 `Microsoft.Extensions.Options` 的集成。
- **数据抽象** _(`Zongsoft.Data`)_
  > 查询条件、条件集合、范围、分页、排序、操作数、模型描述、数据访问/数据服务契约、操作选项和事件、元数据模型、归档契约和事务基础类型。
- **服务** _(`Zongsoft.Services`)_
  > 应用上下文和应用模块、服务注册/发现辅助类型、依赖元数据、模块化服务、服务访问器和分布式锁抽象。
- **缓存** _(`Zongsoft.Caching`)_
  > 内存缓存封装、淘汰/变更事件、缓存扫描器、分布式缓存契约，以及用于批量聚合写入的 `Spooler<T>`。
- **通信与消息** _(`Zongsoft.Communication`、`Zongsoft.Messaging`)_
  > 通道、监听器、发送器、接收器、请求/响应、传输器、分包器、通知器、消息队列、生产者/消费者、轮询器和队列选项抽象。
- **诊断与遥测** _(`Zongsoft.Diagnostics`)_
  > 日志契约、控制台/文本/XML 日志器和格式化器、诊断配置、遥测仪表、指标描述和导出器启动契约。
- **IO 与硬件** _(`Zongsoft.IO`)_
  > 虚拟文件系统契约和本地实现、路径解析、MIME 辅助方法、压缩工具、二进制/文本读取扩展和硬件档案模型。
- **安全** _(`Zongsoft.Security`)_
  > Claims 辅助方法、凭证、证书、密钥/签名契约、密码工具、认证/授权流程、用户、角色、权限和权限评估器。
- **序列化** _(`Zongsoft.Serialization`)_
  > 序列化契约、JSON 序列化辅助方法、序列化选项、命名约定、成员特性和 System.Text.Json 转换器。
- **表达式与文本** _(`Zongsoft.Expressions`、`Zongsoft.Text`)_
  > 词法分析器/分词器基础设施、表达式求值契约、语法异常、正则文本处理和模板契约。
- **反射** _(`Zongsoft.Reflection`)_
  > 高性能反射辅助方法，以及成员表达式解析和求值。
- **运行时辅助** _(`Zongsoft.Resources`、`Zongsoft.Scheduling`、`Zongsoft.Versioning`、`Zongsoft.Terminals`)_
  > 资源定位、触发器抽象、语义化版本解析，以及终端/控制台命令执行。

## 仓库结构

```text
Zongsoft.Core/
  src/        主类库源码。
  test/       覆盖核心行为的 xUnit 测试。
  samples/    MemoryCache、Spooler、Superviser 和 EventExchanger 控制台示例。
  benchmark/  针对反射和数据模型辅助类型的 BenchmarkDotNet 基准测试。
```

## 构建与测试

```powershell
dotnet restore Zongsoft.Core.slnx
dotnet build Zongsoft.Core.slnx -c Release
dotnet test test/Zongsoft.Core.Tests.csproj -c Release
```

仓库也提供了 Cake 脚本：

```powershell
dotnet cake build.cake --target=test --edition=Release
```

## 示例

`samples` 目录包含几个使用本包真实 API 的小型控制台程序：

- `memorycache`
  > `MemoryCache`、过期扫描、容量限制和终端输出。
- `spooler`
  > 高写入量场景下的 `Spooler<T>` 批量聚合。
- `superviser`
  > `Superviser`、`Supervisable`、工作状态报告和终端命令。
- `eventexchanger`
  > `EventExchanger` 通道和应用上下文集成。

## 许可

Zongsoft.Core 基于 [LGPL-3.0-or-later](https://github.com/Zongsoft/framework/blob/main/LICENSE) 许可证发布。
