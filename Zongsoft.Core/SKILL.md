---
name: zongsoft-core
description: 设计、修改、审查或测试 Zongsoft.Core 的公共契约与基础实现。适用于 Data、Messaging、Communication、Services、Configuration、IO、Security、Serialization 等跨项目 API，以及评估公共变更的兼容性和下游影响；不用于只属于某个驱动或第三方适配器的行为。
---

# Zongsoft.Core 公共契约

## 先确定契约所有者

1. 阅读 [AGENTS.md](AGENTS.md) 和目标领域现有接口、基类、扩展方法与测试。
2. 搜索 Data、Plugins、Web、messaging、externals 中的所有实现和消费者。
3. 只有多个生产实现具有相同语义时才修改 Core；单一实现需求留在下游。

## 兼容性检查

- 公共类型、成员、默认值、异常类型、取消和释放语义都属于兼容契约。
- 序列化、文本解析、配置键、URL/Topic、时间和数值格式变化需要兼容旧输入或明确迁移策略。
- 异步 API 不同步阻塞，不吞掉取消或后台异常；集合和注册表明确线程安全与快照语义。
- 可选能力通过 Feature、Provider、Factory 或现有扩展点表达，不用宽泛接口成员迫使所有实现伪支持。
- 热路径优化先确认分配、锁竞争和复杂度，再用聚焦测试或基准证明。

## 领域路由

- 消息契约变化同时使用 [../messaging/SKILL.md](../messaging/SKILL.md) 检查全部驱动。
- 数据 Schema 或映射变化使用 [../Zongsoft.Data/SKILL.md](../Zongsoft.Data/SKILL.md)。
- 插件装配变化使用 [../Zongsoft.Plugins/SKILL.md](../Zongsoft.Plugins/SKILL.md)。

## 验证

构建 `Zongsoft.Core.slnx` 并运行对应测试。公共行为变化至少选择一个真实下游实现做定向构建；多目标差异分别验证 net8.0、net9.0、net10.0。
