---
name: zongsoft-framework
description: 在 Zongsoft framework 仓库中进行修改、审查或排障前完成项目定向。适用于判断工作应归属 Core、Data、Plugins、Web、Security、messaging、externals 或 upgrading，识别插件配置与下游影响，并选择最小验证范围；不用于业务应用仓库。
---

# Zongsoft Framework 仓库定向

## 定位流程

1. 阅读 [AGENTS.md](AGENTS.md) 和目标目录就近的 `AGENTS.md`。
2. 阅读目标项目的 `README.zh-Hans.md`、`.slnx`、`*.csproj`，再搜索相关接口、实现、测试和插件配置。
3. 判断变更拥有者：
	- `Zongsoft.Core`：跨实现公共契约和基础能力。
	- `Zongsoft.Data`：数据引擎、映射和数据库驱动。
	- `Zongsoft.Plugins*`：插件解析、装配、宿主与 Web 桥接。
	- `Zongsoft.Web`、`Zongsoft.Security`：通用 Web 管线与安全能力。
	- `messaging`：消息队列驱动和可靠消息存储。
	- `externals`：第三方 SDK、平台、协议和运行时适配。
	- `upgrading`：升级包生产、管理、消费和部署链路。
4. 选择拥有行为的最窄项目。单一驱动或供应商差异优先使用既有扩展点，不扩充 Core。
5. 修改公开契约时搜索所有实现和调用方；修改插件产物时检查 `.plugin`、`.option`、`.mapping`、`.deploy` 与项目打包项。

## 依赖与兼容

- Core 是底层契约；先检查目标项目的 `Choose`：很多 Debug 构建通过 HintPath 引用本机 Core 输出，Release 使用 NuGet；不能把重新编译插件当成已同步宿主公共程序集。
- 默认目标框架由 `Directory.Build.props` 定义，项目可覆盖；样例和宿主可能仅使用 net10.0。包版本有中央定义也有项目级 `VersionOverride`，以实际项目为准。
- `Zongsoft.CodeAnalysis` 为集中版本管理的 NuGet 分析器包，源码与规则在 guidelines 维护；接入与检查方式详见 [代码规范检查](README.zh-Hans.md#code-analysis)。
- `Zongsoft.Diagnostics/proto` 是外部子模块，除非任务明确要求，否则不修改上游协议内容。
- 双语 README、资源文件和插件清单属于用户可见契约；行为变化时检查同步需要。

## 技能路由

| 工作契约 | 就近技能 |
| --- | --- |
| 公共接口、服务扫描/匹配/模块容器 | [Core](Zongsoft.Core/SKILL.md) |
| 数据引擎、映射、全部数据库驱动 | [Data](Zongsoft.Data/SKILL.md) |
| 插件 XML、构造、配置、宿主和 Plugins.Web | [Plugins](Zongsoft.Plugins/SKILL.md) |
| 控制器、绑定、格式化、OpenAPI/gRPC | [Web](Zongsoft.Web/SKILL.md) |
| 命令和交互操作 | [Commands](Zongsoft.Commands/SKILL.md) |
| 诊断配置与 OTLP 协议接入 | [Diagnostics](Zongsoft.Diagnostics/SKILL.md) |
| 身份、授权与验证码 | [Security](Zongsoft.Security/SKILL.md) |
| 网络收发与缓冲区 | [Net](Zongsoft.Net/SKILL.md) |
| 平台硬件采集 | [Hardwares](Zongsoft.Hardwares/SKILL.md) |
| 报表资源与数据加载 | [Reporting](Zongsoft.Reporting/SKILL.md) |
| 对话服务与模型会话 | [Intelligences](Zongsoft.Intelligences/SKILL.md) |
| ML.NET 管道及元数据 | [Learning](Zongsoft.Learning/SKILL.md) |
| 消息驱动与存储 | [messaging](messaging/SKILL.md)、[ZeroMQ 协议](messaging/zero/SKILL.md) |
| 第三方适配 | [externals](externals/SKILL.md)、[Etcd](externals/etcd/SKILL.md) |
| 升级链路 | [deployer](upgrading/deployer/SKILL.md)、[tool](upgrading/tool/SKILL.md)、[upgrader](upgrading/upgrader/SKILL.md)、[web](upgrading/web/SKILL.md) |

## 插件化修改的追踪顺序

1. 从消费方的 Core/共享接口找到具体提供者注册，不先把实现类注入业务代码。
2. 追踪 `.plugin` 程序集声明 → 服务特性扫描或扩展节点构建 → `.option` 配置 → 应用/模块容器匹配 → 具名提供者返回实例。
3. 分清插件名、模块名、服务别名、连接名。ServiceLocator 的 `name@provider` 与插件解析器的 `{service:...@module}` 不同。
4. 核对 `.deploy` 与包内产物、目标框架、原生资源、宿主根目录公共程序集；部署器默认跳过 Zongsoft 传递依赖，必须在宿主方案中显式组合。
5. README 展示公共接口消费与必要的配置/部署，SKILL 记录内部约束；不要在两处复制实现类清单。

## 验证选择

- 文档：差异、链接、CRLF。
- 局部实现：目标 `.csproj` 或 `.slnx` 加聚焦测试。
- Core 公共契约：Core 测试加直接受影响下游。
- 驱动/外部适配：先离线测试，再按明确开关使用本地服务。
- 发布、部署、容器、云调用和真实宿主只在用户明确要求时执行。
