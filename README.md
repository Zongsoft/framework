# Zongsoft Framework

这是 _**Z**ongsoft_ 开发框架的开源项目集，支持 _**.NET**_ `6`,`7`,`8` 等版本。

## 项目列表

- [Zongsoft.Core](Zongsoft.Core)
	> 包含公共接口、基类、枚举等，为 _**Z**ongsoft_ 开发框架提供了必要的核心功能集。
- [Zongsoft.Data](Zongsoft.Data)
	> 提供类 **G**raph**QL** 功能的 _**ORM**_ 数据引擎，其下 [_drivers_](Zongsoft.Data/drivers/) 包括：
	> - [mssql](Zongsoft.Data/drivers/mssql/)：_**M**icrosoft **SQL** **S**erver_ 驱动
	> - [mysql](Zongsoft.Data/drivers/mysql/)：_**M**y**SQL**_/_**M**aria**DB**_ 驱动
	> - [sqlite](Zongsoft.Data/drivers/sqlite/)：_**SQL**ite_ 驱动
	> - [postgres](Zongsoft.Data/drivers/postgres/)：_**P**ostgre**SQL**_ 驱动
	> - [tdengine](Zongsoft.Data/drivers/tdengine/)：_**TD**engine_ 驱动
	> - [clickhouse](Zongsoft.Data/drivers/clickhouse/)：_**C**lick**H**ouse_ 驱动
- [Zongsoft.Commands](Zongsoft.Commands)
	> 提供了一些常用的命令，为应用层提供以命令行方式执行特定功能的能力。
- [Zongsoft.Messaging.Kafka](Zongsoft.Messaging.Kafka)
	> 提供了 _**K**afka_ 消息队列的插件化支持。
- [Zongsoft.Messaging.Mqtt](Zongsoft.Messaging.Mqtt)
	> 提供了 _**M**qtt_ 消息队列的插件化支持。
- [Zongsoft.Net](Zongsoft.Net)
	> 提供了高性能网络通讯相关的支持，基于 [_**P**ipelines_](https://learn.microsoft.com/zh-cn/dotnet/standard/io/pipelines)、[_**B**uffers_](https://learn.microsoft.com/zh-cn/dotnet/standard/io/buffers) 等新式技术。
- [Zongsoft.Plugins](Zongsoft.Plugins)
	> 提供了插件化应用开发的核心功能。
- [Zongsoft.Plugins.Web](Zongsoft.Plugins.Web)
	> 提供了 **W**eb 应用的插件化支持。
- [Zongsoft.Reporting](Zongsoft.Reporting)
	> 提供了报表相关的核心功能定义。
- [Zongsoft.Security](Zongsoft.Security)
	> 提供了安全(身份验证、授权控制)相关的核心功能。
- [Zongsoft.Web](Zongsoft.Web)
	> 提供了 **W**eb 应用中安全相关的插件化支持。

- [externals](externals/)
	- [aliyun](externals/aliyun)
		> 提供了 _阿里云_ 相关服务的插件化支持，基于阿里云 HTTP API 接口实现。
	- [closedxml](externals/closedxml)
		> 提供了电子表格 _(**E**xcel)_ 生成、导入、导出、模板渲染等功能，基于 [**C**losed**X**ml](https://github.com/ClosedXML) 开源项目的插件化。
	- [hangfire](externals/hangfire)
		> 提供了时间任务调度相关功能，基于 [**H**angfire](https://www.hangfire.io) 开源项目的插件化。
	- [redis](externals/redis)
		> 提供了分布式缓存、分布式锁、序列号生成等功能，基于 [**S**tack**E**xchange.**R**edis](https://github.com/StackExchange/StackExchange.Redis) 开源项目的插件化。
	- [lua](externals/lua)
		> 提供了 [**L**ua](https://lua.org) 表达式解析计算、脚本执行等功能，基于 [**NL**ua](https://github.com/nlua/nlua) 开源项目的插件化。
	- [python](externals/python)
		> 提供了 [**P**ython](https://python.org) 表达式解析计算、脚本执行等功能，基于 [**I**ronPython](https://ironpython.net) 开源项目的插件化。
	- [scriban](externals/scriban)
		> 提供了 [**S**criban](https://github.com/lunet-io/scriban) 表达式解析计算、_文本模板渲染_ 等功能，基于 [**S**criban](https://github.com/scriban/scriban) 开源项目的插件化。
	- [wechat](externals/wechat)
		> 提供了微信认证、微信支付、微信公众号等相关功能，基于微信 HTTP API 接口实现。
