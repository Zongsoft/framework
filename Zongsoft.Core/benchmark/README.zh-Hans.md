# Zongsoft.Core 性能基准

[English](README.md)

此项目使用 BenchmarkDotNet，支持 .NET 8、9、10。以下命令在仓库根目录执行。使用 Release 构建，并通过 `--filter` 显式选择工作负载，避免进入交互式选择界面。正式测量应在无其它重负载的机器上串行执行。

## Spooler 工作负载

| 类型 | 每次操作的工作量 | 参数 | 用例数 |
| --- | --- | --- | --- |
| `SpoolerAdmissionBenchmark` | 接收 1,024 条记录，读取数量，然后清空缓冲 | 容量：无界（`0`）或 `1,024` | 2 |
| `SpoolerFlushBenchmark` | 生产并消费全部 4,096 条记录，包含最终刷新 | 容量：`0` 或 `128`；生产者：`1` 或 `4`；同步或异步回调 | 8 |
| `SpoolerTelemetryBenchmark` | 容量 `1,024` 的接收工作负载，或容量 `128`、单生产者、同步消费的刷新工作负载 | 关闭或启用指标监听器 | 4 |
| `SpoolerMemoryBenchmark` | 通过直接调用、手工批处理或 `Spooler` 消费 10,000 条记录 | 批量大小：`100` 或 `1,000`；生产者：`1` 或 `4` | 12 |
| `SpoolerFileBenchmark` | 使用上述三种方式，将 10,000 条、每条 128 字节的记录写入本地临时文件 | 批量大小：`100` 或 `1,000`；生产者：`1` 或 `4` | 12 |

接收工作负载包含 `Clear`，不能解释为单次入队延迟。刷新工作负载等待全部生产者、生产者触发的满载回调以及最终显式 `FlushAsync` 完成。四生产者场景包含任务调度成本。异步回调通过 `Task.Yield` 验证挂起路径，不模拟磁盘延迟。文件工作负载在三种实现中都包含相同的文件重置操作，并在结束时清理临时目录。

初始化阶段先验证接收或消费的数据。缓冲器周期设为一天，使工作负载聚焦显式刷新和满载触发行为。接收、刷新和遥测夹具在清理阶段等待异步释放。周期调度、故障处理和关闭正确性由单元测试覆盖。

遥测夹具只订阅 `Zongsoft.Caching.Spooler` 指标源；初始化阶段确认开启的监听器确实收到测量值，计时阶段的回调不执行额外工作。它衡量指标采集和回调分发开销，不包含导出器，也不逐项轮询可观察仪表。此夹具只适用于已实现这些指标的版本，不应加入与无遥测版本的双版本比较。

现有 `Data` 和 `Reflection` 目录包含模型构建、属性访问基准。它们使用各自的筛选条件与运行时配置，与下述 Spooler 双版本比较分别运行。

## 运行当前实现

先构建基准项目；它未包含在 `Zongsoft.Core.slnx` 中：

```text
dotnet build Zongsoft.Core/benchmark/Zongsoft.Core.Benchmarks.csproj -c Release -f net8.0 -p:GeneratePackageOnBuild=false
```

通过 Dry 检查 14 个聚焦的 Spooler 用例：

```text
dotnet run --project Zongsoft.Core/benchmark/Zongsoft.Core.Benchmarks.csproj -c Release -f net8.0 --no-build -- --filter "*SpoolerAdmissionBenchmark*" "*SpoolerFlushBenchmark*" "*SpoolerTelemetryBenchmark*" --job Dry --noOverwrite > spooler-dry.log 2>&1
```

单独测量遥测开销：

```text
dotnet run --project Zongsoft.Core/benchmark/Zongsoft.Core.Benchmarks.csproj -c Release -f net8.0 --no-build -- --filter "*SpoolerTelemetryBenchmark*" --job Short --noOverwrite > spooler-telemetry.log 2>&1
```

`Dry` 只验证构建和执行，其时间不作为性能证据。`Short` 用于初步测量；需要更多数据时，去掉 `--job Short`，使用 BenchmarkDotNet 默认的测量策略。普通模式保持 BenchmarkDotNet 原有命令行选项。

