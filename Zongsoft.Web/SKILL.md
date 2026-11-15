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
- [GrpcInitializer](grpc/src/GrpcInitializer.cs) 注册 gRPC，按服务标签发现类型并以 Handler 字面基路径挂载到 Web 端点区域；注解与 gRPC 监听基路径忽略大小写及可选尾斜杠关联，Order 为零。缺注解、缺监听、空区域及服务/方法冲突均失败。反射包装器从区域元数据取得该区域的上游 ReflectionServiceImpl；不建立全局业务服务目录。
- [Plugins.Web 默认策略](../Zongsoft.Plugins.Web/src/WebApplicationBuilder.cs) 包含宽松 CORS。安全部署必须校准宿主策略，不把默认认证中间件存在等同于所有接口已经授权保护。

## 契约

- 保持路由、参数来源、模型绑定、分页、HTTP 状态码、错误负载和序列化形状兼容。
- ServiceController 的 CRUD 与子服务行为应复用 Data/Services 契约，不复制领域验证。
- Filter、Formatter 和 Binder 必须正确处理空输入、取消、不可读 Body、重复读取和响应已开始。
- 安全组件只从 ASP.NET 上下文桥接身份与授权，不在 Web 层重新定义安全模型。
- OpenAPI 和 gRPC 为可选包；主库不得引入其依赖。配置或插件变化同步 `.plugin`、`.option`、`.deploy`。
- `.deploy` 不引用 Zongsoft.Core、Zongsoft.Web、Zongsoft.Plugins、Zongsoft.Plugins.Web；这四个基础库由宿主统一提供，编译依赖保留在项目文件中。

WebUtility.Paginate 订阅 IPageable.Paginated 的可等待委托，收到分页结果后先解除订阅，再写入分页响应头，返回已完成的 ValueTask。数据库结果在分页通知完成后才输出首条记录。服务控制器的批量删除使用 await using 管理事务，并等待 CommitAsync；响应状态与事务范围保持既定约定。

WebFileAccessor.Write 使用 IFile.OpenAsync 打开目标流，等待内容复制及自有目标流的异步释放后才输出 FileInfo；S3 上传在释放流时完成，不能以复制完成代替远端写入完成。表单源流仍由 ASP.NET 所有。服务控制器的上传回调返回 null 时等待异步删除刚写入的文件，清理使用 CancellationToken.None，删除失败保持既有吞错语义。

## 启动端点与请求范围

[Hosting 端点设施](docs/endpoints.zh-Hans.md) 将 KestrelUtility 配置转换、WebEndpoint/WebEndpointCollection 逻辑端点和请求范围管线分开。Convert 从调用方指定的配置节生成独立 ConfigurationRoot，并可通过 out 参数返回同次输入的逻辑端点；调用方负责释放配置。转换不注册服务或检查应用处理器。集合只接受逻辑端点及协议规则，不持有配置源，不提供 Configure/HasChanged，不引用 Kestrel 枚举。AddWebEndpoints 接收逻辑端点；UseWebEndpoints 在路由、转发头和静态文件之前安装。KestrelUtility.Configure 单独适配 IPv6 TCP socket。AddWebEndpoints 另提供服务提供器工厂重载，延迟到最终容器解析端点；AddWebEndpointPipeline 注册私有启动过滤器，不能再手动重复安装。Plugins.Web 的 IServiceRegistration 注册器在 Web 宿主存在 Kestrel:Endpoints 子项且未注册 WebEndpointCollection 时自动调用 AddWebEndpoints 和 AddWebEndpointMonitoring。显式接入保留自选配置节及可选监视；无端点或非 Web 宿主不接入，通用构建器无网络专用步骤，hosting 无须修改。

Core 只解析通用网络值；HTTP 基路径限制、Kestrel 承载转换及类别内的范围选择属于 Web。上层协议通过 WebEndpointProtocol 注册，默认普通 HTTP；编码协议可注册默认承载并共用 http 类别。gRPC 简写、媒体类型、方法/版本校验、精确方法路径及无范围响应位于 Web.Grpc 的私有协议策略，由 AddGrpcWebEndpoints 和 GrpcInitializer.Registration 注册。不得在主库重加 grpc/protobuf 名称判断、IsGrpc 或 GrpcProtocols。即时重载要求先注册协议；工厂重载允许最终容器构建前继续注册。缺失、重名及类别识别歧义失败，不按注册顺序猜归属。

