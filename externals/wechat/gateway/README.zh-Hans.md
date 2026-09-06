# Zongsoft.Externals.Wechat.Gateway

[English](README.md) | [简体中文](README.zh-Hans.md)

## 定位与职责边界

本包接收 HTTP 回调并分派给具名框架处理器。它配合[微信客户端适配器](../README.zh-Hans.md)使用，区别于面向应用管理操作的 [Web API 包](../api/README.zh-Hans.md)。

**回调**是服务方发来的入站请求，超时后可能重试。路由名称只是分派键，不是发送方身份证明。本网关不会自动验证所有微信产品的签名，也不会自动解密所有负载。

## 安装与宿主

```shell
dotnet add package Zongsoft.Externals.Wechat.Gateway
```

在[插件化 Web 宿主](../../../Zongsoft.Plugins.Web/README.zh-Hans.md)中加载网关插件及其 Wechat 依赖，按附带的 `.deploy` 部署程序集和 `.plugin`。使用框架路由约定时，端点为 `POST /Externals/Wechat/Fallback/{name}/{key?}`。

账户、证书及回调秘密按父级适配器选项配置。网关不会自动提供 GET 地址验证处理器。

## 注册处理器

以下应用装配辅助方法注册一个已经构造好的处理器。该处理器必须实现对应服务的验证及应答协议：

```csharp
using Zongsoft.Components;
using Zongsoft.Externals.Wechat.Gateway;

static void RegisterCallback(string name, IHandler handler)
{
	ArgumentException.ThrowIfNullOrEmpty(name);
	ArgumentNullException.ThrowIfNull(handler);
	FallbackExecutor.Instance.Handlers[name] = handler;
}
```

在启动阶段、接收请求前注册，字典不是可并发修改的运行时注册表。执行器把请求正文流和请求参数交给选中的处理器。正文读取不得超出请求生命周期；交给后台队列前应在大小限制内复制负载。

## 应答与投递

非空结果返回 `200 OK`，空结果返回 `204 No Content`。OperationException 的 Unfound 映射为 404、Unsupported 为 400、Unprocessed 为 422、Unsatisfied 为 412，其它原因返回 500。请确认此结果格式符合选定的微信回调协议，它不是通用应答格式。

💡 测试重复通知与应答丢失后的重试，使用已验证的服务方事件标识实现业务写入幂等。

## 安全与验证

🚨 按要求对原始字节验签，检查时间戳、随机数和重放窗口，完成必要验证后再解密；限制正文大小并使用 TLS。不要记录秘密或完整支付/用户负载。控制器不会替处理器提供这些防护。

用本地签名夹具验证成功、错误签名、过期时间、重复事件、无效正文及处理失败，这些检查不需要真实微信请求。实现入口见 [FallbackExecutor](FallbackExecutor.cs)、[控制器](Controllers/FallbackController.cs)和[外部适配技能](../../SKILL.md)。

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../../Zongsoft.Plugins/README.zh-Hans.md)。

使用 Plugins.Web 宿主，并部署下列依赖主适配器。服务/控制器发现发生在宿主初始化阶段，无需把业务逻辑放进 Program.cs；接收请求前验证端点及授权。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Wechat.Gateway` | [Zongsoft.Externals.Wechat.Gateway.plugin](Zongsoft.Externals.Wechat.Gateway.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Wechat.Gateway.deploy](Zongsoft.Externals.Wechat.Gateway.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals wechat]
nuget:Zongsoft.Externals.Wechat

[plugins zongsoft externals wechat gateway]
nuget:Zongsoft.Externals.Wechat.Gateway
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Wechat.Gateway.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。
