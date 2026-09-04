---
name: zongsoft-web
description: 开发、审查、调试或测试 Zongsoft.Web 及其 OpenAPI、gRPC 扩展。适用于服务控制器、模型绑定、过滤器、格式化、路由、HTTP、安全管线、SignalR、OpenAPI 文档和 gRPC 服务接入；不用于业务站点控制器或插件宿主装配。
---

# Zongsoft.Web 通用 Web 能力

## 定位

1. 阅读 [AGENTS.md](AGENTS.md) 和目标功能附近的测试。
2. 确认行为属于 ASP.NET 通用管线、服务控制器约定、OpenAPI 扩展或 gRPC 扩展。
3. 若问题是插件宿主/控制器发现，转到 [../Zongsoft.Plugins/SKILL.md](../Zongsoft.Plugins/SKILL.md)；若是身份权限模型，检查 `Zongsoft.Security`。

## 契约

- 保持路由、参数来源、模型绑定、分页、HTTP 状态码、错误负载和序列化形状兼容。
- ServiceController 的 CRUD 与子服务行为应复用 Data/Services 契约，不复制领域验证。
- Filter、Formatter 和 Binder 必须正确处理空输入、取消、不可读 Body、重复读取和响应已开始。
- 安全组件只从 ASP.NET 上下文桥接身份与授权，不在 Web 层重新定义安全模型。
- OpenAPI 和 gRPC 为可选包；主库不得引入其依赖。配置或插件变化同步 `.plugin`、`.option`、`.deploy`。

## 验证

构建 `Zongsoft.Web.slnx` 并运行相关测试。HTTP 行为使用最小 TestServer/WebApplicationFactory 或现有夹具验证成功、无效输入、未认证、取消和异常映射；OpenAPI/gRPC 分别构建对应项目。
