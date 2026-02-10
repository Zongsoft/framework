# OPC.UA 客户端范例

[English](README.md) |
[简体中文](README-zh_CN.md)

-----

### 连接命令 `connect`

连接到指定的 OPC.UA 服务器，如果未指定参数则默认为本机。

- 以匿名用户连接到指定的服务器：

```bash
connect 'opc.tcp://192.168.2.74:49320'
```

- 以指定用户连接到指定的服务器：
```bash
connect 'server=opc.tcp://192.168.2.74:49320;username=admin;password=xxxxxx'
```

- 以指定证书文件连接到指定的服务器：
```bash
connect 'server=opc.tcp://192.168.2.74:49320;Certificate=zfs.local:./certificates/certificate.pfx;CertificateSecret=xxxxxx'
```

### 断开连接 `disconnect`

断开当前连接，无参数。

```bash
disconnect
```

### 显示信息 `info`

显示当前客户端信息，包括：

- 客户端名称
- 当前连接状态
- 最后心跳时间
- 连接设置
- 订阅者信息
	- 订阅者标识
	- 订阅者状态
	- 订阅者统计
	- 订阅指标集
	- 内部对象信息(需要打开 `detailed` 选项)

- 显示所有订阅者信息：

```bash
info
```

- 显示指定订阅者信息，通过参数指定要显示的订阅者编号：

```bash
info 11 12 13
```

### 重置 `reset`

重置订阅者的统计信息。

- 重置所有订阅统计信息：

```bash
reset
```

- 重置指定订阅者的统计信息，通过参数指定一个或多个 _(使用空格分隔)_ 订阅者编号：

```bash
reset 11 12 13
```

> 注：订阅者编号可通过 `info` 命令查看。


### 订阅 `subscribe`

订阅指定的指标数据，该命令别名为：`sub`。

- 订阅指定的指标，一个或多个 _(使用空格分隔)_：

```bash
subscribe ns=2;s=variable1 ns=2;i=1001 ns=2;g=E9DBF5F2-0AAB-49C0-AEE4-E1251A2CDCEA
```

- 在指定的订阅者上添加新的订阅项：

```bash
subscribe -subscriber:11 ns=2;s=variable1 ns=2;i=1001 ns=2;g=E9DBF5F2-0AAB-49C0-AEE4-E1251A2CDCEA
```

- 从文件中获取订阅指标进行大批量订阅：

```bash
subscribe -directory
subscribe -directory        filename1.txt filename2.txt filenameN.txt
subscribe -directory:subdir filename1.txt filename2.txt filenameN.txt
```

> - 如果上述命令未指定 `directory` 选项，则该选项值默认为：`subscription`；
> - 如果上述命令未指定参数（即文件名），则默认加载该子目录中的所有 `.txt` 文件。

### 取消订阅 `unsubscribe`

取消订阅，该命令别名为：`unsub`。

- 取消指定的订阅者，通过参数指定一个或多个 _(使用空格分隔)_ 订阅者编号：

```bash
unsubscribe 11 12 13
```

> 注：订阅者编号可通过 `info` 命令查看。

- 取消所有订阅：

```bash
unsubscribe
```

### 监听 `listen`

监听已经订阅的指标数据，进入监听模式后，可同时按压 `Ctrl` 和字母 `C` 键退出监听模式。
该命令可通过参数指定要监听的订阅者编号，如果未指定参数则监听所有订阅信息。

通过 `spooling` 选项开启缓冲监听模式，缓冲模式还支持如下选项进行相关的缓冲设置：

> - limit 选项：表示缓冲数量限制，默认值为 `1000`；
> - period 选项：表示缓冲周期时长 _(毫秒)_，默认值为 `1000`；
> - distinct 选项：表示是否启用去重处理，默认不去重。

- 监听

> 注意：普通监听可能会因为订阅的数量过多而无法跟上数据的刷新频率，而导致积压。

```bash
listen
```

- 缓冲监听

```bash
listen -spooling -limit:10000
listen -spooling -limit:10000 -distinct
```
