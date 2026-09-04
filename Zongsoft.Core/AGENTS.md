## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。`Zongsoft.Core` 是框架最底层公共契约和基础实现，不应依赖其它 Zongsoft 功能包。

## 工作边界

- 按 `src` 下的领域目录定位代码：配置、数据抽象、通信、消息、诊断、IO、服务、安全、序列化、调度等。
- 新增公共抽象前搜索现有接口、基类和扩展点，并检查 Data、Plugins、Web、messaging 与 externals 的调用方。
- 保持公共 API、序列化格式、解析规则、取消语义、生命周期和线程安全兼容；破坏性变更必须由任务明确要求。
- Core 只承载跨实现的共同语义；驱动协议、第三方 SDK 和宿主专属行为留在下游项目。
- 修改消息、通信、数据或服务契约时，使用 [SKILL.md](SKILL.md) 的跨项目检查流程。

## 验证

- 构建 `Zongsoft.Core.slnx`，运行 `test/Zongsoft.Core.Tests.csproj` 中的相关测试。
- 性能相关改动才运行 `benchmark`；样例用于人工冒烟，不代替单元测试。
- 公共契约变化后定向构建受影响的直接下游项目。
