---
name: zongsoft-diagnostics
description: 修改或审查 Zongsoft.Diagnostics 的诊断配置、OTLP 客户端/服务端生成边界及 Listener 指标、日志、追踪转换和分派时使用；不用于更新 OpenTelemetry proto 子模块。
---

# Zongsoft.Diagnostics 实现协作

先阅读 [AGENTS.md](AGENTS.md)；使用者场景、配置与示例见 [README](README.zh-Hans.md)。本技能聚焦实现边界，不替代公开 API 文档。

## 契约与源码入口

- 公共诊断模型和 `Diagnostor` 在 [Core Diagnostics](../Zongsoft.Core/src/Diagnostics)；本包 [Configurator](src/Configuration/Configurator.cs) 将指定路径的 `DiagnostorOptions` 转为 Meters/Traces 的过滤和导出配置。不要把选项绑定改动误认为 OTLP 线协议改动。
- [客户端项目](protocols/client/src/Zongsoft.Diagnostics.Protocols.Client.csproj) 从既有 proto 生成消息与 Client 服务；[服务端项目](protocols/server/src/Zongsoft.Diagnostics.Protocols.Server.csproj) 生成 Server 服务并加入手写 Listener。
- `proto` 是独立 Git 子模块：修改前后记录提交指针和内部状态；本技能不授权写入、生成文件或更新指针。生成代码留在构建产物中。
- [Listener](protocols/server/src/Listener.cs) 是日志、指标、追踪 partial 的共同边界；服务标签 `gRPC` 由 [Web gRPC](../Zongsoft.Web/grpc) 接入。

## 转换与分派审查

调用链为生成的 Export 服务 → 对应 `Listener.*` 转换 → `HandleAsync` → 注册的 Handler。
审查不能只看返回的 gRPC 成功状态：

- `HandleAsync` 通过 `Parallel.ForEachAsync` 并行分派，捕获处理异常并记录日志，不将每个 Handler 失败传播为调用失败。因此协议完成不代表业务存储成功。
- 时间戳从 Unix 纳秒换算为毫秒，存在精度损失；修改时测试零值、大值及边界。
- 指标批次包含 Resource、Scope 和 DataPoint 三层，需多资源、多作用域、多点用例。当前 `Listener.Metrics.cs` 在资源循环中重新赋值集合，不能假设完整保留所有资源批次。
- 核对 Gauge、Sum、Histogram 与框架模型的映射，不能由同名推断无损转换；Gauge 当前投射到 Counter 相关模型。
- 日志级别、异常事件、Trace/Span 标识及属性必须逐字段核对；未知类型和缺失属性需要明确降级。
- 保持取消与 Handler 异常语义可区分；变更吞异常行为会改变发送方重试与重复处理边界。

## 配置与验证

- `FilteringExporter.Enabled` 默认 true；Configurator 对指标和追踪均先过滤禁用项，再投影驱动与设置。禁用项不解析驱动或连接设置；保持旧配置省略开关时的启用行为，使用真实 XML 绑定和工作器验证，不扩大 Core 契约。

- 导出器由 `Telemetry.ExporterLauncher` 静态分部类组织：[主文件](src/Telemetry/ExporterLauncher.cs) 集中维护共用的私有 `Configure` 重载及私有嵌套 `Registration`；[Default](src/Telemetry/ExporterLauncher.Default.cs) 与 [Console](src/Telemetry/ExporterLauncher.Console.cs) 分别包含公开静态嵌套类及其 Meters/Tracers/Logs 单例，统一调用主文件中的配置方法。服务注解位于这两个公开嵌套类上，私有注册器由 `IServiceRegistration` 扫描发现；不要为扫描或测试扩大注册器作用域。

- 强类型设置位于 `Configuration/ExporterLauncherSettings.Default.cs` 及 `.Console.cs`；两个独立模型使用 `IConnectionSettingsDriver<TSettings>` 作为基类驱动类型。`ExporterLauncherSettingsDriver.Default`、`.Console` 在两个分部文件中暴露强类型单例，具体驱动为私有嵌套类；通过 `.plugin` 显式注册，不能假定 Core 的 `Instance` 扫描会发现它们。规范驱动名为 `opentelemetry`、`opentelemetry.console`；启动器保留旧别名及 OTLP 空名匹配。
- 字符串配置边界保持不变；全部 SDK 投影与共享校验仍在 `ExporterLauncher.cs`。整个 settings 为空时不覆盖 SDK/环境选项；非空时使用类型化默认值，无效类型不再静默回退。时间转换前检查毫秒范围，校验协议、目标、端点及批量/队列关系；Console 日志/跟踪不读取指标时间。未知参数保留原始字典行为。用独立模型和选项投影测试覆盖，不扩大私有作用域或发送网络请求。

- Microsoft `ILogger` 由 `ExporterLauncher.Registration` 读取 `/Diagnostics/Diagnostor/Logs` 后接入；Provider 归宿主 DI 所有，正常释放时刷新批次。日志级别遵守宿主 `Logging` 规则，开关独立于指标/跟踪；不桥接 Zongsoft 自有日志，不扩充 Core Diagnostor 契约。缺省不启用日志，修改日志配置需重启宿主。通过服务扫描和真实 ILogger 验证结构化字段、Scope、关联标识、过滤与批次释放。
	- Console 与 OTLP 日志驱动均实现 `IExporterLauncher<OpenTelemetryLoggerOptions>`，可以同时启用。Console 日志只配置 `target`，逐条输出；通过程序集扫描和真实 ILogger 验证输出目标、禁用项及多导出器并存，不依赖私有 SDK 字段。

- Prometheus 启动器、Kestrel 服务、绑定与测试位于 [Diagnostics.Web](../Zongsoft.Diagnostics.Web/SKILL.md)，仅供 Web 宿主。主库保留 Console/OTLP、资源身份和唯一的指标/跟踪工作器生命周期；先解析全部导出驱动，再构建提供程序，失败时释放已创建资源。

- 服务端 telemetry 声明 grpc+http2 和 /opentelemetry，三个 Processor 的 Handler 路径保持一致；binding 读取源 Url/Protocols。客户端 GrpcClientHandler 只处理直接 GrpcChannel 的基路径，不用于官方 OTLP 导出器；后者直接配置含前缀的 Endpoint。新增传输需核对 `.option`、`.plugin`、生成项目和 Web gRPC。
- Logs/Traces 当前以 null argument 加 Parameters 中的原始请求分派；不要把三信号收包验收写成已实现日志/追踪模型转换。Metrics 仍需单独处理上述多资源转换问题。
- 最小范围为主库或对应 Client/Server 项目，不默认构建所有协议消费者。
- 协议单元测试位于各自的 `test/unit` 目录；HTTP/2 和 OTLP 收发测试及其宿主位于独立的 `test/integration` 项目。默认解决方案和 Cake `test` 只运行单元测试，集成测试使用与项目同目录的 `protocols/client/test/integration/Zongsoft.Diagnostics.Protocols.Client.Tests.slnx`、`protocols/server/test/integration/Zongsoft.Diagnostics.Protocols.Server.Tests.slnx`，或显式的 `integration-test` 任务。
- 当前样例在 [server/samples](protocols/server/samples)，只打印指标，不证明持久化、全部信号覆盖或端到端可靠性。
- 优先构造内存 Export 请求和记录型 Handler；覆盖多资源、空批次、精度、Handler 失败和取消。网络冒烟只用明确授权的本地端口。
