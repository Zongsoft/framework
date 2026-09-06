---
name: zongsoft-intelligences
description: 修改或审查 Zongsoft.Intelligences 的 Assistant、聊天会话、历史、工具调用、Ollama 转换或 Web 流式端点时使用；不用于 ML.NET 训练或付费模型评测。
---

# Zongsoft.Intelligences 实现协作

先阅读 [AGENTS.md](AGENTS.md)；使用者场景、配置与示例见 [README](README.zh-Hans.md)。本技能聚焦实现边界，不替代公开 API 文档。

## 所有者与调用链

- [AssistantManager](src/AssistantManager.cs) 协调提供者；[AssistantProviderBase](src/AssistantProviderBase.cs) 与 `IAssistant` 定义适配边界。
- [ChatSessionManager](src/ChatSessionManager.cs) 管理会话缓存、Current 和生命周期事件；[ChatSession](src/ChatSession.cs) 包装 `IChatService`/Microsoft.Extensions.AI 的调用。
- [Ollama](src/Ollama) 负责 HTTP/模型/聊天协议转换；[api](api) 只负责路由、HTTP 输入输出与串流。
- 每次修改先区分框架会话 Identifier、提供者 ConversationId 和模型名称；三者不能互换。

## 会话与串流不变量

- 请求消息先追加历史；没有 ConversationId 时发送前奏与全部历史，有 ConversationId 时发送前奏与当前消息。
- 普通响应把消息逐条加入历史；当前 `GetStreamingResponseAsync` 聚合文本并在枚举完成后追加 Assistant 消息。中途取消/停止枚举不能假定产生完整历史。
- 文件中存在其他 Response 包装器，会在释放时聚合 Contents。审查必须跟踪实际调用路径，不因辅助类存在就宣称当前文本流完整保留工具调用元数据。
- ChatOptions 的 ConversationId 会被写入；共享选项与同一会话并发请求可能互相影响。
- Manager 的 Current 不是 HTTP 请求局部状态。多个用户必须有明确会话归属和授权，不能把 Current 当作每用户隔离。
- Abandon 会调用 session.Dispose，后者释放保存的 service 并清理历史；Manager 创建的会话共享传入 service，修改释放策略要验证兄弟会话是否受影响。
- 会话枚举在 NET9_0_OR_GREATER 走缓存 Keys，在 .NET 8 抛 NotSupportedException；跨框架不可宣称完全一致。

## Web、工具与验证

- Web Chat 路径在找不到指定会话时可回退无状态调用；历史查询与显式会话操作的错误语义需分别验证。
- 工具调用是应用能力执行，不因模型输出可信。授权、参数校验、调用次数与副作用控制留在工具边界。
- 最小目标为 `src/Zongsoft.Intelligences.csproj`，Web 变化再选 `api/Zongsoft.Intelligences.Web.csproj`；当前没有独立测试项目。
- 使用替身 IChatService 覆盖普通响应、增量输出、工具内容、取消、半途枚举、重复释放、会话切换及 .NET 8 枚举。
- 不用真实对话或付费请求作默认测试；真实 Ollama 验证需指定本地端点、模型和资源预算。
