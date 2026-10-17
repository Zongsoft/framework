# Zongsoft.Net Benchmarks

[English](README.md) | [简体中文](README.zh-Hans.md)

These tools target .NET 8, 9, and 10 and provide framing microbenchmarks, client workloads, device simulation, and concurrent-send tests. They use generated data and local loopback connections without real devices or external services. .NET 10 is the main performance target; use .NET 8 and 9 for correctness and executability checks, with full measurements when comparing runtime behavior.

## Build and microbenchmarks

Install the corresponding .NET SDKs and verify compilation from the repository root. Debug builds require local Core outputs first:

```powershell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -c Debug -p:GeneratePackageOnBuild=false
dotnet build Zongsoft.Net/benchmark/Zongsoft.Net.Benchmarks.csproj -c Debug -p:GeneratePackageOnBuild=false
```

Performance measurements require Release without a debugger; BenchmarkDotNet also builds generated projects in Release. Ensure the referenced Core NuGet package is available before running them. Debug results are for functional verification only. Build from the repository root, then run from `Zongsoft.Net/benchmark`:

```powershell
dotnet build Zongsoft.Net/benchmark/Zongsoft.Net.Benchmarks.csproj -c Release -p:GeneratePackageOnBuild=false
# Run the following commands from benchmark
dotnet bin/Release/net10.0/Zongsoft.Net.Benchmarks.dll micro --list flat
dotnet bin/Release/net10.0/Zongsoft.Net.Benchmarks.dll micro --filter "*" --job Dry --runtimes net8.0 net9.0 net10.0 --artifacts artifacts/dry --noOverwrite > artifacts-dry.log 2>&1
dotnet bin/Release/net10.0/Zongsoft.Net.Benchmarks.dll micro --filter "*HeadedPacketizerBenchmarks.UnpackSingle*128*" --launchCount 3 --exporters fulljson --artifacts artifacts/representative --noOverwrite > artifacts-representative.log 2>&1
dotnet bin/Release/net10.0/Zongsoft.Net.Benchmarks.dll micro --filter "*" --job Short --launchCount 3 --exporters fulljson --artifacts artifacts/matrix --noOverwrite > artifacts-matrix.log 2>&1
```

| Benchmark | Scope |
| --- | --- |
| Headed Pack / UnpackSingle / UnpackSegmented | Payloads 0, 32, 128, 512, 1024, 4096, 16384 and 65536 bytes; reused writer; segmented input splits the four-byte header |
| Headless Pack / Unpack | 128 and 65536 bytes, borrowed decoding and unframed writing |
| HandlerBenchmarks | Omitted versus explicit null parameters; neither allocates empty Parameters |
| SendBenchmarks | Zero-byte send on a connected Headless channel, measuring send-call overhead; no wire traffic, so this is not network throughput |

Each invocation measures one operation, excluding input preparation. MemoryDiagnoser records allocations; the send benchmark also uses ThreadingDiagnoser. Dry establishes executability only. Three-launch Short jobs cover the broad matrix, with default adaptive measurement for a representative path. Do not run builds or other loads during measurement. Full JSON retains samples; logs retain configuration and environment. Use `--noOverwrite`.

## Separate-process workloads

All peers bind loopback and use generated data. Start the server, wait for Ready, then start its matching client with identical options. Server port 0 selects an available port; pass Ready.Port to the client.

| Pair | Scope |
| --- | --- |
| server + client | Raw Socket echo server and Net client; closed-loop measurement with one outstanding request per connection |
| service + device | Actual Net server and raw Socket device simulator: bounded batches, fixed planned arrivals and high/idle/high phases |
| service + contention | Net server and clients, concurrent senders sharing connections; one outstanding request per sender |

```powershell
# Run in separate terminals: a fixed-rate batching example
dotnet bin/Release/net10.0/Zongsoft.Net.Benchmarks.dll service --port 7969 --connections 16 --messages 16384 --size 128 --warmup 100 --batch 64 --rate 50000 --mixed 1 --diagnostics 1 --timeout 120
dotnet bin/Release/net10.0/Zongsoft.Net.Benchmarks.dll device --port 7969 --connections 16 --messages 16384 --size 128 --warmup 100 --batch 64 --rate 50000 --mixed 1 --diagnostics 1 --timeout 120
```

