# 数据库消息存储集成测试

[English](README.md) | [简体中文](README.zh-Hans.md)

SQLite 契约测试默认使用临时文件数据库运行。

设置 `ZONGSOFT_MESSAGING_DATABASE_TESTS=1` 可启用 MySQL、PostgreSQL 和 SQL Server 集成测试。可通过以下环境变量覆盖连接字符串：

- `ZONGSOFT_MESSAGING_DATABASE_MYSQL_CONNECTION_STRING`
- `ZONGSOFT_MESSAGING_DATABASE_POSTGRESQL_CONNECTION_STRING`
- `ZONGSOFT_MESSAGING_DATABASE_MSSQL_CONNECTION_STRING`

MySQL 和 PostgreSQL 的 Podman 资源提供默认本地服务。启用集成测试前，先运行 `Zongsoft.Messaging.Storages.Data-pod.start.cmd` 并等待两个服务器就绪。测试夹具会应用对应的 `database/*/schema.sql`；它只清理随机生成的消息命名空间，不会删除共享表。
