# PostgreSQL 表结构

启用存储器前执行 `schema.sql`。脚本使用内置 `C` 排序规则进行序数键值和主题比较，使用 `timestamptz` 保存 UTC 时间点，并以 `bytea` 保存负载。建议使用 PostgreSQL 12 或更高版本。

应用账号只需拥有 `Messaging_Message` 表的数据读写权限。
