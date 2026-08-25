---
name: zongsoft-zeromq
description: Create, review, debug, test, or refactor the Zongsoft.Messaging.ZeroMQ NetMQ adapter under messaging/zero, including queue lifecycle, broadcast routing visibility, reliable Control delivery, wire protocol, request/response, event transport, and plugin hosting. Do not use for unrelated ZeroMQ applications.
---

# Zongsoft ZeroMQ Adapter

Use this skill for work in `messaging/zero`. Preserve the contracts established by `Zongsoft.Core/src/Messaging` and `Zongsoft.Core/src/Communication`; do not treat this adapter as an isolated NetMQ wrapper.

## Start with the Current State

1. Read `messaging/zero/REFACTORING.zh-Hans.md` when diagnosing, planning, or refactoring. Revalidate relevant findings against the current source before relying on them.
2. Inspect `git status --short` before editing. The project and deployment files may contain user-owned dependency changes; do not overwrite or revert them.
3. Read only the Core abstractions relevant to the task:
   - Queue work: `MessageQueueBase`, `MessageConsumerBase`, `IMessageQueue`, `Message`, `MessageOptions`, and `MessageReliability`.
   - Request/response work: `IRequester`, `IResponder`, `IRequest`, `IResponse`, and `IRequestToken`.
   - Event work: `IEventChannel`, `EventExchanger`, and `EventContext`.
4. Inspect the corresponding ZeroMQ implementation and tests before proposing changes. Treat tests containing sleeps or repeated sends as integration evidence, not as proof of readiness semantics.

## Architecture

- `ZeroQueueServer` exposes discovery REP, MostOnce XSUB/XPUB data endpoints, and an optional LeastOnce ROUTER control endpoint. Its `ServerAgent` owns one Poller; internal `ZeroBroadcastServer` owns XSUB/XPUB, while `ZeroControlServer` owns online registrations, Broker Pending, competing delivery, retry, ACK, and a bounded `StorageWorker`. Storage completion returns to the Poller through an Actor command.
- `ZeroQueue` is the only public queue facade. Its private nested `ZeroQueue.Transport` Actor owns discovery; internal `ZeroBroadcast` and `ZeroControl` share the same Poller and respectively own XPUB/SUB broadcast state and DEALER reliable-control state. Do not introduce a transport interface unless a second production transport actually requires one.
- `ZeroSubscriber` has one `SubscriberSocket` for one physical topic and a bounded, ordered handler channel. `MessageQueueBase.Subscribers` owns the single logical-topic registry: initializing entries are shared but hidden, and only successfully initialized consumers appear in its public active view.
- `ZeroRequester`/`ZeroResponder` map communication URLs to queue topics. `ZeroQueueEventChannel` maps events to `Events/...` topics.
- The daemon plugin starts `ZeroQueueServer`; the main plugin registers the connection driver, event transport, requester, and responder.

## Non-Negotiable Invariants

- Keep every `NetMQSocket` confined to one owning poller/actor thread for creation-dependent operations, send/receive, and deterministic teardown. Cross-thread callers must submit commands through `NetMQQueue`, a channel, or an actor boundary.
- Never synchronously wait for `IMessageStorage` on the server Poller. Queue bounded storage work and route completion back through `ServerAgent` before changing Control state or touching ROUTER sockets.
- Do not use a fixed delay, `HasOut`, or a successful local send as proof that a remote subscription has propagated. Distinguish transport connected, subscription propagated, message accepted locally, message sent, and handler completed.
- Preserve the distinct contracts: `MostOnce` returns `null` without sending when the application XPUB has no matching subscription at that instant, otherwise completes after one local send; `LeastOnce` returns `null` when the Broker has no online match, otherwise completes after Broker Pending persistence. Neither is `ExactlyOnce`.
- `ProduceAsync` snapshots payload memory before deferred work and generates a unique identifier for every publication. A non-null identifier is not a remote or Handler acknowledgement. ZeroMQ publishers never persist messages.
- Define `ProduceAsync` completion and payload ownership together. If sending remains deferred, snapshot borrowed memory before returning or document and enforce an equivalent lifetime contract.
- Roll back and dispose a newly created subscriber when subscription initialization fails or is cancelled. Keep initialization and active state in the same `MessageQueueBase.Subscribers` registry; never introduce a second topic dictionary or expose a failed/initializing subscriber through the active view.
- Closing or disposing a queue must close its consumers, detach handlers, stop asynchronous work, and release sockets without racing the poller.
- Keep logical topics separate from physical grouped topics. Add `Group` exactly once, and normalize it before event or request/response routing.
- Keep per-subscriber dispatch bounded and ordered. Capacity pressure pauses only that subscriber socket and resumes it through an Actor command when the handler frees space.
- Bound handler concurrency and define ordering/backpressure behavior. Do not create an unbounded `Task.Run` for every received message.
- Parse untrusted frames defensively: validate frame count, header delimiters, option values, compression, payload size, and empty-payload semantics without terminating the poller.
- Do not silently claim support for Core options. Implement or explicitly document tags, delay, expiration, priority, reliability, and fallback behavior.
- Put transport-neutral option normalization, reliability capability validation, and duplicate-subscription consistency in Core `MessageQueueBase`. Keep delivery tags, offset commits, ACK routing, retry windows, persistence, and Socket state in the driver. Before extracting more behavior, compare RabbitMQ, Kafka, MQTT, Redis, and Aliyun implementations and require genuinely identical semantics.
- Preserve explicit acknowledgement through `Message.AcknowledgeAsync`; do not treat a Handler returning successfully as an implicit acknowledgement unless Core changes that contract for every driver.

