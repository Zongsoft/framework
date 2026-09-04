## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。本项目适配 OPC UA 连接、读写、订阅与证书安全。

## 工作边界

- 保持 Session、Subscription、MonitoredItem 和证书对象的生命周期与重连语义明确。
- NodeId、数据类型、状态码和时间戳转换不得丢失协议信息。
- 证书、私钥、密码和信任目录属于敏感资产；测试只使用专用证书，不提交真实材料。
- 回调与订阅分派需要有界并发、取消和异常隔离，关闭后不得继续触发用户处理器。

## 验证

- 构建 `Zongsoft.Externals.Opc.slnx` 并运行网络无关测试。
- 端到端验证使用 samples 的本地 client/server；证书生成或真实设备连接需明确要求。
