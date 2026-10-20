# 终端输入

[English](terminal-input.md) | [简体中文](terminal-input.zh-Hans.md)

## 输入契约

`ITerminal.Input` 使用 `System.IO.TextReader`。终端及交互命令通过 `ReadLineAsync(CancellationToken)` 按行读取：返回值不含行结束符，空字符串表示空行，`null` 表示 EOF。

一个进程使用一个终端，默认标准输入与进程同寿命。应用可以注入 `StringReader`、`StreamReader` 或自定义 `TextReader`；注入实例的读取、取消及释放遵循其自身契约，所有权属于创建者。

`ITerminalExecutor` 提供三个 `RunAsync` 重载，分别使用默认欢迎内容、字符串或 `CommandOutletContent`。调用方等待返回的 `Task<int>` 获取命令循环结束后的退出码，并传入本次运行的取消令牌。

## 标准输入

`Terminal.Console.Input` 默认返回同一个进程级 `TextReader`，设置 `Input = null` 可恢复该默认输入。它支持 `ReadLine()`、`ReadLineAsync()` 和 `ReadLineAsync(CancellationToken)`，三个入口共用同一读取状态。同步入口等待整行返回；异步入口不阻塞调用线程。此读取器仅提供按行输入，`Peek`、逐字符及字符块读取不受支持。

首次读取时启动一个后台线程，按需调用 `Console.In.ReadLine()`；没有调用方请求下一行时不预读下一行。文本解码、行结束符和控制台编辑由 .NET 处理。

同一时刻只允许一个调用方等待输入，并发等待抛出 `InvalidOperationException`。取消只结束本次等待，尚未完成的读取及其结果由读取器持有。后续调用接续同一次读取，不另起读取，也不丢弃已经取得的结果。结果只有在成功返回给调用方时才被领取；输入完成与取消竞争时，允许成功或取消两种结果。

EOF 和读取故障保留在读取器中，后续调用返回 EOF 或传播同一故障，不自动重试。宿主停止不等待进程级读取完成，后台线程随进程退出结束。`Dispose()` 和 `Close()` 不执行任何操作，也不会关闭标准输入。

在首次读取前配置 `Console.InputEncoding`。读取开始后不得更换 `Console.In`，或混用 `Console.In`、`Console.ReadKey` 及其它标准输入读取入口。控制台模式及尚未提交的编辑内容由操作系统和 .NET 管理。

## 自定义输入

普通流直接使用 `StreamReader`，内存文本使用 `StringReader`。以下代码假定 `stream` 是应用持有的流，`terminal` 是终端实例；输入只能在执行器空闲时更换。

```csharp
using var input = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
var previous = terminal.Input;
terminal.Input = input;

try { await terminal.Executor.RunAsync(cancellation); }
finally { terminal.Input = previous; }
```

读取器由创建者释放，执行器不释放注入的输入。`StreamReader` 负责文本解码和缓冲，`leaveOpen` 控制是否保留原流。它的取消语义由 .NET 和底层流决定，不具有共享标准输入的结果保留保证；有及时停止需求时，注入实现必须支持相应的取消操作。

标准输入直接使用 `Terminal.Console.Input`，不另建读取器消费同一标准输入。

## 命令与宿主

默认控制台执行器串行运行。EOF 和运行期间取消使 `RunAsync` 返回 `0`；调用前已取消则抛出 `OperationCanceledException`。输入故障向调用方传播。执行器结束时保留输入，后续运行继续使用它。

仅在执行器空闲时更换 `Terminal.Console.Input`。设置 `null` 恢复进程共享标准输入。注入的流读取器由创建者负责释放，执行器不取得其所有权。

确认、参数提示和交互命令传递运行取消令牌。EOF 不表示同意确认，也不表示有效文件路径；交互聊天遇到 EOF 结束对话。

`ITerminal.Abort()` 同步触发 `Aborting`，返回事件参数最终的 `Cancel` 值；`true` 表示已处理，没有订阅或未处理时返回 `false`。它只发送通知，处理器异常向调用方传播。处理器必须及时返回，不同步等待所属宿主停止。

`Terminal.ReactiveAsync` 等待 `Aborting` 或运行取消。通知正常结束等待，取消向调用方传播。进入失败或取消后仍等待 `exit` 清理，传入原始异常和 `CancellationToken.None`；运行与清理同时失败时抛出 `AggregateException`。

[Plugins 终端宿主](../../Zongsoft.Plugins/README.zh-Hans.md#终端信号与停止)统一处理进程信号：Ctrl+C 优先通知当前终端，未处理时停止；SIGQUIT／Ctrl+Break 和 SIGTERM 直接请求停止。宿主等待命令、工作器和插件清理，标准输入保持进程级所有权。独立使用 Core 时由应用配置自己的信号处理；Core 不注册全局控制台信号或修改输入模式。

## 验证

单元测试位于 `test/Terminals`，验证文本读取器注入、执行器取消、EOF、故障、输入所有权和命令清理。独立的[集成测试](../test/integration/README.zh-Hans.md)通过隔离子进程验证共享标准输入、取消后的结果与故障保留、同步及异步读取和进程退出，不进入主解决方案的默认单元测试。
