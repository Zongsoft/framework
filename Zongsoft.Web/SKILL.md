---
name: zongsoft-web
description: 开发、审查、调试或测试 Zongsoft.Web 及其 OpenAPI、gRPC 扩展。适用于服务控制器、模型绑定、过滤器、格式化、路由、HTTP、安全管线、SignalR、OpenAPI 文档和 gRPC 服务接入；不用于业务站点控制器或插件宿主装配。
---

# Zongsoft.Web 通用 Web 能力

## 定位

1. 阅读 [AGENTS.md](AGENTS.md) 和目标功能附近的测试。
2. 确认行为属于 ASP.NET 通用管线、服务控制器约定、OpenAPI 扩展或 gRPC 扩展。
3. 若问题是插件宿主/控制器发现，转到 [../Zongsoft.Plugins/SKILL.md](../Zongsoft.Plugins/SKILL.md)；若是身份权限模型，检查 `Zongsoft.Security`。

## 源码与跨项目同步点

- [ServiceControllerBase](src/ServiceControllerBase.cs) 与 [ServiceController](src/ServiceController.cs) 把 HTTP 操作委托给数据服务；最小派生类使用真实的无参构造/GetService 扩展点，不编造 service 基类构造参数。
- [ControllerActivator](src/ControllerActivator.cs) 与 [ControllerFeatureProvider](src/ControllerFeatureProvider.cs) 负责激活/发现边界；插件程序集由 Plugins.Web 的 WebApplicationContext 加入 ApplicationParts，单独包引用不能证明路由存在。
- [Binders](src/Binders) 解析条件/排序/范围等文本，[Formatters](src/Formatters) 负责 JSON，[Filters](src/Filters) 负责横切处理。HTTP Schema 与排序格式也受 Core/Data 契约约束。
- [Plugins.Web Application](../Zongsoft.Plugins.Web/src/Application.cs) 调用初始化器后依次加入 CORS、本地化、方法覆盖、路由、认证、授权、压缩与静态文件，最后映射 Controller/Hub。不能在可选扩展中无意重复注册整条管线。
- [GrpcInitializer](grpc/GrpcInitializer.cs) 注册 gRPC 并按服务标签映射类型；只继承生成基类而未满足注册/标签约定时，不会自动出现端点。
- [Plugins.Web 默认策略](../Zongsoft.Plugins.Web/src/WebApplicationBuilder.cs) 包含宽松 CORS。安全部署必须校准宿主策略，不把默认认证中间件存在等同于所有接口已经授权保护。

## 契约

- 保持路由、参数来源、模型绑定、分页、HTTP 状态码、错误负载和序列化形状兼容。
- ServiceController 的 CRUD 与子服务行为应复用 Data/Services 契约，不复制领域验证。
- Filter、Formatter 和 Binder 必须正确处理空输入、取消、不可读 Body、重复读取和响应已开始。
- 安全组件只从 ASP.NET 上下文桥接身份与授权，不在 Web 层重新定义安全模型。
- OpenAPI 和 gRPC 为可选包；主库不得引入其依赖。配置或插件变化同步 `.plugin`、`.option`、`.deploy`。

## 验证

构建 `Zongsoft.Web.slnx` 并运行相关测试。HTTP 行为使用最小 TestServer/WebApplicationFactory 或现有夹具验证成功、无效输入、未认证、取消和异常映射；OpenAPI/gRPC 分别构建对应项目。
