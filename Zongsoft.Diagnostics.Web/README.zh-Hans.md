# Zongsoft.Diagnostics.Web

[English](README.md) | [简体中文](README.zh-Hans.md)

本插件仅部署到 Web 宿主。它依赖 `Microsoft.AspNetCore.App`，使用 Kestrel 为当前进程的指标提供抓取端点。daemon、terminal 只部署通用 [Diagnostics](../Zongsoft.Diagnostics/README.zh-Hans.md)，通过 OTLP → Collector → Prometheus 导出指标。

通用包负责 `/Diagnostics/Diagnostor/Meters/filter` 和唯一的 DiagnostorWorker；本包只添加 `prometheus` 导出器和绑定声明，不重复创建工作器或提供程序。通用配置将同一批指标的 OTLP 导出器设为 `enabled="false"`，避免重复采集。hosting 仓库共用一份 Diagnostics 模板：方案默认 `diagnostics.telemetry:enabled=true`，各 Web 站点在导入方案后覆盖为 `false`。

## Prometheus 监听

`prometheus` 导出器接受 HTTP 抓取请求。`urls` 保存包含抓取路径的完整监听地址，尾斜杠可省略，绑定声明读取同一份地址，并用常量模板声明本启动器固定的 HTTP/1 能力：

```xml
<!-- Zongsoft.Diagnostics.Web/src/Zongsoft.Diagnostics.Web.option；只列监听相关配置 -->
<?zongsoft.tools.binding name="prometheus"
	path="Diagnostics/Diagnostor/Meters/Exporters/*"
	filter="Driver:prometheus"
	template="${this.Settings['urls']}"
	protocols="http1"?>
<options>
	<option path="/Diagnostics">
		<diagnostor>
			<meters>
				<exporters>
					<exporter exporter.name="prometheus" driver="prometheus"
						settings="urls=http://127.0.0.1:9465/metrics,http://localhost:9465/metrics;totalSuffix=true;cacheDuration=300ms;" />
				</exporters>
			</meters>
		</diagnostor>
	</option>
</options>
```

`urls` 必填；省略 `protocol` 时默认为 `http1`，显式指定时只接受忽略大小写的单个 `http1`，例如 `urls=http://127.0.0.1:9465/metrics;protocol=http1`。空值、协议列表（包括重复的 `http1`）和 `protocols` 设置均无效。绑定使用常量模板 `protocols="http1"`，省略设置也不需要制作工具猜测默认协议；本启动器未实现 HTTP/2、HTTP/3 或应用层协议组合。这是运行配置校验，与制作工具的绑定映射检查职责独立。

每个 URL 使用 http 或 https，监听地址支持 IP、通配地址、localhost 及普通域名。同一导出器的基路径忽略大小写和可选尾斜杠，/metrics、/Metrics/ 等效，未指定路径表示根 /。声明路径段只允许 ASCII 字母、数字及 -._~，拒绝中间空段、独立点段、凭据、query、fragment 和编码路径。源配置保持原文，抓取选项去掉非根路径的尾斜杠。独立 Kestrel 服务器使用原始 URI 按路径段匹配，不解码或折叠点段后选择范围；自身及子路径均可抓取，/metrics-extra 返回 404，无重定向或双斜杠补偿。Windows 与 Linux 行为一致。

IP 声明保留网卡地址和地址族范围：`*`、`0.0.0.0` 只监听 IPv4，`[::]` 只监听 IPv6，`localhost` 监听可用的 IPv4/IPv6 回环地址。普通域名在启动时不做 DNS 解析，而是在可用的 IPv4/IPv6 通配地址上监听，按 HTTP Host 匹配声明的域名，忽略大小写及末尾点号；国际化域名与其 ASCII 形式等效。例如 `urls=http://metrics.example.com:9465/metrics` 接受 9465 端口上使用该域名的请求，调用方须将域名解析到本服务器。这是主机路由规则，不是身份认证。

同一导出器的多条声明可以共用物理监听，但每个请求仍须符合某条原始声明的 scheme、本地端口、网卡地址、地址族，以及普通域名声明的 Host 条件。因此，域名与 `127.0.0.1` 混用时，仅该回环地址接受任意 Host，其它本地地址仍要求声明的域名；不同域名、端口不会相互扩大访问范围。

