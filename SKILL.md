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

- Core 是底层契约；功能包通常通过 NuGet 包引用 Core，在本仓测试项目中再使用 `ProjectReference`。
- 所有主库支持 .NET 8、9、10；目标框架特定依赖由 `Directory.Packages.props` 管理。
- `Zongsoft.Diagnostics/proto` 是外部子模块，除非任务明确要求，否则不修改上游协议内容。
- 双语 README、资源文件和插件清单属于用户可见契约；行为变化时检查同步需要。

## 验证选择

- 文档：差异、链接、CRLF。
- 局部实现：目标 `.csproj` 或 `.slnx` 加聚焦测试。
- Core 公共契约：Core 测试加直接受影响下游。
- 驱动/外部适配：先离线测试，再按明确开关使用本地服务。
- 发布、部署、容器、云调用和真实宿主只在用户明确要求时执行。
