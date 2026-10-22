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

文本格式为 `algorithm#factor:nonce|value`，因子为零时可以省略 `#factor`。密码摘要必须采用上述迭代规则生成。

`Password` 实现 `ISpanParsable<Password>` 和 `IParsable<Password>`，提供字符串与字符跨度的公开 `Parse`、`TryParse` 重载。文本格式不受区域设置影响，接口的格式提供者参数被忽略。空引用或空输入表示空密码；无效文本使 `TryParse` 返回 `false` 并输出空密码，`Parse` 则抛出 `FormatException`。

## 服务注册

使用 `ServiceCollectionExtension.Register` 注册程序集中的服务。标注 `[Service]` 的公开类型参与自动注册；需要自定义注册时实现 `IServiceRegistration`。注册失败会向调用方传播。

## 事件调度

通过 `RaiseAsync` 触发事件并等待处理器完成，绑定的事件处理器必须返回可等待结果。`EventExchanger` 支持按事务状态控制事件投递，应用可通过 `IFeatureProvider` 增加条件，或通过 `EventDispatcher.Pipeline` 配置策略。详见[事件调度与环境特性](docs/event-dispatching.zh-Hans.md)。

## 异步数据操作

IDataAccess 和 IDataService 使用异步数据库方法。标量和写入操作返回 ValueTask，查询返回 IAsyncEnumerable<T>；取消令牌位于参数末尾。查询用 await foreach 消费，提前结束也会释放读取资源。自建数据访问器使用 await using，共享访问器由其所有者释放。

InsertManyAsync、UpsertManyAsync、UpdateManyAsync 和访问层 ImportAsync 支持内存集合与 IAsyncEnumerable<T>。事件处理器接受取消令牌并返回 ValueTask；需要数据库或网络访问的处理器应等待操作完成。

单条查询的 Getted 事件提供已经取得的 Result 快照；读取该属性不会再次查询。IPageable.Paginated 在首条结果输出前完成，适合设置分页响应信息。数据服务的模板导出通过 GetModelAsync 等待模型提供程序取数。

状态机使用 `await using`，通过 `await machine.RunAsync(state, description, cancellation)` 发起流转。每次运行完成后再开始下一次或释放状态机；释放会等待状态持久化与事务终结。

`DataDictionary.GetDictionariesAsync<T>(items, cancellation)` 将内存集合转换为数据字典异步序列；异步输入使用 `GetDictionariesAsync<T, TSource>(items, cancellation)`。需要逐条准备数据时，在令牌前传入 `Func<IDataDictionary<T>, CancellationToken, ValueTask>` 回调。枚举时按输入顺序等待回调，跳过空元素，不预先读取整个集合。

## 数据验证与结果处理

实现 `IDataValidator.ValidateAsync` 验证查询条件，通过 `OnImportAsync`、`OnInsertAsync`、`OnUpdateAsync` 提供字段值。字段验证返回 `DataValidatorResult`：`default` 表示不替换原值；`HasValue = true` 表示使用 `Value` 替换，允许明确写入 `null`。替代值依赖当前记录时，可通过 `IDataValueBinder.BindAsync` 在绑定时计算。

`Pageable.FilterAsync` 和 `MapAsync` 为 `IAsyncEnumerable<T>` 提供可等待的筛选和映射回调。枚举时逐条处理，保留分页信息，并将枚举取消令牌传给回调。筛选返回 `Pageable.FilterResult<T>`：`Accepted = true` 表示输出 `Data`，允许替换记录或输出 `null`；`default` 表示丢弃当前记录。

## 事务

`Transaction` 使用 `CommitAsync`、`RollbackAsync` 和 `DisposeAsync`，通过 `await using` 管理范围。未提交的事务在释放时回滚；嵌套事务回滚使根事务只能回滚。提交仅在接受决议前响应取消，已经开始的提交、回滚和释放会等待实际完成。重复终结等待相同结果，失败也会继续向等待者传播。

`RollbackAsync(exception)` 在回滚及完成通知成功时正常返回；失败时抛出包含该业务异常与终结异常的 `AggregateException`。调用方可以在 `catch` 中等待回滚，再根据业务需要选择是否用 `throw;` 继续传播原异常：

```csharp
catch(Exception exception)
{
	await transaction.RollbackAsync(exception);
	throw; // 省略此行表示回滚成功后不再传播业务异常。
}
```

批量更新、角色和用户的批量删除，以及状态机停止会保留业务操作与事务终结同时失败时的两类异常；只有业务操作失败时，继续传播原异常。

该异常参数只在首次发起回滚时采用；提交或回滚已经开始时，后续调用只等待已有结果，不会重新回滚或追加异常。传入 `null` 时只传播终结异常。不带参数的回滚使用 `RollbackAsync()`；带取消令牌的调用应传入明确类型的令牌，`RollbackAsync(default)` 无法区分两个重载。

`TransactionContext.Completed` 接受返回 `ValueTask` 的处理器，并按订阅顺序等待所有处理器。通知失败不会改变已确定的事务状态；提交时连接中断等无法确认结果的情况保持 `Undetermined`，应用不能据此假定写入已回滚或自动重试。处理器不得等待包含自身的事务终结任务。

## 应用生命周期

