# Zongsoft.Net Networking Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Net)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Net)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**N**et](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Net) is a high-performance networking library in the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides functionality for _**S**ocket_ network development.

It provides asynchronous TCP clients, servers, channels, connection statistics, broadcast, and packet framing. It supports .NET 8, 9, and 10 with only the corresponding `Microsoft.NETCore.App` runtime. Use it for application protocols with long-lived device connections, heartbeats, small and medium messages, and device batching.

## Packets and streams

TCP is a byte stream; one send does not correspond to one receive. `Headed` uses a four-byte **unsigned big-endian body length**. `Headless` returns bounded chunks of currently available bytes and defines no application message boundaries. Both expose borrowed `ReadOnlySequence<byte>` values without copying the whole body. The default body limit is 64 KiB; only Headed encodes an empty body as a complete frame.

Derive from `PacketizerBase<T>` for a custom protocol and follow these extension contracts. A shared packetizer must support concurrent connections and must not store connection-specific state.

| Operation | Contract |
| --- | --- |
| `GetLength` | Return the exact wire length including the header, without modifying the packet |
| `Pack` | Encode one frame into the supplied writer, matching the declared length, without flushing |
| `Unpack` | Return `false` with unchanged input when data is insufficient; consume at least one byte on success; throw for invalid data |
| `Release` | Release each successfully decoded packet once after handling completes or fails; the default is a no-op, so override for owned resources |

Consecutive complete frames are delivered in order; an incomplete tail waits for more bytes. A legal multi-frame batch can exceed the single-frame limit. If a protocol wraps multiple records in one outer frame, that whole frame remains subject to the frame limit; the application must bound its records and decompressed size. When the peer ends input, complete requests and their responses can finish; an incomplete tail causes a protocol error.

Handlers receive `TcpReceiveContext<T>`. Reply to the originating connection with `context.Channel.SendAsync(context.Package, cancellation)`. Connections invoke handlers concurrently; each connection awaits its handler in frame order. Borrowed bytes remain valid until that asynchronous call completes. Copy data before retaining it or handing it to background work.

For borrowed binary packets, import `Zongsoft.Common` and call `context.Package.Lease()` before the handler completes to copy the sequence into a pooled `IMemoryOwner<byte>`. The owner may outlive the handler. When transferring it to background work, that consumer must dispose it after use; if the handoff fails, the handler must dispose it. `Memory<T>.Owned()` only wraps the original memory and does not extend borrowed storage lifetime.

## Creating and sending

```shell
dotnet add package Zongsoft.Net
```

Every access to `TcpClient.Headed/Headless` or `TcpServer.Headed/Headless` creates an independent instance. Use `CreateHeaded(options)`, `CreateHeadless(options)`, or a generic constructor with your own `PacketizerBase<T>` implementation. Options are copied at construction; later changes to the original object do not affect the instance.

IPv4 and IPv6 endpoints are supported. With server port zero, `Address` returns the assigned endpoint after startup; use that endpoint for the client.

```csharp
using System;
using System.Net;
using System.Threading;

using Zongsoft.Net;
using Zongsoft.Common;

using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await using var client = TcpClient.CreateHeaded(new() { MaximumPendingSends = 32 });
client.Address = new IPEndPoint(IPAddress.Loopback, 7969);
await client.ConnectAsync(cancellation: cancellation.Token);

using(var message = "hello".Encode())
	await client.SendAsync(message.Memory, cancellation.Token);

await client.DisconnectAsync(cancellation.Token);
```

This snippet demonstrates local submission and cleanup. To wait for a response, assign an `IHandler<TcpReceiveContext<ReadOnlySequence<byte>>>` and await application confirmation before disconnecting. See the [runnable samples](samples/README.md) for request/reply, broadcast, heartbeat, and batching. The memory convenience overload also invokes the packetizer.

`Buffer.Encode` uses UTF-8 by default and returns pooled memory. Keep its owner alive until the awaited send or broadcast completes, then dispose it even on cancellation or failure. Sending does not transfer packet ownership; do not modify or release the underlying data until the operation completes. Use `Encoding.GetString(ReadOnlySequence<byte>)` to decode segmented text directly.

Concurrent sends on one connection never interleave frames, but call times across callers do not establish a strict send order. Await sends sequentially when ordering matters. Without a handler, received packets are ignored. The provider of an injected handler owns its lifetime.

Concurrent initial sends share a connection attempt. The first caller's cancellation token controls that attempt; later callers can cancel their own waits. Changing the address invalidates old attempts and closes the previous connection. Sending after disconnect can reconnect; failed messages are never replayed automatically.

## Shutdown, limits, and deadlines

