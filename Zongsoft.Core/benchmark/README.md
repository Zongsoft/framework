# Zongsoft.Core benchmarks

[简体中文](README.zh-Hans.md)

This project uses BenchmarkDotNet and supports .NET 8, 9, and 10. Run commands below from the repository root. Use Release builds and select a workload explicitly with `--filter` to avoid the interactive benchmark picker. Run measurements sequentially on an otherwise idle machine.

## Spooler workloads

| Class | Workload per operation | Parameters | Cases |
| --- | --- | --- | --- |
| `SpoolerAdmissionBenchmark` | Accept 1,024 records, read the count, then clear the buffer | Capacity: unbounded (`0`) or `1,024` | 2 |
| `SpoolerFlushBenchmark` | Produce and consume all 4,096 records, including the final flush | Capacity: `0` or `128`; producers: `1` or `4`; synchronous or asynchronous callback | 8 |
| `SpoolerTelemetryBenchmark` | The admission workload at capacity `1,024`, or the flush workload at capacity `128` with one producer and synchronous consumption | Metrics listener disabled or enabled | 4 |
| `SpoolerMemoryBenchmark` | Consume 10,000 records using direct calls, manual batching, or `Spooler` | Batch size: `100` or `1,000`; producers: `1` or `4` | 12 |
| `SpoolerFileBenchmark` | Write 10,000 records of 128 bytes to a temporary local file using the same three approaches | Batch size: `100` or `1,000`; producers: `1` or `4` | 12 |

Admission includes `Clear`; it does not isolate individual enqueue latency. Flush waits for all producers, their full-buffer callbacks, and the final explicit `FlushAsync`. Four-producer cases include task scheduling. The asynchronous callback uses `Task.Yield` to exercise suspension; it does not simulate disk latency. The file workload includes the same file reset for all three methods and cleans up its temporary directory.

Setup validates admitted or consumed records before timing. Spoolers use a one-day period so these workloads measure explicit and capacity-triggered behavior. Admission, flush, and telemetry fixtures await asynchronous disposal during cleanup. Periodic scheduling, fault handling, and shutdown correctness belong to the unit tests.

The telemetry fixture subscribes only to the `Zongsoft.Caching.Spooler` meter. Setup verifies that the enabled listener receives measurements; timed callbacks do no work. It measures instrumentation and dispatch overhead without an exporter or per-item observable-gauge polling. Use this fixture only with a version that implements these instruments; do not include it in comparisons against a version without telemetry.

The existing `Data` and `Reflection` directories contain model construction and property access benchmarks. Use their own filters and runtime configuration independently of the Spooler comparison below.

## Run the current implementation

Build the benchmark project first; it is not included in `Zongsoft.Core.slnx`:

```text
dotnet build Zongsoft.Core/benchmark/Zongsoft.Core.Benchmarks.csproj -c Release -f net8.0 -p:GeneratePackageOnBuild=false
```

Check the 14 focused Spooler cases with a dry run:

```text
dotnet run --project Zongsoft.Core/benchmark/Zongsoft.Core.Benchmarks.csproj -c Release -f net8.0 --no-build -- --filter "*SpoolerAdmissionBenchmark*" "*SpoolerFlushBenchmark*" "*SpoolerTelemetryBenchmark*" --job Dry --noOverwrite > spooler-dry.log 2>&1
```

Measure telemetry overhead separately:

```text
dotnet run --project Zongsoft.Core/benchmark/Zongsoft.Core.Benchmarks.csproj -c Release -f net8.0 --no-build -- --filter "*SpoolerTelemetryBenchmark*" --job Short --noOverwrite > spooler-telemetry.log 2>&1
```

`Dry` checks building and execution; its timings are not performance evidence. `Short` is an initial measurement. Omit `--job Short` for BenchmarkDotNet's default measurement strategy when a decision requires more data. Ordinary mode preserves BenchmarkDotNet's command-line options.

