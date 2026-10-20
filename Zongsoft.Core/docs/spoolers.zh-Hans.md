# 批处理缓冲器

`Zongsoft.Caching.Spooler<T>` 将高频、零散的输入汇集为批量回调，用于日志、采集事件和批量存储等场景。它支持组合委托和继承重写，不负责去重、持久化确认或失败重试，也不调用日志系统。

## 构造与触发

```csharp
await using var spooler = new Spooler<string>(
	(items, cancellation) => StoreAsync(items, cancellation),
	TimeSpan.FromSeconds(1),
	limit: 1000);

await spooler.PutAsync("sample", cancellation);
await spooler.FlushAsync(cancellation);
```

处理委托为 `Func<IEnumerable<T>, CancellationToken, ValueTask>`。派生类使用受保护的 `(TimeSpan period, int limit = 0, TimeProvider clock = null)` 构造函数，并重写 `OnFlushAsync`。

`Limit` 大于零时同时限制排队数量和每个枚举器的取出数量；零或负数按无数量限制处理。缓冲区填满后的下一次写入触发刷新：例如限制为 3，A/B/C 填满，写入 D 时刷新，再尝试接收 D。数量触发由写入操作执行回调，没有专属消费任务。

两个构造函数均可通过 `clock` 注入 `TimeProvider`，默认使用 `TimeProvider.System`；时钟只控制周期触发，回调耗时使用单调时钟测量。

构造时启动周期刷新。`Period` 可调整，由 `Common.Timer` 将毫秒值限制在 1 到 4,294,967,294 之间。它表示定期刷新机会，不是每条数据的最长等待时间；慢回调会延迟后续处理。`FlushAsync` 可以主动触发刷新。

## 投递与消费

`PutAsync` 成功表示数据已接收，不表示这条数据已经处理。已取消的令牌阻止接收；入队后取消不会撤回数据。满载时，该操作等待刷新权、调用处理回调，并重新尝试投递。如果回调没有消费数据，则异步等待容量，避免空转。调用方必须等待返回的操作，才能观察容量等待、取消和异常。

回调获得消费性序列：每次枚举从队列取走数据，重复枚举继续取数，不会重放已经取出的数据。不枚举则不取走；部分枚举留下的项目仍可被后续刷新处理。并发到达的数据可能被当前枚举取到。需要固定内容或多次读取时，由回调自行物化序列。

同一缓冲器的处理回调串行执行。公开的 `IEnumerable<T>` 同样从队列取走数据，`Clear` 丢弃队列中的数据；这两个入口不等待处理回调。`Count` 和 `IsEmpty` 仅描述尚未取出的数据，不包含在途处理，也不表示数据库提交状态。

## 刷新与取消

`FlushAsync` 先等待正在执行的刷新，然后对剩余数据执行一轮回调；即使队列已被前一回调取空，也会等待前一回调退出。它不固定调用时的接收范围，不循环强制排空，也不替回调确认处理结果。

显式刷新和满载投递将自己的取消令牌传给其执行的回调。取消一个等待者不会取消另一调用方正在执行的回调。周期刷新使用计时器运行令牌。

处理回调及故障通知中不得等待同一缓冲器的 `FlushAsync`、`DisposeAsync` 或满载的 `PutAsync`，以免形成循环等待；调用方负责遵守这一约束。有剩余容量时仍可投递。所有者应在回调和通知调用链外等待释放。

## 失败与释放

显式刷新或满载投递执行的回调异常，由该操作直接传播，后续操作仍可使用缓冲器。已经取出的数据不回队；调用方自行决定补偿或重试。

周期刷新失败时，该次定时运行结束，触发 `Failed` 事件；缓冲器仍接受数据，也可显式刷新或由满载投递触发刷新。事件使用 `FailureEventArgs`，`State` 为空；修改异常或处理标志不会恢复定时运行。`Failed` 使用普通事件调用：订阅者抛错会中断后续订阅者；内部计时器保留运行及通知异常，由异步释放传播，不写日志。订阅者负责安排恢复或业务侧诊断，不应假定通知会补发历史故障。