按真实本地连接、请求类别和原始 URI 路径选择范围；基路径忽略大小写及可选尾斜杠，后续业务路径保持协议语义，不改写源配置或请求。已解析路径不得进入另一逻辑范围；这是拒绝校验，不改派后端。WebEndpointFeature 固定选择结果及 Kind，WebEndpointMetadata 关联范围与类别。中间件和 MatcherPolicy 为 WebEndpointExtension 的私有嵌套类，测试通过公开入口执行。Web.Grpc 将 Handler 服务、区域 fallback 和区域反射放入带范围元数据的 MapGroup；关闭上游全局 unknown-service/method fallback，未知 RPC 不回退到其它范围。主库不引入 gRPC 依赖。HTTP/3 仅声明及配置投影通过不代表 QUIC 运行验收通过。

主库测试为 test/Hosting 的 KestrelUtilityTest、WebEndpointCollectionTest 与 WebEndpointPipelineTest，不引用 gRPC；协议专项在 grpc/test 的独立项目中，通过公开注册和真实 ASP.NET 管线验证，不开放内部方法。grpc 与 openapi 均以 src 存放主项目和插件构件，test 存放独立测试，README 保留在各扩展根目录。Release 使用项目指定的 Core/Web 包，必须与当前异步契约匹配；发布前使用隔离本地包源验证 Release 依赖，Debug 本地引用构建不代替包依赖验证。

## OpenAPI 文档与 Scalar

[路径生成器](openapi/src/DocumentGenerator.Paths.cs) 从控制器描述和运行时路由构造操作。只有进入文档的操作及其祖先需要导航 Tag。Tag 生成器按模块和服务命名空间逐级关联原生 parent，命名空间前缀对应已发现的服务时复用其 Tag。操作引用服务限定名，目录 ID 包含作用域，summary 仅用于显示。Tag 按名称作序数排序，父子关系决定展示层级。

文档端点按注册实例、Request.PathBase 和选定环境名缓存同一文档的 JSON 和 YAML，缓存不依赖请求 Host。OpenAPI.NET 以 3.2 写出文档，版本写入器只将根 openapi 字段的 3.2.0 更新为 3.2.1。Scalar 使用 Modern 布局渲染原生 Tag 层级。x-scalar-environments 的变量写成 name/value 条目数组。Microsoft.AspNetCore.OpenApi 10.x 的 2.x 库依赖与该插件直接使用的 Microsoft.OpenApi 3.x 应在部署层保持隔离。

服务器列表将保留名称 current 放在首位，其余服务器、认证方案及服务器变量枚举值按 XML 声明顺序输出。当前站点的默认 URL 为已转义的 PathBase 或 /，也可通过配置显式覆盖。ServerOption.Name 写入 OpenAPI name，Description 单独用于显示。服务器变量仅在 Values 非空时输出 enum，并校验默认值属于枚举。服务器变量按 URL 模板中首次出现的位置排序，未使用的变量置后。默认选项提供当前站点、主机端口和子域名站点模式；完整地址仅通过显式配置提供。常用站点是 sites 服务器的 site 变量枚举，由 Scalar 渲染为原生下拉框。Scalar 认证持久化读取 AuthenticationOption.Persisted，不预填随机凭据。

配置了绝对 HTTP(S) Url 的 EnvironmentOption 由 WebInitializer 启动时校验。/scalar/ 使用 Scalar 原生文档选择器，默认的手动文档 v1 显示为 none 并排在第一项。其余环境按 XML 子键声明顺序注册。发布构建需要包含 `GetDeclaredOptionKeys` 的 Zongsoft.Core 包。配置了 URL 的环境使用 /openapi/environments/{documentName}.{extension}。环境名用作文档标题和网址片段。环境文档路径按完整环境名查找，未知名称返回 404。每份环境文档只写自己的 Server Object 和 Scalar 变量，x-scalar-active-environment 使用目标环境名。手动文档使用宿主环境名和手动服务器列表。

手动和环境文档共享生成锁，因为操作发现会填充控制器模型的元数据。Scalar 页面将 AuthenticationOption.Persisted 映射到原生的 persistAuth。Credential 方案声明为 Authorization 请求头类型的 API Key，由用户填写完整头值。

[OpenAPI 测试](openapi/test/Zongsoft.Web.OpenApi.Tests.csproj) 通过本地 ASP.NET Core 管线读取两种文档，覆盖多级标签、父级服务自有操作、同名目录、路径前导斜杠、约定式路由、同操作路由别名、Scalar 环境变量和版本字段，并验证当前站点、服务器排序、路径前缀隔离、显式公开地址、自由变量与枚举限制，并从随插件提供的选项文件验证默认模式及 JSON/YAML 变量顺序。宿主集成验收使用部署后的插件和浏览器。

## 验证

构建 `Zongsoft.Web.slnx` 并运行相关测试。HTTP 行为使用最小 TestServer/WebApplicationFactory 或现有夹具验证成功、无效输入、未认证、取消和异常映射；OpenAPI/gRPC 分别构建对应项目。
