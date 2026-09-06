# 使用说明

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 测试场景

### 测试被监视对象生命周期

* 启动应用后，以每秒一次的频率执行 `info S5` 命令，大约 `10` 秒后，确保名为 `S5` 的被监视对象因为失活 _(`Inactived`)_ 而被剔除出监视器。
	> 该命令会调用监视器的索引器，以获取指定名称的被监视对象，通过执行该命令确保监视器的索引器不会造成被监视对象的生命周期被顺延。

* 接下来，以每秒一次的频率执行 `info` 命令，大约 `20` 秒后，确保名为 `S3` 和 `S4` 这两个被监视对象因为失活 _(`Inactived`)_ 而被剔除出监视器。
	> 该命令会调用监视器的遍历枚举，以获取所有被监视对象，通过执行该命令确保监视器的遍历不会造成被监视对象的生命周期被顺延。
	> 持续该操作，大约 `30` 秒后，确保 `S1` 和 `S2` 这两个被监视对象因为失活 _(`Inactived`)_ 而被剔除出监视器。

* 执行 `create key --lifecycle:5s` 命令，等待 `5` 秒后，确保名为 `key` 的被监视对象因为失活 _(`Inactived`)_ 而被剔除出监视器。
* 执行 `create key --lifecycle:5s | open key` 命令，然后等待超过 `5` 秒后，确保名为 `key` 的被监视对象没有被剔除出监视器。
	> 通过不断执行 `info` 命令，来观察名为 `key` 的被监视对象的状态为 `Running | Observed` 且时间戳至少以 `2` 秒为单位进行更新。

### 测试被监视对象的错误失败

#### 可失败

1. 执行 `create key --lifecycle:5s | open key` 命令，确保名为 `key` 的被监视对象被监视；
2. 执行 `error key --round:5` 命令后，确保名为 `key` 的被监视对象因为失败 _(`Failed`)_ 而被剔除出监视器。

#### 不可失败

1. 执行 `create key --lifecycle:1h --errors:-1 | open key` 命令，确保名为 `key` 的被监视对象被监视；
2. 执行 `error key --round:10 | info key` 命令后，确保名为 `key` 的被监视对象不会因为失败 _(`Failed`)_ 而被剔除出监视器。

### 测试手动取消监视

1. 执行 `create key --lifecycle:1h --errors:-1 | open key` 命令，确保名为 `key` 的被监视对象被监视；
2. 执行 `close key` 命令，确保名为 `key` 的被监视对象被剔除出监视器。

## 前置条件与运行

这是 .NET 10 交互样例，不需要服务器或数据库。在 framework 根目录执行：

```shell
dotnet run --project Zongsoft.Core/samples/superviser/Zongsoft.Samples.Superviser.csproj
```

[Program.cs](Program.cs) 注册终端命令，[MySupervisable.cs](MySupervisable.cs) 通过定时器产生观察值，并向观察者报告错误或完成。

## 活动不等于查看

按名称获取对象或遍历监视器不会报告活动。`open` 开启周期观察，`pause` 暂停，`resume` 恢复，`close` 报告完成。前面的测试正是为了区分这两类操作。

由于扫描与调度，过期时间是近似值。S1–S5 的初始配置请以当前 Program.cs 选项为准，不要把原有耗时叙述视为严格截止时间。

## 清理方式

用 `reset` 清理样例监视对象，或用 `exit` 释放监视器。没有文件或外部资源产生。避免过大的 `error --round`，大量输出可能淹没交互终端。
