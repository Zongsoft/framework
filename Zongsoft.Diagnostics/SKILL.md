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

- Prometheus 的 urls 保存完整地址，允许省略尾斜杠；仅登记 HttpListener 前缀时补齐，不改写绑定或 ScrapeEndpointPath。多个地址的声明路径必须按 Ordinal 一致，protocol 省略时默认为 http1，显式配置只接受单个 http1，空值、协议列表、其它协议及 protocols 设置无效。独立 path 设置、缺少 urls 或空地址项在登记导出器前失败；不拼接另一份 path、不启动默认监听。运行与 binding PI 共读 Settings 中的 urls；PI 用常量模板 http1 声明固定能力，工具不补默认协议。公开使用方式见 README。
- Prometheus 的公开入口、选项投影及临时回环抓取由 [主库测试](test) 验证；不为测试扩大实现作用域。原始 option 的 PI 和模板检查不等同于后续 Packager 提取器验收。

- 服务端选项的 telemetry 端点为 HTTP/2；新增传输需核对 `.option`、`.plugin`、生成项目和 Web gRPC。
- 最小范围为主库或对应 Client/Server 项目，不默认构建所有协议消费者。
- 当前样例在 [server/samples](protocols/server/samples)，只打印指标，不证明持久化、全部信号覆盖或端到端可靠性。
- 优先构造内存 Export 请求和记录型 Handler；覆盖多资源、空批次、精度、Handler 失败和取消。网络冒烟只用明确授权的本地端口。
