## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。`Zongsoft.Reporting` 定义报表、参数、导出和相关插件基础能力。

## 工作边界

- 保持本项目为报表抽象与通用实现，不引入具体报表产品或存储提供商逻辑。
- 修改公开模型或导出契约时检查序列化、流所有权、格式标识和下游实现兼容性。
- 插件入口变化同步 `Zongsoft.Reporting.plugin` 和 `.deploy`。

## 验证

- 构建 `Zongsoft.Reporting.slnx`。
- 格式或流处理变化至少验证空报表、多参数、取消、异常和流释放语义。