`ApplicationContext` 接入宿主可等待的启动与停止阶段。`Exit()` 和 `Exit(exitCode)` 只请求宿主停止，不强制结束进程；`DisposeAsync()` 等待工作器实际收尾后释放上下文资源。应用构建器使用 `BuildAsync()`，以便初始化失败时等待异步释放。容器解析的初始化器和借用的服务容器视图保留其原有所有者。

## 终端输入

`ITerminal.Input` 使用 `TextReader`，普通流和内存文本可直接使用 `StreamReader`、`StringReader`。`Terminal.Console.Input` 提供与进程同寿命的共享行读取器：同时只允许一个等待者，取消仅结束等待，读取及结果保留给后续调用。执行器停止不释放输入；注入读取器遵循其自身的取消及资源契约。详见[终端输入](docs/terminal-input.zh-Hans.md)。

`ITerminalExecutor` 通过三个 `RunAsync` 重载运行命令循环，返回结束后的退出码；调用方必须等待该任务。

`ITerminal.Abort()` 同步发送中断通知并返回是否已处理，不停止执行器或进程。进程信号策略由宿主负责，Core 不注册全局控制台信号。

## 日志

同步 `Error(...)`、`Log(...)`、`Flush()` 立即调用日志器，不等待异步操作完成。`Logging` 不缓存日志，也不跟踪在途任务。日志器失败会被观察，但不向同步调用方传播；缓存、完成保证与失败报告由具体日志器负责。

需要等待完成或接收异常时，使用并等待异步入口。`FlushAsync()` 只转发日志器刷新，不等待此前尚未等待的日志调用。释放日志器前应停止日志生产，等待自有异步日志操作，再等待 `FlushAsync()`。日志器处理完成不等同于已持久化。

## 周期任务

`Common.Timer` 通过构造回调串行执行周期任务，支持注入 `TimeProvider`。`Start()` 建立周期调度；启动、停止和释放方法均不接受取消令牌。`Stop()` 请求内部取消，不等待回调和清理结束，`StopAsync()` 等待停止；`Dispose()` 永久关闭但不等待清理，`DisposeAsync()` 永久关闭并等待清理，先同步释放后仍可异步等待。调用方负责避免释放后使用业务功能及回调等待自身。`Failed` 通知运行故障，`Stopped` 通知本次回调及资源清理已结束；异步停止和异步释放等待通知返回并传播运行及通知异常。详见[异步周期计时器](docs/timers.zh-Hans.md)。

[`Spooler<T>`](docs/spoolers.zh-Hans.md) 按周期、满载写入或显式请求串行处理缓冲数据，支持消费性枚举与继承扩展。`PutAsync` 表示接收，`FlushAsync` 等待在途刷新并处理一轮剩余数据；仅支持异步释放，`DisposeAsync` 停止接收并等待在途刷新及清理结束，不排空缓冲区。周期故障通过 `Failed` 通知，并由异步释放传播，缓冲器不写日志。

两个构造函数均支持可选的 `TimeProvider`。`Zongsoft.Caching.Spooler` 指标源提供接收数、队列数量、刷新耗时、满载等待和故障指标，由应用选择导出；`FileLogger` 仅支持异步释放，等待文件写入结束后清理资源。

## 通信生命周期

调用工作器时等待 `StartAsync`、`StopAsync`、`PauseAsync` 和 `ResumeAsync`。`Enabled` 控制是否允许启动，修改它不会停止正在运行的工作器；暂停或恢复前检查 `CanPauseAndContinue`。成功停止的工作器可以重新启动，已释放的工作器不可复用。

自有工作器、监听器、消息队列、轮询器和分布式锁使用 `await using` 或等待 `DisposeAsync`；容器管理的服务由容器释放。通过 `CloseAsync` 等待通道关闭，关闭后的通道不能重新打开。生命周期回调不得等待自身的停止或释放。

通过 `await foreach` 枚举 `IRequestToken.GetResponsesAsync` 的响应，通过 `Message.AcknowledgeAsync` 等待消息确认；处理器完成不自动表示消息已确认。

工作器命令使用 `--timeout` 限制等待时间，默认五秒，非正数使用五秒，上限三十秒。超时抛出 `TimeoutException`，工作器操作继续执行。

Windows 的 `shell` 命令等待进程退出及输出读取完成。超时默认为一秒，非正数使用三十秒；超时返回 `-1`，调用方取消则传播取消，两者都不终止已启动的进程。

## 仓库结构

```text
Zongsoft.Core/
  src/               主类库源码。
  test/unit/         xUnit 单元测试。
  test/integration/  独立的终端输入与进程集成测试。
  samples/           MemoryCache、Spooler、Superviser 和 EventExchanger 控制台示例。
  benchmark/         针对反射、数据模型辅助类型和 Spooler 批处理的 BenchmarkDotNet 基准测试。
```

## 构建与测试

```powershell
dotnet restore Zongsoft.Core.slnx
dotnet build Zongsoft.Core.slnx -c Release
dotnet test test/unit/Zongsoft.Core.Tests.csproj -c Release
```

仓库也提供了 Cake 脚本：

```powershell
dotnet cake build.cake --target=test --edition=Release
```

主解决方案与 Cake 默认目标仅运行 `test/unit`。[集成测试](test/integration/README.zh-Hans.md)需要显式执行：

```powershell
dotnet test test/integration/Zongsoft.Core.Tests.slnx -c Release -p:GeneratePackageOnBuild=false
# 或：dotnet cake build.cake --target=integration-test --edition=Release
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
