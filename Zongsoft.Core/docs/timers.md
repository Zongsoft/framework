# Asynchronous periodic timer

[English](timers.md) | [简体中文](timers.zh-Hans.md)

`Zongsoft.Common.Timer` waits for ticks with `PeriodicTimer`, runs asynchronous callbacks serially, and owns cancellation, completion, and cleanup for each run. It does not write logs or select a recovery policy.

## Construction and startup

The constructor accepts a period, a required callback, and an optional `TimeProvider`. Construction neither creates the underlying clock nor invokes the callback. The default provider is the system clock. Construction and `Period` assignments clamp the period to 1 through 4,294,967,294 milliseconds. Values outside this range use the nearest boundary, and values within it retain their fractional milliseconds. `Period` returns the clamped value; the clock determines the actual timing resolution.

```csharp
await using var timer = new Zongsoft.Common.Timer(
	TimeSpan.FromSeconds(5),
	async (state, cancellation) => await ProcessAsync(cancellation));

timer.Start();
//Perform other work.
await timer.StopAsync();
```

This is a method-body example; the application supplies `ProcessAsync(CancellationToken)`. Periodic work is composed through the callback. Derived classes can override `OnFailed(FailureEventArgs)` and `OnStopped(EventArgs)` to customize event notifications; call the corresponding base method to notify subscribers.

`Start()` or `Start(state)` starts a run with a new internal cancellation source. The first callback occurs after one period. Starting an active run again does not replace its state or cancellation token. Starting while stopping or finishing a failed run throws `InvalidOperationException`. A fully completed timer can be started again unless disposal has been requested.

`Period` can change while idle or active. A new value is published only after the underlying clock accepts the schedule; a rejected update throws to the property caller and leaves the published value unchanged. Callbacks never overlap. Multiple elapsed ticks coalesce while a callback is busy; the timer does not replay every missed interval.

## Stopping and disposal

| Member | Contract |
| --- | --- |
| `IsRunning` | The run has not completed, including stopping, cleanup, and event notifications. |
| `Stop()` | Synchronously requests cancellation without waiting for callbacks, cancellation notifications, cleanup, or event notifications. Safe to call from the run's callbacks and notifications. |
| `StopAsync()` | Requests cancellation and waits for the active callback to exit, run cleanup, and event notifications. |
| `Dispose()` | Permanently closes the timer and requests cancellation without waiting for the active callback. Resources are cleaned up as the run ends. |
| `DisposeAsync()` | Permanently closes the timer and waits for the active callback to exit, resource cleanup, and event notifications. |
| `IsDisposed` | Permanent closure has been requested. Await `DisposeAsync()` to confirm cleanup has completed. |

Returning from `Stop()` only confirms that a stop was requested; `IsRunning` may still be `true`. Repeated calls are safe, and calls while idle, completed, or disposed have no effect. Await `StopAsync()` to confirm shutdown and observe failures. Do not release resources used by the callback or start another run before completion.

`Start`, `Stop`, `StopAsync`, `Dispose`, and `DisposeAsync` do not accept cancellation tokens. The callback receives the current run's internal token; stopping and disposal request cancellation through that source. Callbacks must honor cancellation or return normally; the timer cannot forcibly terminate them. Completing cancellation notifications does not mean the callback has exited: asynchronous stopping and disposal wait for the entire run. Callers can use `WaitAsync` on the task returned by `StopAsync()` to limit their wait; ending that wait does not reverse the stop request.

Once disposal is requested, callers must not start the timer or access `Period`. `IsRunning` and `IsDisposed` remain readable. Repeated disposal does not repeat cleanup. Calling `DisposeAsync()` after `Dispose()` still waits for the same cleanup. Synchronous disposal can return while the callback is still active; callers must keep its external dependencies alive until the run ends.

An `OperationCanceledException` carrying the run token is normal shutdown only when that token has been canceled. Other exceptions, including cancellation involving an unrelated token, are failures.

Callbacks, cancellation notifications, event notifications, and work on which they depend must not await their own timer's `StopAsync()` or `DisposeAsync()`: doing so waits for their own completion and cannot finish. Use `Stop()` or `Dispose()` to request stopping or permanent closure, then let the owner await completion outside the callback. Avoiding these dependency cycles is the caller's responsibility. The timer does not inspect execution contexts or promise an exception for violations.

## Stop notification

`Stopped` fires once after the callback exits and the clock and cancellation source have been cleaned up. Explicit stopping, failure, and disposal all end a run and raise the event. Stopping an idle timer or repeating a stop request does not raise additional notifications. A failed run raises `Failed` before `Stopped`.

Notifications and their virtual methods execute synchronously outside internal locks as part of the run's cleanup. `StopAsync()` and `DisposeAsync()` wait for notification invocation to finish; `IsRunning` becomes `false` afterward. Keep notifications short, and do not wait for the current run or restart the timer inside them. To continue an asynchronous business workflow, signal a task owned by the caller. Outside the event call chain, that task awaits `StopAsync()` before proceeding.

## Failure notification

A callback, clock, or cleanup failure ends the run. After cleaning up the clock and cancellation source, the timer raises `Failed`. Notifications use `Zongsoft.Diagnostics.FailureEventArgs`: `Exception` initially contains the run failure, `ExceptionHandled` initially equals `false`, and the read-only `State` retains the startup state.

Subscribers can change the notification's `Exception` or `ExceptionHandled`, and subsequent subscribers can read those values. The timer retains the original failure independently; these changes neither alter the result of stopping or disposal nor skip subsequent notifications.

Subscribers to `Failed` are called independently in registration order; a subscriber failure does not skip subsequent subscribers. `Stopped` uses ordinary event invocation: a subscriber failure prevents subsequent subscribers from running. The error remains part of the run result without preventing completion. Multiple failures form an `AggregateException` in occurrence order, with the original run failure first. A single failure retains its original exception object. `StopAsync()` and `DisposeAsync()` expose the failed run's result. Notification failures do not recursively raise `Failed`. Errors from overrides also propagate through stopping and disposal; if `OnFailed` throws, `OnStopped` is still called.

The owner chooses whether to log, alert, or schedule a restart. A restart must wait until the current stop operation has finished.

## Ownership and testing

The timer owns each run's clock and cancellation source; a custom `TimeProvider` must support synchronous disposal of its `ITimer`. It does not dispose the supplied `TimeProvider`, state, or callback owner. It implements `IDisposable` and `IAsyncDisposable` and observes background task exceptions internally. The owner subscribes to events as needed and uses `await using` or awaits `DisposeAsync()` to confirm callback exit and resource cleanup and handle failures. `using` and `Dispose()` only request permanent closure.

Custom `TimeProvider.CreateTimer` implementations must successfully return a valid `ITimer`. On .NET 8, 9, and 10, an exception from this method can cause the `PeriodicTimer` finalizer to terminate the process. This class does not provide recovery from that runtime limitation.

Tests can inject a `TimeProvider` and use completion signals to verify callback order, shutdown waiting, and failure results without relying on elapsed wall-clock time.
