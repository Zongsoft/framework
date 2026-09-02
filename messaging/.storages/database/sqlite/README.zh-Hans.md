# SQLite 表结构

启用存储器前执行一次 `schema.sql`。脚本可重复执行，并创建使用二进制、区分大小写键值的 `Messaging_Message` 表。时间戳和过期时间均使用 UTC；过期时间为空表示永久消息。

运行时不会自动建表或迁移。对于文件数据库，可在可靠性策略允许时启用 WAL 和 `synchronous=NORMAL`。
