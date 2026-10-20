# Batch buffer

`Zongsoft.Caching.Spooler<T>` groups frequent individual inputs into batch callbacks for logging, acquisition events, and bulk storage. It supports delegates and inheritance. It does not deduplicate, acknowledge persistence, retry failed data, or write logs.

## Construction and triggers

```csharp
await using var spooler = new Spooler<string>(
	(items, cancellation) => StoreAsync(items, cancellation),
	TimeSpan.FromSeconds(1),
	limit: 1000);

await spooler.PutAsync("sample", cancellation);
await spooler.FlushAsync(cancellation);
```

The delegate is `Func<IEnumerable<T>, CancellationToken, ValueTask>`. Subclasses use the protected `(TimeSpan period, int limit = 0, TimeProvider clock = null)` constructor and override `OnFlushAsync`.

A positive `Limit` bounds both queued items and items taken by each enumerator. Zero or negative values mean no count limit. The next write after the buffer becomes full triggers a flush: with limit 3, A/B/C fill the buffer; writing D flushes existing data before attempting to admit D. This callback runs as part of the writing operation, without a dedicated consumer task.

Both constructors accept an optional `TimeProvider clock`, defaulting to `TimeProvider.System`. It controls periodic triggers; operation durations use a monotonic clock.

Construction starts periodic flushing. `Period` is mutable; `Common.Timer` clamps its milliseconds to 1 through 4,294,967,294. It provides periodic flush opportunities, not a maximum per-item latency. Slow callbacks delay processing. `FlushAsync` also requests a flush explicitly.

## Admission and consumption

Successful `PutAsync` means admission, not processing completion. A pre-canceled token prevents admission; cancellation after admission does not remove data. When full, the writing operation waits for the flush gate, invokes the callback, and retries admission. If the callback consumes nothing, it waits asynchronously for capacity rather than polling. Callers must await the returned operation to observe waiting, cancellation, and errors.

The callback receives a consuming sequence: enumeration removes queued items. Enumerating again takes more items rather than replaying previous ones. No enumeration means no removal; partial enumeration leaves remaining data for later flushes. Concurrent arrivals may be included. Callbacks that need a stable collection or repeated access must materialize their own collection.

Callbacks on one spooler execute serially. Public `IEnumerable<T>` enumeration also removes queued items; `Clear` discards queued items. Neither operation waits for processing callbacks. `Count` and `IsEmpty` describe queued data, excluding items already taken and still being processed. They do not indicate database commit status.

## Flushing and cancellation

`FlushAsync` waits for an active flush, then invokes one callback for any remaining queued data. It waits for the active callback even when that callback has emptied the queue. It does not capture an admission watermark, force a complete drain, or acknowledge the callback's business effects.

Explicit flushes and full-buffer writes pass their operation token to the callback they execute. Canceling one waiter does not cancel another caller's active callback. Periodic flushing uses the timer run token.

Callbacks and failure notifications must not wait for the same spooler's `FlushAsync`, `DisposeAsync`, or a full-buffer `PutAsync`. Such calls can form a dependency cycle; callers are responsible for avoiding it. Admission with available capacity remains allowed. The owner awaits disposal outside the callback and notification call chain.

## Failure and disposal

Callback errors from explicit flushes or full-buffer writes propagate to that operation. Subsequent operations remain available. Removed data is not requeued; compensation and retries belong to the caller.

A periodic failure ends that timer run and raises `Failed`. The spooler still admits data and supports explicit or capacity-triggered flushes. Notifications use `FailureEventArgs` with null `State`; changing its exception or handled flag does not resume the timer. `Failed` uses ordinary event invocation: a subscriber exception skips later subscribers. The internal timer retains the callback and notification errors for asynchronous disposal to propagate, without writing logs. Subscribers arrange recovery or application diagnostics; past failures are not replayed to later subscribers.

`Spooler<T>` implements only `IAsyncDisposable`; owners use `await using` or await `DisposeAsync`. Disposal stops admission, completes queue-capacity waits, and requests timer cancellation. Operations waiting for the flush gate observe closure after acquiring it; they may wait for the active callback to finish. It does not force cancellation of business I/O in an explicit flush. Subsequent admission returns an operation faulted with `ObjectDisposedException`.

`DisposeAsync` stops admission and waits for active flushes, timer cleanup, and failure notifications without draining queued data. Repeated and concurrent calls await the same shutdown result. Periodic and notification failures propagate from asynchronous disposal. Errors from explicit flushes and full-buffer writes remain with their original operations; disposal only waits for their exit.

Subclasses can override `DisposeAsyncCore`, await its base implementation, and release callback dependencies afterward. Use `finally` to release those dependencies even when the base wait fails. Derived cleanup must tolerate repeated calls.

Owners needing remaining data processed must first stop and await producers, await explicit flushing, and then await asynchronous disposal. Partial consumption requires an owner decision about further flushes; a callback that consumes nothing must not be driven by an infinite drain loop. Release callback dependencies after asynchronous disposal finishes.

See [SpoolerTest](../test/unit/Caching/SpoolerTest.cs) for behavioral coverage.

## File logger ownership

`FileLogger<TLog, TModel>` implements only `IAsyncDisposable` and forwards periodic write failures through `Failed`. `DisposeAsync` stops admission and waits for active file writes and failure notifications before releasing synchronization resources. In direct-write mode, requests already waiting for the same file also finish. Once closed, non-null log entries and flush requests are rejected.

Disposal does not automatically write buffered logs. Owners requiring them written must stop producers, await `((ILogger)logger).FlushAsync(cancellation)`, and then await `logger.DisposeAsync()`. Write callbacks and failure notifications must not dispose their owner; the owner must arrange disposal outside those callbacks. Subscriber errors do not skip later subscribers; editing event arguments does not change the original failure result.

## Metrics

Subscribe to the `Zongsoft.Caching.Spooler` meter. Instruments use `System.Diagnostics.Metrics`. Select this name in the `Zongsoft.Diagnostics` Meters filters and configure the required exporters; Web hosts can use `Zongsoft.Diagnostics.Web` metric exporters. Core does not configure export or write logs.

| Instrument (prefix `zongsoft.caching.spooler.`) | Type / unit | Meaning |
| --- | --- | --- |
| `items.accepted` | Counter / `{item}` | Items admitted during subscription; not persistence acknowledgements |
| `items.queued` | ObservableGauge / `{item}` | Total queued items across open spoolers; late subscribers observe current state |
| `flush.duration` | Histogram / `s` | Business callback duration, excluding flush-gate waiting |
| `put.wait.duration` | Histogram / `s` | Full-buffer admission duration, including its callback and capacity waits |
| `failures` | Counter / `{failure}` | Callback errors, excluding cancellation with the matching canceled operation token |

`flush.duration` and `failures` use `zongsoft.spooler.trigger`: `explicit`, `capacity`, or `periodic`. Flush duration also uses `zongsoft.spooler.outcome`: `success`, `canceled`, or `error`. Queue observations are concurrent samples, not transaction snapshots across buffers. Tags contain no payloads, instance names, or other high-cardinality identifiers. Operation timestamps are skipped without listeners. Measurement listener exceptions do not change business results.

Batching does not automatically create per-item spans or infer parent contexts from multiple inputs. Applications can establish business tracing inside their callbacks.

See [SpoolerDiagnosticsTest](../test/unit/Caching/SpoolerDiagnosticsTest.cs), [FileLoggerLifecycleTest](../test/unit/Diagnostics/FileLoggerLifecycleTest.cs), and the [benchmark workloads](../benchmark/README.md).
