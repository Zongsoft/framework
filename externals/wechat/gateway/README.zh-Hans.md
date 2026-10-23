# Zongsoft.Externals.Wechat.Gateway

[English](README.md) | [简体中文](README.zh-Hans.md)

## 功能与配置

本包在插件式 Web 宿主中接收微信支付 APIv3 支付、退款和离线刷脸签约状态通知。它依赖[微信主库](../README.zh-Hans.md)提供的商户配置、签名验证和资源解密；应用 API 见 [Web API 包](../api/README.zh-Hans.md)。

| POST 路径 | 支持的事件 |
| --- | --- |
| `/Externals/Wechat/Fallback/payment/{merchant}` | `TRANSACTION.SUCCESS` |
| `/Externals/Wechat/Fallback/refundment/{merchant}` | `REFUND.SUCCESS`、`REFUND.ABNORMAL`、`REFUND.CLOSED` |
| `/Externals/Wechat/Fallback/offlinefaceagreement/{merchant}` | `FACEPAY.USER_STATE_CHANGE` |

`merchant` 必须是配置的普通商户号或服务商号，只包含数字，不带 `:direct`、`:broker` 等后缀。不接受查询参数；Content-Type 为 JSON，正文不超过 2 MiB。公众号消息、第三方授权和 GET 地址验证不属于上述入口。

按[商户凭据配置](../docs/implementation.zh-Hans.md#merchant-credentials)设置 `ApiV3Key`、显式验签模式及允许的应用。主库先验证原始字节签名及五分钟时间窗口，再认证解密并核对事件和商户身份。宿主仍须核对订单、用户/组织、金额、币种和业务幂等。

## 注册业务处理器

继承 `PaymentHandlerBase`、`RefundmentHandlerBase` 或对应的 `FallbackHandlerBase<TRequest>`，实现业务处理；需要持久接收时重写 `OnReceiveAsync`。经 `/Workbench/Externals/Wechat/Fallback/Handlers` 插件节点挂载实现 `IFallbackHandler` 的对象，名称为 `Payment`、`Refundment`、`OfflineFaceAgreement`，与处理器 `Kind` 一致。仅在启动阶段装配，不在请求中修改注册表。

`Completed`、`Accepted`、`Duplicate` 返回 HTTP 204，无正文。其中 `Accepted` 表示通知已可靠保存，后续业务执行与恢复由宿主承担。未接收、处理失败或四秒接收期限到期返回 503；输入错误返回 400，超出服务器正文限制可返回 413；未知路径返回 404。处理器失败应答使用 `code=FAIL` 的 JSON，不包含异常详情。

应用内部的 `HandleAsync(TRequest, ...)` 不验证 HTTP 通知；网络请求必须进入 `ReceiveAsync`。使用 TLS，不记录密钥或完整支付/用户正文。完整公开约束见[通知接收契约](../docs/implementation.zh-Hans.md#payment-notifications)。

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../../Zongsoft.Plugins/README.zh-Hans.md)。

使用 Plugins.Web 宿主，并部署下列依赖主适配器。服务/控制器发现发生在宿主初始化阶段，无需把业务逻辑放进 Program.cs；接收请求前验证端点及授权。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Wechat.Gateway` | [Zongsoft.Externals.Wechat.Gateway.plugin](src/Zongsoft.Externals.Wechat.Gateway.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Wechat.Gateway.deploy](src/Zongsoft.Externals.Wechat.Gateway.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals wechat]
nuget:Zongsoft.Externals.Wechat@7.10.0

[plugins zongsoft externals wechat gateway]
nuget:Zongsoft.Externals.Wechat.Gateway@7.10.0
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Wechat.Gateway.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

## 第三方平台授权事件

`POST /Externals/Wechat/Components/{componentAppId}/Events` 接收验证票据和授权变更的加密 XML。须单独配置组件并注册可靠的 `IComponentStore`；验证并可靠保存后返回 `success`，存储失败返回 503。限长、验签、幂等及宿主职责见[组件契约](../docs/implementation.zh-Hans.md#component-platform)。本入口不接收授权者的业务消息，也不自动建立租户关联。
