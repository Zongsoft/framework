# Web 插件测试

[English](README.md) | [简体中文](README.zh-Hans.md)

[单元测试](unit/Zongsoft.Plugins.Web.Tests.csproj) 使用内存组件验证控制器发现、请求主体与路由值、控制器描述和插件元数据，不构建 Web 宿主、不启动子进程、不绑定端口。

[集成测试](integration/Zongsoft.Plugins.Web.Tests.csproj) 在隔离进程中验证公开的 `WebApplicationBuilder.BuildAsync` 和 `Application.WebAsync` 入口，覆盖插件配置与服务扫描、端点投影、路径作用域、本地 HTTP 请求、原生传输选择、监视器所有权、停止以及初始化失败后的异步释放。程序集名称为 `Zongsoft.Plugins.Web.IntegrationTests`。

Plugins.Web 主解决方案和 Cake `test` 任务只包含单元测试。集成测试有独立的[解决方案](integration/Zongsoft.Plugins.Web.Tests.slnx)和 Cake `integration-test` 任务。两个项目仅通过 `ProjectReference` 引用 Plugins.Web；Core、Plugins 和 Web 使用本地程序集 `HintPath`。共用的[文件夹具](.shared/PluginTestDirectory.cs) 管理临时插件目录，复用产品交付的 Main 注册。

## 运行方式

从框架根目录运行，需要 .NET 10 SDK 和 .NET 8、9、10 Runtime。按顺序构建本地 Debug 依赖：

```powershell
dotnet build ./Zongsoft.Core/src/Zongsoft.Core.csproj -p:GeneratePackageOnBuild=false
dotnet build ./Zongsoft.Plugins/src/Zongsoft.Plugins.csproj -p:GeneratePackageOnBuild=false
dotnet build ./Zongsoft.Web/src/Zongsoft.Web.csproj -p:GeneratePackageOnBuild=false
dotnet build ./Zongsoft.Plugins.Web/Zongsoft.Plugins.Web.slnx -p:GeneratePackageOnBuild=false
dotnet test ./Zongsoft.Plugins.Web/test/unit/Zongsoft.Plugins.Web.Tests.csproj --no-build --no-restore
```

按需显式运行集成测试：

```powershell
dotnet test ./Zongsoft.Plugins.Web/test/integration/Zongsoft.Plugins.Web.Tests.slnx -p:GeneratePackageOnBuild=false
```

添加 `-f net8.0`、`-f net9.0` 或 `-f net10.0` 可选择单个目标框架。VSTest 发现并执行 xUnit v3 测试。集成子进程通过 PATH 中的 `dotnet` 运行，必须同时满足退出码为零及存在完成标记。本地 HTTP 用例限制请求与进程等待时间，需要 IPv4/IPv6 回环套接字支持，不访问真实外部服务。传输替身不验证真实 QUIC，HTTPS 端口投影不代表完成 TLS 握手。

程序集禁用用例并行以隔离进程级应用状态。夹具等待应用和服务容器释放后删除文件。初始器失败用例通过门闩控制异步释放，验证工厂在清理完成前保持未完成状态。
