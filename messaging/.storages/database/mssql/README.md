# SQL Server schema

Run `schema.sql` before enabling the storage. SQL Server 2016 or later is required for the nonclustered index-key size used by the binary-collated namespace and identifiers. The primary key is nonclustered; the namespace-expiration index is clustered.

All timestamps are UTC `datetime2(6)` values. Application accounts do not require DDL permissions.


## Table Contract

`Messaging_Message` stores one reliable-message snapshot per `Namespace` and `Identifier`. `Topic` supports topic-scoped reads and cleanup; `Identity` and `Tags` preserve message metadata; `Timestamp` and nullable `Expiration` drive ordering and expiry filtering; `Data` stores the payload bytes.

Indexes support namespace-wide and namespace/topic expiry scans. The primary key is nonclustered and the namespace/expiration index is clustered; test index layout against the workload before changing it.

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

For backup and high availability, follow the [SQL Server documentation](https://learn.microsoft.com/sql/). The parent [storage guide](../../README.md) explains partition identifiers and connection resolution.