- Completing `SendAsync` means local submission and its backpressure wait completed, not remote delivery or application confirmation. Cancellation after writing terminates the connection and leaves remote delivery uncertain.
- `CloseAsync` closes the transport. `WaitAsync(cancellation)` waits for handlers, admitted sends, buffer cleanup, and lifecycle notifications. Waiting does not close the connection; cancellation affects only that waiter, and later waits can still observe the channel's failure. A handler may await `CloseAsync`, but must not await its own `WaitAsync`, `DisposeAsync`, or owning service's shutdown.
- `DisconnectAsync` and `StopAsync` wait for the current handler, sends, and final notifications to finish. A timeout closes the transport and reports failure. Individual channel I/O or handler failures are reported through `Error` and that channel's `WaitAsync`, without being rethrown by client disconnect or server shutdown; shutdown or drain timeouts and server acceptance failures still propagate. A handler ignoring cancellation retains its borrowed memory and channel registration until it actually exits. The server shutdown deadline covers acceptance shutdown, current handlers, send draining, and final notifications together. A failed stop remains `Stopping`; retries continue waiting for unfinished operations, and the server can restart after cleanup.
- Canceling `DisconnectAsync` cancels only that wait; shutdown still follows `ShutdownTimeout`. Disconnect affects connections owned when it starts. Connections established during shutdown are unaffected by the older operation's timeout. An explicit `CloseAsync` ends send draining; pending data is no longer guaranteed to be delivered.
- `BroadcastAsync` bounds admitted broadcasts and per-broadcast send concurrency, returning `TcpBroadcastResult` with `Succeeded/Failed/Rejected` counts. Caller cancellation propagates. Channel enumeration is live and need not include concurrent additions.

| Setting | Default | Purpose |
| --- | --- | --- |
| `MaximumPacketLength` / `MaximumFrameLength` | 65,536 / 65,540 bytes | Body and complete wire-frame limits |
| `MaximumBufferedReceiveBytes` | 128 KiB | Incomplete-frame retention limit; legal multi-frame batches may be larger |
| `SendPauseThreshold` / `SendResumeThreshold` | 64 KiB / 32 KiB | Send-buffer pause/resume thresholds |
| `ReceivePauseThreshold` / `ReceiveResumeThreshold` | 64 KiB / 32 KiB | Receive-buffer pause/resume settings; the effective pause threshold allows legal frames to arrive |
| `ReceiveFrameBudget` / `ReceiveByteBudget` | 64 frames / 64 KiB | Consecutive synchronous processing budget; at least one frame can progress |
| `MaximumPendingSends` / `MaximumPendingSendBytes` | 64 / 256 KiB | Waiting/encoding operations and bytes not yet sent by Socket; overflow raises `TcpException` with `TcpError.SendRejected` |
| `MaximumConnections` / `Backlog` | 10,000 / 1,024 | Server admission limit and listener backlog |
| `BroadcastConcurrency` / `MaximumConcurrentBroadcasts` | 32 / 1 | Per-broadcast concurrency and admitted broadcasts |
| `ConnectTimeout` / `ShutdownTimeout` | 10 s / 10 s | Connection establishment and shutdown deadlines |
| `SendTimeout` / `FrameTimeout` | 30 s / 30 s | Stalled sending and complete-frame reception since the first retained byte |
| `IdleTimeout` / `MaintenanceInterval` | Disabled / 1 s | No actual receive activity and shared scan interval |
| `NoDelay` / `ZeroLengthReads` | Enabled / enabled | Small-message latency and idle receive-buffer strategy |

Time settings are validated when a client or server is constructed. `MaintenanceInterval` must be at least 1 ms. It, `ConnectTimeout`, `ShutdownTimeout`, and server `SendTimeout` must not exceed 4,294,967,294 ms. Timeouts must be positive; client `SendTimeout`, `FrameTimeout`, and `IdleTimeout` do not have this timer limit, and zero still disables the idle timeout.

Buffer thresholds provide flow control, not hard process-memory caps. Heartbeat intervals should be shorter than the idle deadline and leave margin for jitter and monitoring. Dripping bytes do not renew `FrameTimeout`. Device identity, application acknowledgments, command correlation, retry idempotency, and heartbeat formats belong to the application protocol.

Byte counters reflect completed I/O; sent-frame counters reflect local submission. Closed-connection counters accumulate into the owner. Concurrent reads are approximate snapshots. Enable `EnableDiagnostics` to observe channel read counts, exhausted budgets, maximum frames per read, and send waits through channel `Statistics` (`TcpChannelStatistics`).

With diagnostics enabled, client and server `Statistics` return a `TcpMonitorStatistics` snapshot with `ScanCount`, `MaximumScanDuration`, `OverrunCount`, and `MaximumTimeoutDelay`. Connections using the same `TimeProvider` and interval share these cumulative monitoring counters. They accumulate while any connection in that group enables diagnostics and remain readable after all connections leave. Disabled instances return zero values; reading the property does not trigger a scan. Timeout delay is relative to the tracked deadline and excludes closure and handler completion; I/O progress adds up to one actual sampling interval of observation error. Scan duration includes scheduling waits, and overruns can extend sampling beyond the configured interval. A nonzero `OverrunCount` indicates a capacity or scheduling concern.

