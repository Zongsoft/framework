# MySQL schema

Run `schema.sql` with a DDL-capable deployment account before enabling the storage. The table uses InnoDB, `utf8mb4_bin`, and microsecond UTC timestamps. MySQL 8.0 or a compatible server with a 3072-byte InnoDB index-key limit is required.

Application accounts only need SELECT, INSERT, UPDATE, and DELETE permissions on `Messaging_Message`.


## Table Contract

`Messaging_Message` stores one reliable-message snapshot per `Namespace` and `Identifier`. `Topic` supports topic-scoped reads and cleanup; `Identity` and `Tags` preserve message metadata; `Timestamp` and nullable `Expiration` drive ordering and expiry filtering; `Data` stores the payload bytes.

Indexes support namespace-wide and namespace/topic expiry scans. The composite key sizes rely on MySQL 8-era InnoDB limits and binary utf8mb4 collation.

## Initialization

1. Back up any existing messaging data.
2. Run [`schema.sql`](schema.sql) with a deployment identity that can create tables and indexes.
3. Grant the runtime identity only the documented DML permissions.
4. Configure the matching Zongsoft data driver and a connection whose name equals the broker name.
5. Start the storage and verify set/get/remove/expiry behavior with a disposable message.

The script creates missing objects; it is not a versioned migration framework and does not alter an incompatible existing table.

🚨 Review and apply schema changes through the application's normal migration process. Dropping or recreating `Messaging_Message` loses pending reliable messages and can cause externally visible message loss or duplication.

## Compatibility and Operations

Keep namespace, identifier, and topic comparisons ordinal and case-sensitive across migrations. Store timestamps as UTC and preserve payload bytes exactly. Expired rows are filtered by the storage but are not reclaimed by a background worker; schedule bounded cleanup and monitor table/index growth.

For backup and high availability, follow the [MySQL documentation](https://dev.mysql.com/doc/). The parent [storage guide](../../README.md) explains partition identifiers and connection resolution.
