# Zongsoft.Externals.Aliyun 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Aliyun)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Aliyun)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**E**xternals.**A**liyun](https://github.com/Zongsoft/framework/tree/main/externals/aliyun) 将部分[阿里云](https://www.aliyun.com/)服务集成到 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 框架中。

## 主要功能

- 通过 Zongsoft 文件系统抽象和 `zfs.oss` 方案访问 OSS 存储桶；
- 提供阿里云消息服务的队列与主题访问能力；
- 为框架消息服务提供 MQTT 连接设置驱动器；
- 通过通信发送器和命令支持短信及语音呼叫；
- 支持移动推送及相应的命令集成。

加载 `Zongsoft.Externals.Aliyun.plugin`，并在 `/Externals/Aliyun` 下配置所需的服务中心、凭证、存储桶、消息、通信或推送应用参数。随包提供的[选项文件](src/Zongsoft.Externals.Aliyun.option)展示了配置层次及各项占位值。

## 安装与服务选择

```shell
dotnet add package Zongsoft.Externals.Aliyun
```

`general` 选择默认地域/服务中心，以及是否使用内网端点。具名 Certificate 保存 Access Key ID 与 Secret；各 OSS Bucket、消息 Queue/Topic、通信模板或推送应用都可覆盖地域与凭据选择。

```xml
<option path="/Externals/Aliyun">
	<general name="Shenzhen" intranet="false">
		<certificates default="main">
			<certificate certificate.name="main"
			             code="REPLACE_ACCESS_KEY_ID"
			             secret="REPLACE_ACCESS_KEY_SECRET" />
		</certificates>
	</general>
</option>
```

> 💡 部署环境支持时优先采用实例/工作负载身份。无法避免静态密钥时，应从秘密提供程序注入，并按服务边界使用不同的最小权限凭据。

## 应用工作流

- 通过注册的 `zfs.oss` 文件系统访问 OSS 对象；Bucket 与 Key 遵循对象存储语义，而不是本地磁盘。
- 通过 Zongsoft 消息契约解析阿里云 Queue/Topic 提供程序，再使用统一消息模型发布、订阅和确认。
- 解析 `ITransmitter` 发送短信，或使用 `Phone.Send`；语音使用 `Phone.Call`。模板名称映射到配置的云端模板码。
- 使用 `Pushing.Send` 或 `PushingSender` 向已配置移动应用和目标类型推送。

类库通过 `HttpAuthenticator` 签名 HTTP 请求，并按配置选择公网或内网服务中心。提供程序错误以 `AliyunException` 暴露；诊断时应记录关联信息，而不是凭据。

## 运行边界

🚨 通信、推送、存储与消息调用可能产生费用或不可逆外部影响。请验证接收者与对象路径、限制速率，只对幂等调用配置重试，并确保测试仅显式连接专用云资源。

云 API 仍受配额、载荷限制、地域、最终一致性、模板审批和回调要求约束；适配器不会消除这些限制。启用服务的准确规则请查阅[阿里云帮助中心](https://help.aliyun.com/)。

## 延伸阅读

- [完整选项层次](src/Zongsoft.Externals.Aliyun.option)
- [回调网关](gateway/README.zh-Hans.md)
- [外部适配器实现协作指南](../SKILL.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

清单注册阿里云连接驱动、文件系统提供程序及电话/消息命令。证书与服务设置由部署选项管理，解析提供程序不代表获得云调用授权。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Aliyun` | [Zongsoft.Externals.Aliyun.plugin](src/Zongsoft.Externals.Aliyun.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Aliyun.deploy](src/Zongsoft.Externals.Aliyun.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals aliyun]
nuget:Zongsoft.Externals.Aliyun
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Aliyun.plugin`、`Zongsoft.Externals.Aliyun.option`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。
