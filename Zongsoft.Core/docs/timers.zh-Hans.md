# 异步周期计时器

[English](timers.md) | [简体中文](timers.zh-Hans.md)

`Zongsoft.Common.Timer` 使用 `PeriodicTimer` 等待周期信号，串行执行异步回调，管理每次运行的取消、故障及资源释放。它不记录日志，也不选择故障恢复策略。

## 创建与启动

构造函数接受周期、必需的异步回调和可选的 `TimeProvider`。构造时不创建底层时钟、不执行回调；默认使用系统时钟。构造和设置 `Period` 时将周期限制在 1～4,294,967,294 毫秒之间；超出范围时取最近的边界值，范围内保留小数毫秒。`Period` 返回限幅后的值，实际计时精度由底层时钟决定。

```csharp
await using var timer = new Zongsoft.Common.Timer(
	TimeSpan.FromSeconds(5),
	async (state, cancellation) => await ProcessAsync(cancellation));

timer.Start();
//执行其它工作。
await timer.StopAsync();
```

以上为方法体片段，`ProcessAsync(CancellationToken)` 由应用提供。周期任务通过构造回调组合行为；派生类可重写 `OnFailed(FailureEventArgs)` 和 `OnStopped(EventArgs)` 定制事件通知，调用对应基类方法以通知订阅者。

`Start()` 或 `Start(state)` 启动一次运行，每次运行创建独立的内部取消源。首次回调在一个周期后发生。重复启动正在运行的计时器不替换状态或取消令牌；停止和故障收尾尚未完成时再次启动抛出 `InvalidOperationException`。运行完全结束后可以重新启动，释放后不得再次启动。

`Period` 可以在空闲或运行期间修改。底层时钟接受调度修改后才发布新值；修改失败直接向设置属性的调用方抛错，属性仍保留原值。回调不会重叠；回调耗时超过周期时，多个到期信号合并，不累计执行所有错过的周期。

## 停止与释放

| 成员 | 契约 |
| --- | --- |
| `IsRunning` | 本次运行尚未完成，包括停止、资源清理和事件通知阶段。 |
| `Stop()` | 同步请求本次运行停止，不等待回调、取消通知、资源清理或事件通知完成；可在本次运行的回调和通知中调用。 |
| `StopAsync()` | 请求取消并等待在途回调实际退出、本次运行清理和事件通知结束。 |
| `Dispose()` | 永久关闭并请求取消，不等待在途回调退出；资源在本次运行结束时清理。 |
| `DisposeAsync()` | 永久关闭并等待在途回调退出、资源清理及事件通知结束。 |
| `IsDisposed` | 已请求永久关闭；应等待 `DisposeAsync()` 确认清理完成。 |

`Stop()` 返回仅表示已发出停止请求，`IsRunning` 此时仍可能为 `true`。重复调用安全，未启动、运行已结束或已经释放时不执行任何操作。需要确认停止完成并观察故障，应等待 `StopAsync()`。完成前不得释放回调依赖的资源或重新启动。

`Start`、`Stop`、`StopAsync`、`Dispose` 和 `DisposeAsync` 均不接受取消令牌。回调接收本次运行的内部令牌；停止和释放通过内部取消源请求取消。回调必须自行响应取消或正常返回；计时器不强制终止回调。内部取消通知完成不等于回调已经退出，异步停止和异步释放等待整个运行完成。调用方需要超时或取消等待时，可以对 `StopAsync()` 返回的任务使用 `WaitAsync`；结束等待不会撤销停止请求。

调用释放方法后，调用方不得再启动计时器或访问 `Period`，仍可读取 `IsRunning` 和 `IsDisposed`。重复释放不重复清理；先调用 `Dispose()` 后，仍可等待 `DisposeAsync()` 确认同一次清理结束。同步释放返回不表示在途回调已经退出，调用方必须保持回调依赖的外部资源有效，直至运行结束。

本次运行令牌已取消，且回调抛出的 `OperationCanceledException` 携带该令牌时，计时器将其视为正常停止。其它异常，包括无关取消令牌产生的异常，属于运行故障。

回调、取消通知、事件通知及其依赖工作不得等待所属计时器的 `StopAsync()` 或 `DisposeAsync()`，否则会等待自身，导致无法完成。需要请求停止或永久关闭时，调用 `Stop()` 或 `Dispose()`，由回调外的所有者等待运行结束。调用方负责避免这些循环依赖；计时器不检测调用上下文，也不承诺对违规自等待抛出异常。

## 停止通知

`Stopped` 在本次回调退出、计时资源和取消源清理完成后通知一次。主动停止、故障终止和释放导致的停止均触发通知；未启动或重复停止不会额外通知。发生运行故障时，先通知 `Failed`，再通知 `Stopped`。

事件通知及其虚方法在内部锁外同步调用，属于本次运行的收尾阶段；`StopAsync()` 和 `DisposeAsync()` 等待通知调用结束，随后 `IsRunning` 才变为 `false`。通知应保持简短，不直接等待本次运行结束或重新启动计时器。需要衔接异步业务流程时，通知只向所有者持有的任务发出信号，由该任务在事件调用链外等待 `StopAsync()` 后继续。

## 故障通知

回调、底层时钟或资源清理失败时，本次运行终止。计时资源和取消源清理完成后，计时器触发 `Failed`。通知使用 `Zongsoft.Diagnostics.FailureEventArgs`，`Exception` 初始为运行故障，`ExceptionHandled` 初始为 `false`，只读 `State` 保留启动时传入的状态。

订阅者可以修改通知参数的 `Exception` 或 `ExceptionHandled`，后续订阅者可读取这些值。计时器独立保留原始故障；这些修改不影响停止、释放操作的失败结果，也不跳过后续通知。

`Failed` 的订阅者按注册顺序独立调用，一个订阅者抛错不会跳过其余订阅者。`Stopped` 使用普通事件调用，订阅者抛错会中断后续订阅者；异常仍保留在本次运行结果中，不会阻止运行收尾。原始故障与通知异常按发生顺序组成 `AggregateException`。只有一个异常时保留原始异常对象。`StopAsync()` 和 `DisposeAsync()` 均可观察本次运行的失败；通知异常不再递归触发 `Failed`。重写方法的异常同样由停止和释放操作传播；`OnFailed` 抛错仍会继续调用 `OnStopped`。

需要记录日志、告警或安排重启时，由所有者订阅并决定具体行为；重启必须在本次停止操作完成后进行。

## 所有权与验证

计时器拥有并释放每次运行的底层时钟和取消源；自定义 `TimeProvider` 返回的 `ITimer` 必须支持同步释放。计时器不释放传入的 `TimeProvider`、状态或回调所属对象。计时器实现 `IDisposable` 和 `IAsyncDisposable`，内部负责观察后台任务异常。调用方按需订阅事件；需要确认回调退出、资源清理完成及处理失败结果时，使用 `await using` 或等待 `DisposeAsync()`。`using` 和 `Dispose()` 只发出永久关闭请求。

自定义 `TimeProvider.CreateTimer` 须成功返回有效的 `ITimer`。在 .NET 8、9、10 中，该方法抛出的创建异常可能导致 `PeriodicTimer` 的终结器终止进程；本类不提供针对该运行库限制的恢复保证。

测试可注入 `TimeProvider` 控制时间推进，配合完成信号验证回调顺序、停止等待和故障结果，无需依赖实际睡眠时间。
