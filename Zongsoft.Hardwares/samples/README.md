# Hardware Inventory Sample

[English](README.md) | [简体中文](README.zh-Hans.md)

## Purpose

This read-only console application demonstrates [HardwareCollector and HardwareProfile](../README.md). It prints the combined profile identifier and dumps each collected device; it neither installs drivers nor controls hardware.

## Prerequisites and Run

Use .NET 10 SDK on Windows, Linux or macOS. Results depend on available OS facilities, permissions and whether the process runs in a container. Debug references require the locally built Core DLL. From the framework root:

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -f net10.0
dotnet run --project Zongsoft.Hardwares/samples/Zongsoft.Hardwares.Samples.csproj
```

Use an interactive terminal: the program ends with `Console.ReadKey()`.

## Key Code and Expected Result

[Program.cs](Program.cs) calls `HardwareCollector.Instance.Collect()`, constructs `Zongsoft.IO.Hardwares.HardwareProfile`, prints `profile.Identifier`, and uses `CommandOutletDumper.Dump` for each hardware item.

Expect a profile string followed by hierarchical hardware properties, not a fixed number or order of devices. Missing firmware information and different identifiers across virtual machines are not by themselves collection failures. Compare the actual fields and platform permissions.

## Cleanup and Privacy

Press any key to exit. The sample has no application data files to delete. If you redirected output, protect or delete only that output file when no longer needed.

🚨 Output may contain serial numbers and network identifiers. Do not paste an unredacted dump into issues or CI logs. A hardware fingerprint is not a trustworthy authentication credential.
