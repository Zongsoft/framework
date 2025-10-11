# Zongsoft Framework

[English](README.md) | [简体中文](README-zh.md)

[![Contributor Covenant](https://img.shields.io/badge/Contributor%20Covenant-2.1-4baaaa.svg)](CODE_OF_CONDUCT.md)

-----

This is a collection of open source projects for the _**Z**ongsoft_ development framework, supporting _**.NET**_ `6`, `7`, `8`, `9`, and other versions.
The ecosystem of pluggable applications is a big strength of _**Z**ongsoft_, help us [build it](CONTRIBUTING.md)!

## Projects

- [_**Z**ongsoft.**C**ore_](Zongsoft.Core) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Core)](https://nuget.org/packages/Zongsoft.Core)
	> Includes shared _interfaces_, _classes_, _enumerations_, etc., providing the necessary core functionality for the _**Z**ongsoft_ development framework.
- [_**Z**ongsoft.**D**ata_](Zongsoft.Data) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data)](https://nuget.org/packages/Zongsoft.Data)
	> An _**ORM**_ data engine that provides **G**raph**QL**-like functionality, with [_drivers_](Zongsoft.Data/drivers/) including:
	> - [_mssql_](Zongsoft.Data/drivers/mssql/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.MsSql)](https://nuget.org/packages/Zongsoft.Data.MsSql)
	> _**M**icrosoft **SQL** **S**erver_ Driver
	> - [_mysql_](Zongsoft.Data/drivers/mysql/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.MySql)](https://nuget.org/packages/Zongsoft.Data.MySql)
	> _**M**y**SQL**_/_**M**aria**DB**_ Driver
	> - [_sqlite_](Zongsoft.Data/drivers/sqlite/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.SQLite)](https://nuget.org/packages/Zongsoft.Data.SQLite)
	> _**SQL**ite_ Driver
	> - [_postgres_](Zongsoft.Data/drivers/postgres/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.Postgres)](https://nuget.org/packages/Zongsoft.Data.Postgres)
	> _**P**ostgre**SQL**_ Driver
	> - [_influxdb_](Zongsoft.Data/drivers/influx/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.Influx)](https://nuget.org/packages/Zongsoft.Data.Influx)
	> _**I**nflux**DB**_ Driver
	> - [_tdengine_](Zongsoft.Data/drivers/tdengine/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.TDengine)](https://nuget.org/packages/Zongsoft.Data.TDengine)
	> _**TD**engine_ Driver
	> - [_clickhouse_](Zongsoft.Data/drivers/clickhouse/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.ClickHouse)](https://nuget.org/packages/Zongsoft.Data.ClickHouse)
	> _**C**lick**H**ouse_ Driver
- [_**Z**ongsoft.**C**ommands_](Zongsoft.Commands) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Commands)](https://nuget.org/packages/Zongsoft.Commands)
	> Provides some commonly used commands, enabling the application layer to execute specific functions via the command line.
- [_**Z**ongsoft.**D**iagnostics_](Zongsoft.Diagnostics) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Diagnostics)](https://nuget.org/packages/Zongsoft.Diagnostics)
	> Provides diagnostic capabilities related to the _**O**pen**T**elemetry_ protocol, including reception and processing of the _**O**pen**T**elemetry_ protocol, as well as exporter plugins such as _**C**onsole_, _**P**rometheus_, _**Z**ipkin_, and more.
- [Zongsoft.Intelligences](Zongsoft.Intelligences) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Intelligences)](https://nuget.org/packages/Zongsoft.Intelligences)
	> Provides a suite of AI functionalities including **L**arge **L**anguage **M**odels, **A**gents, and **R**etrieval **A**ugmented **G**eneration，implemented as plugins based on libraries such as [**M**icrosoft.**E**xtensions.**AI**](https://www.nuget.org/packages/Microsoft.Extensions.AI) and [**M**icrosoft.**A**gents.**AI**](https://www.nuget.org/packages/Microsoft.Agents.AI).
- [_**Z**ongsoft.**N**et_](Zongsoft.Net) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Net)](https://nuget.org/packages/Zongsoft.Net)
	> Provides support for high-performance network communication based on new technologies such as [_**P**ipelines_](https://learn.microsoft.com/zh-cn/dotnet/standard/io/pipelines) and [_**B**uffers_](https://learn.microsoft.com/zh-cn/dotnet/standard/io/buffers).
- [_**Z**ongsoft.**P**lugins_](Zongsoft.Plugins) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Plugins)](https://nuget.org/packages/Zongsoft.Plugins)
	> Provides essential features for plugin application development.
- [_**Z**ongsoft.**P**lugins.**W**eb_](Zongsoft.Plugins.Web) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Plugins.Web)](https://nuget.org/packages/Zongsoft.Plugins.Web)
	> Provides plugin support for the **W**eb applications.
- [_**Z**ongsoft.**R**eporting_](Zongsoft.Reporting) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Reporting)](https://nuget.org/packages/Zongsoft.Reporting)
	> Provides report application development capabilities.
- [_**Z**ongsoft.**S**ecurity_](Zongsoft.Security) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Security)](https://nuget.org/packages/Zongsoft.Security)
	> Provides security-related capabilities, including _authentication_, _authorization_, _password_, _certificates_, etc.
- [_**Z**ongsoft.**W**eb_](Zongsoft.Web) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Web)](https://nuget.org/packages/Zongsoft.Web)
	> Provides general capabilities for the **W**eb application development.
	- [api](Zongsoft.Web/api/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Web.OpenApi)](https://nuget.org/packages/Zongsoft.Web.OpenApi)
		> Provides a pluggable extension for the _**O**pen-**API**_ specification.
	- [grpc](Zongsoft.Web/grpc/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Web.Grpc)](https://nuget.org/packages/Zongsoft.Web.Grpc)
		> Provides a pluggable extension for _gRPC_ on the _ASP.NET_ server side.

- [_messaging_](messaging/)
	- [_kafka_](messaging/kafka/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.Kafka)](https://nuget.org/packages/Zongsoft.Messaging.Kafka)
		> Provides plugin support for the _**K**afka_ message queues.
	- [_rabbit_](messaging/rabbit/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.RabbitMQ)](https://nuget.org/packages/Zongsoft.Messaging.RabbitMQ)
		> Provides plugin support for the _**R**abbitMQ_ message queues.
	- [_mqtt_](messaging/mqtt/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.Mqtt)](https://nuget.org/packages/Zongsoft.Messaging.Mqtt)
		> Provides plugin support for the _**M**qtt_ message queues.
	- [_zero_](messaging/zero/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.ZeroMQ)](https://nuget.org/packages/Zongsoft.Messaging.ZeroMQ)
		> Provides plugin support for the _**Z**eroMQ_ message queues.

- [_externals_](externals/)
	- [_aliyun_](externals/aliyun/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Aliyun)](https://nuget.org/packages/Zongsoft.Externals.Aliyun)
		> Provides plugin support for _**A**libaba Cloud_-related services, implemented based on _**A**libaba Cloud_ _**REST**ful API_ interfaces.
	- [_closedxml_](externals/closedxml/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.ClosedXml)](https://nuget.org/packages/Zongsoft.Externals.ClosedXml)
		> Provides functions such as spreadsheet _(**E**xcel)_ generation, extract, import, export, and template rendering, based on the [**C**losed**X**ml](https://github.com/ClosedXML) open source project's plugin architecture.
	- [_hangfire_](externals/hangfire/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Hangfire)](https://nuget.org/packages/Zongsoft.Externals.Hangfire)
		> Provides time-based task scheduling functionality based on the [**H**angfire](https://www.hangfire.io) open source project's plugin architecture.
	- [_redis_](externals/redis/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Redis)](https://nuget.org/packages/Zongsoft.Externals.Redis)
		> Provides features such as distributed caching, distributed locks, and sequence number generation, based on the plugin architecture of the [**S**tack**E**xchange.**R**edis](https://github.com/StackExchange/StackExchange.Redis) open-source project.
	- [_polly_](externals/polly/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Polly)](https://nuget.org/packages/Zongsoft.Externals.Polly)
		> Provides transient fault resilience handling features such as _**T**imeout_, _**R**etry_, _**F**allback_, _**C**ircuit **B**reaker_, and _**R**ate **L**imiter_, based on the plugin architecture of the [**P**olly](https://www.pollydocs.org) open source project.
	- [_opc_](externals/opc/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Opc)](https://nuget.org/packages/Zongsoft.Externals.Opc)
		> Provides OPC IoT protocol connection, read&write, subscription, and other functions based on the [**OPC** **F**oundation](https://github.com/OPCFoundation/UA-.NETStandard) open source projects.
	- [_lua_](externals/lua/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Lua)](https://nuget.org/packages/Zongsoft.Externals.Lua)
		> Provides [**L**ua](https://lua.org) expression parsing and calculation, script execution, and other functions based on the [**NL**ua](https://github.com/nlua/nlua) open source project's plugin architecture.
	- [_python_](externals/python/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Python)](https://nuget.org/packages/Zongsoft.Externals.Python)
		> Provides [**P**ython](https://python.org) expression parsing and calculation, script execution, and other functions based on the [**I**ron**P**ython](https://ironpython.net) open source project's plugin architecture.
	- [_scriban_](externals/scriban/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Scriban)](https://nuget.org/packages/Zongsoft.Externals.Scriban)
		> Provides [**S**criban](https://github.com/lunet-io/scriban) expression parsing and _calculation_, text template _rendering_, and other functions based on the [**S**criban](https://github.com/scriban/scriban) open source project's plugin architecture.
	- [_wechat_](externals/wechat/) [![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Wechat)](https://nuget.org/packages/Zongsoft.Externals.Wechat)
		> Provides [_WeChat_](https://weixin.qq.com) authentication, [_WeChat **P**ay_](https://pay.weixin.qq.com), [_WeChat **M**edia **P**latform_](https://mp.weixin.qq.com), and other related functions, implemented based on the _WeChat **REST**ful API_ interface.
