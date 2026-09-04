# Database message storage integration tests

[English](README.md) | [简体中文](README.zh-Hans.md)

SQLite contract tests run by default against a temporary file database.

Set `ZONGSOFT_MESSAGING_DATABASE_TESTS=1` to enable the MySQL, PostgreSQL and SQL Server integration cases. Override their connections with:

- `ZONGSOFT_MESSAGING_DATABASE_MYSQL_CONNECTION_STRING`
- `ZONGSOFT_MESSAGING_DATABASE_POSTGRESQL_CONNECTION_STRING`
- `ZONGSOFT_MESSAGING_DATABASE_MSSQL_CONNECTION_STRING`

The MySQL and PostgreSQL Podman assets provide the default local services. Run `Zongsoft.Messaging.Storages.Data-pod.start.cmd` and wait for both servers to become ready before enabling the integration tests. The fixture applies the corresponding `database/*/schema.sql`; it only clears its random messaging namespace and never drops the shared table.
