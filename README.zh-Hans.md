# Zongsoft Framework

[![Contributor Covenant](https://img.shields.io/badge/Contributor%20Covenant-2.1-4baaaa.svg)](CODE_OF_CONDUCT-zh.md)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

这是 _**Z**ongsoft_ 开发框架的开源项目集，支持 _**.NET**_ `8`,`9`,`10` 等版本。
可插拔应用程序生态系统是 _**Z**ongsoft_ 的特点，欢迎与我们[携手共建](CONTRIBUTING-zh.md)。

> 💡 在 `clone` 本项目源码后，需要使用 `git submodule update` 命令来更新 [子模块](.gitmodules)。

> - 🛠️ 开发时使用 [代码规范检查](#code-analysis) 核对编码风格与构建诊断。
> - 📦 维护者发布包前，请先阅读 [NuGet 发布指南](PUBLISHING.zh-Hans.md)。

## 从哪里开始

Zongsoft 的典型应用由“宿主 + 业务插件 + 能力插件”组成。宿主负责启动和生命周期；业务模块针对 Core 或公共契约编译；数据库、缓存、消息和表达式等实现由部署清单与配置组合。

| 你要做什么 | 阅读入口 |
| --- | --- |
| 理解应用容器、模块容器、具名提供者与注入 | [Core 使用指南](Zongsoft.Core/README.zh-Hans.md) |
| 创建宿主、部署能力并编写解耦的消费插件 | [插件运行时入门](Zongsoft.Plugins/README.zh-Hans.md) |
| 将控制器作为插件部署并验证 HTTP 请求 | [Web 插件完整示例](Zongsoft.Plugins.Web/README.zh-Hans.md) |
| 配置模型映射和数据库驱动 | [数据引擎](Zongsoft.Data/README.zh-Hans.md) |
| 选择消息驱动、订阅与可靠存储 | [消息驱动目录](messaging/)与 [Core 消息契约](Zongsoft.Core/src/Messaging/) |
| 修改框架实现或开展 AI 协作 | [AGENTS.md](AGENTS.md) 与 [技能路由](SKILL.md) |

💡 `dotnet add package` 解决编译引用，不能替代插件部署。应用通常通过 `ApplicationContext.Current.Services`、应用定义的 `Module.Current.Services` 和约定配置取得公共接口；无需在业务模块中逐一构造第三方实现。

🚨 插件以宿主权限执行，并非沙箱。部署清单可能覆盖文件，数据库、云服务、消息代理和模型调用也可能产生副作用；先在隔离环境验证，再接入真实业务。

## 项目列表

- [_**Z**ongsoft.**C**ore_](Zongsoft.Core) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Core)](https://nuget.org/packages/Zongsoft.Core)
	> 包含公共接口、基类、枚举等，为 _**Z**ongsoft_ 开发框架提供了必要的核心功能集。
- [_**Z**ongsoft.**D**ata_](Zongsoft.Data) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data)](https://nuget.org/packages/Zongsoft.Data)
	> 提供类 **G**raph**QL** 功能的 _**ORM**_ 数据引擎，其下 [_drivers_](Zongsoft.Data/drivers/) 包括：
	> - [mssql](Zongsoft.Data/drivers/mssql/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.MsSql)](https://nuget.org/packages/Zongsoft.Data.MsSql)
	> _**M**icrosoft **SQL** **S**erver_ 驱动
	> - [mysql](Zongsoft.Data/drivers/mysql/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.MySql)](https://nuget.org/packages/Zongsoft.Data.MySql)
	> _**M**y**SQL**_/_**M**aria**DB**_ 驱动
	> - [sqlite](Zongsoft.Data/drivers/sqlite/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.SQLite)](https://nuget.org/packages/Zongsoft.Data.SQLite)
	> _**SQL**ite_ 驱动
	> - [duckdb](Zongsoft.Data/drivers/duckdb/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.DuckDB)](https://nuget.org/packages/Zongsoft.Data.DuckDB)
	> _**D**uckDB_ 驱动
	> - [postgres](Zongsoft.Data/drivers/postgres/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.PostgreSql)](https://nuget.org/packages/Zongsoft.Data.PostgreSql)
	> _**P**ostgre**SQL**_ 驱动
	> - [influxdb](Zongsoft.Data/drivers/influx/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.Influx)](https://nuget.org/packages/Zongsoft.Data.Influx)
	> _**I**nflux**DB**_ 驱动
	> - [tdengine](Zongsoft.Data/drivers/tdengine/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.TDengine)](https://nuget.org/packages/Zongsoft.Data.TDengine)
	> _**TD**engine_ 驱动
	> - [clickhouse](Zongsoft.Data/drivers/clickhouse/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.ClickHouse)](https://nuget.org/packages/Zongsoft.Data.ClickHouse)
	> _**C**lick**H**ouse_ 驱动
- [_**Z**ongsoft.**C**ommands_](Zongsoft.Commands) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Commands)](https://nuget.org/packages/Zongsoft.Commands)
	> 提供了一些常用的命令，为应用层提供以命令行方式执行特定功能的能力。
- [_**Z**ongsoft.**D**iagnostics_](Zongsoft.Diagnostics) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Diagnostics)](https://nuget.org/packages/Zongsoft.Diagnostics)
	> 提供了 _**O**pen**T**elemetry_ 协议相关的诊断能力，包括 _**O**pen**T**elemetry_ 协议的接收处理，以及 _**C**onsole_、_**P**rometheus_、_**Z**ipkin_ 等输出器插件等。
- [_**Z**ongsoft.**H**ardwares_](Zongsoft.Hardwares) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Hardwares)](https://nuget.org/packages/Zongsoft.Hardwares)
	> 提供了跨平台获取硬件信息的功能。
- [_**Z**ongsoft.**I**ntelligences_](Zongsoft.Intelligences) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Intelligences)](https://nuget.org/packages/Zongsoft.Intelligences)
	> 提供了大语言模型、智能体、_**R**etrieval **A**ugmented **G**eneration_ 等 _**AI**_ 功能集，基于 [**M**icrosoft.**E**xtensions.**AI**](https://www.nuget.org/packages/Microsoft.Extensions.AI) 及 [**M**icrosoft.**A**gents.**AI**](https://www.nuget.org/packages/Microsoft.Agents.AI) 等相关库的插件化。
- [_**Z**ongsoft.**N**et_](Zongsoft.Net) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Net)](https://nuget.org/packages/Zongsoft.Net)
	> 提供了高性能网络通讯相关的支持，基于 [_**P**ipelines_](https://learn.microsoft.com/zh-cn/dotnet/standard/io/pipelines)、[_**B**uffers_](https://learn.microsoft.com/zh-cn/dotnet/standard/io/buffers) 等新式技术。
- [_**Z**ongsoft.**P**lugins_](Zongsoft.Plugins) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Plugins)](https://nuget.org/packages/Zongsoft.Plugins)
	> 提供了插件化应用开发的核心功能。
- [_**Z**ongsoft.**P**lugins.**W**eb_](Zongsoft.Plugins.Web) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Plugins.Web)](https://nuget.org/packages/Zongsoft.Plugins.Web)
	> 提供了 **W**eb 应用的插件化支持。
- [_**Z**ongsoft.**R**eporting_](Zongsoft.Reporting) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Reporting)](https://nuget.org/packages/Zongsoft.Reporting)
	> 提供了报表相关的核心功能定义。
- [_**Z**ongsoft.**S**ecurity_](Zongsoft.Security) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Security)](https://nuget.org/packages/Zongsoft.Security)
	> 提供了安全(身份验证、授权控制)相关的核心功能。
- [_**Z**ongsoft.**W**eb_](Zongsoft.Web) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Web)](https://nuget.org/packages/Zongsoft.Web)
	> 提供了 **W**eb 应用开发的通用能力。
	- [open-api](Zongsoft.Web/openapi/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Web.OpenApi)](https://nuget.org/packages/Zongsoft.Web.OpenApi)
		> 提供了 _**O**pen-**API**_ 规范的插件化扩展。
	- [grpc](Zongsoft.Web/grpc/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Web.Grpc)](https://nuget.org/packages/Zongsoft.Web.Grpc)
		> 提供了 _gRPC_ 基于 _ASP.NET_ 服务端的插件化扩展。

- [_messaging_](messaging/)
	- [kafka](messaging/kafka/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.Kafka)](https://nuget.org/packages/Zongsoft.Messaging.Kafka)
		> 提供了 _**K**afka_ 消息队列的插件化支持。
	- [rabbit](messaging/rabbit/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.RabbitMQ)](https://nuget.org/packages/Zongsoft.Messaging.RabbitMQ)
		> 提供了 _**R**abbitMQ_ 消息队列的插件化支持。
	- [mqtt](messaging/mqtt/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.Mqtt)](https://nuget.org/packages/Zongsoft.Messaging.Mqtt)
		> 提供了 _**M**qtt_ 协议的消息队列的插件化支持。
	- [zero](messaging/zero/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.ZeroMQ)](https://nuget.org/packages/Zongsoft.Messaging.ZeroMQ)
		> 提供了 _**Z**eroMQ_ 消息队列的插件化支持。

- [_upgrading_](upgrading/)
	- [deployer](upgrading/deployer/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Upgrading.Deployer)](https://nuget.org/packages/Zongsoft.Upgrading.Deployer)
		> 自动升级的本地部署器，是一个以 _**N**ative **AOT**_ 发布的独立程序。
	- [upgrader](upgrading/upgrader/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Upgrading.Upgrader)](https://nuget.org/packages/Zongsoft.Upgrading.Upgrader)
		> 自动升级插件库的升级器，为宿主应用提供更新检测、下载与部署支持。
	- [web](upgrading/web/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Upgrading.Web)](https://nuget.org/packages/Zongsoft.Upgrading.Web)
		> 自动升级插件库的 _**W**eb_ 服务端，提供发布包的发布与下载服务支持。
	- [tool](upgrading/tool/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Tools.Upgrader)](https://nuget.org/packages/Zongsoft.Tools.Upgrader)
		> 自动升级插件库的打包工具，提供打包、校验与发布三个子命令。

- [_externals_](externals/)
	- [aliyun](externals/aliyun/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Aliyun)](https://nuget.org/packages/Zongsoft.Externals.Aliyun)
		> 提供了 _阿里云_ 相关服务的插件化支持，基于阿里云 _**REST**ful API_ 接口实现。
	- [amazon](externals/amazon/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Amazon)](https://nuget.org/packages/Zongsoft.Externals.Amazon)
		> 提供了 _亚马逊(AWS)_ 相关服务的插件化支持，基于 [AWS-SDK](https://github.com/aws/aws-sdk-net) 开源项目的插件化。
	- [closedxml](externals/closedxml/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.ClosedXml)](https://nuget.org/packages/Zongsoft.Externals.ClosedXml)
		> 提供了电子表格 _(**E**xcel)_ 生成、导入、导出、模板渲染等功能，基于 [**C**losed**X**ml](https://github.com/ClosedXML) 开源项目的插件化。
	- [hangfire](externals/hangfire/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Hangfire)](https://nuget.org/packages/Zongsoft.Externals.Hangfire)
		> 提供了时间任务调度相关功能，基于 [**H**angfire](https://www.hangfire.io) 开源项目的插件化。
	- [redis](externals/redis/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Redis)](https://nuget.org/packages/Zongsoft.Externals.Redis)
		> 提供了分布式缓存、分布式锁、序列号生成等功能，基于 [**S**tack**E**xchange.**R**edis](https://github.com/StackExchange/StackExchange.Redis) 开源项目的插件化。
	- [polly](externals/polly/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Polly)](https://nuget.org/packages/Zongsoft.Externals.Polly)
		> 提供了 超时 _(**T**imeout)_、重试 _(**R**etry)_、后备 _(**F**allback)_、熔断 _(**C**ircuit **B**reaker)_、限速 _(**R**ate **L**imiter)_ 等瞬态故障弹性处理相关功能，基于 [**P**olly](https://www.pollydocs.org) 开源项目的插件化。
	- [opc](externals/opc/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Opc)](https://nuget.org/packages/Zongsoft.Externals.Opc)
		> 提供了 OPC 物联网协议的连接、读写、订阅等功能，基于 [**OPC** **F**oundation](https://github.com/OPCFoundation/UA-.NETStandard) 开源项目的插件化。
	- [lua](externals/lua/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Lua)](https://nuget.org/packages/Zongsoft.Externals.Lua)
		> 提供了 [**L**ua](https://lua.org) 表达式解析计算、脚本执行等功能，基于 [**NL**ua](https://github.com/nlua/nlua) 开源项目的插件化。
	- [python](externals/python/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Python)](https://nuget.org/packages/Zongsoft.Externals.Python)
		> 提供了 [**P**ython](https://python.org) 表达式解析计算、脚本执行等功能，基于 [**I**ron**P**ython](https://ironpython.net) 开源项目的插件化。
	- [scriban](externals/scriban/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Scriban)](https://nuget.org/packages/Zongsoft.Externals.Scriban)
		> 提供了 [**S**criban](https://github.com/lunet-io/scriban) 表达式解析计算、_文本模板渲染_ 等功能，基于 [**S**criban](https://github.com/scriban/scriban) 开源项目的插件化。
	- [wechat](externals/wechat/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Wechat)](https://nuget.org/packages/Zongsoft.Externals.Wechat)
		> 提供了[_微信_](https://weixin.qq.com)认证、[_微信支付_](https://pay.weixin.qq.com)、[_微信公众号_](https://mp.weixin.qq.com) 等相关功能，基于微信 _**REST**full API_ 接口实现。

<a id="code-analysis"></a>

## 代码规范检查

本仓库通过 [`Zongsoft.CodeAnalysis` NuGet 包](https://github.com/Zongsoft/Guidelines/blob/main/analysis/README.zh-Hans.md) 执行规范检查。[公共构建配置](Directory.Build.props) 为 C# 项目添加开发期包引用，[中央包配置](Directory.Packages.props) 管理版本。分析器源码、测试、构建开关和 C# 规则只在 [guidelines](https://github.com/Zongsoft/Guidelines/tree/main/analysis) 维护，升级包即可更新。根 [`.editorconfig`](.editorconfig) 保留编辑器设置与既有 VB 偏好；完整规则与覆盖范围见 [开发规范](https://github.com/Zongsoft/Guidelines/blob/main/zongsoft.csharp.guidelines.md) 和 [配套说明](https://github.com/Zongsoft/Guidelines/blob/main/README.zh-Hans.md#code-analysis)。

[检查命令](#检查命令) · [EditorConfig 同步](#editorconfig-同步) · [获取与升级](#获取与升级)

### 检查命令

在仓库根目录完成目标项目还原后，可执行以下只读源码检查；以 Core 为例，其他项目替换路径：

```powershell
dotnet build ./Zongsoft.Core/src/Zongsoft.Core.csproj --no-restore -p:GeneratePackageOnBuild=false
dotnet format whitespace ./Zongsoft.Core/src/Zongsoft.Core.csproj --no-restore --verify-no-changes --include ./Zongsoft.Core/src/Components/Handler.cs
dotnet format style ./Zongsoft.Core/src/Zongsoft.Core.csproj --no-restore --verify-no-changes --diagnostics IDE0049
```

`--include` 路径相对于当前工作目录，将示例文件替换为实际修改文件。普通构建将明确的风格问题报告为警告；未使用引用改用 `ZS0005`，允许普通 `using System;`，此项检查仍要求开启 XML 文档生成；`CS4014` 与 `CA2012` 保持为错误。`IDE0049` 不在构建时运行，必须由编辑器或上面的 `dotnet format style` 命令补充检查。首次使用前可用 `dotnet restore` 还原目标项目。

在构建命令追加 `-p:ZongsoftCodeStyleStrict=true`，将配置指定的风格警告升级为错误。严格模式检查整个项目及实际构建的项目引用，不能限制为 Git 修改行。多目标编译按项目的实际目标框架执行，局部格式检查不替代各目标框架验证。CI 必须检查构建和 IDE0049 验证命令的退出码。

> 💡 `dotnet format whitespace` 直接检查格式化结果，不应用分析器的诊断抑制，仍可能报告合法条件编译缩进和单行 try/catch/finally 的排版差异；这些例外以加载配套分析器的构建诊断为准。只读检查保留 `--verify-no-changes`。需要修正时限定本次修改文件并检查差异；不要对全仓执行无范围限制的格式修正、整理导入或公共 API 重命名。

`using` 别名、正式项目的全局引用限制、依赖分组及组内长度排序、中文职责分段、字段 `this.` 的可见性差异和设计契约仍需按开发规范审查。标准导入整理器使用字母顺序，不能实现本规范的长度排序。

C# 规则随包更新；不要在根 EditorConfig 重复维护包内规则。新增文本默认 CRLF，`.sh` 遵循 LF，已有编码与 BOM 保留；已有 LF 文件需在就近 EditorConfig 中明确覆盖，避免编辑器统一换行。

### EditorConfig 同步

`Directory.Build.props` 已设置以下属性，将同步目录指向 framework 根目录：

```xml
<ZongsoftGuidelinesSynchronization>$(MSBuildThisFileDirectory)</ZongsoftGuidelinesSynchronization>
```

使用 `Zongsoft.CodeAnalysis` **0.2.0 或后续版本**后，实际构建会自动用包内模板同步根 `.editorconfig`，大小和修改时间都相同时跳过复制；子目录配置保持不变。无需手动同步或检查命令，更新后检查并提交差异。

保留 VS 快速最新检查；若项目已是最新而被跳过，可执行“重新生成”，或先“清理”再“生成”。单独清理、还原或设计时构建不触发同步。需还原包含此同步实现的包版本。详见 [同步说明](https://github.com/Zongsoft/Guidelines/blob/main/analysis/README.zh-Hans.md#editorconfig-同步)。

### 获取与升级

正式使用时，确保配置的 NuGet 源已发布所需版本，然后正常还原项目。包自动加载分析器、构建设置和全局 C# 规则；不需要源码副本、手工导入 props 或子模块。包引用使用 `PrivateAssets="all"`，不经框架业务包强制传递给下游；业务系统应自行引用该分析器包。

首次发布前，在本机验证已打包的产物：

```powershell
dotnet pack ../guidelines/analysis/analyzers/src/Zongsoft.CodeAnalysis.Analyzers.csproj -c Release
dotnet restore ./Zongsoft.Core/src/Zongsoft.Core.csproj --source ../guidelines/analysis --source 'https://api.nuget.org/v3/index.json'
dotnet build ./Zongsoft.Core/src/Zongsoft.Core.csproj --no-restore -p:GeneratePackageOnBuild=false
```

本地包源仅用于发布前验证，不写入仓库配置。版本未发布时，全新工作区或 CI 无法仅从公共源还原；发布到业务项目可访问的源后，日常开发不需要相邻 guidelines 目录。

以后先在 guidelines 修改、验证并发布新包，再更新 `Directory.Packages.props` 中的版本，执行还原和构建。回退使用此前的包版本。编辑器通用设置变化时，执行同步目标更新包内 `.editorconfig` 模板。

包内 Global AnalyzerConfig 管理 C# 检测规则，本地 EditorConfig 同名配置优先；保留有意的项目例外即可。两仓库根 EditorConfig 已移除重复的 C# 规则，内容仍保持一致。配置优先级与包分发方式见 [Microsoft 文档](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files)。

<a name="contribution"></a>
## 贡献

请不要在项目的 **I**ssues 中提交询问(**Q**uestion)以及咨询讨论，**I**ssue 是用来报告问题(**B**ug)和功能特性(**F**eature)。如果你希望参与贡献，欢迎提交 代码合并请求(_[**P**ull**R**equest](https://github.com/Zongsoft/framework/pulls)_) 或问题反馈(_[**I**ssue](https://github.com/Zongsoft/framework/issues)_)。

对于新功能，请务必创建一个功能反馈(_[**I**ssue](https://github.com/Zongsoft/framework/issues)_)来详细描述你的建议，以便我们进行充分讨论，这也将使我们更好的协调工作防止重复开发，并帮助你调整建议或需求，使之成功地被接受到项目中。

欢迎你为我们的开源项目撰写文章进行推广，如果需要我们在官网(_[http://zongsoft.com/blog](http://zongsoft.com/blog)_) 中转发你的文章、博客、视频等可通过 [**电子邮件**](mailto:zongsoft@qq.com) 联系我们。

> 强烈推荐阅读 [《提问的智慧》](https://github.com/ryanhanwu/How-To-Ask-Questions-The-Smart-Way/blob/main/README-zh_CN.md)、[《如何向开源社区提问题》](https://github.com/seajs/seajs/issues/545) 和 [《如何有效地报告 Bug》](http://www.chiark.greenend.org.uk/~sgtatham/bugs-cn.html)、[《如何向开源项目提交无法解答的问题》](https://zhuanlan.zhihu.com/p/25795393)，更好的问题更容易获得帮助。

<a name="sponsor"></a>
## 支持赞助

非常期待您的支持与赞助，可以通过下面几种方式为我们提供必要的资金支持：

1. 关注 **Zongsoft 微信公众号**，对我们的文章进行打赏；
2. 关注 [**Zongsoft 组织账号**](https://github.com/Zongsoft)，向我们捐赠；
3. 如果您的企业需要现场技术支持与辅导，又或者需要特定新功能、即刻的错误修复等请[发邮件](mailto:zongsoft@qq.com)给我。

[![微信公号](https://raw.githubusercontent.com/Zongsoft/guidelines/main/zongsoft-qrcode%28wechat%29.png)](http://weixin.qq.com/r/zy-g_GnEWTQmrS2b93rd)

<a name="license"></a>
## 授权协议

本项目采用 [LGPL](https://opensource.org/licenses/LGPL-2.1) 授权协议。
