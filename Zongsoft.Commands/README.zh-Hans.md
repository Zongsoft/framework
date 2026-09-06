# Zongsoft.Commands 命令插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Commands)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Commands)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**C**ommands](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Commands) 为 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 命令框架提供一组常用终端命令，适合应用诊断、维护脚本、交互式管理，以及在不编写专用界面的情况下测试框架服务。

本包通过 [`Zongsoft.Commands.plugin`](src/Zongsoft.Commands.plugin) 挂载命令。插件加载后，这些命令会出现在 `/Workbench/Executor/Commands` 下，可从 Zongsoft 终端宿主调用，也可通过 `CommandExecutor` 在程序中执行。

## 命令模型

[命令](../Zongsoft.Core/src/Components/ICommand.cs)通过命令上下文接收解析后的表达式，并异步返回结果。框架将命令执行分为四个部分：

- **命令树**：以 `File Info`、`Messaging Queue Produce` 这样的层级名称选择命令节点。
- **参数与选项**：位置参数承载输入值，命名选项使用 `--algorithm:SHA256` 等形式。
- **管道值**：前一条命令的结果可以成为后一条命令的输入值。
- **输出**：命令既可返回对象，也可通过 `ICommandOutlet` 输出格式化内容。

底层公共抽象参见 [`CommandExecutor`](../Zongsoft.Core/src/Components/CommandExecutor.cs)、[`CommandContext`](../Zongsoft.Core/src/Components/CommandContext.cs) 和 [`CommandLine`](../Zongsoft.Core/src/Components/CommandLine.cs)。

## 安装与部署

```shell
dotnet add package Zongsoft.Commands
```

对于插件式应用，应部署包内附属文件，确保 `Zongsoft.Commands.plugin` 位于宿主的 `plugins` 目录中。该清单依赖框架主插件，并会自动注册命令树。

> 💡 最方便的交互入口是 [Zongsoft 终端宿主](https://github.com/Zongsoft/hosting/tree/main/terminal)。启动后执行 `help`，即可查看当前应用实际加载的命令。

## 快速入门

```text
help
range --type:int --min:1 --max:5
checksum --algorithm:SHA256 "hello"
file.info ./appsettings.json
directory.list ./plugins
```

插件初始化后，应用代码也可以调用同一棵命令树：

```csharp
using Zongsoft.Components;

using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var checksum = await CommandExecutor.Default.ExecuteAsync(
	"checksum --algorithm:SHA256 hello",
	cancellation: cancellation.Token);
```

> 🚨 `CommandExecutor.Default` 只包含当前宿主已注册的节点。仅添加包引用并不会加载 `Zongsoft.Commands.plugin`；找不到命令时应先检查部署结果。

## 内置命令

| 分组 | 命令 | 用途 |
| --- | --- | --- |
| 通用 | `Help`、`Echo`、`Cast`、`Dump`、`Json`、`Range`、`Random`、`Shuffle` | 查看、转换、格式化和生成数据。 |
| 运行时 | `Assembly`、`Checksum` | 查看程序集和计算校验和。 |
| 文件系统 | `File Open/Save/Copy/Move/Info/Exists/Delete`、`Directory List/Move/Info/Exists/Delete` | 操作框架文件系统抽象。 |
| 安全 | `RSA Export/Import`、`Secret Generate/Verify`、`Password Generate/Parse` | 管理密钥、机密和密码表示。 |
| 调度 | `Scheduler`、`Schedule`、`Reschedule`、`Unschedule` | 查看和修改已注册的调度计划。 |
| 序号 | `Sequence Info/Reset/Increase/Decrease` | 操作已配置的序号提供程序。 |
| 配置 | `Configuration Get` | 读取应用的有效配置。 |
| 消息 | `Messaging Queue Produce/Subscribe` | 通过已配置的消息队列发布和观察消息。 |

部分命令依赖其他插件提供的服务，例如调度、序号、消息和虚拟文件系统命令都会从当前应用解析相应提供程序。

## 扩展命令树

自定义命令通常继承 `CommandBase<CommandContext>`，使用 `CommandOptionAttribute` 声明选项，再由插件清单挂载。命令实现应保持轻量：校验终端输入、调用应用服务并返回可读结果，不要复制领域逻辑。

> 💡 使用父命令节点组织同类操作，既有利于命令补全和 `help` 展示，也能避免冗长的扁平命令名。

## 错误与安全

- 无效或缺失的选项会以命令选项异常报告；安全敏感参数不得静默采用默认值。
- 文件删除、密钥导出、调度修改和消息发布都会改变外部状态，执行前应确认宿主、环境和提供程序。
- 取消会传递给异步命令，但外部提供程序可能已经接受操作。
- 不要直接在命令表达式中传入凭据，以免被终端历史或诊断输出保留。

## 相关资源

- [Core 命令抽象](../Zongsoft.Core/src/Components)
- [命令插件清单](src/Zongsoft.Commands.plugin)
- [部署清单](src/Zongsoft.Commands.deploy)
- [插件框架](../Zongsoft.Plugins/README.zh-Hans.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../Zongsoft.Plugins/README.zh-Hans.md)。

清单把命令集挂载到 `/Workbench/Executor/Commands`。终端宿主可用 `help`、`echo hello` 验证，依赖提供程序的命令仍需配套插件。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Commands` | [Zongsoft.Commands.plugin](src/Zongsoft.Commands.plugin) |
| 文件复制及依赖 | [Zongsoft.Commands.deploy](src/Zongsoft.Commands.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft commands]
nuget:Zongsoft.Commands
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Commands.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。
