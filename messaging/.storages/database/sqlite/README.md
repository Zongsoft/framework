# SQLite schema

Run `schema.sql` once before enabling the storage. The script is idempotent and creates `Messaging_Message` with binary, case-sensitive keys. Timestamps and expirations are UTC; a null expiration means the message is permanent.

The implementation does not create or migrate tables at runtime. For file databases, WAL mode and `synchronous=NORMAL` are recommended when the durability policy permits them.


## Table Contract

`Messaging_Message` stores one reliable-message snapshot per `Namespace` and `Identifier`. `Topic` supports topic-scoped reads and cleanup; `Identity` and `Tags` preserve message metadata; `Timestamp` and nullable `Expiration` drive ordering and expiry filtering; `Data` stores the payload bytes.

Indexes support namespace-wide and namespace/topic expiry scans. The `WITHOUT ROWID` table uses binary text keys; file permissions, backup, locking, and journal settings belong to the host.

## Initialization

1. Back up any existing messaging data.
2. Run [`schema.sql`](schema.sql) with a deployment identity that can create tables and indexes.
3. Give the runtime process minimal filesystem permissions on the database file and its directory; SQLite has no separate server accounts or GRANT-style DML authorization.
4. Configure the matching Zongsoft data driver and a connection whose name equals the broker name.
5. Start the storage and verify set/get/remove/expiry behavior with a disposable message.

The script creates missing objects; it is not a versioned migration framework and does not alter an incompatible existing table.

🚨 Review and apply schema changes through the application's normal migration process. Dropping or recreating `Messaging_Message` loses pending reliable messages and can cause externally visible message loss or duplication.

## Compatibility and Operations

Keep namespace, identifier, and topic comparisons ordinal and case-sensitive across migrations. Store timestamps as UTC and preserve payload bytes exactly. Expired rows are filtered by the storage but are not reclaimed by a background worker; schedule bounded cleanup and monitor table/index growth.

For backup and high availability, follow the [SQLite documentation](https://sqlite.org/docs.html). The parent [storage guide](../../README.md) explains partition identifiers and connection resolution.
