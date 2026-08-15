# Zongsoft.Externals.Opc 范例

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 项目

| 项目 | 用途 |
| --- | --- |
| [server](server) | 启动 OPC UA 服务端，并创建包含文件夹、变量、对象和数组的内存地址空间。 |
| [client](client) | 连接 OPC UA 服务端，演示浏览、读取、写入、订阅和监视。 |

两个项目均面向 .NET 10。服务端使用命令行传入的 OPC UA 端点设置；未附加参数时，客户端连接 `opc.tcp://localhost:4840`。

## 运行

在仓库根目录启动服务端：

```shell
dotnet run --project externals/opc/samples/server/Zongsoft.Externals.Opc.Samples.Server.csproj
```

然后在另一个终端启动客户端：

```shell
dotnet run --project externals/opc/samples/client/Zongsoft.Externals.Opc.Samples.Client.csproj
```

## 服务端命令

服务端会创建示例文件夹、标量变量、数组和一个 `Person` 对象。请使用客户端的 `browse` 命令发现它们实际的 OPC 节点标识。

显示服务状态、运行时间、证书信息和活动通道：

```text
info
```

读取一个或多个节点：

```text
get <节点标识>
get <节点标识-1> <节点标识-2> <节点标识-3>
```

写入标量或数组值，输入值会转换为节点声明的数据类型：

```text
set <标量节点标识> 42
set <数组节点标识> 10 20 30
```

对于数值节点，`--round:<次数>` 会以 100 毫秒间隔重复写入，并在每轮递增输入值：

```text
set --round:10 <数值节点标识> 100
```

使用 `stop` 和 `start` 演示服务端生命周期；`start` 会复用启动服务端进程时传入的端点参数。

## 客户端命令

客户端支持连接管理、浏览、单项与批量读取、写入、订阅、统计和实时值监听。典型执行顺序如下：

```text
connect
browse
subscribe <节点标识-1> <节点标识-2>
info
listen
unsubscribe <订阅者标识>
disconnect
```

全部命令参数、别名、基于文件的批量订阅和输出选项参见[完整客户端说明](client/README.zh-Hans.md)。启用安全模式时，客户端与服务端的证书和身份认证设置必须匹配。
