---
name: zongsoft-zeromq
description: Create, review, debug, test, or refactor the Zongsoft.Messaging.ZeroMQ NetMQ adapter under messaging/zero, including queue lifecycle, PUB/SUB readiness, wire protocol, request/response, event transport, and plugin hosting. Do not use for unrelated ZeroMQ applications.
---

# Zongsoft ZeroMQ Adapter

Use this skill for work in `messaging/zero`. Preserve the contracts established by `Zongsoft.Core/src/Messaging` and `Zongsoft.Core/src/Communication`; do not treat this adapter as an isolated NetMQ wrapper.

## Start with the Current State

1. Read `messaging/zero/REFACTORING.zh-Hans.md` when diagnosing, planning, or refactoring. Revalidate relevant findings against the current source before relying on them.
2. Inspect `git status --short` before editing. The project and deployment files may contain user-owned dependency changes; do not overwrite or revert them.
3. Read only the Core abstractions relevant to the task:
   - Queue work: `MessageQueueBase`, `MessageConsumerBase`, `IMessageQueue`, and `MessageOptions`.
   - Request/response work: `IRequester`, `IResponder`, `IRequest`, `IResponse`, and `IRequestToken`.
   - Event work: `IEventChannel`, `EventExchanger`, and `EventContext`.
4. Inspect the corresponding ZeroMQ implementation and tests before proposing changes. Treat tests containing sleeps or repeated sends as integration evidence, not as proof of readiness semantics.

## Architecture

- `ZeroQueueServer` exposes a discovery REP endpoint, binds an XSUB endpoint for application publishers and an XPUB endpoint for application subscribers, and joins the data endpoints with a NetMQ `Proxy`.
- `ZeroQueue` discovers both data ports, owns the application `PublisherSocket`, translates logical topics, queues outgoing packets, filters producer instances, and manages subscribers.
- `ZeroSubscriber` owns one `SubscriberSocket` for one effective topic. `MessageQueueBase` caches one consumer per effective topic.
- `ZeroRequester`/`ZeroResponder` map communication URLs to queue topics. `ZeroQueueEventChannel` maps events to `Events/...` topics.
- The daemon plugin starts `ZeroQueueServer`; the main plugin registers the connection driver, event transport, requester, and responder.

## Non-Negotiable Invariants

- Keep every `NetMQSocket` confined to one owning poller/actor thread for creation-dependent operations, send/receive, and deterministic teardown. Cross-thread callers must submit commands through `NetMQQueue`, a channel, or an actor boundary.
- Do not use a fixed delay, `HasOut`, or a successful local send as proof that a remote subscription has propagated. Distinguish transport connected, subscription propagated, message accepted locally, message sent, and handler completed.
- Preserve the current transient, best-effort PUB/SUB contract unless the user explicitly approves a new reliability protocol. PUB/SUB has no persistence, acknowledgement, retry, deduplication, or replay.
- Define `ProduceAsync` completion and payload ownership together. If sending remains deferred, snapshot borrowed memory before returning or document and enforce an equivalent lifetime contract.
- Roll back and dispose a newly created subscriber when subscription initialization fails or is cancelled. Never leave a failed subscriber in `MessageQueueBase.Subscribers`.
- Closing or disposing a queue must close its consumers, detach handlers, stop asynchronous work, and release sockets without racing the poller.
- Keep logical topics separate from physical grouped topics. Add `Group` exactly once, and normalize it before event or request/response routing.
- Bound handler concurrency and define ordering/backpressure behavior. Do not create an unbounded `Task.Run` for every received message.
- Parse untrusted frames defensively: validate frame count, header delimiters, option values, compression, payload size, and empty-payload semantics without terminating the poller.
- Do not silently claim support for Core options. Implement or explicitly document tags, delay, expiration, priority, reliability, and fallback behavior.

## Current Wire Format

- Discovery: the client sends an empty request to `Server:Port`; the response contains `Publisher=<port>;Subscriber=<port>`.
- Data messages contain two frames:
  1. UTF-8 header: `<effective-topic>@<instance>` followed by optional newline-delimited `Key:Value` entries.
  2. Binary payload.
- Compression uses header option `Compressor:Brotli`; producers enable it with `MessageEnqueueOptions.Properties["Compressive"]` containing a byte threshold.
- The XPUB welcome frame is `\0Zongsoft.Messaging.ZeroMQ\nProtocol-Version:1.0\0` and synchronizes the subscriber-side connection only.
- Heartbeats are anonymous messages with an empty payload. Do not conflate them with valid empty business messages.
- Requests prefix the payload with `<request-identifier>\n`; responses use the same identifier prefix and normally publish to `<url>/reply`.

Changing any frame or topic rule is a protocol change. Specify compatibility, mixed-version behavior, and rollout before editing it.

## Configuration Facts

- Client settings live under `/Messaging/ConnectionSettings` with driver `ZeroMQ`.
- `Server` is required; discovery `Port` defaults to `7969`; `Timeout` and `Heartbeat` default to `10s`.
- `Topic`, `Group`, `Client`, `Instance`, and `Filter` alter routing or identity. The default filter excludes the current instance; `Filter=*` accepts all instances.
- Server data ports live under `/Messaging/ZeroMQ/Servers`. Omitted data ports are random; fixed ports are required for transparent reconnect after a broker restart with the current client lifecycle.
- Server TCP endpoints bind all interfaces and the adapter does not configure authentication or encryption.

## Validation

Build Core before this adapter for each target framework being changed:

```powershell
dotnet build Zongsoft.Core\src\Zongsoft.Core.csproj -f net8.0
dotnet build Zongsoft.Core\src\Zongsoft.Core.csproj -f net9.0
dotnet build Zongsoft.Core\src\Zongsoft.Core.csproj -f net10.0
dotnet build messaging\zero\src\Zongsoft.Messaging.ZeroMQ.csproj -f net8.0
dotnet build messaging\zero\src\Zongsoft.Messaging.ZeroMQ.csproj -f net9.0
dotnet build messaging\zero\src\Zongsoft.Messaging.ZeroMQ.csproj -f net10.0
```

Integration tests are opt-in and use real sockets and pollers:

```powershell
$env:ZONGSOFT_MESSAGING_TESTS = 'true'
dotnet test messaging\zero\test\Zongsoft.Messaging.ZeroMQ.Tests.csproj -f net10.0 --no-restore --blame-hang-timeout 2m
```

- Run target frameworks sequentially because NetMQ tests share process-global state and network resources.
- For first-publication regressions, run the whole suite first, then repeat `ZeroQueueConcurrencyTests.ConcurrentProduceOnSingleQueueIsThreadSafe` in isolation. A passing isolated repetition does not disprove an order-sensitive race.
- Add focused tests for empty payloads, failed/cancelled subscription rollback, grouped event/RPC topics, queue disposal with active consumers, malformed frames, and broker restart behavior when those areas change.
- Use the standalone samples for smoke tests. Use `D:\Zongsoft\hosting` terminal or daemon hosts only when the local plugin has been deliberately deployed to an isolated host; do not run deploy/install scripts or alter host packages without explicit authorization.

## Documentation and Style

- Keep `README.md` and `README.zh-Hans.md` user-oriented and synchronized. Put implementation analysis in `REFACTORING.zh-Hans.md`.
- Use CRLF in repository text files and Tab indentation in code and fenced code examples, except Unix scripts that require LF.
- When runtime behavior changes, update the README support matrix, delivery semantics, tests, and the assessment status in the same change.

