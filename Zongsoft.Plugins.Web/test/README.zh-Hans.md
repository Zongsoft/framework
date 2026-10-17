# Web 宿主集成测试

[English](README.md)

xUnit 测试通过公开的 `Zongsoft.Web.WebApplicationBuilder` 与 `Application.Web` 入口验证宿主集成。端点场景和 Web 宿主构建在独立进程及临时插件目录中运行。[WebIntegrationTests](WebIntegrationTests.cs) 还使用进程内隔离夹具验证控制器发现、请求主体/路由、控制器描述和插件诊断；测试程序集禁用用例并行。夹具保留产品交付的 `Main.plugin` 注册，移除工作台扩展，因此不启动业务工作器或外部服务。

测试覆盖：

- Web 宿主构建验证插件上下文仅初始化一次、控制器激活器注册及宿主容器中的插件服务，不启动监听。
- 没有 Kestrel:Endpoints 声明时保持原生构建行为：允许其他跨插件键覆盖，原生 urls、http_ports、app.Urls 及代码 Listen 可处理真实 HTTP 请求；https_ports 验证构建阶段不禁止该配置，不宣称 TLS 握手。
- 既有程序集扫描为声明端点的 Web 宿主自动接入，延迟消费完整插件配置及随后注册的协议；验证 Import、入口来源和跨插件冲突。非 Web 服务扫描不启用，未注册的应用协议在启动时失败。显式 AddWebEndpoints 优先，可在存在默认 Kestrel 声明时选择其他配置节。
- 公开的 Application.Web 与 WebApplicationBuilder 入口无须手工接入。含路径声明共享监听，启动过滤器在初始化器、静态文件和路由之前约束真实回环请求；额外代码监听可以启动，其请求在无匹配范围时被拒绝。空范围集合拒绝全部请求。
- 自动接入后重复扫描程序集仍只有一个监视器；显式接入保留可选监视，重复登记只提示一次；业务变化不提示，网络及新增插件冲突提示重启，监听与范围保持快照。停止和直接释放都会解除订阅；初始化失败释放已创建服务。
- 原生 TCP/多路复用工厂实例不被包装，选择器、取消、特征和容器所有权保持原义；IPv6 TCP 保留独立地址族。传输替身不代表实际 QUIC 验收。

Debug 构建依赖本地 Core、Plugins、Web 输出。先依次构建这三个源码项目，再构建本解决方案并运行测试。测试项目通过项目引用依赖 Plugins.Web 和 Plugins，通过 `HintPath` 引用本地 Core、Web 程序集，不通过项目引用构建 Core 或 Web：

```powershell
dotnet build ../Zongsoft.Core/src/Zongsoft.Core.csproj -p:GeneratePackageOnBuild=false
dotnet build ../Zongsoft.Plugins/src/Zongsoft.Plugins.csproj -p:GeneratePackageOnBuild=false
dotnet build ../Zongsoft.Web/src/Zongsoft.Web.csproj -p:GeneratePackageOnBuild=false
dotnet build ./Zongsoft.Plugins.Web.slnx -p:GeneratePackageOnBuild=false -m:1
dotnet test ./test/Zongsoft.Plugins.Web.Tests.csproj --no-build -f net8.0
dotnet test ./test/Zongsoft.Plugins.Web.Tests.csproj --no-build -f net9.0
dotnet test ./test/Zongsoft.Plugins.Web.Tests.csproj --no-build -f net10.0
```

测试标记为 `Category=Integration`。子进程必须同时满足退出码为零及写入完成标记；提前退出不能让测试通过。本机 HTTP 测试在使用前临时取得可用端口，设置请求与进程超时，并取消工作台关闭时的退出进程行为，以完成 HTTP 停止验证。运行环境需要 IPv4 和 IPv6 回环 socket 支持。本项目不包含 gRPC 处理器挂载、反向代理部署或业务宿主启动。

请求上下文用例覆盖主体切换和路由读取。Session 字典的完整一致性仍不在覆盖范围：ContainsKey 要求本地字典及路由同时含键，Count/枚举未包含本地条目，CopyTo 先自增下标。
