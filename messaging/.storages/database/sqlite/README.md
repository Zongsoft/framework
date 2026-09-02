# SQLite schema

Run `schema.sql` once before enabling the storage. The script is idempotent and creates `Messaging_Message` with binary, case-sensitive keys. Timestamps and expirations are UTC; a null expiration means the message is permanent.

The implementation does not create or migrate tables at runtime. For file databases, WAL mode and `synchronous=NORMAL` are recommended when the durability policy permits them.
