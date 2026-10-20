---
name: zongsoft-opc
description: 修改或验证 OPC UA 适配器的认证、订阅通知、异步生命周期和证书所有权时使用。
---

# OPC 实现约定

先阅读 [AGENTS.md](AGENTS.md)。公开配置与调用方式见双语 README；本文件记录实现和验证边界。

## 认证与依赖

- SDK 版本由 [项目文件](src/Zongsoft.Externals.Opc.csproj) 统一为 2.0.1；同一进程中的 OPC 组件必须使用兼容依赖。
- 服务端通过 SDK `IdentityRegistry` / `IUserTokenAuthenticator` 等待框架身份验证，传递取消并保留失败，不能在同步认证回调中阻塞等待异步任务。
- `NodeId`、`Variant`、集合及证书转换遵守 SDK 类型契约；SDK 提供的借用证书不由适配器释放，从字节创建的证书由认证身份拥有。
- 客户端按信任库和安全策略校验证书，不无条件接受未受信任的服务端证书。

## 订阅与释放

- SDK 同步通知入口通过订阅者持有的有界队列衔接异步消费者，按顺序等待四参数 `ValueTask` 委托。容量由 `SubscriberOptions.NotificationCapacity` 指定，默认 2048；溢出拒绝新通知并保存故障。
- `FlushAsync` 等待此前接受的通知并报告首个尚未观察的故障；取消刷新等待不能吞掉故障或遗弃通知。三参数 `Action` 消费者只适用于同步本地逻辑。
- `DisposeAsync` 关闭通知接收、等待排空并释放自有订阅，重复调用共享完成或失败。`OpcClient` 的连接、断开及释放均可等待。
- `Spooler` 自身的缓冲不由订阅刷新自动排空；所有者解绑消费者后，先刷新订阅，再根据业务要求显式刷新缓冲器。

## 验证

[AuthenticationAsyncTests](test/AuthenticationAsyncTests.cs) 验证挂起、取消和认证失败。[SubscriberAsyncTest](test/SubscriberAsyncTest.cs) 使用未连接订阅验证顺序、慢消费者、处理故障、容量溢出、取消刷新和并发释放。[SubscriberSpoolingTest](test/SubscriberSpoolingTest.cs) 使用 SDK 通知与单个可等待生产者验证缓冲接纳、刷新和释放，不打开端口或生成证书。

从框架根目录先构建 Core，再构建 OPC。Debug 使用本机 Core 程序集；分别验证三个目标框架：

```powershell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -c Debug -p:GeneratePackageOnBuild=false
dotnet build externals/opc/Zongsoft.Externals.Opc.slnx -c Debug -p:GeneratePackageOnBuild=false
dotnet test externals/opc/test/Zongsoft.Externals.Opc.Tests.csproj -c Debug -f net8.0 --no-build
dotnet test externals/opc/test/Zongsoft.Externals.Opc.Tests.csproj -c Debug -f net9.0 --no-build
dotnet test externals/opc/test/Zongsoft.Externals.Opc.Tests.csproj -c Debug -f net10.0 --no-build
```

离线测试不覆盖服务端采样、网络 Publish、重连或 SDK 自有队列溢出。端到端验证使用隔离的本地服务和测试信任库；设备连接、证书生成与服务启动须遵守仓库操作边界。
