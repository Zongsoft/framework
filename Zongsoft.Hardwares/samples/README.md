# Zongsoft.Hardwares Samples

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

This sample collects hardware information from the current machine with `HardwareCollector`, creates a `HardwareProfile`, prints its stable profile identifier, and dumps every detected hardware entry to the terminal.

## Run

The project targets .NET 10. Run it from the repository root:

```shell
dotnet run --project Zongsoft.Hardwares/samples/Zongsoft.Hardwares.Samples.csproj
```

The output starts with the profile identifier and then lists the available hardware properties. Results vary by operating system, runtime permissions, virtualization, and the hardware exposed to the process. Press any key after reviewing the output to exit.

When comparing two runs, use the profile identifier for a quick identity check and inspect individual entries to diagnose which reported component changed.