`Spooler<T>` 仅实现 `IAsyncDisposable`，所有者通过 `await using` 或等待 `DisposeAsync` 释放。释放开始时停止接收、结束容量等待并请求停止计时器。等待刷新权的操作在取得刷新权后检查关闭状态，可能需要等当前回调返回；不强制终止显式刷新中的业务 I/O。释放后的投递返回带 `ObjectDisposedException` 的任务。

`DisposeAsync` 停止接收，等待已经进入的刷新、计时器清理和故障通知结束，同样不排空缓冲区。重复及并发调用等待相同的关闭结果。周期运行和故障通知异常由异步释放传播；显式刷新和满载投递的回调异常仍由原操作传播，释放只等待其退出。

派生类可重写 `DisposeAsyncCore`，等待基类实现后释放回调依赖的资源；基类等待失败时仍需通过 `finally` 清理这些资源。派生类应保证自身清理可重复调用。

需要处理剩余数据时，所有者应先停止并等待生产者，再等待显式刷新，最后等待异步释放。回调若只部分消费，所有者需要自行决定是否继续刷新；回调不消费时不能用无限排空循环等待。回调使用的外部资源应在异步释放结束后释放。

回归测试见 [SpoolerTest](../test/Caching/SpoolerTest.cs)。

## 文件日志的所有权

`FileLogger<TLog, TModel>` 仅实现 `IAsyncDisposable`，并通过 `Failed` 转发周期写入故障。`DisposeAsync` 停止接收并等待在途文件写入和故障通知，再释放内部同步资源。直接写入模式下，关闭前已经进入并等待同源文件写入权的请求也会完成。已经关闭后，非空日志和刷新请求被拒绝。

文件日志器不在释放时自动写出剩余日志。需要保留缓冲内容时，所有者先停止生产，等待 `((ILogger)logger).FlushAsync(cancellation)`，再等待 `logger.DisposeAsync()`。写入回调和故障通知不得释放自身；所有者应在回调外等待释放。故障通知异常不跳过后续订阅者；修改事件参数不改变原始失败结果。

## 指标

订阅名称为 `Zongsoft.Caching.Spooler` 的指标源。指标通过 `System.Diagnostics.Metrics` 发布；`Zongsoft.Diagnostics` 的 Meters 过滤器可以选择此名称，再由配置的导出器输出。Web 宿主可使用 `Zongsoft.Diagnostics.Web` 的指标导出功能。Core 不配置导出器或写日志。

| 指标（前缀 `zongsoft.caching.spooler.`） | 类型 / 单位 | 含义 |
| --- | --- | --- |
| `items.accepted` | Counter / `{item}` | 订阅期间成功接收的数量，不表示持久化成功 |
| `items.queued` | ObservableGauge / `{item}` | 所有未关闭缓冲器尚未取出的数据总量；晚订阅仍可采集当前值 |
| `flush.duration` | Histogram / `s` | 业务回调耗时，不包含等待刷新权的时间 |
| `put.wait.duration` | Histogram / `s` | 满载投递慢路径的总耗时，包含其触发的回调和容量等待 |
| `failures` | Counter / `{failure}` | 业务回调错误数量；持有已取消且匹配操作令牌的取消异常不计为错误 |

`flush.duration` 和 `failures` 带 `zongsoft.spooler.trigger` 标签：`explicit`、`capacity`、`periodic`；耗时另带 `zongsoft.spooler.outcome`：`success`、`canceled`、`error`。队列值是并发采样，不表示跨缓冲器的事务快照。没有数据内容、实例名称或其它高基数标签。没有监听器时不采集操作时间；监听器抛出的测量异常不改变业务操作的结果。

批处理不自动为每条数据创建跟踪跨度，也不推断多条输入的父跟踪关系。应用可在处理回调中按业务上下文建立跟踪。

指标测试见 [SpoolerDiagnosticsTest](../test/Caching/SpoolerDiagnosticsTest.cs)，文件日志生命周期测试见 [FileLoggerLifecycleTest](../test/Diagnostics/FileLoggerLifecycleTest.cs)，性能工作负载见 [benchmark](../benchmark/README.zh-Hans.md)。
