# PostgreSQL schema

Run `schema.sql` before enabling the storage. It uses the built-in `C` collation for ordinal key and topic comparisons, `timestamptz` for UTC instants, and `bytea` for payloads. PostgreSQL 12 or later is recommended.

The application account only needs data-manipulation permissions on `Messaging_Message`.