## Current 2.0 Wire Format

- Discovery is a versioned text request/response carrying `Epoch`, `Control`, `Incoming`, and `Outgoing`. There is no old-field fallback or mixed-client branch.
- Data messages contain two frames:
  1. UTF-8 header: `<effective-topic>@<instance>` followed by `Protocol-Version:2.0`, `Identifier:<id>`, and optional newline-delimited `Key:Value` entries.
  2. Binary payload.
- Compression uses header option `Compressor:Brotli`; producers enable it with `MessageEnqueueOptions.Properties["Compressive"]` containing a byte threshold.
- Every business header contains `Protocol-Version:2.0`. The XPUB welcome frame includes the same Broker Epoch as discovery.
- Heartbeats are anonymous messages with an empty payload. Do not conflate them with valid empty business messages.
- Requests prefix the payload with `<request-identifier>\n`; responses use the same identifier prefix and normally publish to `<url>/reply`.

LeastOnce uses ROUTER/DEALER commands `REGISTER`, `UNREGISTER`, `PING`, `PUBLISH`, `DELIVER`, `ACK`, `ACCEPTED`, `UNROUTABLE`, and `ERROR`. `PUBLISH` and `DELIVER` carry Identifier, physical Topic, producer Identity, Tags, original Timestamp, expiration/attempt fields, and payload. Session and subscription identifiers are runtime routing identities, not durable business identities. Changing a frame or command is a protocol change; no old-frame compatibility branch is required.

## 2.0 Reliability Implementation

- The user explicitly does not require compatibility with protocol 1.0 and guarantees that old and new clients will not be mixed.
- The normative design and implementation status are in `messaging/zero/PROTOCOL-2.0.zh-Hans.md` and `.testagent/status.md`.
- Stage 2A implements `MessageReliability.MostOnce` with immediate application-XPUB subscription detection. It sends only when that exact XPUB currently knows a matching prefix, and has no acknowledgement or retry.
- Subscription propagation is routing visibility only. Do not wait for a future subscription or infer remote receipt from it.
- Discovery and Welcome carry a Broker Epoch. Disconnect or Epoch change invalidates cached subscription readiness; fixed and random runtime ports are rediscovered.
- A MostOnce publish with no matching prefix returns `null` immediately and is never sent later.
- Stage 2B implements `LeastOnce` through addressable runtime sessions, explicit `Message.AcknowledgeAsync`, Broker-only persistence, competing consumers, and retry with the same identifier. Duplicates are part of the contract.
- Broker acceptance requires an online matching subscription, persists Pending before returning `ACCEPTED`, and does not wait for Handler ACK. Each attempt chooses one online consumer; any valid ACK removes Pending. New or returning subscriptions may consume already accepted Pending.
- Core owns the transport-neutral `IMessageStorage` and `MessageStorageBase<TSettings>` contracts. Storage implementations are independent plugins; each Broker uses an independently configured instance and the ZeroMQ driver has no default file store and must not dispose injected Storage. Persist complete outer `Message` metadata, but keep ZeroMQ expiration and retry envelopes private. Do not add publisher storage, target/ACK sets, logical partition arguments, or a terminal partition.
- `ExactlyOnce` is unsupported and must fail before transport state is created.

## Configuration Facts

- Client settings live under `/Messaging/ConnectionSettings` with driver `ZeroMQ`.
- `Server` is required; discovery `Port` defaults to `7969`; `Timeout` and `Heartbeat` default to `10s`.
- `Topic`, `Group`, `Client`, `Instance`, and `Filter` alter routing or identity. The default filter excludes the current instance; `Filter=*` accepts all instances.
- `ReconnectInterval` controls rediscovery. There is no client `Storage`, `ReadinessTimeout`, or `PendingCapacity` setting.
- Server ports live under `/Messaging/ZeroMQ/Servers`. Three values use `Control,Incoming,Outgoing`; two values remain `Incoming,Outgoing` with the configured Control port set to zero, which binds randomly when Storage is present. Omitted ports are random and supported across Broker restart; fixed ports remain operationally preferable.
- Server Storage can change only while stopped. A Broker without Storage advertises `Control:0` and still serves Broadcast. `IMessageStorage.Name` identifies the provider and `Settings` defines the independent instance's connection and data scope.
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
- Put user-facing exceptions and diagnostic text in `src/Properties/Resources.resx` with matching `Resources.zh-Hans.resx` entries; keep protocol tokens, topic names, endpoint schemes, and wire-field names culture-invariant.
- Use CRLF in repository text files and Tab indentation in code and fenced code examples, except Unix scripts that require LF.
- When runtime behavior changes, update the README support matrix, delivery semantics, tests, and the assessment status in the same change.

