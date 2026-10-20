# Terminal input

[English](terminal-input.md) | [简体中文](terminal-input.zh-Hans.md)

## Input contract

`ITerminal.Input` uses `System.IO.TextReader`. Terminals and interactive commands call `ReadLineAsync(CancellationToken)`: the result excludes the line ending, an empty string is a blank line, and `null` is EOF.

A process uses one terminal, and its default standard input lives for the process lifetime. Applications can inject a `StringReader`, `StreamReader` or custom `TextReader`. Each injected reader follows its own reading, cancellation and disposal contract, and remains owned by its creator.

`ITerminalExecutor` provides three `RunAsync` overloads for default splash content, a string, or `CommandOutletContent`. Callers await the returned `Task<int>` for the exit code and pass the cancellation token for that run.

## Standard input

`Terminal.Console.Input` defaults to one process-wide `TextReader`; assign `Input = null` to restore it. It supports `ReadLineAsync()` and `ReadLineAsync(CancellationToken)` through the same reading state, without blocking the calling thread. `ReadLine()` always throws `NotSupportedException`, including while an async read is pending and after EOF or failure; it never consumes a pending result. This reader only provides asynchronous line input; `Peek`, character and character-block reads are unsupported.

Input is read only when requested. .NET handles decoding, line endings and console editing.

Only one caller can wait at a time. Concurrent waits throw `InvalidOperationException`. Cancellation ends only that wait. The reader retains the unfinished read and its result. A subsequent call awaits the same operation without starting another read or discarding its result. A result is consumed only when successfully returned to a caller. Completion racing with cancellation may produce either outcome.

EOF and read failures remain in the reader. Subsequent calls return EOF or propagate the same failure without retrying the source. Host shutdown does not wait for an unfinished standard-input read. `Dispose()` and `Close()` do nothing and never close standard input.

Configure `Console.InputEncoding` before the first read. Once reading starts, do not replace `Console.In` or mix this reader with `Console.In`, `Console.ReadKey` or another standard input entry. The operating system and .NET own console modes and unsubmitted editing state.

## Custom input

Use `StreamReader` for ordinary streams and `StringReader` for in-memory text. The following code assumes that `stream` is application-owned and `terminal` is a terminal instance. Replace input only while the executor is idle.

```csharp
using var input = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
var previous = terminal.Input;
terminal.Input = input;

try { await terminal.Executor.RunAsync(cancellation); }
finally { terminal.Input = previous; }
```

The creator disposes the reader; the executor never disposes injected input. `StreamReader` handles decoding and buffering, while `leaveOpen` controls ownership of the underlying stream. Cancellation follows .NET and the underlying stream; it does not provide the shared standard input's retained-result guarantee. Injected implementations must support cancellation when prompt shutdown is required.

Use `Terminal.Console.Input` for standard input instead of creating another reader for the same source.

## Commands and hosting

The default console executor runs serially. EOF or cancellation during execution returns exit code `0`; a token canceled before invocation throws `OperationCanceledException`. Input failures propagate. Ending an executor run preserves its input for subsequent runs.

Replace `Terminal.Console.Input` only while the executor is idle. Assigning `null` restores the shared standard input. Creators retain ownership of injected stream readers; executors do not dispose them.

Confirmation prompts, parameter prompts and interactive commands propagate the run token. EOF is neither confirmation nor a valid file path; interactive chat ends on EOF.

`ITerminal.Abort()` synchronously raises `Aborting` and returns the final `Cancel` value. `true` means handled; no subscription or an unhandled request returns `false`. It only sends a notification, and handler exceptions propagate. Handlers must return promptly and never synchronously wait for their own host to stop.

`Terminal.ReactiveAsync` waits for `Aborting` or run cancellation. Notifications end the wait normally; cancellation propagates. Entry failures and cancellation still await `exit` cleanup with the original failure and `CancellationToken.None`. Operation and cleanup failures together produce `AggregateException`.

The [Plugins terminal host](../../Zongsoft.Plugins/README.md#terminal-signals-and-shutdown) owns process signals. Ctrl+C first notifies the terminal and stops the host if unhandled; SIGQUIT / Ctrl+Break and SIGTERM directly request shutdown. Hosts await command, worker and plugin cleanup while standard input retains process-wide ownership. Applications using Core directly configure their own signal handling. Core registers no global console signal handlers and changes no input modes.

## Verification

Unit tests in `test/unit/Terminals` cover text-reader injection, executor cancellation, EOF, failures, ownership and command cleanup. Separate [integration tests](../test/integration/README.md) use isolated child processes to verify shared standard input, retained results and failures after cancellation, both asynchronous overloads, rejection of synchronous reads, and process exit. They are excluded from the main solution's default unit tests.
