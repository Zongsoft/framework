## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。本项目以 Zongsoft 工作器托管 Microsoft Garnet 服务器，负责配置转换、启停和绑定声明。

## 工作边界

- 保持内嵌 Garnet 服务器生命周期、原生设置和 Zongsoft 工作器契约的边界清晰。
- 不因协议兼容就假设 Garnet 与 Redis 的全部行为相同；只声明代码和测试已支持的能力。
- 启动失败、取消和释放不得遗留已创建的服务器或监听；遵循工作器状态与异常报告机制。
- 插件、选项和部署文件随配置与服务注册同步。
- 连接设置与驱动位于 Configuration；参数转换由配置对象负责，GarnetServer 只选择配置并管理生命周期。辅助成员不因测试而扩大可见性。
- binding 与插件 Setting 必须选择同一配置项；固定 TCP 监听从显式 address（IP:端口）读取，由适配器转换为 SDK 的 bind/port，通告地址不作为监听来源。

## 验证

- 构建 `Zongsoft.Externals.Garnet.slnx` 并先运行本地测试。
- 需要 Garnet 服务的场景先确认测试端点，不默认连接共享或生产实例。
