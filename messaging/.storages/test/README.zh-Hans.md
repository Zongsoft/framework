# 数据库消息存储集成测试

[English](README.md) | [简体中文](README.zh-Hans.md)

SQLite 契约测试默认使用临时文件数据库运行。

设置 `ZONGSOFT_MESSAGING_DATABASE_TESTS=1` 可启用 MySQL、PostgreSQL 和 SQL Server 集成测试。可通过以下环境变量覆盖连接字符串：

- `ZONGSOFT_MESSAGING_DATABASE_MYSQL_CONNECTION_STRING`
- `ZONGSOFT_MESSAGING_DATABASE_POSTGRESQL_CONNECTION_STRING`
- `ZONGSOFT_MESSAGING_DATABASE_MSSQL_CONNECTION_STRING`

MySQL 和 PostgreSQL 的 Podman 资源提供默认本地服务。启用集成测试前，先运行 `Zongsoft.Messaging.Storages.Data-pod.start.cmd` 并等待两个服务器就绪。测试夹具会应用对应的 `database/*/schema.sql`；它只清理随机生成的消息命名空间，不会删除共享表。

## 运行默认套件

使用仓库要求的 SDK，并按项目引用需要准备 Debug 依赖。在 framework 根目录执行：

```shell
dotnet test messaging/.storages/test/Zongsoft.Messaging.Storages.Data.Tests.csproj -f net10.0
```

保持外部测试开关未设置，即只运行 SQLite 路径；不要为了默认套件启动容器。

## 夹具与预期结果

[DataMessageStorageTests.cs](DataMessageStorageTests.cs) 检查快照独立性、null 与空载荷、完整替换、主题序数匹配、过期过滤、分区冻结、取消、同标识并发及 IDataAccess 所有权。工厂测试验证同名连接查找，SQLiteDatabaseFixture 创建文件并应用表结构。

通过套件验证的是存储契约，不是 Broker 重放或业务恰好一次投递。外部用例为条件执行，未启用时不能记为已执行。

## 清理与安全

SQLite 夹具拥有临时文件，外部夹具只删除其随机命名空间，不删除共享表。失败测试残留数据时应先确认夹具命名空间再清理。即使建表可重复执行，结构创建仍会修改所选数据库，应使用专用测试库。

🚨 外部连接覆盖值可能指向真实服务器。设置启用开关前确认端点及数据库，不要把连接秘密贴入测试报告。
