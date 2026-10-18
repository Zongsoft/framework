# 事件交换范例

[English](README.md) | [简体中文](README.zh-Hans.md)

范例演示交互式本地事件激发和基于 ZeroMQ 的远程事件投递。事件调度的契约和边界见[事件调度与环境特性](../../docs/event-dispatching.zh-Hans.md)。

## 交互运行

在 framework 根目录执行：

```powershell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -f net10.0 -p:GeneratePackageOnBuild=false
dotnet build messaging/zero/src/Zongsoft.Messaging.ZeroMQ.csproj -f net10.0 -p:GeneratePackageOnBuild=false
dotnet run --project Zongsoft.Core/samples/eventexchanger/Zongsoft.Samples.EventExchanger.csproj -- --local
```

`--local` 只注册本地事件处理程序，不创建消息队列。输入 `3/5` 会并发激发五次事件，每次包含三项度量，所有处理结束后才输出耗时。`start`、`stop`、`restart` 控制交换器，`reset` 清零计数，`exit` 或输入结束会等待交换器停止并释放范例拥有的资源。

省略 `--local` 时，范例使用本机 ZeroMQ 默认发现端口 `7969`；需先启动本机消息代理，再在两个终端运行范例并输入 `start`。同一事件在源端执行本地处理，并发送给其它订阅者。共享通道借用队列，范例在退出时负责释放自己创建的队列。
