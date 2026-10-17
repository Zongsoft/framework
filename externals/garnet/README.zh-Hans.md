# Zongsoft.Externals.Garnet 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Garnet)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Garnet)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.Garnet` 以 Zongsoft `WorkerBase` 托管 [Microsoft Garnet](https://microsoft.github.io/garnet/)。Garnet 是高性能、兼容 Redis 协议的数据存储；本适配器负责服务器启停，并把框架设置转换为 Garnet 命令行选项。

## 安装与配置

```shell
dotnet add package Zongsoft.Externals.Garnet
```

请部署插件和选项产物。随包插件以 `setting="default"` 选择以下服务器，绑定声明只读取这一项：

```xml
<?xml version="1.0" encoding="utf-8" ?>

<?zongsoft.tools.binding name="garnet"
	path="Externals/Garnet/default"
	template="tcp://${this.Value['address']}"
	protocols="tcp"
	required="true"?>

<options>
	<option path="/Externals/Garnet">
		<server server.name="default"
			value="address=127.0.0.1:6379;auth=Password;password=REPLACE_ME;lua=true" />
	</option>
</options>
```

`GarnetServer.Setting` 选择具名设置；未指定时使用工作器名称。配置项缺失会使启动失败，不回退到其它预设或 SDK 默认监听；额外定义的设置不会自动启动。更改插件的 `Setting` 时，应同时调整本插件 binding 的 `path`，使两者指向同一项。只能在工作器停止后修改 `Setting`。

每个实例通过 `address` 配置单个 IP 和固定端口，例如 `address=127.0.0.1:6379` 或 `address=[::1]:6379`；端口为 1 至 65535。适配器将它转换为 Garnet 所需的 `bind` 和 `port` 参数，绑定模板直接使用同一地址。配置键不区分大小写。不接受 DNS、多地址列表、通配符、路径、查询参数或随机端口，也不允许另外指定 `bind/port`。跨容器访问时须明确选择可达的监听地址。

`protocols="tcp"` 是常量模板，不向 Garnet 设置中添加工具专用参数。绑定按 TCP 发布，启用 `tls/EnableTLS` 时由 Garnet 终止 TLS，代理保持连接透传。`cluster-announce-ip/port` 是集群通告值，不作为绑定来源。首版静态绑定不接受 Unix socket 或原生 `config-import-path`（包括友好别名），避免其它配置来源增加未声明监听；需要拆分文件时使用框架的 `<?import ... ?>` 合并到同一选项树。

相对路径选项从适配器程序集目录解析，`~/` 从当前应用根目录解析；证书文件 `cert-file-name/CertificateFileName`、`issuer-certificate-path/IssuerCertificatePath` 同样遵守此规则。`tls=true` 配合 `cert-file-name=~/certificates/garnet.pfx` 等 Garnet 原生选项启用 TLS。

## 类型化配置

[配置对象](src/Configuration/GarnetConnectionSettings.cs) 与 [配置驱动](src/Configuration/GarnetConnectionSettingsDriver.cs) 沿用 `ConnectionSettingsBase<TDriver>` / `ConnectionSettingsDriver<TSettings>`。插件将 `Garnet` 驱动注册到 `/Workbench/Configuration/ConnectionSettings/Drivers`；服务器按名称取得普通设置后，由该驱动创建类型化配置。

```csharp
var settings = GarnetConnectionSettingsDriver.Instance.GetSettings(
	"default", "address=127.0.0.1:6379;tls=true;cert-file-name=~/certificates/garnet.pfx;");

settings.EnableAOF = true;
settings.CommitFrequency = TimeSpan.FromSeconds(1);
settings.CheckpointDirectory = "~/.garnet";
```

引用命名空间 `Zongsoft.Externals.Garnet.Configuration`。常用属性包括 Address、AuthenticationMode、Password、TLS 与证书、AOF 与提交间隔、Lua、恢复、检查点及静默模式；原生名称和友好名称指向同一设置项。同名键及别名按框架约定后值覆盖前值。未声明的其它 Garnet 原生参数仍保留，不要求为每个 SDK 参数增加属性。

AuthenticationMode 直接采用 Garnet SDK 的 `GarnetAuthenticationMode?`，包括 NoAuth、Password、Aad、ACL、AclWithAad；配置文本仍使用 `auth=Password`，运行端由枚举生成规范参数。未指定时保持 null，不在适配器中补成 NoAuth。

时长属性采用 `TimeSpan?`：`CommitFrequency` 转为整数毫秒，`CertificateRefreshFrequency` 转为整数秒。配置文本可以写成 `CommitFrequency=1s`、`CertificateRefreshFrequency=1m`；无单位整数仍按对应原生单位解释。值必须能用该单位精确表示且处于原生整数范围内；零值和提交间隔 -1ms 保留原生语义。布尔和时长属性采用可空类型，表示没有显式值时不由框架补充 SDK 默认值。建议布尔选项写成 `tls=true`、`quiet=true`；原生无值标志 `quiet` 仍保留在字典中并交给 SDK 解释。Address 不提供默认值，启动时必须明确指定。

地址验证、原生参数转换及路径解析由配置对象承担，参数转换方法仅供程序集内的服务器调用；GarnetServer 负责选择配置和管理实例生命周期。

## 生命周期与持久化

启动会创建底层 `Garnet.GarnetServer` 并调用 `Start`，成功后才保存实例；取消或启动失败时释放已创建的实例，停止和释放工作器会清空并释放服务器。启动失败通过工作器 `StateChanged` 事件报告异常并回到 `Stopped`。适配器把 `EnableAOF`、`CheckpointDirectory`、`EnableTLS` 等友好名称映射为 Garnet 选项，同时透传原生选项名；拒绝 `help/version` 等进程级命令。

> 💡 兼容 Redis 协议不表示全部 Redis 命令、模块、持久化模式或客户端行为完全相同。请验证应用实际使用的命令集。

🚨 随包密码只是占位值。请绑定可信网卡、明确设置身份验证与 TLS、保护 Checkpoint/AOF 目录、限制模块加载，并在保存持久数据前测试恢复。进程内服务器与宿主共享故障和资源边界。

## 验证

使用可丢弃目录与端口，启动 Worker，以兼容客户端连接并执行所需命令，再停止；启用持久化时还要重启验证恢复。投产前测试内存/连接限制、关闭时间和损坏 Checkpoint 的处理。

- 配置文件说明
	> [Garnet 默认值](https://github.com/microsoft/garnet/blob/main/libs/host/defaults.conf)

- 配置参数映射
	> [Garnet 选项模型](https://github.com/microsoft/garnet/blob/main/libs/host/Configuration/Options.cs)

- [外部适配器实现协作指南](../SKILL.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

本插件挂载启动工作器，启动宿主前应配置 `/Externals/Garnet`；加载工作器可能启动内嵌服务器并打开监听端口。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Garnet` | [Zongsoft.Externals.Garnet.plugin](src/Zongsoft.Externals.Garnet.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Garnet.deploy](src/Zongsoft.Externals.Garnet.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals garnet]
nuget:Zongsoft.Externals.Garnet
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Garnet.plugin`、`Zongsoft.Externals.Garnet.option`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。
