# Zongsoft.Net samples

[English](README.md) | [简体中文](README.zh-Hans.md)

Both sample projects target .NET 10; the Net library targets .NET 8, 9, and 10. Install the .NET 10 SDK and build local Core in Debug first. The default endpoint is `127.0.0.1:7969`. No database, broker, or real device is required.

| Project | Purpose |
| --- | --- |
| [server](server/Program.cs) | Independent TCP service replying to each originating channel; broadcast is a separate command |
| [client](client/Program.cs) | Interactive sending, scripted request/reply, heartbeat, and bounded device batching |
| [Shared protocol](shared/SampleProtocol.cs) | Optional LF text packetizer demonstrating exact length and decode consumption rules |

## Interactive use

Build Core once from the repository root with `dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -c Debug -p:GeneratePackageOnBuild=false`, then run these commands in separate terminals:

```shell
dotnet run --project Zongsoft.Net/samples/server/Zongsoft.Net.Samples.Server.csproj -f net10.0 -- 127.0.0.1 7969
dotnet run --project Zongsoft.Net/samples/client/Zongsoft.Net.Samples.Client.csproj -f net10.0 -- 127.0.0.1 7969
```

Client commands: `connect`, `disconnect`, `info`, `send <message>`. Sending connects lazily when needed. Server commands: `start`, `stop`, `info`, `broadcast <message>`. Ordinary requests receive `ACK: <message>` only on their originating connection; `PING` receives `PONG`. Broadcast reports successful, failed, and capacity-rejected sends.

Start two clients to check routing: a request from client one must not deliver its ACK to client two. A server broadcast reaches both.

## Scripted demonstrations

Run the following in two processes. Server `--count N` drains and stops after N handled requests, with a 60-second overall wait limit. Scripted clients validate responses and exit. The server prints `READY` and `STOPPED`; clients finish with `DISCONNECTED` or `BATCH completed=17`. Port zero is supported on the server; use the actual port printed in `READY`.

```shell
dotnet run --project Zongsoft.Net/samples/server/Zongsoft.Net.Samples.Server.csproj -f net10.0 -- 127.0.0.1 7969 --count 1
dotnet run --project Zongsoft.Net/samples/client/Zongsoft.Net.Samples.Client.csproj -f net10.0 -- 127.0.0.1 7969 --smoke
```

For custom framing, add `--custom` to both commands. The shared `LinePacketizer` derives from Net's `PacketizerBase<ReadOnlySequence<byte>>`, implements `GetLength`, `Pack`, and `Unpack`, and inherits the no-op `Release` for borrowed sequences. The body must not contain LF and may contain at most 64 KiB; its trailing LF is part of the wire format. This protocol cannot interoperate with Headed.

For heartbeat, use server `--count 3` and client `--heartbeat`. The client sends `PING`, awaits `PONG`, and repeats three times at one-second intervals. Device deployments may use 60–300-second intervals with jitter and a larger idle deadline. Receiving bytes is not application health confirmation.

## Device batches and partial frames

Use server `--count 17` and client `--batch`. This mode uses Headed only. The simulated device uses a raw Socket to control its writes: it accumulates at most 17 small frames, deliberately waits 20 ms, then writes 16 complete frames and the first two header bytes of the next frame. After checking 16 ordered replies, it writes the remaining bytes and validates the last reply.

The library invokes handlers per frame, retains incomplete tails, and continues buffered complete frames after yielding at its processing budget. Accumulation is bounded on the device side; the batch is not wrapped into one oversized message. Deliberate accumulation adds latency. TCP may merge or split any write, so the example makes no promise about preserving batch boundaries.

## Encoding and buffer reuse

Text sends, replies, and broadcasts use Core's `Buffer.Encode` extension to obtain pooled UTF-8 leases. Each `using` scope awaits the send or complete broadcast before returning the lease. Device batching returns each message lease after `Pack` copies its contents into the batch writer. Receiving keeps borrowed sequences and decodes segmented input directly into strings. To retain raw bytes beyond the callback, use `context.Package.Lease()` with explicit ownership transfer and disposal; see the [library contract](../README.md).

## Optional diagnostic output

Add `--diagnostics` to either program to subscribe explicitly to `TcpDiagnostics.Error`. Shared [SampleDiagnostics](shared/SampleDiagnostics.cs) only attempts to enqueue in the callback, using a capacity of 64. A separate consumer prints the operation, exception type, and captured TraceId; a full queue drops and counts incoming events. Disposal unsubscribes and drains the queue. The example does not print payloads, addresses, or exception text. Channel handling faults remain available through channel events and `WaitAsync`; the application decides whether to turn them into logs.

Hosts can subscribe to the `Zongsoft.Net` metric and trace sources through Diagnostics OTLP/Console or Diagnostics.Web Prometheus exporters. See [telemetry and logging policy](../README.md#telemetry-and-logging-policy). These samples do not create exporters.

## Cancellation and cleanup

Both programs use independent instances, `await using`, and cancellation tokens. Interactive mode requires a terminal; scripted modes support redirected output. Ctrl+C requests cancellation and cleanup. Normal exit drains sends; scripted timeouts and incorrect responses produce failure exits. ACK is sample application text, not durable confirmation.

`CloseAsync` closes the transport; `WaitAsync` waits for all handling and resource cleanup. Handlers must not wait for completion that includes themselves. Borrowed sequences are valid only until the current asynchronous handler operation completes. See the [library contract](../README.md). Samples have no authentication or encryption; retain loopback binding unless the application supplies the appropriate protocol protection.
