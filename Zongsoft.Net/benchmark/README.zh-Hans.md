# Zongsoft.Net 性能测试

[English](README.md) | [简体中文](README.zh-Hans.md)

性能工具面向 .NET 8、9、10，提供分帧微基准、客户端负载、设备模拟和并发发送测试。使用生成数据与本机回环连接，无需真实设备或外部服务。.NET 10 是主要性能测量目标；.NET 8、9 用于正确性和可运行性检查，需要比较运行时差异时再执行完整测量。

## 构建与微基准

安装对应 .NET SDK，在仓库根目录验证编译。Debug 构建需要先生成本地 Core 产物：

```powershell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -c Debug -p:GeneratePackageOnBuild=false
dotnet build Zongsoft.Net/benchmark/Zongsoft.Net.Benchmarks.csproj -c Debug -p:GeneratePackageOnBuild=false
```

正式测量要求 Release 且不附加调试器，BenchmarkDotNet 也以 Release 构建生成项目；运行前必须确认所引用版本的 Core NuGet 包可用。Debug 结果仅用于功能验证。在仓库根目录构建，然后在 `Zongsoft.Net/benchmark` 运行：

```powershell
dotnet build Zongsoft.Net/benchmark/Zongsoft.Net.Benchmarks.csproj -c Release -p:GeneratePackageOnBuild=false
# 以下命令从 benchmark 目录执行
dotnet bin/Release/net10.0/Zongsoft.Net.Benchmarks.dll micro --list flat
dotnet bin/Release/net10.0/Zongsoft.Net.Benchmarks.dll micro --filter "*" --job Dry --runtimes net8.0 net9.0 net10.0 --artifacts artifacts/dry --noOverwrite > artifacts-dry.log 2>&1
dotnet bin/Release/net10.0/Zongsoft.Net.Benchmarks.dll micro --filter "*HeadedPacketizerBenchmarks.UnpackSingle*128*" --launchCount 3 --exporters fulljson --artifacts artifacts/representative --noOverwrite > artifacts-representative.log 2>&1
dotnet bin/Release/net10.0/Zongsoft.Net.Benchmarks.dll micro --filter "*" --job Short --launchCount 3 --exporters fulljson --artifacts artifacts/matrix --noOverwrite > artifacts-matrix.log 2>&1
```

| 基准 | 范围 |
| --- | --- |
| Headed Pack / UnpackSingle / UnpackSegmented | 正文 0、32、128、512、1024、4096、16384、65536 字节；复用写入器，跨段输入拆分四字节包头 |
| Headless Pack / Unpack | 128、65536 字节，无包头的借用与写入 |
| HandlerBenchmarks | 省略参数和显式 `null` 的泛型派发；两者均不创建空 `Parameters` |
| SendBenchmarks | 已连接 Headless 通道的零字节发送，测量发送调用开销；不发送线协议字节，不能代表网络吞吐 |

每次调用测量一个操作，不包含输入准备。MemoryDiagnoser 报告分配，发送基准附带 ThreadingDiagnoser。Dry 只检查可执行性，不能用于性能结论。Short 三次独立启动用于宽矩阵比较，代表路径另用默认自适应预热／测量。不要同时运行构建、其他负载或基准。JSON 保留原始测量，日志含配置与环境；`--noOverwrite` 防止覆盖。

## 分进程负载

两端只使用本机 loopback，所有数据均为测试生成。先启动服务端，等 `Ready` 后启动对应客户端；两端参数一致。`--port 0` 由服务端选择空闲端口，将 `Ready.Port` 传给客户端。

| 组合 | 测量对象 |
| --- | --- |
| `server` + `client` | 原生 Socket 回显端与 Net 客户端，闭环测量，每连接一个在途请求 |
| `service` + `device` | 真实 Net 服务端，原生 Socket 设备模拟器，有界合批、固定计划到达及高峰／低峰阶段 |
| `service` + `contention` | 真实 Net 服务端和客户端，同连接多发送者与多连接并行，每发送者一个在途请求 |

```powershell
# 分别在两个终端执行；这是相同到达率的合批测量示例
dotnet bin/Release/net10.0/Zongsoft.Net.Benchmarks.dll service --port 7969 --connections 16 --messages 16384 --size 128 --warmup 100 --batch 64 --rate 50000 --mixed 1 --diagnostics 1 --timeout 120
dotnet bin/Release/net10.0/Zongsoft.Net.Benchmarks.dll device --port 7969 --connections 16 --messages 16384 --size 128 --warmup 100 --batch 64 --rate 50000 --mixed 1 --diagnostics 1 --timeout 120
```

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `--connections` / `--messages` | 16 / 1000 | 主连接数／每连接每轮请求数，最多 100,000 真实连接和 10,000,000 延迟样本 |
| `--size` / `--warmup` | 128 / 100 | 正文字节／每连接预热；device 支持 0—65536，client 最小 8，contention 最小 12 |
| `--timeout` | 60 | 整体期限秒数，包含建连、预热和所有阶段，最大 3600 |
| `--batch` / `--rate` | 1 / 0 | device 每批最多 512 帧；全局计划消息率，0 表示闭环 |
| `--rounds` / `--idle` | 1 / 0 | device 高频轮数／轮间低峰秒数 |
| `--heartbeat` | 60 | 低峰心跳间隔，支持 1—300 秒并加入 0—5% 正向随机抖动 |
| `--settle` | 0 | device 全部连接及预热完成后、第一轮开始前的空闲秒数，用于内存取样 |
| `--accumulate-ms` | 0 | device 每批额外设备积累时间，包含在端到端延迟中 |
| `--mixed` | 0 | device 增加两个独立探针连接，分别每 100ms 和 1s 请求一次 |
| `--senders` | 1 | contention 每连接发送者数，最多 64 |
| `--broadcast` | 0 | service 每秒广播全 255 正文；仅与 contention 配合，客户端独立校验并计数 |
| `--disturbance` | 0 | contention 增加不读、慢读、慢分片和持续重连四类真实 Socket 干扰 |
| `--zero-length-reads` | 1 | service 的 ZeroLengthReads 开关 |
| `--frame-budget` / `--byte-budget` | 64 / 65536 | service 连续同步处理的公平调度预算 |
| `--diagnostics` | 0 | service 细粒度诊断；contention 客户端始终统计发送等待 |

