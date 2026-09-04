## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。`Zongsoft.Intelligences` 提供基于 Microsoft.Extensions.AI/Microsoft.Agents.AI 的模型、智能体、工具和 Ollama 插件化能力。

## 工作边界

- 核心 AI 抽象和提供商集成位于 `src`，Web 接入位于 `api`；保持提供商专属选项不泄漏到通用契约。
- 流式响应、工具调用、取消和消息角色转换必须保留顺序与终止语义。
- 不提交 API Key、模型服务凭据、真实对话数据；测试优先使用替身或本地可控服务。
- 插件或配置变化同步 `.plugin`、`.option`、`.deploy` 和 Web 项目。

## 验证

- 构建 `Zongsoft.Intelligences.slnx`；Web 变化同时构建 `api`。
- 提供商集成无法离线验证时记录所需服务、模型和配置，不默认产生付费请求。
