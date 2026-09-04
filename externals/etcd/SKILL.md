---
name: zongsoft-etcd
description: 实现、审查、调试、测试或重构 Zongsoft.Externals.Etcd 插件，包括连接生命周期、KV 命令、CAS 序号、基于租约的分布式锁、Podman 集成和样例；不用于普通 etcd 应用或其它协调服务。
---

# Zongsoft Etcd

开始工作前先阅读 [AGENTS.md](AGENTS.md)、[../AGENTS.md](../AGENTS.md) 和相关 README。本技能适用于 `externals/etcd` 及其在本地 hosting 仓库中的 Podman 清单。

## 架构

- `EtcdService` 是唯一公共门面，拥有一个延迟创建的 `EtcdClient`；激活后冻结 `Namespace`，并且只释放客户端一次。
- 配置实例通过 `EtcdServiceProvider` 解析。它必须提供 `IServiceProvider<ISequence>`、`IServiceProvider<ISequenceBase>` 和 `IServiceProvider<IDistributedLockManager>`。
- 物理键统一添加 `<Namespace>:` 前缀；公共查找结果只移除该前缀。
- README 面向使用者；算法、不变量、风险和维护说明保留在本技能中。

## 序号算法

序号修改使用可重试的 etcd 事务：

1. 读取当前值、Lease ID 和 ModRevision。
2. 使用 `InvariantCulture` 解析数字，使用 `R` 格式化 double。
3. interval 为零时直接返回当前值，不写入。
4. 键不存在时比较 `CreateRevision == 0`，已存在时比较读取到的 `ModRevision`。
5. 比较成功才写入 `current + interval`；失败时撤销尚未使用的租约并重试。

首次返回值为 `seed + interval`。Expiry 只在创建缺失序号时生效；递增保留已有租约；Reset 替换值，并按新设置替换或移除租约。

## 分布式锁算法

不要使用 etcd 的阻塞 Lock 服务，因为 Zongsoft 契约要求非阻塞获取，并在竞争失败时返回可复用但未持有的锁对象。

- 租约 TTL 使用 `ceil(expiry.TotalSeconds)`，最小一秒。
- 获取锁使用事务比较 `CreateRevision == 0`，成功后以租约写入所有权令牌。
- 使用成功事务 Header Revision 作为栅栏令牌；获取失败时栅栏令牌为零。
- 释放时比较存储值与所有权令牌，匹配后才删除键并撤销旧租约。
- 续期先读取值、Revision 和 Lease，授予替代租约，再以相同令牌条件写入；失败时撤销替代租约，成功后撤销旧租约。
- 任意续期失败都视为失去所有权。只有 `DistributedLockOptions.RenewalInterval` 为正且小于 Expiry 时才自动续期。

旧持有者绝不能删除或续期继任者的锁；不得弱化令牌和 Revision 比较。

## KV 与命令

- 字符串按 UTF-8 保存，只有正 Expiry 才附加租约。
- `Find` 使用 Prefix Range，`Count` 使用仅计数 Range。
- 插件中的命令声明与具体命令类型保持同步。
- 不声称实现 `IDistributedCache`；本项目只提供基础 KV、序号和协调原语。

## 测试

- 若存在 `.testagent/research.md` 和 `.testagent/plan.md`，先核对其结论是否仍符合当前源码。
- 网络无关验证默认运行；集成测试用 `ZONGSOFT_ETCD_TESTS=1` 显式开启，并先探测 `127.0.0.1:2379`。
- 使用唯一 Namespace，禁用集成测试并行。
- 覆盖并发递增、租约过期、所有权不匹配、手动/自动续期、竞争、取消和严格递增的栅栏令牌。
- 构建继承的全部目标框架，只在 Podman 服务就绪后运行 net10.0 集成套件。

## Podman

本地清单设计为临时环境：`D:\Zongsoft\hosting\zongsoft.pod-etcd.yaml` 暴露 2379，且没有持久卷。修改相关集成说明时检查 hosting 仓库的 `zongsoft.pod(start).cmd` 和 `zongsoft.pod(stop).cmd` 中 etcd 选项是否同步；未经明确要求不运行这些脚本。
