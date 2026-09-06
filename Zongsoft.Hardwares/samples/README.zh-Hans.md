# 硬件清单样例

[English](README.md) | [简体中文](README.zh-Hans.md)

## 用途

这个只读控制台程序演示 [HardwareCollector 与 HardwareProfile](../README.zh-Hans.md)：打印汇总指纹并展开每个设备，不安装驱动，也不控制硬件。

## 前置条件与运行

需要 .NET 10 SDK，可在 Windows、Linux 或 macOS 上运行。结果取决于系统设施、权限以及是否运行于容器中。Debug 引用依赖本地构建的 Core DLL。在 framework 根目录执行：

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -f net10.0
dotnet run --project Zongsoft.Hardwares/samples/Zongsoft.Hardwares.Samples.csproj
```

请使用交互终端，程序最后会调用 `Console.ReadKey()`。

## 关键代码与预期结果

[Program.cs](Program.cs) 调用 `HardwareCollector.Instance.Collect()`，构造 `Zongsoft.IO.Hardwares.HardwareProfile`，打印 `profile.Identifier`，再对每个设备执行 `CommandOutletDumper.Dump`。

预期输出是指纹字符串及层级硬件属性，不保证固定的设备数量或顺序。固件信息缺失、虚拟机间标识不同，本身不代表采集失败，应比较实际字段及平台权限。

## 清理与隐私

按任意键退出即可，样例没有需要删除的应用数据文件。若重定向了输出，妥善保护并在不再需要时仅删除该输出文件。

🚨 输出可能包含序列号和网络标识，不要将未脱敏清单贴入 Issue 或 CI 日志。硬件指纹不是可信身份验证凭据。
