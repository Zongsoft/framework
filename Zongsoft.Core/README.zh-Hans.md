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

## 服务：不引用具体实现也能使用能力

### 应用容器与模块容器

插件宿主扫描部署的程序集并注册服务。应用代码依赖 Core 契约或共享模块契约程序集，不必引用每个提供者的实现包。部署另一个兼容提供者改变的是装配，而不是消费方的业务代码。

- `ApplicationContext.Current.Services` 是应用服务容器，宿主初始化后可用。
- `Module.Current.Services` 是应用自定义模块常用的入口。这里 `Module` 是你的模块类，不是 Core 通用单例。[ApplicationModule.Services](src/Services/ApplicationModule.cs) 优先解析模块注册，再回退共享应用服务。
- 构造函数注入和 `[ServiceDependency]` 可让宿主直接提供契约，无需反复查找。模块自己的选择使用模块容器，不要在单例中保留 HTTP 请求服务。

默认特性扫描器将服务实现注册为单例。模块容器不会自动实现租户隔离，也不等于请求作用域。不要在每次操作后释放从容器取得的共享服务。

### 选择正确的定位方式

| 需求 | API | 含义 |
| --- | --- | --- |
| 一个已注册契约 | `ResolveRequired<T>()` | 契约不可用时抛出错误 |
| 所有实现 | `ResolveAll<T>()` | 枚举已注册的契约实现 |
| 符合条件的实现 | `FindRequired<T>(argument)` | 使用匹配器行为或契约 Name 匹配 |
| 注册的服务别名 | `ResolveRequired("name")` | 解析注册时建立的别名 |
| 提供者供应的实例 | `IServiceProvider<T>.GetService(name)` | 选择配置中的具名服务，不是另一个 DI 容器 |
| 配置中的限定名 | `services.Locate<T>("name@provider")` | 先选具名提供者，再获取它的具名实例 |

可选形式 `Resolve`、`Find`、`Locate` 可能返回 null。存在多个提供者时应明确选择，不要让注册顺序意外决定数据库、队列或缓存。[定位实现](src/Services/ServiceProviderExtension.cs)定义了匹配规则。

### 示例：真实模块的服务定位

[Discussions Module](../../discussions/src/Module.cs) 在程序集元数据中声明模块身份，并通过 Core 契约获取数据访问器。以下为成员摘录，不是新造的模块实现：

```csharp
[assembly: ApplicationModule(Zongsoft.Discussions.Module.NAME)]

public const string NAME = nameof(Discussions);
public static readonly Module Current = new();

private IDataAccess _accessor;
public IDataAccess Accessor => _accessor ??=
	this.Services.ResolveRequired<IDataAccessProvider>().GetAccessor(this.Name);
```

[插件清单](../../discussions/src/Zongsoft.Discussions.plugin)将 `Module.Current` 挂载到 `/Workbench/Modules`；[选项文件](../../discussions/src/Zongsoft.Discussions.option)拥有 `/Discussions/General` 配置，宿主提供具名数据库连接。[ThreadService.Posting](../../discussions/src/Services/ThreadService.cs) 通过 `this.ServiceProvider.ResolveRequired<PostService>()` 取得 `PostService`，而非自行构造。

该用例区分模块归属、公共契约与约定配置。表达式提供者匹配请参阅真实的 [Scriban 适配器](../externals/scriban/README.zh-Hans.md)；Core 不包含内建的 `Rules:Evaluator` 配置或 Rules 应用。

### 示例：通过提供者取得具名缓存

以下连接名沿用[真实 Redis 缓存样例](../externals/redis/samples/distributedcache/Program.cs)，定位代码改为插件宿主方式。提供者是额外一层间接定位：一个 Redis 提供者可供应多个已配置缓存。部署 Redis 插件并配置名为 `Redis` 的连接后：

```csharp
using Zongsoft.Caching;
using Zongsoft.Services;

var services = ApplicationContext.Current.Services;
IDistributedCache cache = services.Locate<IDistributedCache>("Redis@Redis")
	?? throw new InvalidOperationException("The configured cache is unavailable.");
Console.WriteLine(cache.GetType().Name);
```

字符串可来自应用选项，而不是源码常量。示例只定位服务，不访问服务器。[Redis 配置](../externals/redis/README.zh-Hans.md)定义具名连接的选择及回退行为。不是每个提供者都有注册别名，使用 `@provider` 前应核对注册。

### 注册与注入模块契约

提供者实现使用 `[Service<TContract>]` 或 `IServiceRegistration`，插件也可通过服务表达式装配对象。程序集上的 `[ApplicationModule(Zongsoft.Discussions.Module.NAME)]` 标明服务注册的模块归属，模块及扩展节点仍需由应用清单贡献。

`[ServiceDependency]` 可注入契约。非空 ServiceName 表示向 `IServiceProvider<T>` 请求具名实例，`~` 或 `.` 表示所属模块名。Provider 选择模块容器，`/` 或 `*` 表示应用容器，详见 [ServiceDependencyAttribute](src/Services/ServiceDependencyAttribute.cs)。

💡 插件表达式 `{service:~@Discussions}` 中的 `@...` 选择**模块容器**；ServiceLocator 的 `Redis@Redis` 中 `@Redis` 选择**具名服务提供者**。它们是两种不同语法，不能混用。

### 配置与生命周期检查

提供者名、连接名和扩展路径放在应用拥有的配置/程序集元数据中。依次确认提供者插件已加载、请求契约已注册、具名配置存在。缺失服务应产生可定位的启动错误，而不是静默构造默认实现。

仅对简单 Core 值对象，或所有权明确的独立适配场景直接构造具体类型。数据库连接、队列、表达式运行时等共享实现通常应通过配置的提供者获得。部署和宿主准备见 [Plugins](../Zongsoft.Plugins/README.zh-Hans.md)。
