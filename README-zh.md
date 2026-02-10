# Zongsoft Framework

[![Contributor Covenant](https://img.shields.io/badge/Contributor%20Covenant-2.1-4baaaa.svg)](CODE_OF_CONDUCT-zh.md)

[English](README.md) |
[简体中文](README-zh.md)

-----

这是 _**Z**ongsoft_ 开发框架的开源项目集，支持 _**.NET**_ `8`,`9`,`10` 等版本。
可插拔应用程序生态系统是 _**Z**ongsoft_ 的特点，欢迎与我们[携手共建](CONTRIBUTING-zh.md)。

> 💡 在 `clone` 本项目源码后，需要使用 `git submodule update` 命令来更新 [子模块](.gitmodules)。

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
