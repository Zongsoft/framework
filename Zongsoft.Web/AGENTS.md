## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。`Zongsoft.Web` 提供 ASP.NET Core 的控制器、绑定、过滤、格式化、路由、安全、HTTP 和 SignalR 通用能力。

## 工作边界

- `src` 保持通用 Web 能力；`openapi` 和 `grpc` 是独立扩展，不把可选依赖引入主库。
- 修改服务控制器、模型绑定、格式化或路由时，保持既有 HTTP 状态、序列化形状、分页和异常映射兼容。
- 安全行为与 `Zongsoft.Security` 分工：Web 负责管线接入，身份、凭据和权限模型留在 Security。
- 新增插件组件时同步检查对应 `.plugin`、`.option`、`.deploy`；详细流程见 [SKILL.md](SKILL.md)。

## 验证

- 构建 `Zongsoft.Web.slnx` 并运行 `test/Zongsoft.Web.Tests.csproj` 的相关测试。
- OpenAPI 或 gRPC 改动优先构建对应项目，并验证生成文档或协议端点的最小场景。
