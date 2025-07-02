# Zongsoft Framework

[English](README.md) | [简体中文](README-zh.md)

[![Contributor Covenant](https://img.shields.io/badge/Contributor%20Covenant-2.1-4baaaa.svg)](CODE_OF_CONDUCT.md)

-----

This is a collection of open source projects for the _**Z**ongsoft_ development framework, supporting _**.NET**_ `6`, `7`, `8`, `9`, and other versions.
The ecosystem of pluggable applications is a big strength of _**Z**ongsoft_, help us [build it](CONTRIBUTING.md)!

## Projects

- [_**Z**ongsoft.**C**ore_](Zongsoft.Core)
	> Includes shared _interfaces_, _classes_, _enumerations_, etc., providing the necessary core functionality for the _**Z**ongsoft_ development framework.
- [_**Z**ongsoft.**D**ata_](Zongsoft.Data)
	> An _**ORM**_ data engine that provides **G**raph**QL**-like functionality, with [_drivers_](Zongsoft.Data/drivers/) including:
	> - [_mssql_](Zongsoft.Data/drivers/mssql/)：_**M**icrosoft **SQL** **S**erver_ Driver
	> - [_mysql_](Zongsoft.Data/drivers/mysql/)：_**M**y**SQL**_/_**M**aria**DB**_ Driver
	> - [_sqlite_](Zongsoft.Data/drivers/sqlite/)：_**SQL**ite_ Driver
	> - [_postgres_](Zongsoft.Data/drivers/postgres/)：_**P**ostgre**SQL**_ Driver
	> - [_influxdb_](Zongsoft.Data/drivers/influx/)：_**I**nflux**DB**_ Driver
	> - [_tdengine_](Zongsoft.Data/drivers/tdengine/)：_**TD**engine_ Driver
	> - [_clickhouse_](Zongsoft.Data/drivers/clickhouse/)：_**C**lick**H**ouse_ Driver
- [_**Z**ongsoft.**C**ommands_](Zongsoft.Commands)
	> Provides some commonly used commands, enabling the application layer to execute specific functions via the command line.
- [_**Z**ongsoft.**D**iagnostics_](Zongsoft.Diagnostics)
	> Provides diagnostic capabilities related to the _**O**pen**T**elemetry_ protocol, including support for outputs such as _**P**rometheus_ and _**Z**ipkin_.
- [_**Z**ongsoft.**N**et_](Zongsoft.Net)
	> Provides support for high-performance network communication based on new technologies such as [_**P**ipelines_](https://learn.microsoft.com/zh-cn/dotnet/standard/io/pipelines) and [_**B**uffers_](https://learn.microsoft.com/zh-cn/dotnet/standard/io/buffers).
- [_**Z**ongsoft.**P**lugins_](Zongsoft.Plugins)
	> Provides essential features for plugin application development.
- [_**Z**ongsoft.**P**lugins.**W**eb_](Zongsoft.Plugins.Web)
	> Provides plugin support for the **W**eb applications.
- [_**Z**ongsoft.**R**eporting_](Zongsoft.Reporting)
	> Provides report application development capabilities.
- [_**Z**ongsoft.**S**ecurity_](Zongsoft.Security)
	> Provides security-related capabilities, including _authentication_, _authorization_, _password_, _certificates_, etc.
- [_**Z**ongsoft.**W**eb_](Zongsoft.Web)
	> Provides general capabilities for the **W**eb application development.

- [_messaging_](messaging/)
	- [_kafka_](messaging/kafka/)
		> Provides plugin support for the _**K**afka_ message queues.
	- [_rabbit_](messaging/rabbit/)
		> Provides plugin support for the _**R**abbitMQ_ message queues.
	- [_mqtt_](messaging/mqtt/)
		> Provides plugin support for the _**M**qtt_ message queues.
	- [_zero_](messaging/zero/)
		> Provides plugin support for the _**Z**eroMQ_ message queues.

- [_externals_](externals/)
	- [_aliyun_](externals/aliyun/)
		> Provides plugin support for _**A**libaba Cloud_-related services, implemented based on _**A**libaba Cloud_ _**REST**ful API_ interfaces.
	- [_closedxml_](externals/closedxml/)
		> Provides functions such as spreadsheet _(**E**xcel)_ generation, extract, import, export, and template rendering, based on the [**C**losed**X**ml](https://github.com/ClosedXML) open source project's plugin architecture.
	- [_hangfire_](externals/hangfire/)
		> Provides time-based task scheduling functionality based on the [**H**angfire](https://www.hangfire.io) open source project's plugin architecture.
	- [_redis_](externals/redis/)
		> Provides features such as distributed caching, distributed locks, and sequence number generation, based on the plugin architecture of the [**S**tack**E**xchange.**R**edis](https://github.com/StackExchange/StackExchange.Redis) open-source project.
	- [_polly_](externals/polly/)
		> Provides transient fault resilience handling features such as _**T**imeout_, _**R**etry_, _**F**allback_, _**C**ircuit **B**reaker_, and _**R**ate **L**imiter_, based on the plugin architecture of the [**P**olly](https://www.pollydocs.org) open source project.
	- [_opc_](externals/opc/)
		> Provides OPC IoT protocol connection, read&write, subscription, and other functions based on the [**OPC** **F**oundation](https://github.com/OPCFoundation/UA-.NETStandard) open source projects.
	- [_lua_](externals/lua/)
		> Provides [**L**ua](https://lua.org) expression parsing and calculation, script execution, and other functions based on the [**NL**ua](https://github.com/nlua/nlua) open source project's plugin architecture.
	- [_python_](externals/python/)
		> Provides [**P**ython](https://python.org) expression parsing and calculation, script execution, and other functions based on the [**I**ron**P**ython](https://ironpython.net) open source project's plugin architecture.
	- [_scriban_](externals/scriban/)
		> Provides [**S**criban](https://github.com/lunet-io/scriban) expression parsing and _calculation_, text template _rendering_, and other functions based on the [**S**criban](https://github.com/scriban/scriban) open source project's plugin architecture.
	- [_wechat_](externals/wechat/)
		> Provides [_WeChat_](https://weixin.qq.com) authentication, [_WeChat **P**ay_](https://pay.weixin.qq.com), [_WeChat **M**edia **P**latform_](https://mp.weixin.qq.com), and other related functions, implemented based on the _WeChat **REST**ful API_ interface.
