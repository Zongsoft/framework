# MySQL 表结构

启用存储器前，使用具备 DDL 权限的部署账号执行 `schema.sql`。表使用 InnoDB、`utf8mb4_bin` 和微秒精度 UTC 时间。要求 MySQL 8.0 或兼容且支持 3072 字节 InnoDB 索引键的服务器。

应用账号只需拥有 `Messaging_Message` 表的 SELECT、INSERT、UPDATE、DELETE 权限。


## 表契约

`Messaging_Message` 按 `Namespace` 与 `Identifier` 保存一份可靠消息快照。`Topic` 支持按主题读取和清理；`Identity`、`Tags` 保留消息元数据；`Timestamp` 与可空 `Expiration` 用于排序和过期筛选；`Data` 保存原始载荷字节。

索引支持命名空间以及命名空间/主题的过期扫描。复合键长度依赖 MySQL 8 时代的 InnoDB 限制以及二进制 utf8mb4 排序规则。

## 初始化

1. 备份已有消息数据。
2. 使用可创建表和索引的部署身份执行 [`schema.sql`](schema.sql)。
3. 只向运行身份授予文档所述 DML 权限。
4. 配置匹配的 Zongsoft 数据驱动，以及名称等于 Broker 名称的连接。
5. 启动存储，并用可丢弃消息验证设置、读取、删除与过期行为。

脚本只创建缺失对象；它不是带版本的迁移框架，也不会修改不兼容的已有表。

🚨 Schema 变更应经应用正常迁移流程审查并执行。删除或重建 `Messaging_Message` 会丢失待处理可靠消息，可能造成外部可见的消息丢失或重复。

## 兼容性与运维

迁移时应保持命名空间、标识符和主题的序数、区分大小写比较，时间统一使用 UTC，并原样保留载荷字节。存储会过滤过期行，但没有后台回收器；请调度有界清理并监控表和索引增长。

备份与高可用请参阅 [MySQL 文档](https://dev.mysql.com/doc/)。上级[存储指南](../../README.zh-Hans.md)解释分区标识符和连接解析。
