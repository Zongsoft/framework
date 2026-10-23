# Zongsoft.Externals.Wechat 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Wechat)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Wechat)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 当前能力与限制

本库提供小程序登录与手机号兑换、公众号网页授权与模板消息、支付和退款的部分 REST 封装。支付实现同时包含 APIv3 和 APIv2/XML 路径；并非微信全产品接口的完整 SDK。

支付、退款、刷脸、进件、银行、上传和会员卡的 APIv3 HTTP 调用提供请求签名，并在解析响应前验证原始正文、签名头和时间窗口；204 空响应也必须验签。未签名网关错误只作为失败，资金请求不自动重试。超时、错误码、传输配置与资源所有权见 [HTTP 契约](docs/implementation.zh-Hans.md#http-pipeline)。会员卡须显式绑定有产品权限的商户、服务号和品牌。

支付、退款与离线刷脸签约通知使用明确产品路由；主库验证原始字节签名、时间窗口、事件、商户和应用绑定后认证解密。宿主负责业务归属、金额、币种及持久幂等，只有业务完成或可靠接收后才确认成功。接入方式见[Gateway](gateway/README.zh-Hans.md)和[通知契约](docs/implementation.zh-Hans.md#payment-notifications)。

Web 登录入口通过宿主鉴证器签发应用会话，微信服务器令牌和小程序 `session_key` 不向客户端返回。宿主须配置账号资源授权；手机号、用户和消息操作还须校验登录身份，JS-SDK 另需配置站点白名单。详见 [Web API](api/README.zh-Hans.md)。

第三方组件支持验证票据与授权事件接收、组件令牌、预授权及服务端授权交换；宿主提供可靠存储和租户关联规则，支持授权者令牌刷新及按实际权限委托已有公众号/小程序调用。接入见[组件契约](docs/implementation.zh-Hans.md#component-platform)。

支付和退款使用明确的 CNY 分单位，decimal 入口拒绝精度丢失与溢出。下单结果区分预支付凭据、Native 二维码和实际订单；未知状态必须查询确认。公开属性、异常与宿主映射见[支付模型契约](docs/implementation.zh-Hans.md#payment-models)。

JSAPI/小程序、APP、Native 基础支付的身份配置、下单结果、客户端参数和查单约束见[基础支付契约](docs/implementation.zh-Hans.md#basic-payments)。

付款码、在线刷脸凭据、V2 查单/撤销及 Automao 状态处理见[付款码契约](docs/implementation.zh-Hans.md#ticket-payments)。

退款申请受理与资金到账分别处理，支持原单查询和部分退款；商户绑定、状态及宿主累计额度约束见[订单退款](docs/implementation.zh-Hans.md#refunds)。

## 运行要求

主库、Web 与 Gateway 使用同版 7.10.0，支持 .NET 8、9、10。宿主提供 Core 7.72.0 或更高版本，HTTP 入口还需要 Web 7.20.0 或更高版本。包源、授权配置与未完成业务切换要求见[部署契约](docs/implementation.zh-Hans.md#deployment)。

## 商户与密钥

商户通过 `await AuthorityUtility.GetAuthorityAsync(name, cancellation)` 获取。签名身份、`ApiV2Key`、`ApiV3Key` 与微信验签材料分别配置，支持显式公钥和平台证书两种模式；文件加载可取消，缺失或不匹配的材料会失败。配置示例、轮换和宿主刷新要求见[商户与密钥契约](docs/implementation.zh-Hans.md#merchant-credentials)。平台证书下载响应先由已有可信材料验签，再用于更新证书集合。

## 产品支持范围

| 能力 | 当前支持范围 |
| --- | --- |
| JSAPI/小程序、APP、Native 支付 | 普通商户与服务商 APIv3，下单、原单查询和关单 |
| 付款码、在线刷脸支付 | 对应产品的 APIv2 提交、查询及撤销；APIv3 产品不自动回退 V2 |
| 退款 | APIv3 申请、查询及通知；受理与到账分别处理 |
| 离线刷脸 | 专用服务商支付及签约接口；设备 SDK 由外部提供 |
| 商户进件 | 平台收付通二级商户申请、独立的普通服务商特约商户申请与查询 |
| 银行、素材 | 多候选银行识别、支行查询及限长图片/PDF/视频上传 |
| 会员卡 | 商户、公众号、品牌绑定的模板/code 操作，非卡券全产品 |
| 小程序身份 | code2Session、授权手机号兑换、宿主会话签发 |
| 公众号 | 网页 OAuth、JS-SDK 签名、关注用户查询及模板消息 |
| 基础凭据 | 自有账号稳定令牌、独立 JS-SDK/卡券票据及分布式协调 |
| 开放平台 | SHA-1/CBC、组件事件、预授权、授权交换、授权者令牌及委托调用；须提供可靠存储 |

产品准入和实际账号权限是使用前提。逐接口范围及限制见[功能核对表](docs/implementation.zh-Hans.md#capability-matrix)。

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

清单暴露微信组件及服务。账户、证书与支付设置由应用选项管理；API 与回调网关是可选、独立的宿主贡献。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Wechat` | [Zongsoft.Externals.Wechat.plugin](src/Zongsoft.Externals.Wechat.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Wechat.deploy](src/Zongsoft.Externals.Wechat.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals wechat]
nuget:Zongsoft.Externals.Wechat@7.10.0
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Wechat.plugin`、`Zongsoft.Externals.Wechat.option`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

## 账户获取

使用 `IAccountProvider.GetAccountAsync` 提供动态账户。小程序和公众号分别通过 `await AppletManager.GetAppletAsync(code, cancellation)`、`await ChannelManager.GetChannelAsync(code, cancellation)` 获取；允许未找到时使用 `FindAppletAsync`、`FindChannelAsync` 并检查空值。调用支持取消，已获取的实例可以复用。

## 进件与素材

`PartnerService` 提供普通服务商特约商户申请与查询；签约、支付产品权限和 AppID 绑定分别确认，见[普通服务商接入](docs/implementation.zh-Hans.md#partner-onboarding)。

`MerchantService.RegisterAsync/GetAsync` 提供平台收付通二级商户申请及原业务申请号查询，受理不表示支付已开通。银行识别返回候选数组；图片、PDF 与视频上传受类型和大小限制。调用者须具备对应产品权限，具体模型和运行要求见[进件、银行与素材](docs/implementation.zh-Hans.md#merchant-onboarding)。

## 会员卡

通过 `new Membership(authority, channelAccount, brandId)` 调用已有会员卡模板和 code 接口。公众号必须是有品牌关联的服务号，操作商户须具备产品权限；预存返回各 code 的独立结果。支持范围及 Automao 限制见[会员卡契约](docs/implementation.zh-Hans.md#membership)。

## 基础凭据与票据

自有公众号和小程序通过 `GetCredentialAsync` 获取稳定版 token；普通调用复用共享缓存，强制刷新仅在明确需要时使用。公众号 `jsapi`、`wx_card` 票据分别缓存，取消和错误向调用方传播。宿主须提供分布式缓存、锁，以及关闭重定向的命名 HTTP 传输，见[凭据配置](docs/implementation.zh-Hans.md#account-credentials)。

小程序身份、结构化手机号与宿主会话接入见[身份契约](docs/implementation.zh-Hans.md#applet-identity)。

公众号 OAuth 的 scope、刷新和有效性校验，以及 JS-SDK 站点策略、用户/消息路由见[公众号服务契约](docs/implementation.zh-Hans.md#channel-services)。

公众平台消息提供严格接收方校验的 CBC 加解密和 SHA-1 验签，支持文本及二进制消息；调用顺序、异常和接收层职责见[消息密码协议](docs/implementation.zh-Hans.md#platform-cryptography)。
