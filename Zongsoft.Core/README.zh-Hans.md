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
  > 类型转换、随机数、序列、断言谓词、计时器、位向量、字符串/类型/URI 扩展、时间戳、异步锁和验证接口。变量来源包括契约 [`IVariables`](docs/variables.zh-Hans.md#变量契约)、忽略大小写的内存变量字典 `Variables`、[通过 `Variables.Wrap()` / `ToVariables()` 获取的字典实时视图](docs/variables.zh-Hans.md#字典变量视图)，以及[环境变量实时视图 `Variables.Environments()`](docs/variables.zh-Hans.md#环境变量视图)。
- **集合** _(`Zongsoft.Collections`)_
  > 层次节点、分类树、参数包、同步集合、队列、对象池，以及集合和字典扩展。
- **组件模型** _(`Zongsoft.Components`)_
  > 命令基础设施、命令行解析、[命令选项变量](docs/variables.zh-Hans.md#命令选项变量)、事件交换、特性管道、重试/回退/熔断/限流/超时特性、状态机、工作者、监视器、处理器、执行器、过滤器、转换器和标识符。
- **配置** _(`Zongsoft.Configuration`)_
  > [普通设置（Settings）与连接设置（ConnectionSettings）](docs/settings.zh-Hans.md)、配置绑定和识别、[XML 配置提供程序与处理指令](docs/xml-configuration.zh-Hans.md)、模型配置、[Profile/INI 解析、指令扩展及变量视图](docs/profiles.zh-Hans.md)，以及与 `Microsoft.Extensions.Options` 的集成。
- **数据抽象** _(`Zongsoft.Data`)_
  > 查询条件、条件集合、范围、分页、排序、操作数、模型描述、数据访问/数据服务契约、操作选项和事件、元数据模型、归档契约和事务基础类型。
- **服务** _(`Zongsoft.Services`)_
  > 应用上下文和应用模块、服务注册/发现辅助类型、依赖元数据、模块化服务、服务访问器和分布式锁抽象。
- **缓存** _(`Zongsoft.Caching`)_
  > 内存缓存封装、淘汰/变更事件、缓存扫描器、分布式缓存契约，以及用于批量聚合写入的 `Spooler<T>`。
- **通信与消息** _(`Zongsoft.Communication`、`Zongsoft.Messaging`)_
  > [网络地址与协议值](docs/networking.zh-Hans.md)、通道、监听器、发送器、接收器、请求/响应、传输器、分包器、通知器、消息队列、生产者/消费者、轮询器和队列选项抽象。
- **诊断与遥测** _(`Zongsoft.Diagnostics`)_
  > 日志契约、控制台/文本/XML 日志器和格式化器、诊断配置、遥测仪表、指标描述和导出器启动契约。
- **IO 与硬件** _(`Zongsoft.IO`)_
  > 虚拟文件系统契约和本地实现、路径解析、[Searcher 本地通配搜索](docs/searcher.zh-Hans.md)、MIME 辅助方法、压缩工具、二进制/文本读取扩展和硬件档案模型。
- **安全** _(`Zongsoft.Security`)_
  > Claims 辅助方法、凭证、证书、密钥/签名契约、密码工具、认证/授权流程、用户、角色、权限和权限评估器。
- **序列化** _(`Zongsoft.Serialization`)_
  > 序列化契约、JSON 序列化辅助方法、序列化选项、命名约定、成员特性和 System.Text.Json 转换器。
- **表达式** _(`Zongsoft.Expressions`)_
  > 词法分析器/分词器基础设施、表达式求值契约和语法异常。
- **文本与模板** _(`Zongsoft.Text`、`Zongsoft.Text.Templating`)_
  > 正则文本处理、模板契约，以及[文本模板求值、成员导航、格式化和诊断](docs/expressions.zh-Hans.md)。
- **反射** _(`Zongsoft.Reflection`)_
  > 高性能反射辅助方法，以及成员表达式解析和求值。
- **运行时辅助** _(`Zongsoft.Resources`、`Zongsoft.Scheduling`、`Zongsoft.Versioning`、`Zongsoft.Terminals`)_
  > 资源定位、触发器抽象、语义化版本解析，以及终端/控制台命令执行。

## 密码哈希

[`Password`](src/Security/Password.cs) 使用 PBKDF2，默认哈希算法为 SHA256。`Factor` 的范围为 `0～255`，默认值为 `10`。因子为零时关闭额外迭代，仅执行一次 PBKDF2；正值对应 `Factor × 65,535` 次迭代。默认因子 `10` 对应 655,350 次，因子 `127` 对应 8,322,945 次。实际耗时取决于硬件、哈希算法和负载。

文本格式为 `algorithm#factor:nonce|value`，因子为零时可以省略 `#factor`。`Factor` 取代 `Exponent`；使用旧迭代映射生成的密码摘要需要重新生成。

`Password` 实现 `ISpanParsable<Password>` 和 `IParsable<Password>`，提供字符串与字符跨度的公开 `Parse`、`TryParse` 重载。文本格式不受区域设置影响，接口的格式提供者参数被忽略。空引用或空输入表示空密码；无效文本使 `TryParse` 返回 `false` 并输出空密码，`Parse` 则抛出 `FormatException`。

## 服务注册

`ServiceCollectionExtension.Register` 在整个程序集中发现 `IServiceRegistration` 实现，包括内部类和私有嵌套类。优先使用公开静态字段或属性供应的实例，否则调用公开或非公开的无参构造函数。未闭合的泛型类型，以及没有供应实例的抽象注册器不会被实例化；注册异常向调用方传播。

普通 `[Service]` 注册及选项扫描仍要求类型对外可见。非公开外层类型中的公共嵌套服务也不属于对外可见类型；因此可以隐藏注册辅助类，而不会自动公开内部服务。

## 事件调度

`EventExchanger` 通过 `IEventDispatcher` 捕获环境特性，再由内部固定的 `IExecutor<EventContext>` 执行远程投递。`FeatureProvider.Default` 默认包含 `Transaction.Features`，该共享提供者捕获事务绑定的 `IFeature` 与 `IPredication` 组合特性，独立使用时同样有效；应用可扩展其 `Providers` 集合，或注册 `IFeatureProvider` 服务追加条件。通道通过 `IEventReceiver` 接收事件；消息队列通道复用 `Events.GetTopic()` 的主题约定与 `Events.GetHandler(receiver)` 的通用接收处理器。默认投递不启用策略管线；应用插件可以通过 `EventDispatcher.Pipeline` 显式组合 Polly 策略。详见[事件调度与环境特性](docs/event-dispatching.zh-Hans.md)。

## 应用生命周期

`ApplicationContext` 接入宿主可等待的启动与停止阶段。`Exit()` 和 `Exit(exitCode)` 只请求宿主停止，不强制结束进程；`DisposeAsync()` 等待工作器实际收尾后释放上下文资源。应用构建器使用 `BuildAsync()`，以便初始化失败时等待异步释放。容器解析的初始化器和借用的服务容器视图保留其原有所有者。

## 终端输入

`ITerminal.Input` 使用 `TextReader`，普通流和内存文本可直接使用 `StreamReader`、`StringReader`。`Terminal.Console.Input` 提供与进程同寿命的共享行读取器：同时只允许一个等待者，取消仅结束等待，读取及结果保留给后续调用。执行器停止不释放输入；注入读取器遵循其自身的取消及资源契约。详见[终端输入](docs/terminal-input.zh-Hans.md)。

`ITerminalExecutor` 通过三个 `RunAsync` 重载运行命令循环，返回结束后的退出码；调用方必须等待该任务。

`ITerminal.Abort()` 同步发送中断通知并返回是否已处理，不停止执行器或进程。进程信号策略由宿主负责，Core 不注册全局控制台信号。

## 周期任务

`Common.Timer` 通过构造回调串行执行周期任务，支持注入 `TimeProvider`。`Start()` 建立周期调度；启动、停止和释放方法均不接受取消令牌。`Stop()` 请求内部取消，不等待回调和清理结束，`StopAsync()` 等待停止；`Dispose()` 永久关闭但不等待清理，`DisposeAsync()` 永久关闭并等待清理，先同步释放后仍可异步等待。调用方负责避免释放后使用业务功能及回调等待自身。`Failed` 通知运行故障，`Stopped` 通知本次回调及资源清理已结束；异步停止和异步释放等待通知返回并传播运行及通知异常。计时器内部观察后台任务异常，不写日志。详见[异步周期计时器](docs/timers.zh-Hans.md)。

[`Spooler<T>`](docs/spoolers.zh-Hans.md) 按周期、满载写入或显式请求串行处理缓冲数据，支持消费性枚举与继承扩展。`PutAsync` 表示接收，`FlushAsync` 等待在途刷新并处理一轮剩余数据；仅支持异步释放，`DisposeAsync` 停止接收并等待在途刷新及清理结束，不排空缓冲区。周期故障通过 `Failed` 通知，并由异步释放传播，缓冲器不写日志。

两个构造函数均支持可选的 `TimeProvider`。`Zongsoft.Caching.Spooler` 指标源提供接收数、队列数量、刷新耗时、满载等待和故障指标，由应用选择导出；`FileLogger` 仅支持异步释放，等待文件写入结束后清理资源。

## 通信生命周期

`ChannelBase.CloseAsync` 只执行一次，并与并发调用者共享结果。进入前取消会阻止关闭，开始后取消只影响本调用方的等待；前后通知异常不能跳过传输清理。释放先等待关闭，再执行一次派生资源清理。关闭后不能重新打开通道，应创建新实例。生命周期回调可以请求关闭，但不能同步等待包含自身的操作。

`WorkerBase` 以异步操作协调启停、暂停及恢复，并支持 `IAsyncDisposable`。相同的在途动作共享结果；停止等待当前动作结束后再执行，启动、暂停和恢复遇到其它在途动作直接返回。例如，停止中调用启动不会安排重启，启动中调用暂停不会安排后续暂停。启动失败向上传播，并调用停止扩展点清理部分初始化的资源；停止失败保持 `Stopping`，实际清理完成前拒绝重启。通知异常不回滚已经确定的状态。`IWorker` 仅提供 `StartAsync`、`StopAsync`、`PauseAsync` 和 `ResumeAsync`；`WorkerBase` 与 `IListener<T>` 的公开释放契约为 `IAsyncDisposable`，所有者使用 `await using` 或等待 `DisposeAsync`。`Enabled` 仅控制是否允许启动；启动后修改只影响下一次启动，不会停止当前运行，也不影响暂停、恢复、停止或释放。

状态通知的 `ActionName` 分别为 `Start`、`Stop`、`Pause` 和 `Resume`。`OnStopAsync` 停止运行并释放全部自有资源，允许部分初始化及失败后重试；停止成功后可重新启动。暂停保留资源与内部状态，恢复继续使用这些资源和状态；失败时实现者负责撤销部分操作，基类恢复原来的 `Running` 或 `Paused`。不支持暂停与恢复的实现通过 `CanPauseAndContinue = false` 表达。

`DisposeAsync` 永久禁用实例，并通过 `OnStopAsync` 清理资源，包括尚未启动的实例。并发释放共享当前尝试；清理失败向上传播，后续可再次调用 `DisposeAsync` 重试，成功后重复释放不再清理。释放开始后新的启动、停止、暂停和恢复调用均抛出 `ObjectDisposedException`，之前已经接收的操作继续收尾。`IsDisposed` 表示清理已成功；诊断属性和 `ToString` 仍可读取。通知失败不改变已完成的清理。取消停止等待只结束该调用方的等待。

生命周期扩展点及 `StateChanged` 处理器应完成各自的工作，不得反向等待所属工作器的生命周期或释放操作；调用方负责避免循环等待。需要继续操作所属工作器时，由回调外的所有者在当前操作结束后协调。不同工作器之间可以调用，但不得形成循环依赖。回调中可以修改 `Enabled`，该赋值不触发生命周期操作。工作器组合及后台任务的顺序调用见 `WorkerCallbackTest`。

`start`、`stop`、`pause` 和 `resume` 命令等待对应的异步工作器操作，并传递命令取消标记。`--timeout` 仅限制命令等待：默认五秒，非正数使用五秒，最大三十秒。超时抛出 `TimeoutException`，不取消工作器操作；后续结果仍由工作器负责通知，命令不再输出完成结果。暂停与恢复命令忽略不适用于当前状态的请求。完成扩展点接收命令上下文、工作器及等待期限，恢复扩展点为 `WorkerResumeCommand.OnResumed`。

`ListenerBase<T>` 只派发处理器实参，不解码字节，也不释放注入的共享处理器。未配置处理器时空处理并检查取消。`HandlerBase<T>` 的无附加参数调用传递 null，显式参数保持引用身份。两种 `IPacketizer` 接口仅定义 `Pack` 与 `Unpack`：编码不负责刷新，数据不足时不消费输入，解码成功后推进输入。精确帧长度及解码资源释放属于具体传输实现，由 Net 的打包器基类提供。

## 仓库结构

```text
Zongsoft.Core/
  src/        主类库源码。
  test/       xUnit 单元测试；test/integration 为独立的操作系统输入测试。
  samples/    MemoryCache、Spooler、Superviser 和 EventExchanger 控制台示例。
  benchmark/  针对反射、数据模型辅助类型和 Spooler 批处理的 BenchmarkDotNet 基准测试。
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