These TCP APIs do not provide authentication, TLS, UDP, QUIC, or persistent application sessions. Supply authentication or encryption in the application protocol where needed. Samples bind to loopback by default.

## Channel lifecycle notifications

Subscribe on `server.Channels` or `client` to observe their channels. Each `TcpChannelEventArgs<T>` provides an immutable snapshot containing `Channel`, `CloseReason`, and `Error`. Normal closure has a `null` error.

| Event | Timing |
| --- | --- |
| `Closing` | Transport closure begins; cannot be canceled, and the current handler may still be running |
| `Error` | Reports the first channel failure at closure start or final cleanup, at most once per channel |
| `Closed` | Handlers, sends, and buffer cleanup have finished; the channel is unregistered and its counters accumulated |

A failure already known when closure starts is reported in `Closing`, `Error`, `Closed` order. Later handler or cleanup failures are reported before final `Closed`, without changing previously delivered snapshots. The first close reason and first non-null exception are preserved separately: a handler failing after local closure can produce a final snapshot with both `Local` and that exception. Capacity rejection and connection failures before a channel exists are returned by their operations and do not raise channel `Error`.

Events run synchronously. Subscriber exceptions are logged and isolated individually; they do not prevent later subscribers or cleanup and do not recursively raise `Error`. Return promptly and never block waiting for shutdown, disposal, or `WaitAsync` that includes the callback itself. Subscribe before starting or connecting. To wait after completion, use `WaitAsync`, which retains the original completion result.

The channel's own `Closing` and `Closed` events describe transport closure. The client/manager `Closed` above describes complete channel cleanup. Await channel `WaitAsync` or use the owner's `Closed` when all resources must have been returned.

## Error handling

Errors identified by this library use `TcpException` with an `ErrorCode`. It derives from `IOException` and uses localized default messages. Branch on error codes rather than message text.

| `TcpError` | Meaning |
| --- | --- |
| `SendRejected` | Send or broadcast capacity is exhausted; this operation has not been admitted |
| `InvalidFrame` | Frame length, encoded length, or decoder consumption violates the framing contract |
| `IncompleteFrame` | The peer ended input with an incomplete frame remaining |
| `ConnectionClosed` | The connection is closed or the server has stopped |

Broadcast counts only `SendRejected` as `Rejected`; other channel send errors count as `Failed`. Caller cancellation still propagates. Error codes describe an operation failure; observe the owner's `Closed` arguments for the final connection state, and await channel `WaitAsync` for complete cleanup. Capacity rejection and encoding errors before any write do not actively close the connection.

Cancellation, timeout, underlying Socket, and application-handler exceptions are not universally converted to `TcpException`. An acceptance failure reported during server shutdown preserves its original exception in `InnerException`. A custom packetizer's `InvalidDataException` counts as a protocol failure. Use `catch(TcpException exception) when(exception.ErrorCode == TcpError.SendRejected)` to handle capacity rejection separately.

## Web file system

`WebFileSystem` uses the host's `IHttpClientFactory` to access a Web file service. Reading a file returns a download stream that the caller must dispose promptly. The stream need not support seeking or provide its length in advance. Pass cancellation tokens to asynchronous stream operations. Directory queries support Web JSON responses and patterns containing special characters.

A writable stream uploads its content when disposed. `Flush` flushes local writes without submitting the upload. Use `await using` in asynchronous code to await the upload and observe HTTP or network failures. To retry a failed upload, open a new stream and write the content again. Repeated disposal does not upload again.

Opening files supports only `FileMode.Open + FileAccess.Read` for downloads and `FileMode.Create + FileAccess.Write` for creating or overwriting files. Other combinations, including append, exclusive creation, open-or-create, truncation, and combined read/write access, immediately throw `NotSupportedException`. Specify access explicitly. `FileShare` does not provide remote file sharing locks.

## Related Resources

- [Runnable samples](samples/README.md)
- [Benchmarks and load tools](benchmark/README.md)
- [System.IO.Pipelines](https://learn.microsoft.com/dotnet/standard/io/pipelines)
- [TCP specification](https://www.rfc-editor.org/rfc/rfc9293)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../Zongsoft.Plugins/README.md).

The manifest contributes the Web file-system provider; the TCP API examples remain explicitly constructed network objects. Loading this plugin does not automatically start a TCP listener.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Net` | [Zongsoft.Net.plugin](src/Zongsoft.Net.plugin) |
| File copying and dependencies | [Zongsoft.Net.deploy](src/Zongsoft.Net.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft net]
nuget:Zongsoft.Net
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Net.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
