---
name: zongsoft-commands
description: 修改或审查 Zongsoft.Commands 的命令实现、终端参数、命令树挂载、输出与交互订阅生命周期时使用；不用于命令解析器底层设计或业务应用自定义命令。
---

# Zongsoft.Commands 实现协作

先阅读 [AGENTS.md](AGENTS.md)；使用者场景、配置与示例见 [README](README.zh-Hans.md)。本技能聚焦实现边界，不替代公开 API 文档。

## 源码路由与调用链

- 命令协议所有者在 [Core Components](../Zongsoft.Core/src/Components)，本包只组合已有服务。先查 `CommandBase<CommandContext>`、选项特性和执行器，再看 [命令清单](src/Zongsoft.Commands.plugin) 的实际路径。
- `src/Configuration` 操作配置；`src/IO` 操作文件系统；`src/Scheduling` 调用调度器；`src/Security` 处理密码、Secret 与 RSA；`src/Messaging` 处理队列。不要因命令入口方便而复制底层实现。
- 执行路径：命令树定位 → 上下文参数/选项转换 → `OnExecuteAsync` → 服务调用 → `context.Result`/输出。结果对象与终端展示不是同一契约，管道调用者需要结果的原始类型。
- 具名命令在插件中挂载；仅添加类不会自动使既有命令路径可用。类型名、构造函数默认名称、插件节点名、帮助资源要一起核对。

## 交互订阅与副作用

[QueueSubscribeCommand](src/Messaging/QueueSubscribeCommand.cs) 使用 `context.ReactiveAsync`：
进入时从祖先 `QueueCommand` 获取队列，按参数建立多个消费者，将消费者集合保存在结果中；退出时逐个 `UnsubscribeAsync`。
参数中的 `:` 或 `?` 分隔 Topic 与过滤表达式，不能随意改为 URI 解析。

- `acknowledgeable` 默认 `true`；这是**命令处理器主动确认**，不是驱动自动确认规则。
- 输出成功后调用 `Message.AcknowledgeAsync`，然后打印已确认标记。确认失败不得仍打印成功。
- 订阅中途失败、退出时令牌已取消、同一终端多消费者并发输出都要单独验证；不能仅测试正常进入/退出。
- 文件删除、保存覆盖、密钥导入/导出、调度与发布会改变外部状态。测试需临时文件或替身服务，不默认执行帮助文本中列出的危险命令。

## 兼容与验证

- 保留短选项、默认值、枚举名称、空输入、返回类型和本地化资源键；不要将服务异常统一转成模糊成功提示。
- `CancellationToken` 应传递到底层异步操作，退出路径必须观察后台任务异常。
- 最小构建目标为 `src/Zongsoft.Commands.csproj`，当前没有专属测试项目。解析器回归定位 Core 测试；命令行为使用内存输出与替身执行上下文验证。
- 离线覆盖参数错误、空管道值、取消、服务缺失、返回值及输出；消息与文件副作用另行明确启用。