device 每连接至多一批在途数据；所有输出缓冲总量限制为 512 MiB。每条响应逐字节校验（含包头）；空正文仍有四字节 Headed 帧。固定计划时间由全局序号和 `--rate` 推算，前批阻塞不会重设后续计划时间，积累与排队均计入延迟。该模型是有界的固定计划驱动：不能在过载时无限独立产生请求，也不代表无限开放到达模型。正文小于 8 字节时依赖单连接有序逐字节匹配，不携带完整编号。

contention 校验连接、发送者、序号和正文；拒绝单独计数，不自动重放。其延迟包括拒绝尝试，应结合成功／拒绝数一起解释。干扰连接独立计数；持续重连可能在旧连接退场前触发接入容量拒绝，服务端按实际结果记录。广播与回包共用帧长度，全 255 内容保留给本程序的广播标记。失败或超时退出码非零，不能作为成功吞吐样本。

## 测量矩阵

以同一配置预热后至少测量三轮，优先观察尾延迟、并发容量与空闲内存，再比较吞吐。

| 维度 | 测量范围 |
| --- | --- |
| 正文长度 | 0、32、128、512、1024、4096、16384、65536 字节，遵循各负载模式的最小长度 |
| 设备合批 | 1、8、64、512 帧，在相同计划消息率下比较，积累时间计入端到端延迟 |
| 连接容量 | 以真实连接逐级尝试 1,000、10,000、50,000、100,000；报告实际成功数和限制来源，不视为容量承诺 |
| 长连接阶段 | 高频／低峰／高频重复运行，低峰按需使用 60—300 秒心跳和抖动，记录空闲及恢复时的资源占用 |
| 竞争与干扰 | 同连接多发送者、多连接并行、慢读、不读、碎片输入、重连与广播组合 |

比较版本或配置时保持硬件、SDK、负载与正确性要求一致，同时报告成功、失败、拒绝、延迟、CPU、分配及内存。连接规模受到本机端口或负载端资源限制时，应报告实际上限；跨机器或多源地址测量需要相应的负载工具，不能由回环结果推算。

## 指标与边界

JSONL 输出配置、运行时、程序集版本与 MVID。device 每消息记录计划到达至响应、实际写出至响应、积累及排队三组最近秩分位数 P50/P95/P99/P99.9/最大值；另报低峰心跳和并行探针延迟。`rate=0` 从设备准备本批前开始计时。client 报告请求往返时间。

service 每秒记录在线连接、累计帧／字节、活动连接读取次数、预算耗尽、单次读取最大帧数、发送等待和待发字节。活动诊断随连接移除消失，不是关闭后累计值。进程快照包括 CPU、工作集、私有内存、句柄、托管堆、总分配、GC 次数／暂停、Monitor 竞争和线程池队列。它们包含驱动、JSON 和采样开销，不能解释为库内单次分配。强制 GC 在自然结束快照之后单独标记。

启用 `--diagnostics 1` 后，service 从 `Statistics` 获取共享连接监测的扫描次数、最大耗时、超期次数和超时检测延迟，写入 JSON 字段 `Maintenance`；超期次数增加时输出 `MaintenanceCapacityExceeded` 及完整快照，提示检查容量或调度负载。该统计在连接移除后仍保留，属于同一时钟／周期组；未发生超时时最大延迟为零，不能据此声称检测延迟上限为零。

容量必须用真实 Socket 逐级测量，并说明源端口、内存、句柄和负载端 CPU 限制。此机 loopback 结果不能推算生产网络 SLO。记录完整命令、代码哈希、硬件与至少三轮原始数据；单连接内存通过稳定在线快照减去启动快照后除以连接增量估算。自然回收、池保留和强制 GC 分别解释。

正确性与慢设备、取消、广播、分片边界见 [测试源码](../test)。三目标完整测试命令为 `dotnet test ../test/Zongsoft.Net.Tests.csproj -c Debug -f net8.0 -p:GeneratePackageOnBuild=false`，分别替换 net9.0、net10.0。性能结果不能替代生命周期和协议断言。
