## 范围

遵循 [../AGENTS.md](../AGENTS.md)。本项目仅向 Web 宿主提供 Prometheus Kestrel 抓取端点，禁止部署到 daemon、terminal。

- 实现入口与生命周期约束见 [SKILL.md](SKILL.md)。
- 公共过滤、资源身份及工作器属于 Zongsoft.Diagnostics；本插件不重复注册工作器。
- 修改配置时同步 option、binding PI、部署清单及双语 README。
- 先构建 Core 和通用 Diagnostics；Debug 通过 Reference/HintPath 引用它们的已构建程序集，不会自动构建依赖项目。分别验证 net8.0、net9.0、net10.0。
- `test/unit` 仅保留参数校验、选项投影和绑定声明测试，不构建启动抓取服务的 MeterProvider，也不监听端口或发送网络请求。被测 Web 库仍依赖 ASP.NET Core，不以移除框架引用代替测试隔离。
- HTTP/HTTPS 收发、证书、监听与释放测试位于独立的 `test/integration` 项目；默认解决方案和 Cake `test` 仅运行单元测试，集成测试通过 `test/integration/Zongsoft.Diagnostics.Web.Tests.slnx` 或 Cake `integration-test` 显式运行。
- HTTP/HTTPS 集成测试使用临时本地端口；不修改 DNS、hosts 或业务宿主，不扩大实现作用域。
