# Database message storage integration tests

[English](README.md) | [简体中文](README.zh-Hans.md)

SQLite contract tests run by default against a temporary file database.

Set `ZONGSOFT_MESSAGING_DATABASE_TESTS=1` to enable the MySQL, PostgreSQL and SQL Server integration cases. Override their connections with:

- `ZONGSOFT_MESSAGING_DATABASE_MYSQL_CONNECTION_STRING`
- `ZONGSOFT_MESSAGING_DATABASE_POSTGRESQL_CONNECTION_STRING`
- `ZONGSOFT_MESSAGING_DATABASE_MSSQL_CONNECTION_STRING`

The MySQL and PostgreSQL Podman assets provide the default local services. Run `Zongsoft.Messaging.Storages.Data-pod.start.cmd` and wait for both servers to become ready before enabling the integration tests. The fixture applies the corresponding `database/*/schema.sql`; it only clears its random messaging namespace and never drops the shared table.

## Run the Default Suite

Use the repository's SDK and build the referenced Debug dependencies if required by their project files. From the framework root:

```shell
dotnet test messaging/.storages/test/Zongsoft.Messaging.Storages.Data.Tests.csproj -f net10.0
```

Leave the external-test switch unset for SQLite-only execution. Do not start containers just to run this default suite.

## Fixtures and Expected Result

[DataMessageStorageTests.cs](DataMessageStorageTests.cs) checks snapshot independence, null versus empty payloads, complete replacement, ordinal topics, expiry filtering, partition freezing, cancellation, same-ID concurrency and IDataAccess ownership. Factory tests verify exact-name connection lookup; SQLiteDatabaseFixture creates the file and applies schema.

A passing suite validates the storage contract, not broker replay or business-level exactly-once delivery. External cases are conditional and must not be counted as executed when disabled.

## Cleanup and Safety

The SQLite fixture owns its temporary file; external fixtures delete only their randomized namespace. They do not drop shared tables. If a failed test leaves data, inspect the fixture's namespace before removing anything. Schema creation still changes the selected database, so use a dedicated test database even when table creation is idempotent.

🚨 External connection overrides can target a real server. Inspect the endpoint and database before setting the opt-in switch; never paste connection secrets into test reports.
