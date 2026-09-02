# SQL Server schema

Run `schema.sql` before enabling the storage. SQL Server 2016 or later is required for the nonclustered index-key size used by the binary-collated namespace and identifiers. The primary key is nonclustered; the namespace-expiration index is clustered.

All timestamps are UTC `datetime2(6)` values. Application accounts do not require DDL permissions.
