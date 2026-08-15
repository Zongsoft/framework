# Zongsoft.Hardwares 范例

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

本范例通过 `HardwareCollector` 收集当前计算机的硬件信息，创建 `HardwareProfile`，输出稳定的配置标识，并将检测到的每项硬件数据转储到终端。

## 运行

项目面向 .NET 10，请在仓库根目录运行：

```shell
dotnet run --project Zongsoft.Hardwares/samples/Zongsoft.Hardwares.Samples.csproj
```

输出首先显示硬件配置标识，随后列出可用的硬件属性。结果会因操作系统、运行权限、虚拟化环境及进程可见的硬件而异。查看完成后按任意键退出。

比较两次运行结果时，可先使用配置标识快速判断是否一致，再检查各硬件条目以定位发生变化的组件。
