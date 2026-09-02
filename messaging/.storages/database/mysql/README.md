# MySQL schema

Run `schema.sql` with a DDL-capable deployment account before enabling the storage. The table uses InnoDB, `utf8mb4_bin`, and microsecond UTC timestamps. MySQL 8.0 or a compatible server with a 3072-byte InnoDB index-key limit is required.

Application accounts only need SELECT, INSERT, UPDATE, and DELETE permissions on `Messaging_Message`.