## Compare two saved builds

Save two compatible Release builds of `Zongsoft.Core.dll` in different directories, including any dependencies required by those builds. Keep both directories unchanged throughout the run. Both versions must support the APIs used by the selected workloads. The comparison entry point targets .NET 8 and requires the .NET 8 SDK/runtime.

The entry point accepts `--compare <baseline DLL> <candidate DLL>` and an optional `--compare-job Dry|Short|Default` (default: `Default`). Paths may be absolute or relative to the current working directory. These options are removed before the remaining arguments are passed to BenchmarkDotNet. Comparison mode rejects `--job`/`-j`, `--runtimes`/`-r`, and `--inProcess`/`-i` so that both jobs retain the same runtime and isolated execution.

Use the admission and flush filters, which produce 10 cases for each build and 20 results:

```text
dotnet run --project Zongsoft.Core/benchmark/Zongsoft.Core.Benchmarks.csproj -c Release -f net8.0 --no-build -- --compare "D:/bench/before/Zongsoft.Core.dll" "D:/bench/after/Zongsoft.Core.dll" --compare-job Dry --filter "*SpoolerAdmissionBenchmark*" "*SpoolerFlushBenchmark*" --keepFiles --noOverwrite > spooler-compare-dry.log 2>&1
```

After the dry run succeeds, run measurements with the same inputs:

```text
dotnet run --project Zongsoft.Core/benchmark/Zongsoft.Core.Benchmarks.csproj -c Release -f net8.0 --no-build -- --compare "D:/bench/before/Zongsoft.Core.dll" "D:/bench/after/Zongsoft.Core.dll" --compare-job Short --filter "*SpoolerAdmissionBenchmark*" "*SpoolerFlushBenchmark*" --apples --keepFiles --noOverwrite > spooler-compare.log 2>&1
```

Adjust the example paths for your machine. `--apples` uses a shared invocation count for each pair of cases; it requires an explicit iteration count, supplied here by `Short`. For a longer comparison with this option, specify an appropriate `--iterationCount`.

The `Baseline` job is the job baseline; `Candidate` uses the other DLL. Each passes its own absolute `BenchmarkCorePath` to MSBuild. The project conditionally replaces its Core `ProjectReference` with a copy-local assembly `Reference`. Both jobs also disable package generation. No implementation is copied into the benchmark source, and no DLL is replaced in a running process.

BenchmarkDotNet 0.15.8 [groups builds by their MSBuild arguments](https://github.com/dotnet/BenchmarkDotNet/blob/v0.15.8/src/BenchmarkDotNet/Running/BenchmarkPartitioner.cs) and [sets a separate output directory for each partition](https://github.com/dotnet/BenchmarkDotNet/blob/v0.15.8/src/BenchmarkDotNet/Toolchains/DotNetCli/DotNetCliCommand.cs). With `--keepFiles`, inspect the generated build commands and compare the copied Core DLL hashes with the saved inputs before accepting the measurements. Avoid combining this comparison with benchmark classes that declare their own jobs or method baselines; the focused admission and flush classes declare neither.

## Read results

Read the `*-report-github.md` tables under `BenchmarkDotNet.Artifacts`; `--noOverwrite` preserves separate runs. Keep the logs, input assembly hashes, runtime version, CPU information, and all benchmark parameters alongside the results.

`Mean` and `Allocated` are for one complete workload in the table, not one record. Compute records per second as the workload's record count divided by mean seconds, and bytes per record as allocated bytes divided by that count. Compare only matching workload parameters. The job `Ratio` compares candidate time with baseline time; the `Direct` baseline in the existing memory/file classes instead compares batching approaches within that workload.

Assess timing uncertainty and allocations together. Do not infer a regression or improvement from a dry run, a failed case, or a difference smaller than the observed measurement variability. Memory diagnostics describe managed allocations; they do not measure operating-system file cache usage or exporter memory.
