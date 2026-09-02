# MySQL 表结构

启用存储器前，使用具备 DDL 权限的部署账号执行 `schema.sql`。表使用 InnoDB、`utf8mb4_bin` 和微秒精度 UTC 时间。要求 MySQL 8.0 或兼容且支持 3072 字节 InnoDB 索引键的服务器。

应用账号只需拥有 `Messaging_Message` 表的 SELECT、INSERT、UPDATE、DELETE 权限。