地址以 `,`、`;` 或 `|` 分隔；中间空项和尾部分隔符均报错。在连接设置内部使用分号分隔地址时，将整个 `urls` 值加引号，例如 `urls='http://127.0.0.1:9465/metrics/;http://localhost:9465/metrics/';`。独立的 `path` 设置必须移除，即使值为空也会报错。缺少地址时不会回退到隐含监听。

`totalSuffix` 默认为 `true`，`cacheDuration` 默认为 `300ms`，二者应用于独立监听复用的 ASP.NET Core 导出器；负缓存时间按零处理，过大的值限制到所支持的毫秒范围。登记导出器选项不会自动添加 ASP.NET Core 路由。独立监听由诊断提供程序持有，并随其释放。

默认地址仅限回环访问。反向代理位于另一个容器时，必须显式配置该容器可达的监听地址，绑定工具不改写回环地址。独立服务器占用声明的端口，不能与其它服务器共享同一 socket。HTTPS 使用指标服务容器中 IConfiguration（缺省取 ApplicationContext.Current.Configuration）的 Kestrel:Certificates 配置；不继承宿主 Endpoints，也不使用 HTTP.sys 证书绑定。绑定指令表达暴露意图，本身不启动监听；出站 `opentelemetry` 导出器不参与该绑定。

## 验证

[单元测试](test/unit)检查参数校验、导出器选项投影和绑定声明，不构建启动抓取服务的 MeterProvider，不监听端口或发送网络请求。测试仍会通过被测 Web 库依赖 ASP.NET Core。

在 `Zongsoft.Diagnostics.Web` 目录运行，先构建 Core 和 Diagnostics，Debug 会引用它们的已构建程序集：

```shell
dotnet build ../Zongsoft.Core/src/Zongsoft.Core.csproj -p:GeneratePackageOnBuild=false
dotnet build ../Zongsoft.Diagnostics/src/Zongsoft.Diagnostics.csproj -p:GeneratePackageOnBuild=false
dotnet build test/unit/Zongsoft.Diagnostics.Web.Tests.csproj -p:GeneratePackageOnBuild=false -p:ZongsoftCodeStyleStrict=true
dotnet test test/unit/Zongsoft.Diagnostics.Web.Tests.csproj --no-build
```

[集成测试](test/integration)通过临时 Kestrel HTTP/HTTPS 监听验证域名与 IP 混用、IPv4/IPv6 隔离、实际指标输出、证书选择、释放及启动失败清理。域名请求直接连接回环地址，无须 DNS 或 hosts 文件改动。不启动业务宿主，不代替反向代理及生产 HTTPS 部署验收。构建 Core 和 Diagnostics 后显式运行：

```shell
dotnet build test/integration/Zongsoft.Diagnostics.Web.Tests.slnx -p:GeneratePackageOnBuild=false -p:ZongsoftCodeStyleStrict=true
dotnet test test/integration/Zongsoft.Diagnostics.Web.Tests.slnx --no-build
```

普通 `Zongsoft.Diagnostics.Web.slnx` 解决方案和默认 Cake `test` 任务只包含单元测试。独立的 Cake 命令 `dotnet cake build.cake --target=integration-test` 会构建并运行集成测试项目。


## 部署与兼容性

两个插件分别部署到以下目录；基础库由宿主提供：

```ini
[plugins zongsoft diagnostics]
nuget:Zongsoft.Diagnostics@1.13.0
[plugins zongsoft diagnostics web]
nuget:Zongsoft.Diagnostics.Web@1.0.0
```

以上为本次源码版本，正式部署前需先提供对应构建包；这次改造不发布 NuGet 包。`PrometheusExporterLauncher` 保留 `Zongsoft.Diagnostics.Telemetry` 命名空间，程序集改为 `Zongsoft.Diagnostics.Web`；直接引用类型的调用方需添加新包并重新编译。`driver="prometheus"` 不变。

`Instrumentation.AspNetCore` 依赖已迁入本包，但未新增自动 HTTP 埋点。