| Option | Default | Meaning |
| --- | --- | --- |
| --connections / --messages | 16 / 1000 | Main connections / measured requests per connection per round; at most 100,000 real connections and 10,000,000 samples |
| --size / --warmup | 128 / 100 | Payload bytes / warmup requests per connection; device supports 0–65536, client requires at least 8, contention at least 12 |
| --timeout | 60 | Overall seconds including connection, warmup and all phases, at most 3600 |
| --batch / --rate | 1 / 0 | device batch size, up to 512; global planned message rate, zero means closed loop |
| --rounds / --idle | 1 / 0 | device high phases / seconds between phases |
| --heartbeat | 60 | Idle heartbeat interval, 1–300 seconds with 0–5% positive random jitter |
| --settle | 0 | device idle seconds after all connections/warmup and before the first phase, for memory sampling |
| --accumulate-ms | 0 | Additional device accumulation time per batch, included in latency |
| --mixed | 0 | device adds two independent probes at 100ms and 1s intervals |
| --senders | 1 | contention senders per connection, up to 64 |
| --broadcast | 0 | service broadcasts an all-255 payload each second; pair only with contention, which validates and counts these frames separately |
| --disturbance | 0 | contention adds four real Socket peers: nonreading, slow reading, dripping fragments and continuous reconnects |
| --zero-length-reads | 1 | service ZeroLengthReads option |
| --frame-budget / --byte-budget | 64 / 65536 | service fairness budgets for consecutive synchronous processing |
| --diagnostics | 0 | service fine-grained counters; contention clients always count send waits |

Each device connection has at most one outstanding batch. Combined output buffers are capped at 512 MiB. Every reply, including its header, is checked byte for byte. Empty payloads still have a four-byte Headed frame. Fixed planned timestamps come from global sequence numbers and rate; a blocked batch never resets later timestamps, so reported latency includes accumulation and backlog. This is a bounded fixed-schedule driver, not an unbounded independent arrival generator. Payloads shorter than eight bytes rely on ordered byte matching instead of complete identifiers.

contention verifies connection, sender, sequence, and payload. Rejections are counted and never replayed. Its latency includes rejected attempts, so interpret it with success/rejection totals. Disturbance peers have separate counts. Reconnects can hit admission limits before old peers finish cleanup; server counts retain these outcomes. Broadcast and response frames have the same payload length; all-255 content is reserved for this benchmark protocol. Failure or timeout returns nonzero and is not a successful throughput sample.

## Measurement matrix

Warm up and measure at least three runs with the same configuration. Prioritize tail latency, connection capacity, and idle memory before comparing throughput.

| Dimension | Coverage |
| --- | --- |
| Payload size | 0, 32, 128, 512, 1024, 4096, 16384, and 65536 bytes, subject to each workload's minimum size |
| Device batches | 1, 8, 64, and 512 frames at the same planned message rate, including accumulation in end-to-end latency |
| Capacity | Try 1,000, 10,000, 50,000, and 100,000 real connections progressively; report actual successes and limiting resources, not a capacity guarantee |
| Long-lived phases | Repeat high/idle/high phases; use 60–300-second idle heartbeats with jitter as needed and record idle and recovery resource use |
| Contention and disturbance | Shared-connection senders, parallel connections, slow/nonreading peers, fragments, reconnects, and broadcasts |

Keep hardware, SDK, workload, and correctness requirements identical when comparing versions or settings. Report successes, failures, rejections, latency, CPU, allocations, and memory together. Report the actual limit when local ports or load-generator resources constrain capacity. Multi-host or multiple-source-address measurements require suitable load tools and cannot be inferred from loopback results.

## Metrics and limits

JSONL retains options, runtime, assembly versions and MVIDs. device records per-message planned-arrival-to-response, actual-write-to-response and accumulation/queue time, with nearest-rank P50/P95/P99/P99.9/max. Idle heartbeats and concurrent probes have separate distributions. With rate zero, timing starts before preparing the batch. client reports request round-trip times.

service samples once per second: active connections, cumulative frames/bytes, active reads, budget exhaustion, maximum frames per read, send waits and pending bytes. Active counters disappear when connections leave; they are not final cumulative counters. Process snapshots include CPU, working set, private bytes, handles, managed heap, total allocations, GC counts/pause, Monitor contention and thread-pool queues. These include driver, serialization and sampling costs, not library-only per-message allocations. Forced GC is recorded separately after the natural completion snapshot.

With `--diagnostics 1`, service reads shared connection-monitoring scan counts, maximum duration, overruns and timeout detection delay from `Statistics` into the JSON `Maintenance` field. A new overrun emits `MaintenanceCapacityExceeded` with the full snapshot. These counters survive connection removal and represent the shared clock/interval group; a zero maximum timeout delay does not establish an upper bound if no timeout occurred.

Capacity tests must use real sockets and identify source-port, memory, handle and client CPU limits. Loopback results do not predict production network SLOs. Preserve complete arguments, source hashes, hardware and at least three raw runs. Estimate incremental connection cost from stable connected snapshots minus startup; distinguish natural recovery, pool retention and forced GC.

See the [test sources](../test) for protocol, slow-peer, cancellation, broadcast and lifecycle coverage. Run `dotnet test ../test/Zongsoft.Net.Tests.csproj -c Debug -f net8.0 -p:GeneratePackageOnBuild=false` and repeat for net9.0/net10.0. Performance measurements do not replace correctness assertions.
