# Zongsoft.Core 验证

[Validate.ps1](Validate.ps1) 验证当前 Core 契约及 .NET 8、9、10 产物。需要安装三个目标框架的 SDK 和运行时，并能够还原项目依赖。

在 `Zongsoft.Core` 目录执行：

```powershell
.\baseline\Validate.ps1
```

验证包含：

1. Release 构建。
2. 三个目标框架的全部 Core 单元测试；挂起超过两分钟时收集诊断并使运行失败。
3. NuGet 包内不同目标框架的公开 API 一致性验证。
4. Spooler 基准在三个目标框架上的 `Dry` 检查，包括内存与文件批处理、有界与无界接收、并发生产、异步回调和指标订阅。

包验证检查本次产物，不要求维持旧版本已删除的公开 API。脚本不发布 NuGet 包。

仅检查构建、单元测试和包契约：

```powershell
.\baseline\Validate.ps1 -SkipBenchmarks
```

结果默认写入系统临时目录中的独立 `zongsoft-core-validation-*` 子目录。也可通过 `-ArtifactPath` 指定绝对或相对目录；脚本结束时输出实际路径，确认结果后可删除这些中间文件。

## 性能测量

`Dry` 只验证基准可执行及其正确性断言，不能用于性能结论。正式测量使用 Release 产物，在 `benchmark` 目录执行，并在测量期间停止其它构建和测试：

```powershell
dotnet bin/Release/net10.0/Zongsoft.Core.Benchmarks.dll --filter '*SpoolerAdmissionBenchmark*' '*SpoolerFlushBenchmark*' '*SpoolerTelemetryBenchmark*' --job Short --launchCount 3 --exporters json --artifacts "$env:TEMP/zongsoft-core-performance"
```

接收基准每次操作投递并清空 1,024 项；完整刷新基准每次操作消费 4,096 项，包含生产者调度与最终显式刷新。指标基准比较相同实现启用和关闭轻量指标订阅的成本，不包含导出器的工作。

释放 Spooler 不会自动排空。基准先等待生产完成，再显式刷新，最后等待异步释放。内存与文件基准在正式计时前检查记录身份、数量和校验和；文件基准还检查完整输出内容。

报告测量环境、每次操作的工作量、平均时间、误差和分配；只在硬件、运行时、负载与正确性要求相同的情况下比较结果。
