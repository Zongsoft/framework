# Zongsoft.Hardwares 插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Hardwares)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Hardwares)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**H**ardwares](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Hardwares) 提供了跨平台获取硬件信息的功能。

它为 Windows、Linux 和 macOS 实现 `Zongsoft.Core` 中的硬件契约，并用网络接口补充平台采集结果。返回值是规范化的层级，应用可直接检查，也可组合成 `HardwareProfile` 标识符。

## 硬件身份的含义

硬件清单只是观测结果，不是可信证明。固件、虚拟化、权限、容器、热插拔设备、系统更新和厂商上报都可能改变或隐藏字段。`HardwareProfile` 提供便捷的规范化指纹，但不是密码学证明，不能作为唯一身份验证因素。

## 安装与采集

先完成[插件化接入](#插件化接入)。以下代码放在已初始化宿主内的应用服务/命令中；消费方引用 Core 契约即可，无需引用或构造具体采集器。需要模块专属实现时，从应用自定义的 `Module.Current.Services` 解析。

```shell
dotnet add package Zongsoft.Core
```

```csharp
using Zongsoft.Services;
using Zongsoft.IO.Hardwares;

var collector = ApplicationContext.Current.Services.ResolveRequired<IHardwareCollector>();
var devices = collector.Collect();
var profile = new HardwareProfile(devices);

Console.WriteLine(profile.Identifier);
foreach(var device in profile)
	Console.WriteLine($"{device.Name}: {device.Code}");
```

`IHardwareCollector` 同时便于替换测试实现。`CollectAsync(cancellationToken)` 以异步流公开相同的平台采集，并在逐项返回间检查取消。Profile 是调用方拥有的值对象；采集器是宿主共享服务。

## 平台行为

采集器在 Windows、Linux 或 macOS 上分派到对应 Gatherer，再追加网络硬件；不支持的系统仍会返回网络部分。平台采集使用操作系统设施以及命令/文件解析，权限或工具缺失时可能得到部分清单，而不是完全一致的 Schema。

`HardwareUtility` 负责规范化标识符和值、解析字节量、读取第一个可用系统文件，以及执行有超时的平台命令；这些规则属于 Profile 稳定性的一部分。

> 💡 建议同时保存 Profile 标识符和足以解释差异的单项属性。把变化当作风险信号或重新登记事件，不要自动判定为恶意行为。

🚨 硬件详情可能属于个人或安全敏感信息。只采集应用所需内容，取得必要同意，限制访问和保留期限，切勿在日志或公开遥测中暴露原始序列号。

## 测试与排查

请在每个目标系统运行[范例](samples/README.zh-Hans.md)，比较原生与容器运行结果、确认所需权限，并预期不同云实例类型返回不同设备集合。单元测试应注入契约实现，不要假设 CI 机器具有稳定标识符。

## 延伸阅读

- [范例应用](samples/README.zh-Hans.md)
- [Linux sysfs 文档](https://docs.kernel.org/filesystems/sysfs.html)
- [实现协作指南](SKILL.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../Zongsoft.Plugins/README.zh-Hans.md)。

服务扫描将单例采集器暴露为 `IHardwareCollector`，应用通过宿主解析该契约即可。加载插件不要求对外发布硬件标识。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Hardwares` | [Zongsoft.Hardwares.plugin](src/Zongsoft.Hardwares.plugin) |
| 文件复制及依赖 | [Zongsoft.Hardwares.deploy](src/Zongsoft.Hardwares.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft hardwares]
nuget:Zongsoft.Hardwares
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Hardwares.option`、`Zongsoft.Hardwares.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。
