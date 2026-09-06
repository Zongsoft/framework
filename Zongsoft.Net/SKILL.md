---
name: zongsoft-net
description: 修改或审查 Zongsoft.Net 的 TcpClient、TcpServer、Channel、Packetizer、缓冲区与收发关闭竞态时使用；不用于消息代理专有线协议或业务 HTTP 客户端。
---

# Zongsoft.Net 实现协作

先阅读 [AGENTS.md](AGENTS.md)；使用者场景、配置与示例见 [README](README.zh-Hans.md)。本技能聚焦实现边界，不替代公开 API 文档。

## 源码与所有权

- [TcpClient](src/TcpClient.cs)、[TcpServer](src/TcpServer.cs) 管理端点与连接；[TcpChannelBase](src/TcpChannelBase.cs) 协调通道收发；Client/ServerChannel 与 ServerChannelManager 处理各自生命周期。
- [Packetizer](src/Packetizer.cs) 定义分帧与缓冲契约。Headed 使用 4 字节大端长度，输出 `ReadOnlySequence<byte>`；Headless 输出 `IMemoryOwner<byte>`。TCP 分段不是消息边界。
- 同步缓冲切片仅在所有者允许的窗口内有效；处理器要跨调用保存数据必须快照。池化内存必须明确由谁释放，不能把已归还内存交给异步后台任务。
- 通用包不理解 Broker 的 topic、ack 或重投；该层扩展应转到 [messaging 技能](../messaging/SKILL.md)。

## 关键流程

- 客户端 `SendAsync` 可惰性连接；`ConnectAsync(EndPoint)` 当前没有取消参数，不能在上层文档承诺令牌能取消建连。
- 服务端启动监听并为每连接建立通道；停止必须与接收循环、通道集合变更、广播并发一起检查。
- `BroadcastAsync` 返回成功发送计数并处理各通道异常；返回值不是远端业务确认。
- `TcpClient.Headed/Headless` 是共享实例；不要在可复用库中随意释放全局实例，独立工作负载优先自行构造并管理客户端。
- framing 改动必须同时核对写端编码与读端消费位置。半个头、跨段正文、连续帧、零长度和异常长度是不同路径。
- 不把同步 socket 调用包装成无界 `Task.Run` 来掩盖阻塞；背压、最大帧长度及累计缓冲量需分别评估。

## 最小验证与风险

- 测试入口为 [test](test)，目标 `test/Zongsoft.Net.Tests.csproj`；先用内存序列测试分帧，再在 loopback 验证连接生命周期。
- 生命周期测试覆盖连接失败、对端半关闭、取消发送/接收、重复停止/释放、并发广播及连接集合变更。
- 检查异常是否被观察、内存租约是否全部归还、退出后是否还有接收任务。
- [samples](samples/README.zh-Hans.md) 用于两个进程冒烟，不是负载或安全证明。不要绑定公网或把示例默认端口视为生产部署约定。
