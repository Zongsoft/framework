## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。`Zongsoft.Data` 包含通用数据引擎、映射元数据和数据库驱动；详细领域规则见 [SKILL.md](SKILL.md)。

## 工作边界

- `src` 保持数据库无关，驱动方言和提供程序行为放在 `drivers/{driver}`。
- 数据库操作、过滤器、验证器、绑定器及导入扩展点使用可等待的异步契约；纯内存解析保持同步。取消令牌位于末尾，常用参数组合提供位置令牌重载。具体结果结构、枚举及资源所有权遵循 [SKILL.md](SKILL.md)。
- `.mapping` 以 `Zongsoft.Data.xsd` 和当前加载器行为为依据，不照搬历史非法属性。
- 通用接口只承载多个驱动真正共享的语义；优先使用 Binder、Builder、Slotter、Visitor、Importer 等扩展点。
- 修改 Schema、表达式、语句或连接生命周期时检查 Core 中相应抽象和所有受影响驱动。
- 修改会话、命令、读取器或连接租约时，按 [SKILL.md](SKILL.md) 的资源所有权规则验证异步释放、活跃租约与终结等待顺序；ADO.NET 强制同步成员的行为须显式实现。
- 插件入口变化同步检查 `Zongsoft.Data.plugin`、`.deploy` 和驱动产物。

## 验证

- 通用引擎构建 `Zongsoft.Data.slnx` 并运行 `test/Zongsoft.Data.Tests.csproj` 的相关测试。
- 驱动测试遵循 [drivers/AGENTS.md](drivers/AGENTS.md)，先确认数据库或容器依赖。