## 比较两个已保存的构建

将两个兼容的 Release 版本 `Zongsoft.Core.dll` 分别保存在不同目录，并保留各自需要的依赖；运行期间不要修改这两个目录。两个版本必须支持所选工作负载使用的 API。双版本入口固定使用 .NET 8，需要安装对应 SDK 和运行时。

入口接受 `--compare <基线 DLL> <候选 DLL>`，以及可选的 `--compare-job Dry|Short|Default`，默认 `Default`。路径可以是绝对路径或相对于当前工作目录的路径。入口剥离这些参数，再将剩余参数交给 BenchmarkDotNet。双版本模式拒绝 `--job`/`-j`、`--runtimes`/`-r`、`--inProcess`/`-i`，保持两个任务的运行时一致并分别运行。

以下筛选条件选择接收和刷新工作负载，每个版本 10 个用例，共 20 项结果：

```text
dotnet run --project Zongsoft.Core/benchmark/Zongsoft.Core.Benchmarks.csproj -c Release -f net8.0 --no-build -- --compare "D:/bench/before/Zongsoft.Core.dll" "D:/bench/after/Zongsoft.Core.dll" --compare-job Dry --filter "*SpoolerAdmissionBenchmark*" "*SpoolerFlushBenchmark*" --keepFiles --noOverwrite > spooler-compare-dry.log 2>&1
```

Dry 成功后，用相同输入进行测量：

```text
dotnet run --project Zongsoft.Core/benchmark/Zongsoft.Core.Benchmarks.csproj -c Release -f net8.0 --no-build -- --compare "D:/bench/before/Zongsoft.Core.dll" "D:/bench/after/Zongsoft.Core.dll" --compare-job Short --filter "*SpoolerAdmissionBenchmark*" "*SpoolerFlushBenchmark*" --apples --keepFiles --noOverwrite > spooler-compare.log 2>&1
```

将示例路径替换为本机路径。`--apples` 使每对用例采用相同的调用次数；它需要明确的迭代次数，此处由 `Short` 提供。使用此选项进行更长时间的比较时，应明确指定合适的 `--iterationCount`。

`Baseline` 任务是比较基线，`Candidate` 使用另一份 DLL。两个任务分别通过 MSBuild 参数传入绝对路径 `BenchmarkCorePath`，项目据此将 Core 的 `ProjectReference` 替换为复制到输出目录的程序集 `Reference`；两个任务同时关闭包生成。基准源码不复制被测实现，运行过程中也不替换已加载的 DLL。

BenchmarkDotNet 0.15.8 [按 MSBuild 参数划分构建](https://github.com/dotnet/BenchmarkDotNet/blob/v0.15.8/src/BenchmarkDotNet/Running/BenchmarkPartitioner.cs)，并[为每个分区设置独立输出目录](https://github.com/dotnet/BenchmarkDotNet/blob/v0.15.8/src/BenchmarkDotNet/Toolchains/DotNetCli/DotNetCliCommand.cs)。使用 `--keepFiles` 保留生成物，在采纳测量结果前检查生成的构建命令，并核对输出目录中 Core DLL 与对应输入 DLL 的哈希值。双版本比较不要混入自行声明任务或方法基线的基准类型；聚焦的接收、刷新类型均未声明这些配置。

## 解读结果

查看 `BenchmarkDotNet.Artifacts` 中的 `*-report-github.md` 汇总表；`--noOverwrite` 保留不同运行的结果。将日志、输入程序集哈希值、运行时版本、CPU 信息和全部基准参数一并保存。

`Mean` 和 `Allocated` 表示表格中一次完整工作负载的时间和分配量，不是单条记录的值。记录吞吐率等于本工作负载的记录数除以平均秒数，每条记录分配量等于分配字节数除以记录数。只比较参数相同的工作负载。任务 `Ratio` 比较候选版本与基线版本的耗时；既有内存、文件类型中的 `Direct` 方法基线则用于比较同一工作负载的批处理方式。

同时评估耗时的不确定性与分配量。不要根据 Dry、失败的用例或小于观测波动的差异认定回退或提升。内存诊断反映托管分配，不衡量操作系统文件缓存或导出器内存。
