---
name: zongsoft-hardwares
description: 修改或审查 Zongsoft.Hardwares 的跨平台硬件采集、系统输出解析、硬件归一化和 HardwareProfile 稳定性时使用；不用于设备控制或驱动安装。
---

# Zongsoft.Hardwares 实现协作

先阅读 [AGENTS.md](AGENTS.md)；使用者场景、配置与示例见 [README](README.zh-Hans.md)。本技能聚焦实现边界，不替代公开 API 文档。

## 契约与平台路由

- 公共模型归属 [Core IO/Hardwares](../Zongsoft.Core/src/IO/Hardwares)：`IHardware`、`IHardwareCollector`、`HardwareProfile`。
- [HardwareCollector](src/HardwareCollector.cs) 为单例采集入口；`HardwareCollector.windows.cs`、`.linux.cs`、`.macos.cs` 封装系统差异，`HardwareCollector.Net.cs` 补充网络设备。
- 设备有 Code、Name、Type、Model、Serie 等字段；唯一值通过 `HasUnique(out string)` 判断，不能凭名称添加假设的 `Identifier` 属性。聚合指纹是 `HardwareProfile.Identifier`。
- 探测是观察系统信息，不是证明硬件可信。不要在本包加入授权或设备认证策略。

## 内部流程与不变量

1. 按运行平台选择采集路径。
2. 调用系统设施、读取系统文件或解析工具输出。
3. 归一化设备及属性，再附加网络设备；不支持的平台仍有网络部分。
4. 调用方按需要生成 Profile 或遍历采集结果。

- `CollectAsync` 在同步采集基础上逐项检查取消并让出调度，不等于每次系统查询均可异步取消。
- 修改序列号清理、空值判定、容量单位与设备排序前，检查 Profile 的输入顺序和哈希使用；格式变化可能造成应用重绑定。
- 同一字段跨 Windows/Linux/macOS 可能缺失或含不同单位，不得用虚构默认序列号填补。
- 系统命令路径、超时、退出码、编码与本地化输出须作为解析输入处理；不能拼接不可信 shell 参数。
- 虚拟机、容器和普通用户权限是独立场景；部分结果不应伪装成完整清单。

## 验证

- 构建 `src/Zongsoft.Hardwares.csproj`；测试目标 [test](test) 与只读 [sample](samples/README.zh-Hans.md) 分开使用。
- 归一化和解析优先用脱敏文本夹具，覆盖空值、异常单位、本地化、重复设备及命令失败。
- 平台验证只在可用 OS 进行，明确未覆盖平台，不伪造采集结果。
- 不在测试快照或日志写入真实机器序列号、MAC 和聚合指纹；样例控制台输出同样敏感。
