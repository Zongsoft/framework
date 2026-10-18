---
name: zongsoft-diagnostics-web
description: 修改 Web 专用 Prometheus 导出器、Kestrel 抓取服务、绑定配置及对应测试时使用。
---

# Diagnostics Web 实现协作

先读 [AGENTS.md](AGENTS.md) 和双语 README。本项目独占 ASP.NET Core 依赖；通用 Diagnostics 不得反向引用本项目。

- Prometheus urls 保存完整地址；基路径忽略大小写及可选尾斜杠，同一 exporter 的所有地址须等效。独立 Kestrel 服务器复用 ASP.NET Core Prometheus 导出器，按原始 URI 路径段匹配；不解码或消点后换范围，不重定向。监听归指标提供程序所有并随之释放；监听和请求处理集中在私有嵌套的 [ScrapeServer](src/Telemetry/PrometheusExporterLauncher.ScrapeServer.cs)。普通域名不做 DNS 解析，展开为可用的 IPv4/IPv6 通配监听，再按规范化 Host 匹配；域名大小写、末尾点号及国际化形式等效。IP、通配地址和 localhost 保留各自的网卡/地址族范围；物理监听合并不能合并请求范围，端口和 scheme 也必须匹配。IPv6 TCP 不隐式开启 IPv4。只读取 Kestrel:Certificates，不继承宿主 Endpoints。protocol 默认 http1，显式只接受单个 http1；空值、其它协议、protocols、独立 path 和非法 urls 在注册前失败。PI 共读 urls，以常量模板 http1 声明固定能力。公开用法见 README。
- Prometheus 的公开入口、选项投影和原始 option 绑定声明由 [单元测试](test/unit) 验证；只解析选项，不构建启动抓取服务的 MeterProvider。域名/IP 混合监听、HTTP/HTTPS 抓取、证书、启动失败清理与端口释放由独立的 [集成测试](test/integration) 验证，通过 `test/integration/Zongsoft.Diagnostics.Web.Tests.slnx` 或 Cake `integration-test` 显式运行。不为测试扩大实现作用域。原始 option 的 PI 和模板检查不等同于后续 Packager 提取器验收。

- 保留 `Zongsoft.Diagnostics.Telemetry` 命名空间、prometheus 驱动名及私有 ScrapeServer；程序集为 Zongsoft.Diagnostics.Web。
- 资源归 Web.Properties；提供程序由通用 DiagnostorWorker 持有，Web.plugin 只声明程序集和插件依赖。
