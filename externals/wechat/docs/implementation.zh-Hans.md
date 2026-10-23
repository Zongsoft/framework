# Zongsoft.Externals.Wechat 实现说明

[English](implementation.md) · [项目概览](../README.zh-Hans.md) · [重构任务与计划](../.plan.md)

本文集中定义当前实现、协议约束、插件装配和验证边界。任务进度与真实联调待办只在 `.plan.md` 维护；官方资料的存在不代表本库支持全部产品，也不代表具体账号已获准入。

## 目录

- [架构与工程边界](#architecture)
- [微信功能与接口核对表](#capability-matrix)
- [微信支付商户与密钥契约](#merchant-credentials)
- [微信支付 APIv3 HTTP 契约](#http-pipeline)
- [支付金额、标识与结果契约](#payment-models)
- [APIv3 基础支付契约](#basic-payments)
- [付款码与在线刷脸支付契约](#ticket-payments)
- [订单退款](#refunds)
- [离线刷脸与签约契约](#offline-face)
- [支付通知接收契约](#payment-notifications)
- [平台收付通进件、银行与素材](#merchant-onboarding)
- [普通服务商特约商户接入](#partner-onboarding)
- [会员卡调用契约](#membership)
- [自有账号凭据与票据](#account-credentials)
- [小程序身份与 Web 边界](#applet-identity)
- [公众号 OAuth、JS-SDK 与消息](#channel-services)
- [公众平台消息密码协议](#platform-cryptography)
- [第三方平台协议与接收契约](#component-platform)
- [微信插件运行与数据库契约](#deployment)
- [微信适配器验证契约](#testing)
- [SaaS 开发与测试账号](#saas-development-and-test-accounts)
- [微信平台接口与接入约束](#platform-reference)

<a id="architecture"></a>

## 架构与工程边界

本适配器遵循 [项目约定](../AGENTS.md) 与 [Zongsoft 开发规范](../../../../guidelines/zongsoft.csharp.guidelines.md)，通过既有插件、模块容器、提供者和处理器扩展点装配。宿主负责启动与承载；微信协议归本库，租户关系、业务订单、金额复核、幂等和持久化归 Automao。保持 SOLID、高内聚低耦合与最窄可见性，不为单一适配器扩充 Core 公共接口，也不为测试扩大生产成员可见性。

| 生产项目 | 测试项目 | 责任 |
| --- | --- | --- |
| [src](../src/Zongsoft.Externals.Wechat.csproj) | [test](../test/Zongsoft.Externals.Wechat.Tests.csproj) | 账户、商户、凭据、密码、传输、产品服务和通知解析 |
| [api/src](../api/src/Zongsoft.Externals.Wechat.Web.csproj) | [api/test](../api/test/Zongsoft.Externals.Wechat.Web.Tests.csproj) | 宿主授权、应用会话、管理入口和响应映射 |
| [gateway/src](../gateway/src/Zongsoft.Externals.Wechat.Gateway.csproj) | [gateway/test](../gateway/test/Zongsoft.Externals.Wechat.Gateway.Tests.csproj) | 微信入站协议、可靠接收交接和标准应答 |

各项目的 `.csproj`、`.plugin`、`.deploy` 与源码同目录；主库另有 `.option`，双语 README 位于各项目根目录。六个项目都在 [解决方案](../Zongsoft.Externals.Wechat.slnx) 中；`build.cake` 用 `**/test/*.csproj` 发现测试。四个基础库 Core、Web、Plugins、Plugins.Web 由宿主提供，适配器部署清单不重复复制。

紧密关联的服务使用嵌套类，复杂实现按稳定职责拆分 `partial` 文件，方法专属辅助逻辑使用局部函数；无捕获时使用 `static`。公开类型、插件入口或配置变更须同步项目、清单、资源、README 与 Automao 直接调用。调用链定位与操作流程见 [SKILL.md](../SKILL.md)。

`IAccountProvider.GetAccountAsync` 异步提供账户；`Get…Async` 要求存在，`Find…Async` 允许 null，取消令牌位于末尾。配置和动态自有账户共用成功实例缓存，失败或未完成任务不缓存；委托账户不缓存实例，每次使用检查最新授权。本地解析、签名、加解密和已有对象转换保持同步，不隐藏网络、数据库或文件等待。

支付资源解密使用原生 `AesGcm`，每次独立创建并释放密码器，要求 32 字节密钥、12 字节 nonce 和末尾 16 字节认证标签。字符串按 UTF-8/Base64 处理，null 与空关联数据等价；空明文合法。缺少字节数组抛出 `ArgumentNullException`，长度错误抛出带参数名的 `ArgumentException`，认证失败抛出 `AuthenticationTagMismatchException`。认证成功不代替来源验签。V2 MD5/HMAC、公众平台 SHA-1/CBC 和上传 SHA-256 仍按各自协议使用；不共享可变密码器，借入散列由调用方释放，主库与部署清单不依赖 BouncyCastle。

<a id="capability-matrix"></a>

## 微信功能与接口核对表

本节记录源码中的实际入口、官方协议对应关系及当前约束。存在调用代码不表示接口已通过测试或具备商户权限；只有枚举、DTO、README 概念或占位方法也不等于实现。具体执行与勾选见[实施计划](../.plan.md#refactoring-plan)。

路径以 `externals/wechat` 为根，使用类型和方法名定位源码。官方来源索引在文末。修改接口时继续核对请求/结果字段、错误码及该产品配套流程；公开文档不能证明某商户已获准入。离线验证命令、测试覆盖边界及 Automao 环境条件见[验证契约](#testing)。

<a id="capability-matrix-1"></a>

### 功能与版本

| 能力及源码入口 | 当前调用/行为 | 契约与依据 |
| --- | --- | --- |
| `IAccountProvider`、`AppletManager`、`ChannelManager` | 异步提供者；Get 必需、Find 可空；已完成实例缓存 | 保留异步/取消/失败不缓存契约；明确配置初始化、实例更新和隔离，不改变为共享未完成任务 |
| `CredentialManager.GetCredentialAsync` | `POST /cgi-bin/stable_token` | 自有账号共享缓存、锁内二次读取、剩余 TTL、强制刷新合并及取消见[凭据契约](#account-credentials)；不用于授权者令牌。[W1] |
| `CredentialManager.GetTicketAsync`、`Channel.PostmarkAsync` | `/cgi-bin/ticket/getticket`；jsapi/wx_card 独立缓存；SHA-1 单次计算 | 保留 JS-SDK SHA-1 协议，隔离票据类型与账号，页面 URL 去掉片段；签名站点由宿主约束。[W2] |
| `Applet.LoginAsync`、认证基类 | `/sns/jscode2session`，不查询公众号资料 | OpenID、可选 UnionID 与服务端密钥分离；应用会话与租户隔离见[身份契约](#applet-identity)。[W3] |
| `Applet.GetPhoneNumberAsync` | `/wxa/business/getuserphonenumber`，返回结构化 `PhoneInfo` | 保留独立手机号 code 兑换；结构化号码信息与可选身份绑定，取消/错误完整传播。[W4] |
| `ChannelAuthentication` | 按 scope 调用 `/sns/oauth2/access_token` 与可选 `/sns/userinfo`，提供刷新及有效性校验 | 资料可缺失，令牌不序列化；宿主 state、Web 会话及策略见[公众号契约](#channel-services)。[W5] |
| `UserProvider`、`ChannelMessager`、`Transmitter` | 公众号用户列表/详情、模板列表/发送 | 保留公众号服务，按账号权限调用；小程序不公开公众号用户能力。小程序订阅消息不在当前实现范围。[W6] |
| `PaymentService.DirectPaymentService` / `BrokerPaymentService` | 普通商户/服务商 JSAPI、APP、Native V3 下单、查询和关单，绑定商户与应用，无 V2 自动降级 | 见[基础支付契约](#basic-payments)；真实产品权限由账号联调确认。[P1] |
| `PaymentService.PaymentResult.Parameters` | JSAPI/APP 分别生成同一次下单的调起参数；Native 返回 `CodeUrl` | 客户端调起成功不当作支付到账，服务端查询或验证通知确认。[P1] |
| `PaymentService.Tickets` 付款码 | V2 提交、查单与双向证书撤销，验证 XML 应答及商户/应用身份 | 状态与重试约束见[付款码契约](#ticket-payments)；配套退款使用现有 V3 入口。[P2] |
| `AuthenticateOnlineAsync` | `payapp.weixin.qq.com/face/get_wxpayface_authinfo` | 按在线刷脸现行协议保留凭据获取；设备返回 face_code 后走付款码及查询/撤销闭环。[F1] |
| `AuthenticateOfflineAsync`、`PayOfflineTicketAsync` | `/v3/offlineface/authinfo`、`/v3/offlineface/transactions` | 专用 V3 产品、业务参数及 `GetOfflineAsync` 查单保留；HTTP 成功不代表扣款成功，身份和可选金额按[离线刷脸契约](#offline-face)校验。[F2] |
| `MerchantService.Contracts` | `offlinefacemch/tokens`、签约用户查询、`offlineface/contracts/presign`、签约查询及解约 | 授权凭证、预签约、查询和解约使用独立入口；未知状态保留，通知绑定机构、用户并查询当前合同。[F2] |
| `RefundmentService`、`RefundmentHandlerBase` | `/v3/refund/domestic/refunds` 申请/查询；绑定普通商户或服务商/子商户，申请响应与通知字段独立 | 状态、累计额度与原单重试见[订单退款契约](#refunds)。V2 订单适用条件仍须对应商户实测。[P3] |
| `Authority*`、`CertificateUtility` | 独立签名身份和 API 密钥；显式公钥/证书模式；异步文件加载；证书下载验签、解密与原子轮换 | 见[商户材料契约](#merchant-credentials)；宿主负责可信初始配置和定期刷新。[S1] |
| `HttpClientFactory`、`HttpUtility`、`Json` | 商户签名、微信公钥字段加密；APIv3 响应先验签再解析，空正文和 multipart meta 按协议签名 | 见 [HTTP 契约](#http-pipeline)；未签名网关错误只作失败，不自动重试资金请求，会员卡使用同一管线，通知使用独立接收协议。[S1] |
| Gateway、`FallbackHandlerBase` | 明确产品入口，原始字节验签、时效/身份/算法校验后解密；返回可靠接收结果 | 支付、退款和刷脸的事件、宿主幂等与故障恢复边界见[通知契约](#payment-notifications)。[S2] |
| `MerchantService.RegisterAsync` / `GetAsync` | `/v3/ecommerce/applyments/` 与按业务申请号查询 | 平台收付通二级商户申请号绑定、敏感字段及状态见[进件契约](#merchant-onboarding)；不替代普通服务商进件。[M1] |
| `BankUtility` | `capital/capitallhh/banks/*` 列表/支行/银行卡识别；加密材料与 Wechatpay-Serial 请求头匹配，查询参数转义，使用支付响应解析器 | 普通服务商银行组件；分页含空页总数，识别保留全部候选，开户银行与别名编码分别使用。[M1][S1] |
| `Paying.Uploader` | `merchant/media/upload`、`merchant/media/video_upload` | 图片/视频 5 MiB，PDF 7.5 MiB；有界读取、meta 签名、SHA256 及文件释放，微信仍审核素材内容。[M1] |
| `Membership` / `TemplateService` | V3 `marketing/membercard-open/cards` 模板及 code 接口；统一签名验签管线 | 商户/服务号/品牌绑定、逐项 code 结果及修改前归属校验见[会员卡契约](#membership)。[M2] |
| `Platforms/CryptographyUtility` | SHA-1 验签、AES-CBC、32 字节填充、预期 AppID 校验 | [消息密码协议](#platform-cryptography)要求先验签再解密；支持严格 UTF-8 和二进制消息。组件票据、事件、令牌及授权交换见[组件契约](#component-platform)；授权者委托和业务持久化分别实现。[W7] |
| `api/src` 控制器 | 登录、第三方预授权、手机号、用户、模板、银行、证书、上传 | 宿主授权与账号/租户隔离；协议 DTO 不直接作为前端登录结果；不公开服务器凭据 HTTP 入口。路由及 DTO 的变更同步下游 |

`Scenario.Web` 当前映射 JSAPI；`Scenario.Mobile` 没有独立 H5 请求与结果处理，不能据枚举声称 H5 已实现。`profit_sharing` 字段不等于实现分账服务。商家转账、账单下载、小程序订阅消息等属于独立扩展。组件/授权者令牌、授权委托调用和普通服务商特约商户管理遵循下文各自的实现契约；真实账号和支付权限须另行验证。

<a id="capability-matrix-2"></a>

### 支付角色与配套接口

下表中的路径不含 `https://api.mch.weixin.qq.com`。普通商户支付前缀 `D=/v3/pay/transactions`，服务商支付前缀 `P=/v3/pay/partner/transactions`；APIv3 退款前缀 `R=/v3/refund/domestic/refunds`。引用 D/P/R 仅缩短表格，不引入生产代码别名。

| 产品与角色 | 接口与版本 | 配套查询、关单/撤销及退款 | Automao 落点与验收 |
| --- | --- | --- | --- |
| JSAPI/小程序，普通商户 | `POST D/jsapi`，仅 V3 | `GET D/out-trade-no/{out_trade_no}` 或 `D/id/{transaction_id}`；`POST D/out-trade-no/{out_trade_no}/close`；退款使用 R | `PaymentProvider.PayAsync/GetAsync/CancelAsync`、`RefundmentProvider`；AppID/OpenID 同属应用，调起参数正确，终态由查单/通知确认。[P1] |
| JSAPI/小程序，服务商 | `POST P/jsapi`，仅 V3 | P 下相同查单/关单后缀；退款使用 R 并携带 `sub_mchid` | 同一提供者经账户关系选择服务商；区分 `sp_appid/sp_openid` 与 `sub_appid/sub_openid`，不跨特约商户。[P1][P3] |
| APP，普通商户/服务商 | `POST D/app` 或 `P/app`，仅 V3 | 按 D/P 角色查询及关单；R 退款 | `PaymentProvider` 的 Scenario 分支及客户端调起结果；APP 签名参数与 JSAPI 分别验证。[P1] |
| Native，普通商户/服务商 | `POST D/native` 或 `P/native`，返回 V3 的 `code_url` | 按 D/P 角色查询及关单；R 退款 | `PaymentProvider` 和返回结果映射；二维码内容不当作预支付单号或成功终态。[P1] |
| 付款码，普通商户/服务商 | `POST /pay/micropay` ，使用 V2；通过独立 Tickets 服务提交与验证结果 | V2 `POST /pay/orderquery`、`POST /secapi/pay/reverse`；退款按下文选定 V3 流程 | `PaymentProvider` 的 Ticket 分支及支付业务服务；USERPAYING/超时不能完成业务，撤销与关单不是同一操作。[P2][P4] |
| 在线刷脸，当前普通商户/服务商凭据封装 | `get_wxpayface_authinfo` ，使用对应 V2 产品 | 设备取得 face_code 后按付款码流程提交、查询和必要撤销；退款按角色使用 R | `PaymentRecognizer`、`PaymentProvider`；设备侧不在框架测试范围，后台仍验证待处理与完成结果。[F1][P4] |
| 离线刷脸，当前 Broker 分支 | `/v3/offlineface/authinfo`、`/v3/offlineface/transactions` ，使用专用 V3 | 专用查单保留 business_product_id；认证、扣款与签约分别绑定机构、用户、设备和商户身份 | `PaymentRecognizer`、`PaymentProvider`；退款须沿同一交易身份核对，K12 准入和真实订单留到产品联调。[F2] |
| 订单退款，普通商户/服务商 | `POST R`、`GET R/{out_refund_no}` ，使用 V3 | 服务商保留 `sub_mchid`；申请、查询、通知状态分别处理，重试使用原退款单号 | `RefundmentProvider`、`RefundmentHandler`；订单金额与用户实付金额分开，受理不等于到账，累计退款由业务原子校验。[P3][P5] |

角色、商户号、AppID 和协议版本由明确的订单/账户上下文决定，不能以证书是否存在或是否收到 HTTP 200 推断产品、协议及支付结果。进件、银行、上传、会员及身份接口沿用上方逐项入口和下方 Automao 落点，不从基础支付角色推定其它产品权限。

<a id="capability-matrix-3"></a>

#### V2 付款码与退款的选用

1. 官方付款码产品的同版本闭环为 `micropay` → `orderquery` → 必要时 `reverse`。撤销需要商户双向证书，查询不需要双向证书；二者都仍需各自请求签名与应答校验。查询获得明确结果后按业务状态处理，不能用 V3 `close` 代替付款码撤销。[P4]
2. 官方同时列有 V2 `/secapi/pay/refund` 和 `/pay/refundquery`，前者需要双向证书、后者不需要。它们是官方可选协议，不代表本库已有对应实现；当前 `RefundmentService` 只有 V3 退款入口。[P4]
3. 普通商户及服务商 FAQ 明确支付和退款可以跨 V2/V3，但不建议跨版本。项目保留现有 V3 退款并补齐付款码所必需的 V2 查询/撤销，避免仅因下单为 V2 就新增一套被 V3 覆盖的退款实现。此取舍适用于已核定的境内普通商户/普通服务商支付，不能推广到平台收付通、其它特殊产品或任意产品订单。[P5]
4. V3 退款申请显式配置 `notify_url`，与当前 V3 通知协议一致。商户后台手工退款以及部分未传 `notify_url` 的配置可能发送 V2/XML 通知，不能交给 V3 JSON/GCM 解析器。此类业务须明确采用查询补偿或独立通知接入，不能因解析失败而丢失退款状态。[P5]
5. 离线测试验证各角色请求、V2 撤销与 V3 退款身份的衔接、状态不明时查单及重试；对应商户的跨版本实测、凭据配置和产品权限属于真实联调验收。无账号阶段不把跨版本路径标为微信实测通过。

<a id="capability-matrix-4"></a>

### SaaS 能力

| 能力 | 契约与边界 |
| --- | --- |
| 第三方平台授权 | 组件配置、验证票据、预授权码、授权交换和事件；公众号/小程序按账号类型与权限集区分，可靠接收后应答 |
| 凭据与委托调用 | 组件令牌共享缓存，授权者令牌由权威持久化存储与版本条件刷新；委托调用核对实际权限，撤销后停用，保持自有账号协议独立 |
| 普通服务商特约商户 | PartnerService 普通服务商进件及查询；平台管理端记录实际核实的 AppID 与产品关系；与平台收付通分别建模，见[接入契约](#partner-onboarding) |
| Automao 授权业务 | ComponentStore 与 PartnerRegistry 分别持久化并由插件装配；商户管理与平台确认分站点授权，支付使用前检查当前关系，见[Automao 契约](../../../../../Automao/server/externals/wechat/README.zh-Hans.md) |

账号资格、官方依据与个人测试路径见[SaaS 开发与测试账号](#saas-development-and-test-accounts)。个人第三方平台测试可覆盖实际获准的授权流程；真实服务商支付仍需要合格主体和商户产品权限。代码上传、审核发布、代注册及服务商组件不包含在上述目标中。

<a id="capability-matrix-5"></a>

### Automao 适配落点

以下路径相对 [Automao/server](../../../../../Automao/server)。公开契约变化时逐项追踪成员引用，不只检查项目依赖。

| 范围 | 主要文件 | 验收重点 |
| --- | --- | --- |
| 账户、商户与密钥 | `externals/wechat/abstraction/src/AccountProvider.cs`、`AuthorityAccountProvider.cs`、`AuthorityAccountSetting.cs`、`CertificateExtension.cs`、`Utility.cs` | Account/Branch 同源，服务商与子商户绑定，配置快照与密钥更新 |
| 登录 | 同目录 `WechatAuthenticatorBase.cs`、`AccountVerifier.cs`，`sites/{administration,business,customer}/src/WechatAuthenticator.cs` | 小程序不调用公众号用户接口，UnionID 可空，服务器凭据不下发，租户限制 |
| 支付和退款 | `modules/paying/providers/wechat/src/PaymentProvider.cs`、`RefundmentProvider.cs`、`Utility.cs`、`PaymentHandler.cs`、`RefundmentHandler.cs` | TicketRequest 不直接等于成功；调起参数、金额币种、原子状态与重复/乱序处理 |
| 刷脸签约及进件 | `externals/wechat/abstraction/src/TenantAgreementProvider.cs`、`UserAgreementProvider.cs`、`UserRevocationProvider.cs`、`OfflineFaceAgreementHandler.cs`、`AttachmentSynchronizer.cs` | 进件角色、签约身份、敏感字段、专用状态查询和附件所有权 |
| 会员 | `modules/membership/providers/wechat/src/CardProvider.cs`、`CardTemplateProvider.cs`、`Utility.cs` | 当前存在 NotImplementedException 占位；框架已有 API 修复不等于下游完整会员业务已实现 |
| 回调装配 | 上述三个目录的 `*-gateway.plugin`，`hosting/web/gateway` | `/Workbench/Externals/Wechat/Fallback/Handlers` 注册及新协议入口同步，未验证请求不能进入处理器 |
| 引用与部署 | 各 `.csproj`、`hosting/web/web.deploy`、`hosting/.deploy/web.deploy`、`hosting/.deploy/daemon.deploy` | 本地 HintPath 指向当前源码构建的程序集，NuGet 回退版本同步；不执行部署脚本 |

金额/订单/租户校验须沿 Automao 业务服务及事务边界核查；不能仅凭入口 Handler 调用了 `OnPaidAsync` / `OnRefundedAsync` 就认定通过。

<a id="capability-matrix-6"></a>

### 官方来源

支付 Skill 本地文档相对 `wechatpay-payment-integration/assets/微信支付官网文档`；以下文档 ID 可直接定位同名 Markdown。官网与本地资料覆盖不完全一致，刷脸和会员卡以相应官方网页补充。

| 索引 | 已核对的官方来源 |
| --- | --- |
| P1 | [普通商户 JSAPI 下单](https://pay.weixin.qq.com/doc/v3/merchant/4012791856)、[小程序下单](https://pay.weixin.qq.com/doc/v3/merchant/4012791897)、[服务商 JSAPI 下单](https://pay.weixin.qq.com/doc/v3/partner/4012738519)、[普通商户 Native 下单](https://pay.weixin.qq.com/doc/v3/merchant/4012791877)、[APP 下单](https://pay.weixin.qq.com/doc/v3/merchant/4013070347)。本地路径为 `APIv3/{普通商户,合作伙伴}/支付产品/{产品}/API列表/` |
| P2 | [普通商户付款码 V2 声明](https://pay.weixin.qq.com/doc/v3/merchant/4012647301)、[服务商付款码 V2 声明](https://pay.weixin.qq.com/doc/v3/partner/4012851192)、[普通商户开发指引](https://pay.weixin.qq.com/doc/v2/merchant/4011936672)、[服务商开发指引](https://pay.weixin.qq.com/doc/v2/partner/4011941160)。本地路径为 `APIv2/{角色}/支付产品/付款码支付/` |
| P3 | [普通商户申请退款](https://pay.weixin.qq.com/doc/v3/merchant/4013071036)、[服务商申请退款](https://pay.weixin.qq.com/doc/v3/partner/4013080625)。本地路径为 `APIv3/{角色}/支付产品/订单退款/API列表/` |
| P4 | V2 付款码：[普通商户查询](https://pay.weixin.qq.com/doc/v2/merchant/4011937320)、[撤销](https://pay.weixin.qq.com/doc/v2/merchant/4011937361)、[申请退款](https://pay.weixin.qq.com/doc/v2/merchant/4011937624)、[查询退款](https://pay.weixin.qq.com/doc/v2/merchant/4011939577)；[服务商查询](https://pay.weixin.qq.com/doc/v2/partner/4012202507)、[撤销](https://pay.weixin.qq.com/doc/v2/partner/4012218602)、[申请退款](https://pay.weixin.qq.com/doc/v2/partner/4011941447)、[查询退款](https://pay.weixin.qq.com/doc/v2/partner/4011941513)。本地路径为 `APIv2/{角色}/支付产品/付款码支付/API列表/` |
| P5 | 跨版本规则：[普通商户 JSAPI FAQ](https://pay.weixin.qq.com/doc/v3/merchant/4012791869)、[服务商 JSAPI FAQ](https://pay.weixin.qq.com/doc/v3/partner/4013334850)；退款回调格式与角色限制：[普通商户退款 FAQ](https://pay.weixin.qq.com/doc/v3/merchant/4013071200)、[服务商退款 FAQ](https://pay.weixin.qq.com/doc/v3/partner/4013080629)。本地路径为 `APIv3/{角色}/支付产品/{JSAPI支付,订单退款}/常见问题-<文档ID>.md` |
| S1 | [公钥验签](https://pay.weixin.qq.com/doc/v3/merchant/4013053249)、[敏感字段加密](https://pay.weixin.qq.com/doc/v3/merchant/4013053257)。本地路径为 `APIv3/普通商户/通用规则/开发须知/` 下对应验签/加密章节 |
| S2 | [回调通知注意事项](https://pay.weixin.qq.com/doc/v3/merchant/4012075420)。本地路径为 `APIv3/普通商户/通用规则/最佳实践/回调通知注意事项-4012075420.md` |
| F1 | [在线刷脸 Android 接入文档](https://pay.wechatpay.cn/wiki/doc/wxfacepay/develop/android/facepay.html)，重点是获取调用凭证、发起支付、查询和撤销 |
| F2 | [K12/离线刷脸文档](https://pay.wechatpay.cn/wiki/doc/wxfacepay/develop/k12-development-guidelines.html)、[离线刷脸接入注意事项](https://pay.wechatpay.cn/wiki/doc/wxfacepay/develop/access-specifications.html)，重点是后台接口和专用查单 |
| M1 | [平台收付通提交进件](https://pay.weixin.qq.com/doc/v3/partner/4012713017)，本地路径 `APIv3/合作伙伴/平台收付通-电商交易解决方案/商户进件/API列表/提交申请单-4012713017.md`；银行与上传从该文档引用的配套接口逐项核对 |
| M2 | [会员卡产品](https://pay.wechatpay.cn/doc/v3/merchant/4012157935)、[模板查询及 AppID 约束](https://pay.wechatpay.cn/doc/v3/merchant/4012547108)、[导入 code](https://pay.wechatpay.cn/doc/v3/merchant/4012545728)、[作废会员卡](https://pay.wechatpay.cn/doc/v3/merchant/4012549677)、[删除模板](https://pay.wechatpay.cn/doc/v3/merchant/4012549682) |
| W1 | [稳定版 access_token](https://developers.weixin.qq.com/doc/service/api/base/api_getstableaccesstoken.html) |
| W2 | [JS-SDK](https://developers.weixin.qq.com/doc/service/guide/h5/jssdk.html) |
| W3 | [小程序登录](https://developers.weixin.qq.com/miniprogram/dev/framework/open-ability/login.html) |
| W4 | [手机号兑换](https://developers.weixin.qq.com/miniprogram/dev/server/API/user-info/phone-number/api_getphonenumber.html) |
| W5 | [公众号网页授权](https://developers.weixin.qq.com/doc/service/guide/h5/auth.html) |
| W6 | [服务号 API 目录](https://developers.weixin.qq.com/doc/service/api/) |
| W7 | [消息加解密](https://developers.weixin.qq.com/doc/service/guide/dev/push/encryption.html)、[开放平台接口参考](#platform-reference-11) |

官方资料仍有需要按具体接口澄清的细节：K12 查单 URL 使用 `sub_mchid`，同页表格存在 `sup_mchid` 拼写；OAuth 页面顶部限制与旧字段表不完全一致。实现应记录采用的明确接口契约并验证，不能把资料矛盾或未实际联调写成确定支持。

<a id="merchant-credentials"></a>

## 微信支付商户与密钥契约

商户材料属于微信主库，通过现有 `.option`、`AuthorityUtility` 和提供者装配。主库不增加 Core 接口或插件注册节点。应用账号的 AppSecret、商户 API 密钥和微信验签材料分别配置，不能互相替代。

<a id="merchant-credentials-1"></a>

### 材料角色

| 成员 | 用途与限制 |
| --- | --- |
| `Authority.Code` | 当前签名商户号；服务商调用使用服务商身份，子商户作为独立参数传入 |
| `Authority.Signing` | `SigningCredential` 将商户号、商户 API 证书序列号、RSA 2048 位私钥及有效期绑定；序列号为十六进制，不能填写 `PUB_KEY_ID_…` |
| `Authority.ApiV2Key` | 仅用于仍采用 APIv2 的协议签名；提供时必须为 32 个单字节字符 |
| `Authority.ApiV3Key` | APIv3 GCM 资源解密密钥；提供时必须为 32 个单字节字符，与 APIv2 密钥独立 |
| `Authority.Verification` | 当前商户快照的 `Verifier`；选择公钥或平台证书模式，不能在同一集合混装 |
| `VerificationKey` | 只持有微信 RSA 公钥及标识/有效期，提供验签与 OAEP-SHA1 敏感字段加密；不持有私钥 |

没有签名/解密需求的子商户身份可省略相应材料，调用要求材料的操作时仍必须具备它们。不存在通用 `Secret` 到两种 API 密钥的推断。`IAuthority` 和 `Authority` 的 JSON 序列化排除 API 密钥；配置对象仍含秘密，不能直接用于响应或日志。

<a id="merchant-credentials-2"></a>

### 显式文件配置

以下是宿主 `.option` 的示意片段。文件必须由可信渠道准备，示例值不能直接用于真实支付。

```xml
<authorities default="merchant">
	<authority authority.name="merchant" code="MERCHANT_ID" apiV3Key="33333333333333333333333333333333">
		<signing serialNumber="0123456789ABCDEF" privateKeyFile="./certificates/merchant/private.pem" certificateFile="./certificates/merchant/certificate.pem" />
		<verification mode="PublicKey" encryptionKeyId="PUB_KEY_ID_EXAMPLE">
			<keys>
				<key key.name="PUB_KEY_ID_EXAMPLE" file="./certificates/wechat/public.pem" />
			</keys>
		</verification>
	</authority>
</authorities>
```

该片段位于 `<options><option path="/Externals/Wechat">` 内。应用绑定继续通过 `apps` 配置。`privateKeyFile` 为商户私钥 PEM；可选的 `certificateFile` 为对应商户证书 PEM，加载时检查序列号与公私钥是否匹配，并采用证书有效期。省略商户证书文件时，私钥的有效期和证书序列号正确性由材料提供者负责。文件路径交给框架文件系统，按宿主现有路径规则解析，不扫描目录或猜测文件名。

保留平台证书方式时，显式使用下列配置；`key.name` 必须与 PEM 内实际序列号完全一致。平台证书模式自动选择当前有效且生效时间最新的证书加密，不设置 `encryptionKeyId`。公钥模式只有一项时可省略该选项，多项时必须指定用于加密的公钥 ID。

```xml
<verification mode="PlatformCertificate">
	<keys>
		<key key.name="PLATFORM_CERTIFICATE_SERIAL" file="./certificates/wechat/platform.pem" />
	</keys>
</verification>
```

<a id="merchant-credentials-3"></a>

### 加载、更新与所有权

```csharp
var authority = await AuthorityUtility.GetAuthorityAsync("merchant", cancellation);
var payment = Zongsoft.Externals.Wechat.Paying.PaymentManager.Get(authority);
var key = await authority.GetCertificateAsync("PUB_KEY_ID_EXAMPLE", cancellation);
```

`GetAuthorityAsync` 要求配置存在；`FindAuthorityAsync` 允许返回 null。`CreateAsync(AuthorityOptions, cancellation)` 支持提供者显式加载配置。它们等待文件读取并验证全部材料，成功后才返回完整商户快照；加载失败不会把半成品或失败结果写入全局缓存。

自定义回调商户查询使用 `FallbackHandlerBase.GetAuthorityAsync(code, cancellation)`，涉及 I/O 的派生实现应在此异步完成。默认同步 `IServiceProvider<IAuthority>` 扩展只应交付已准备好的内存对象，不在转换方法中阻塞加载。

商户获取不缓存文件读取结果；文件以共享只读方式打开，允许并发加载同一版本材料。宿主可持有已加载的快照供多个操作使用，并在配置更新时先完整加载和验证新快照，再替换其引用。已有操作继续使用原快照中的商户号、序列号和私钥，不会读取一半新、一半旧的材料。多文件更新应使用新版本路径并在准备完成后发布配置。`Accounts` 保留既有可变集合接口，宿主发布后不应并发修改应用绑定。

`PaymentManager.Get(master, subsidiary)` 接受两个已准备好的 `IAuthority`，不根据字符串商户号同步查找配置。付款管理器不按商户号全局缓存；相同商户号的不同租户快照不会混用配置。APIv3 HTTP 客户端按完整商户快照对象弱引用缓存，同时绑定签名与验签材料；调用方借用客户端，不负责释放它。传输与响应契约见 [HTTP 管线](#http-pipeline)。

`SigningCredential.FromCertificate` 复制已经加载且可导出的 Core `ICertificate` 私钥，既不读取文件，也不接管或释放借入证书。调用方负责原证书生命周期。不可导出的系统私钥不能复制为本快照，应使用显式 PEM 私钥与公开商户证书配置，不修改系统密钥导出策略。快照保存编码后的托管密钥数据；每次密码运算创建并释放本次 RSA 实例，不长期保存原生 RSA 或 X.509 句柄。托管材料随快照回收，不承诺即时清零。加载器和证书下载器释放其自有流、HTTP 应答、客户端和密码器。

<a id="merchant-credentials-4"></a>

### 信任、轮换与刷新

- 公钥模式只使用配置中的 `PUB_KEY_ID_…` 与对应公钥 PEM，不自动下载平台证书。未知 ID 返回 null；通过可信配置发布完成公钥替换。
- 平台证书模式的首次材料必须由官方工具等可信渠道在应用外取得。没有有效的已信任证书时，刷新失败；不能通过本次未验证下载的证书验证自己。
- `RefreshCertificatesAsync` 使用商户签名请求 `/v3/certificates`。只接受已有可信且有效的平台证书签名的响应，先按原始字节验签和检查五分钟时间窗口，再用 APIv3 密钥解密。非法头、未知签名序列号、错误签名/算法/密文、重复序列号、证书标识不一致均拒绝；同序列号替换公钥也拒绝。
- 批次全部验证通过才替换可信集合。有效期取 X.509 期限与接口 `effective_time` / `expire_time` 的交集；接口弃用时间较早时必须提前停用，更新中不保留接口已移除的证书。
- 每个快照独立合并并发刷新，刷新尝试间隔至少一分钟。冷却期重用上一次任务结果，包括错误；未知序列号不会导致连续下载。单个调用方取消只停止自身等待，底层请求有独立的 30 秒超时。
- `GetCertificateAsync` 返回 `VerificationKey`；参数可为精确 ID/序列号，省略时选择加密材料。证书模式超过十小时未刷新、材料不足 36 小时有效期或查找未知序列号时尝试刷新；成功后仍未知则返回 null，刷新错误继续抛出。过期材料不用于密码操作。
- 不内置后台计时器。长期运行的宿主应保留当前快照，并按小于十二小时的间隔显式刷新、观察失败；全部可信证书过期后须通过可信渠道重新准备材料。仅不断重新加载配置不能代替定期证书更新。

证书下载支持宿主注册的 .NET `IHttpClientFactory`，使用客户端名称 `Wechat.Certificates`；未注册时使用主库共用的有连接期限的传输层。下载正文上限为 4 MiB，证书批次上限为 32 项。

支付 HTTP 管线另行验证支付、退款、刷脸、进件、银行和上传的 APIv3 响应；证书下载仍使用独立可信引导流程。支付通知和会员卡的业务约束见 [README](../README.zh-Hans.md)，不能由商户材料或 HTTP 响应验证推定整个业务链路已验证。

<a id="merchant-credentials-5"></a>

### 官方依据
- [微信支付公钥验签](https://pay.weixin.qq.com/doc/v3/merchant/4013053249)：公钥 ID 与验签原文。
- [平台证书验签](https://pay.weixin.qq.com/doc/v3/merchant/4013053420)：平台证书方式和首次可信获取。
- [下载平台证书](https://pay.weixin.qq.com/doc/v3/merchant/4012551764)：证书更新、接口时间字段及证书资源。
- [资源解密](https://pay.weixin.qq.com/doc/v3/merchant/4012071382)：APIv3 密钥与 AES-GCM 参数。

V2 付款码撤销需要完整商户 X.509 证书与私钥。`SigningCredential` 保留经匹配验证的可选证书；`Signing.CertificateFile` 与私钥文件共同加载，已有 X.509 `ICertificate` 也可通过 `FromCertificate` 转换。仅含 RSA 私钥的材料仍可签名 V3，但不能用于双向 TLS 撤销，详见[付款码契约](#ticket-payments)。

Automao 的账户设置 `SigningCertificate` 可提供配套公开商户证书 PEM，与现有 RSA 证书提供者已加载的私钥组合；转换时同样核对证书序列号、公私钥及有效期。不在该字段保存私钥。

<a id="http-pipeline"></a>

## 微信支付 APIv3 HTTP 契约

本管线属于微信主库，覆盖 `PaymentManager` 的 APIv3 调用、`MerchantService`、`BankUtility`、`Paying.Uploader` 和 `Membership`。入口是 `Paying.HttpClientFactory.GetHttpClient(IAuthority)`，同时要求匹配的商户签名身份与微信验签材料，配置方式见[商户与密钥契约](#merchant-credentials)。APIv2/XML、公众号/小程序 HTTP 和通知网关不属于本管线；它们的当前限制见 [README](../README.zh-Hans.md)。

<a id="http-pipeline-1"></a>

### 请求与签名

- 客户端按完整商户快照的对象身份弱引用缓存；即使商户号或签名材料相同，不同快照也不共用客户端。调用方借用客户端，不释放共享实例，也不在并发操作期间修改其属性或默认头。
- 请求只允许 HTTPS、443 端口、`api.mch.weixin.qq.com` 或 `api2.mch.weixin.qq.com`，路径必须以 `/v3/` 开头，不接受 URL 用户信息或片段。默认基址为 `https://api.mch.weixin.qq.com/v3/`。
- 签名输入使用实际请求方法、已转义的路径与查询、Unix 秒时间戳、随机数和原始正文字节；按官方规则以换行符分隔并保留末尾换行。普通正文只序列化并缓冲一次，传输复用这些字节。没有请求对象的 POST 使用空正文。
- multipart 请求必须包含且只包含一个 JSON `meta` 部分，允许其名称带引号。只缓冲并签名 `meta` 正文，文件与 multipart 边界不进入签名。`Uploader` 的文件读取和 multipart 所有权由上传方法承担；产品级大小、类型和摘要规则另由上传契约约束。
- `Wechatpay-Serial` 位于请求头，不能位于内容头。敏感字段加密调用显式传入所用微信公钥 ID 或平台证书序列号；未指定时选择当前加密材料。未知材料立即失败，不用商户 API 证书序列号替代。`Authorization` 单独使用商户签名身份。

<a id="http-pipeline-2"></a>

### 响应验证

读取响应后，先验证原始字节，再解析 JSON。正文上限为 4 MiB，声明长度与实际读取长度都受限制。包括 204 空正文在内的成功响应，必须提供唯一且非空的 `Wechatpay-Serial`、`Wechatpay-Timestamp`、`Wechatpay-Nonce`、`Wechatpay-Signature`；有 `Wechatpay-Signature-Type` 时只接受 `WECHATPAY2-SHA256-RSA2048`。时间戳允许与当前 UTC 时间相差最多五分钟，宿主须保持时钟同步。

验签覆盖时间戳、随机数和原始正文的字节，不能先反序列化再重建正文。缺失/重复头、非法 Base64、未知公钥、过期时间、正文篡改和 `WECHATPAY/SIGNTEST/` 探测均作为密码验证失败处理。公钥模式不自动发现未知 ID；平台证书模式可通过已有可信材料刷新证书并验证本次响应，不重新发送业务请求。刷新规则和冷却期遵循商户与密钥契约。

非成功响应如果完全没有 `Wechatpay-*` 头，只能形成未验证的 HTTP 失败；其 JSON 即使包含 `code`，也不成为可信业务错误码。含任何此类头时必须完成完整验签，部分头不触发降级。HTTP 成功和签名有效仅表示协议响应可信，支付、退款和签约是否完成仍由对应产品状态及宿主业务规则判断。

<a id="http-pipeline-3"></a>

### 错误、超时与重试

| 情形 | 结果 |
| --- | --- |
| 已验签的非成功响应，正文含非空字符串 `code` | `OperationException.Reason` 为微信错误码；字符串 `message` 可作为异常说明 |
| 未签名网关失败，或已验签错误正文不含有效错误码 | `OperationException.Reason` 为 `HTTP_` 加数字状态码，例如 `HTTP_502`；不把 HTML 或未验证正文带入异常 |
| 已验签成功响应不能按目标类型解析 JSON | `OperationException.Reason` 为 `INVALID_RESPONSE`，保留内部 `JsonException` |
| 响应签名/材料/时间窗口不合法，或正文超过限制 | `CryptographicException`；不返回业务成功结果 |
| 调用方取消 | 传播取消异常；不发起业务重试 |
| 客户端超时 | .NET `HttpClient` 的超时取消异常；不据此推定交易失败或成功 |

协议验证、HTTP 状态或 JSON 解析产生的异常，在 `Exception.Data` 中附带整数 `StatusCode`、布尔 `ResponseVerified`；合法单值 `Request-ID` 记录为 `RequestId`。该关联 ID 不包含在微信响应签名原文中，只供排查，不用于授权或业务去重。日志只按需读取这些字段，不记录完整请求、响应、加密字段和签名。

默认超时为 30 秒，覆盖签名准备、传输、读取和响应验签等待；已发起的共享证书刷新具有独立生命周期，调用方取消只停止自身等待。管线没有业务重试或自动重定向。超时、通信失败或未知交易状态由产品服务及宿主沿原业务单号查单/处理，不通过全局重放 POST 解决。

<a id="http-pipeline-4"></a>

### 宿主传输与所有权

签名与验签位于主库内部嵌套处理器，相关实现按请求、响应和结果解析拆为 `partial` 文件。插件清单、商户提供者和业务服务仍按既有装配方式工作，不增加 Core 公共接口。

宿主注册 .NET `IHttpMessageHandlerFactory` 时，主库借用名为 `Wechat.Payments` 的传输处理器，每次发送取得工厂当前处理器，并只释放自己的 `HttpMessageInvoker` 包装。处理器本身由工厂管理。宿主必须为此名称禁用自动重定向和自动重试，并避免在传输层改写已签名的方法、URI 或正文。若底层 `SocketsHttpHandler` / `HttpClientHandler` 仍允许重定向，发送前抛出 `InvalidOperationException`；仅注册通用工厂而未正确配置该名称不能代替支付传输配置。自定义处理器须遵守相同约束。

没有注册工厂时，使用主库共享的 `SocketsHttpHandler`，自动重定向关闭，连接生存期为五分钟。平台证书下载保持独立的 `IHttpClientFactory` 名称 `Wechat.Certificates` 和已有信任验证流程，避免验证新证书时递归调用通用支付管线。

高层 `HttpUtility.GetAsync` / `PostAsync` 创建并释放自己的请求、内容和响应；`BankUtility`、`Uploader` 也释放其自有资源。直接使用借入客户端的调用方负责释放自建请求和返回的响应；响应扩展方法 `GetResultAsync` / `EnsureResultAsync` 借用响应，不替调用方释放。管线读取并释放原始响应流/内容，成功时返回已验证字节的内容；发生验证、解析前的读取或状态错误时释放响应。解析阶段错误由拥有响应的高层方法负责清理。

<a id="http-pipeline-5"></a>

### 验证与官方依据

内存传输测试覆盖原始字节、查询编码、空正文、multipart、204、签名与时间失败、错误映射、证书轮换、取消、超时、正文上限及资源释放，入口见[验证契约](#testing)。这些测试不代表微信账号准入或 Automao 插件业务闭环已验收。账单等文件下载不经过本 JSON 管线，须另按下载协议校验摘要。
- [请求 Query 签名](https://pay.weixin.qq.com/doc/v3/merchant/4012365337)。
- [上传 meta 签名](https://pay.weixin.qq.com/doc/v3/merchant/4012365335)。
- [微信支付公钥验签与空响应](https://pay.weixin.qq.com/doc/v3/merchant/4013053249)。
- [HTTP 状态码](https://pay.weixin.qq.com/doc/v3/merchant/4012081717)与[基本规则及 Request-ID](https://pay.weixin.qq.com/doc/v3/merchant/4012081709)。
- [Wechatpay-Serial 请求头](https://pay.weixin.qq.com/doc/v3/merchant/4012365346)。

<a id="payment-models"></a>

## 支付金额、标识与结果契约

<a id="payment-models-1"></a>

### 金额与币种

境内支付、退款请求使用 `CNY`，最小货币单位为分，JSON 中使用整数。整数构造参数已经是分；decimal 构造参数是元，转换必须精确，不截断、不四舍五入。空币种使用 `CNY`，大小写可归一化，其它币种拒绝。

支付金额必须大于零，最大为 `int.MaxValue` 分，即 `21474836.47` 元。退款额和原支付总额均大于零，退款额不得大于原支付总额。负数、零、超范围值抛出 `ArgumentOutOfRangeException`；不足一分的精度和不支持的币种抛出 `ArgumentException`。金额请求结构为只读；默认结构不能赋给请求的 `Amount`。

原支付总额、用户实付额、退款额分别保留其协议意义。付款人优惠与商户结算金额不能仅凭订单总额推断。累计部分退款的并发上限属于宿主业务校验，不由单次退款 DTO 保证。

<a id="payment-models-2"></a>

### 标识与下单结果

商户号、子商户号、商户订单号和微信交易流水号均作为字符串传递；保留前导零，不转换为整数。

`PaymentService.PayAsync` 和 `PayV3Async` 返回 `PaymentService.PaymentResult`：

| 属性 | 含义 |
| --- | --- |
| `PrepaymentId` | JSAPI/小程序或 APP 下单受理凭据，不是交易流水号 |
| `CodeUrl` | Native 二维码内容，不是预支付单号 |
| `Order` | 付款码或离线刷脸返回的实际订单；须继续检查其 `Status` |

预支付与实际订单互斥。JSAPI/APP 响应缺少 `prepay_id`、Native 缺少 `code_url`，以及两者混合的响应均拒绝。普通下单不能因为缺少签名材料而自动回退到 V2。查询仍返回 `PaymentOrder`，其 `Identifier` 只表示 `transaction_id`。

Automao 的 `PaymentProviderResult.Token` 保存预支付凭据，`Identifier` 保存实际交易流水号，分别进入 `PaymentDetail.PaymentToken` 和 `PaymentCode`。Native 二维码内容进入扩展结果，不占用交易流水号。受理下单保持 Pending，实付/实收金额为零；只有实际订单明确成功才标为成功。其它支付提供者没有独立 Token 时，仍可使用自己的 Identifier 作为支付凭据。

<a id="payment-models-3"></a>

### 状态与乱序

`PaymentUtility.GetStatus` 对空值或未识别状态返回 `PaymentStatus.Unknown`；`NOTPAY`、`USERPAYING`、`SUCCESS`、`REFUND`、`CLOSED`、`REVOKED`、`PAYERROR` 分别保留现有业务意义。

`RefundmentUtility.GetStatus` 将 `ABNORMAL` 映射为独立的 `RefundmentStatus.Abnormal`，不解释为成功或永久失败；未知值返回 `Unknown`。退款异常可能需要处理收款账户并查询确认，不能另建退款单盲目重试。

Automao 将未知支付/退款状态与退款异常保留为 Pending；原始微信状态仍保留在协议 DTO 和通知接收内容中。查询结果不明时继续使用原业务单号核实，不生成新的资金请求。支付与退款汇总刷新以读取到的原状态作为数据库更新条件，已完成的终态不会被较早快照覆盖；更新未命中时，不把本地对象伪装为本次更新成功。终态修正需要独立的对账流程，不能通过普通刷新回退。

<a id="payment-models-4"></a>

### 官方依据
- [JSAPI 下单](https://pay.weixin.qq.com/doc/v3/merchant/4012791856)：金额、币种与预支付凭据。
- [申请退款](https://pay.weixin.qq.com/doc/v3/merchant/4012791862)：退款额、原支付金额及受理语义。
- [商户订单号查单](https://pay.weixin.qq.com/doc/v3/merchant/4012791859)：实际交易和支付状态。

产品调用范围另见[功能与接口核对表](#capability-matrix)，通知的验证与持久化边界见[通知契约](#payment-notifications)。

Automao 的 `PaymentProviderResult.Status` 保留提供者明确的 Pending/Succeed/Failed/Cancelled 状态，`Successful` 只对 Succeed 为真；默认结果按 Pending 处理。微信付款码超时、签名或报文验证失败不证明拒付，按原业务号查询；具体边界见[付款码契约](#ticket-payments)。

<a id="basic-payments"></a>

## APIv3 基础支付契约

<a id="basic-payments-1"></a>

### 场景与角色

`PaymentManager.Get(authority)` 使用普通商户，`Get(master, subsidiary)` 使用普通服务商与特约商户。基础支付支持 `Scenario.Web`（JSAPI/小程序）、`App`（移动应用）和 `Native`（二维码）；未定义枚举值拒绝，不提供 H5 支付入口。

| 场景 | 普通商户下单 | 服务商下单 | 下单结果 |
| --- | --- | --- | --- |
| Web | `/v3/pay/transactions/jsapi` | `/v3/pay/partner/transactions/jsapi` | `PrepaymentId` 与 JSAPI 调起参数 |
| App | `/v3/pay/transactions/app` | `/v3/pay/partner/transactions/app` | `PrepaymentId` 与 APP 调起参数 |
| Native | `/v3/pay/transactions/native` | `/v3/pay/partner/transactions/native` | `CodeUrl`；无客户端签名参数 |

基础支付必须具备商户签名和微信验签材料，不自动降级至 APIv2。付款码/在线刷脸使用独立 V2 路径，离线刷脸使用专用产品路径，不能通过改变基础支付场景替代。

<a id="basic-payments-2"></a>

### 应用与付款人

请求通过当前服务的 `Request.Create(appId, voucher, amount, currency, payer, description)` 创建。`appId` 必须属于所绑定商户或子商户配置；未指定时选择服务商/普通商户默认应用。JSAPI 的 OpenID 必须来自此次所选 AppID。使用子商户 AppID 时发送 `sub_appid`、`sub_openid`；使用服务商 AppID 时发送 `sp_appid`、`sp_openid`，不因子商户恰好配置了默认应用而改变付款人归属。APP 和 Native 不发送 payer 对象，也不要求 Automao 查询用户 OpenID。

移动应用使用 `AccountType.Application` 或 `Account.Application(appId)`。该身份只用于支付，不提供小程序/公众号登录接口，也不需要登录 Secret。配置中可直接声明：

```xml
<apps default="wx-mobile-app-id">
	<app app.name="wx-mobile-app-id" type="application" />
</apps>
```

公众号和小程序的 `channel`、`applet` 配置仍引用对应账号集合。APP 需要移动应用类型，Web 需要公众号或小程序类型，Native 可使用已绑定的任一类型。服务商使用子商户移动应用时，服务商自身也必须有可确定的移动应用；默认应用不匹配时，仅在存在唯一符合类型的应用时选择它，多项则拒绝猜测。

请求必须有业务单号、描述、有效金额和绝对 HTTP(S) 通知地址；片段和 URL 内嵌账号拒绝。调用者可通过 `FallbackUrl` 显式指定，或配置 `/Externals/Wechat/Fallback/Url` 模板（如 `https://callback.example.test/Externals/Wechat/Fallback/{0}/{1}`）。未提供场景信息时不伪造付款人 IP；设置场景字段时必须提供有效的 `Place.DeviceIp`。

<a id="basic-payments-3"></a>

### 客户端参数

`PaymentResult.Parameters` 是只读字符串字典，由本次下单相同的商户私钥生成：

| 场景 | 字段 |
| --- | --- |
| Web | `appId`、`timeStamp`、`nonceStr`、`package=prepay_id=…`、`signType=RSA`、`paySign` |
| App | `appId`、`partnerId`、`prepayId`、`packageValue=Sign=WXPay`、`nonceStr`、`timeStamp`、`sign` |

JSAPI 签名的第四行为 `prepay_id=…`；APP 为原始 prepay_id。两者都以 LF 结束每一行，采用 RSA-SHA256 并输出 Base64。服务商模式始终使用服务商私钥；客户端 `appId` 及 APP `partnerId` 按下单所选的服务商或子商户配对。iOS APP 调用方将 `packageValue` 对应到 SDK 的 `package` 字段。

Native 的 `CodeUrl` 是二维码原始内容；不得当作订单流水号或签名凭据。客户端调起返回成功仍需由服务端查单或已验证通知确认到账。

<a id="basic-payments-4"></a>

### 查询、关单与结果不明

`GetAsync(voucher)` 按商户订单号查询，`GetCompletedAsync(transactionId)` 按微信流水号查询；两者名称不代表结果必然已成功。`CancelAsync(voucher)` 使用对应角色的 V3 关单。路径中的标识和查询参数单独转义，角色与商户字段来自当前绑定上下文。查询响应须匹配请求标识、商户、已配置 AppID、子商户和金额币种；签名有效但身份不符仍拒绝。

资金 POST 不自动重试。超时或网络失败可能已经受理，宿主应保留原业务单号，通过对应产品查询确认；未知 trade_state 保留原字符串并映射为 Unknown，不把它解释成成功。关单不是付款码撤销。

<a id="basic-payments-5"></a>

### Automao

Wechat 提供者将上述客户端参数原名放入扩展结果；Native 放入 `codeUrl`。预支付 Token 与实际 PaymentCode 分开，受理下单保持 Pending。`Paying_PaymentDetail.PaymentToken` 为可空 `varchar(64)`，由[支付建表脚本](../../../../../Automao/documents/database/automao.paying-mysql.sql)及[表结构设计](../../../../../Automao/documents/database/automao.paying.md)定义。无真实账号时仅能验证合成签名、协议行为、业务映射和持久化，不能证明产品权限或真实付款成功。

<a id="basic-payments-6"></a>

### 官方依据
- [普通 JSAPI 下单](https://pay.weixin.qq.com/doc/v3/merchant/4012791856)、[服务商 JSAPI 下单](https://pay.weixin.qq.com/doc/v3/partner/4012738519)。
- [普通 APP 下单](https://pay.weixin.qq.com/doc/v3/merchant/4013070347)、[服务商 APP 下单](https://pay.weixin.qq.com/doc/v3/partner/4013080231)。
- [普通 Native 下单](https://pay.weixin.qq.com/doc/v3/merchant/4012791877)、[服务商 Native 下单](https://pay.weixin.qq.com/doc/v3/partner/4012738659)。
- [APP 调起参数](https://pay.weixin.qq.com/doc/v3/partner/4013080233)、[APP 签名](https://pay.weixin.qq.com/doc/v3/partner/4012365868)、[JSAPI 签名](https://pay.weixin.qq.com/doc/v3/partner/4012365867)。

<a id="ticket-payments"></a>

## 付款码与在线刷脸支付契约

普通商户与服务商的付款码、在线刷脸支付使用 V2 XML：提交为 `/pay/micropay`，查询为 `/pay/orderquery`，撤销为 `/secapi/pay/reverse`。基础 JSAPI、APP、Native 使用[独立的 V3 服务](#basic-payments)。离线刷脸的专用 V3 产品不能使用本页的查询和撤销。

<a id="ticket-payments-1"></a>

### 提交与结果

通过 `manager.Payment.Request.Ticket(appId, voucher, amount, currency, ticketCode, description)` 创建请求，并设置 `Place.DeviceIp` 为实际终端 IP；不填充虚构的环回地址。`amount` 的 decimal 单位为元，只接受可精确表示的正整数分和 CNY。应用必须属于相应商户；服务商应用与子商户应用分别绑定，选择子商户应用时同时传递服务商应用和 `sub_appid`。

`await manager.Payment.PayAsync(request, Scenario.Web, cancellation)` 返回 `PaymentResult.Order`。付款码不返回预支付凭据，也不在后台调用业务处理器。调用方等待返回后根据已验证状态完成业务或保存待确认状态。

- 确认成功须同时满足通信成功、业务成功、合法支付类型及订单/商户/应用/金额匹配；交易号独立于商户订单号。
- `USERPAYING` 保持支付中；系统错误、银行错误和未知错误保持结果不明。尚未确认支付的结果不包含实付金额或伪造交易号。
- 明确的付款码失效、无效、不支持卡种、余额不足映射为支付失败；订单已关闭或撤销分别映射其终态。
- 传输超时、取消、无效签名和解析错误不能证明扣款失败。原订单号必须保留，不以新订单号重试扣款。

<a id="ticket-payments-2"></a>

### 查询与撤销

`Payment.Tickets.GetAsync(appId, voucher, cancellation)` 按原商户订单号查询；`GetCompletedAsync(appId, identifier, cancellation)` 按微信交易号查询。查询结果使用 `trade_state`，不把查询接口的 `result_code=SUCCESS` 当作支付成功。查询到未知状态仍保持未知。

`Payment.Tickets.ReverseAsync(appId, voucher, cancellation)` 返回 `ReversalResult`：`Succeeded` 表示已确认撤销，`RequiresRetry` 表示微信 `recall=Y`，`ErrorCode` 保留已验签的业务错误码。撤销通信异常传播给调用方；调用方只能重试同一撤销，不能再次发起扣款。未付款订单撤销后不能继续付款，已经扣款的订单可能由撤销退还资金。

库不自动重放请求或执行无限轮询。宿主按原单查询、控制轮询时间并可靠保存补偿任务；微信建议系统错误后等待约 5 秒查询，用户支付中约每 10 秒查询，超过约 45 秒仍不明时考虑撤销，撤销不应紧接扣款发起，建议至少间隔 15 秒。具体交互见[普通商户付款码](https://pay.wechatpay.cn/doc/v2/merchant/4011937125)与[服务商付款码](https://pay.wechatpay.cn/doc/v2/partner/4011941293)。

<a id="ticket-payments-3"></a>

### V2 协议与证书

V2 使用独立的 `ApiV2Key`，不使用 APIv3 私钥生成字段签名。字段按 Ordinal 排序，排除 `sign`、null 和空字符串，数值采用固定文化格式；支持 MD5 与 HMAC-SHA256。XML 写入执行转义，读取限制为 2 MiB，拒绝 DTD、嵌套字段、重复字段、命名空间及多余根节点。

通信成功应答必须完成签名验证，签名算法与请求一致，随后核对商户/应用上下文及产品业务字段。通信失败仅形成失败异常，不被解释为确定的交易终态。请求和响应均由协议管线释放；没有自动重定向、扣款重试或不可等待的线程池回调。

撤销必须具备匹配的商户 X.509 证书和私钥。`SigningCredential` 可接收 `certificate` PEM，或通过 `FromCertificate` 复制已有且私钥可导出的 X.509 材料；配置加载使用 `Signing.CertificateFile`。证书序列号、公钥、私钥及有效期必须一致，只有私钥和序列号不足以完成双向 TLS。服务商使用服务商证书，不使用子商户证书。

宿主注册 `IHttpMessageHandlerFactory` 时，普通 V2 请求使用 `Wechat.Payments.Xml`；双向 TLS 使用 `Wechat.Payments.Xml.{商户号}.{证书序列号}`。宿主负责为后者配置包含私钥的对应客户证书，并为两者禁用重定向和重试。通用工厂的默认配置不能代替这些具名配置。未注册工厂时，库使用无重定向传输；撤销请求独立持有并释放双向 TLS 处理器及证书。自定义传输必须遵守相同的身份和生命周期约束。

<a id="ticket-payments-4"></a>

### 在线刷脸初始化

`AuthenticateOnlineAsync` 调用 `https://payapp.weixin.qq.com/face/get_wxpayface_authinfo`，传递设备、门店、原始数据和已选择的商户/应用。返回前验证 XML 签名和身份，并检查 `authinfo` 及存在时的正数有效期。该接口没有付款码的 `result_code` 契约，不能混用支付结果解析。`authinfo` 只供相同设备和商户上下文使用；取得调用凭据不代表付款成功。参见[微信刷脸支付文档](https://pay.wechatpay.cn/wiki/doc/wxfacepay/develop/windows/facepay.html)。

<a id="ticket-payments-5"></a>

### Automao 业务接入

支付参数的 `ClientAddress` 提供实际终端 IP，`Device` 提供设备编号；两者传递给微信提供者，IP 不作为权限证明。付款码参数中的 `Argument` 是付款码，不能按 JSAPI/APP 场景枚举解析。

微信提供者返回 `PaymentProviderResult.Status`，只有确认成功时 `Successful` 为真。网络超时和协议验证失败保持 Pending，原单保留以供查询。调用方取消时，支付业务先用独立于已取消令牌的必要写入保存待确认状态，取消尚未执行的后续提供者，再传播取消异常。

支付服务的取消入口验证 Cancel 权限及站点查询范围，再调用提供者。组合支付必须完整可见；微信付款码按同一 V2 产品查单和撤销，`recall=Y` 或撤销失败不会写成已取消。确认后只条件更新 None/Pending 明细，再汇总支付单；并发完成的终态不会被覆盖。客户端可以沿原键值调用 Refresh 查询，不重新创建扣款订单。

本页的协议和业务契约由离线测试验证，合成证书验证材料绑定与传输选择，不证明微信侧 TLS 接受、真实扣款、设备 SDK、商户准入或刷脸产品权限。真实联调需要获准账号与公网环境。

已收到提供者结果后的状态保存与终态处理不使用调用者的取消令牌。请求取消会阻止尚未开始的支付，并在已确认结果保存完成后传播给调用者；零金额支付仍遵循原请求取消。

<a id="refunds"></a>

## 订单退款

`PaymentManager.Refundment` 使用境内普通商户或普通服务商的 APIv3 订单退款接口。商户请求签名和原始应答验签沿用 [HTTP 管线](#http-pipeline)。付款码订单可按[版本选用约定](#capability-matrix-3)继续调用该入口；实际商户的产品权限与跨版本交易仍须真实联调。

<a id="refunds-1"></a>

### 申请、查询和通知

通过对应管理器的 `Request.Create` 构建请求。原支付单号、商户退款单号、订单总金额、退款金额及币种在重试时保持一致。请求须设置有效的 `FallbackUrl`；配置模板可生成该地址，调用者也可显式指定。通知 URL 不接受查询参数、用户信息或片段。

`RefundAsync` 返回 `RefundmentResult`，其 `Order` 是此次申请受理时的状态快照。受理不表示资金到账；通过 `GetAsync` 的查询结果或已经验证的退款通知确认最终状态。服务商请求携带 `sub_mchid`，退款申请和查询使用服务商的签名身份。

申请与查询响应使用 `status`，通知资源使用 `RefundmentNotification.refund_status`；两个字段不互相回退。应答没有强制要求商户号字段，返回订单的商户上下文来自已签名请求；如果应答额外提供身份字段，则必须与上下文匹配。通知身份仍来自解密报文并在分派前验证。

金额均为 CNY 整数分；订单总金额、实际现金支付金额、退款总额和实际现金退款金额分别保留。申请金额必须大于零，不超过原订单总额。请求单号、金额和应答单号不匹配时拒绝结果。

<a id="refunds-2"></a>

### Automao 状态与重试

- 申请受理和网络、超时或协议不明结果保持 `Pending`。取消请求不会把可能已经发生的退款直接写成失败。
- 查询 `PROCESSING` 保持待确认；`ABNORMAL` 也保留为待确认，同时在说明中保留异常状态，需要查询或异常退款处理。只有查询或通知 `SUCCESS` 确认成功，`CLOSED` 确认关闭；未知状态保持待确认。
- 业务退款入口的正数金额表示本次部分退款，零表示所有剩余额度；失败和关闭的明细不占用额度，未开始与待确认的明细保守占用额度。多个业务支付单逐笔创建退款，部分失败时保留已创建单号用于对账，不承诺跨支付单的资金原子性。
- 显式按退款 ID 重试失败单时重新进入待确认；查询和通知不会自动重开失败终态。重试已经尝试过的退款时先查原商户退款单号；明确不存在时，仍用原单号及原金额申请。传输层不自动重放退款 POST。
- 提供者核对退款单、原支付单、商户、门店和金额。组合支付核对同一微信交易的业务成员，不使用其中一笔的金额代替微信原订单总额。
- 发送退款请求前，在可串行化事务中统计其它未开始、待确认、成功及不明确的退款额度，并将本次明细保留为待确认。事务提交后才发送资金请求；外部事务范围中的提交被拒绝。待确认额度不会因超时被释放。保留额度发生冲突时不发送退款请求。
- 查询、申请回写和通知通过条件更新保护终态。完成事件由成功推进主表终态的处理者触发。通知接收箱按退款单号与协议状态区分业务事件，允许异常状态后收到成功；相同状态重投不会重复处理，迟到的非终态不回退成功。

后台手工退款可能使用独立退款单号或 V2 通知。当前 Gateway 只接受其配置的 V3 通知，不将 V2 XML 交给该解析器。此类退款使用 `GetAsync(商户退款单号)` 查询补偿，并由宿主先建立与原业务订单的关联；未知业务退款单不会自动创建或转记。外部后台产生的退款也需纳入业务对账，不能由本地累计额度检查推断不存在。

<a id="refunds-3"></a>

### 官方依据
- [普通商户申请退款](https://pay.weixin.qq.com/doc/v3/merchant/4013071036)、[服务商申请退款](https://pay.weixin.qq.com/doc/v3/partner/4013080625)。官方 Skill 本地文档：`APIv3/{普通商户,合作伙伴}/支付产品/订单退款/API列表/申请退款-*.md`。
- [服务商查询退款](https://pay.weixin.qq.com/doc/v3/partner/4013080626)、[服务商退款结果通知](https://pay.weixin.qq.com/doc/v3/partner/4013080241)。本地文档分别位于 `订单退款/API列表/查询单笔退款（通过商户退款单号）-4013080626.md` 和 `APP支付/API列表/退款结果通知-4013080241.md`。

<a id="offline-face"></a>

## 离线刷脸与签约契约

离线刷脸使用服务商及特约商户上下文，要求相应产品权限。协议依据为微信支付的 [K12/离线刷脸后台接口](https://pay.wechatpay.cn/wiki/doc/wxfacepay/develop/k12-development-guidelines.html)和[接入注意事项](https://pay.wechatpay.cn/wiki/doc/wxfacepay/develop/access-specifications.html)。本库不提供设备 SDK；合成协议测试不能证明真实账号准入或设备支付可用。

<a id="offline-face-1"></a>

### 认证与扣款

`PaymentManager.Get(serviceProvider, subsidiary).Payment` 提供 `AuthenticateOfflineAsync`。请求绑定服务商应用、子商户、机构和设备，返回相同身份的认证材料。设备号为 1—32 位 ASCII 字母、数字或下划线；原始设备数据上限为 2048 个 UTF-8 字节。普通商户上下文不支持此产品。

通过 `Request.Ticket` 的业务参数重载创建离线扣款请求。K12 产品号为 2，场景为 3—6；团餐产品号为 11，场景为 124—126。请求保留选定的应用和子商户，金额使用 CNY 分，携带授权码、说明、附加数据和设备 IP。扣款使用 `/v3/offlineface/transactions`，未收到确定结果时保留原单号，不自动重发扣款。

`Payment.GetOfflineAsync(voucher, product)` 使用专用查单路径，查询串始终包含 `sp_mchid`、`sub_mchid` 和 `business_product_id`。`OfflineOrder` 保留 `trade_state_description`、欠款状态、补扣交易号及错误类型；实付金额来自 `amount.pay_total`。支付成功必须有对应交易号和完整金额；待处理或未知状态允许缺少金额，HTTP 200 不代表支付完成。响应商户、应用和业务单号必须匹配请求上下文。

Automao 的 `face.offline`/`offline` 支付方式将 `PaymentChannel` 保存为 `K12` 或 `Groupon`，可追加 `:场景号`；默认场景分别为 4 和 125。查询使用保存的业务产品。没有对应离线撤销接口时抛出 `NotSupportedException`，不能先取消本地订单再忽略远端扣款。

<a id="offline-face-2"></a>

### 签约服务

`MerchantService.Contracts` 提供以下独立入口：

| 入口 | 用途与绑定 |
| --- | --- |
| `AuthenticateAsync` | 获取 `WEBSESSION` 授权凭证，绑定机构和商户用户号 |
| `ApplyAsync` | 获取 K12 / GROUP_DINING 预签约凭证；身份信息可选，银行卡限制模式需要完整银行卡身份；敏感字段使用与请求头一致的微信加密材料 |
| `GetSignatoryAsync` | 以机构和商户用户号查询，校验响应归属；同时保留用户状态、人脸采集状态和合同状态 |
| `GetContractAsync` | 查询指定合同，绑定商户及选定应用 |
| `RevokeAsync` | 以机构和微信刷脸用户号解约，验证空正文 204 应答 |

授权凭证和预签约凭证按官方有效期及时使用，不能作为持久身份。未知合同或用户状态保持 `Unknown`，不能解释为已签约。Automao 的现有用户申请入口返回 `WEBSESSION` 凭证；预签约及银行卡模式是主库的独立协议入口。

<a id="offline-face-3"></a>

### Automao 状态与通知

账户必须关联本地签约记录、用户、租户/机构/门店和微信组织。账户启用要求合同已签约、用户正常且已采集人脸。未知状态保持原状态并报告未确认；取消的本地协议不被刷新恢复。同一合同终止后不能被迟到的已签约结果重新启用。

刷脸通知先经框架验签和认证解密，再校验商户、应用、组织与用户。优先匹配合同号；首次签约可匹配唯一的未绑定待签记录。通知可靠保存后，处理器查询当前签约状态；当前合同已变化的旧通知不能重绑定记录。多个可能归属保持失败，不能任选第一条。

签约记录、账户启停和已确认解约记录在同一事务中提交，以观察到的状态及合同身份作条件更新。远端解约前先查验原合同、保存待确认记录，再结束本地事务并发送请求；已验签的解约成功应答需要完成本地关键更新，不因调用方随后取消而丢弃。结果不明保留原解约记录，通过刷新查询确认。已完成解约不重复提交；通知与显式申请共用状态同步逻辑。

独立 [Automao 集成夹具](../../../../../Automao/server/externals/wechat/integration/README.zh-Hans.md)覆盖真实插件装配、MySQL 状态与合成签名传输。真实微信状态变更、设备与账号权限仍需要获准环境联调。

<a id="payment-notifications"></a>

## 支付通知接收契约

<a id="payment-notifications-1"></a>

### 协议与身份

支付通知 Gateway 提供以下三个 POST 入口，第三方平台事件使用独立入口：

| 路径 | 事件 | 资源原始类型 |
| --- | --- | --- |
| `/Externals/Wechat/Fallback/payment/{merchant}` | `TRANSACTION.SUCCESS` | `transaction` |
| `/Externals/Wechat/Fallback/refundment/{merchant}` | `REFUND.SUCCESS`、`REFUND.ABNORMAL`、`REFUND.CLOSED` | `refund` |
| `/Externals/Wechat/Fallback/offlinefaceagreement/{merchant}` | `FACEPAY.USER_STATE_CHANGE` | `facepay` |

`merchant` 是普通商户号或服务商号，只接受数字，不含角色后缀。请求不带查询参数，Content-Type 为 JSON，正文上限 2 MiB。商户配置通过 `Code` 唯一匹配，动态宿主也可以覆盖处理器的 `GetAuthorityAsync` 以异步加载商户快照。

处理器检查唯一的 `Wechatpay-Serial`、`Wechatpay-Timestamp`、`Wechatpay-Nonce`、`Wechatpay-Signature` 头，时间戳允许前后五分钟。可选的 `Wechatpay-Signature-Type` 只接受 `WECHATPAY2-SHA256-RSA2048`。使用商户显式信任的微信支付公钥或平台证书对原始字节验签，然后解析事件并执行 AES-256-GCM 认证解密。配置方式见[商户凭据](#merchant-credentials)。

解析器拒绝重复 JSON 属性、未知事件/算法和混合商户身份。普通商户使用 `mchid`，服务商使用 `sp_mchid` 与必需的 `sub_mchid`，离线刷脸使用 `mch_id`。支付和刷脸 AppID 必须属于该商户配置的账户集合；退款通知没有 AppID，宿主须通过原支付单核对归属。退款状态以 `refund_status` 为准。

<a id="payment-notifications-2"></a>

### 接收扩展点

`IFallbackHandler.ReceiveAsync(FallbackRequest, CancellationToken)` 接收原始正文及签名头。`FallbackHandlerBase<TRequest>` 和双泛型形式负责协议验证，将不可变的 `FallbackNotification` 与已解析的产品对象交给 `OnReceiveAsync`。宿主继承支付、退款或刷脸处理器，在此校验订单、组织、用户、金额与币种，然后完成业务或持久保存通知。

| 返回值 | 含义 |
| --- | --- |
| `Completed` | 本次业务已经完成 |
| `Accepted` | 已持久接收，宿主承担后续处理和故障恢复责任 |
| `Duplicate` | 同一业务通知已可靠接收 |
| `None` | 未可靠接收 |

前三种返回 HTTP 204，无正文；未接收、持久化失败、处理异常或接收超时返回 HTTP 503，协议输入错误返回 HTTP 400，JSON 正文为 `code=FAIL` 和通用说明。Gateway 向处理器传递四秒取消期限，宿主必须遵循取消信号。未知路径返回 404。业务或接收完成后发生的网络断开仍可能触发重投，宿主必须实现持久去重。

`HandleAsync(TRequest, ...)` 只供应用内部已有可信对象的调用，例如付款码结果处理；它不验证网络通知。不得把任意 HTTP 正文反序列化后直接交给此入口。调用方拥有 `FallbackRequest.Body`，处理器不关闭它；流不能跨请求留存。

处理器在启动阶段经 `/Workbench/Externals/Wechat/Fallback/Handlers` 注册，键为 `Payment`、`Refundment`、`OfflineFaceAgreement`，且须与 `Kind` 一致。注册表在提供服务后不修改。协议层没有数据库、租户或业务状态的依赖。

<a id="payment-notifications-3"></a>

### Automao 持久接收

Automao 使用 `NotificationReceiver`、`Wechat.Notification` 映射和 `Wechat_Notification` 表。MySQL 初始化脚本位于 `D:\Automao\documents\database\automao.wechat-mysql.sql`，运行 Gateway 前必须创建该表。外部微信 Gateway 插件挂载接收工作者，支付提供者注册 `INotificationProcessor`；宿主启动代码不处理微信业务。

商户、子商户和产品共同限定去重范围。通知标识与业务键分别建立唯一约束，记录正文指纹；相同业务且正文一致的重复通知确认成功，冲突正文返回失败。支付按业务单号去重，退款按退款单号与通知状态去重，离线刷脸按签约号与通知时间去重。

支付接收核对环境、订单收款商户、金额及币种；退款核对原支付单、微信交易号与退款金额，通知未提供币种时使用业务记录的人民币约束；离线刷脸核对已有签约、用户账户和组织绑定。刷脸通知不自带最终签约状态，后台须查询签约状态后才能变更本地账户。

| 状态 | 含义与恢复 |
| --- | --- |
| `0` | 待处理；重启后继续领取 |
| `1` | 已领取；处理中或进程中断后待核对 |
| `2` | 处理完成 |
| `3` | 处理失败；`Failure` 记录错误码或异常类型 |

领取使用数据库条件更新，多个宿主只允许一个处理者获得该条记录。支付业务目前包含数据库更新后的额次、延迟任务及事件副作用；这些操作不具备统一的事务提交边界。因此状态 `1`、`3` 不自动重放，必须核对支付、退款、事件及外部副作用后再恢复。不得直接批量重置为 `0`，也不能把 HTTP 204 或状态 `2` 以外的记录当作业务完成。此机制保证可靠保存和去重，不承诺跨外部系统的恰好一次执行。

通知正文包含支付或用户标识，应按业务数据限制访问和保留期限；日志不输出完整正文、签名材料或密钥。

<a id="payment-notifications-4"></a>

### 验证

主库的 `FallbackTest` 使用独立 RSA/AES-GCM 夹具覆盖验签、身份和事件拒绝；`gateway/test` 直接构建本地 ASP.NET Core 管线验证应答，不引用插件宿主。Automao 的真实插件集成入口见其 `externals/wechat/integration` 目录。合成回调只能证明本地协议与持久化行为，不代表微信侧产品准入或真实交易联调。

<a id="merchant-onboarding"></a>

## 平台收付通进件、银行与素材

`PaymentManager.GetMerchantService(authority)` 的 `RegisterAsync`、`GetAsync` 对应平台收付通二级商户进件，要求调用商户已具备该产品权限。它们不代表普通服务商特约商户入驻，也不因上传成功或申请受理而开通支付。普通服务商进件使用独立产品契约。

<a id="merchant-onboarding-1"></a>

### 申请与状态

提交使用商户自己的业务申请号 `Registration.RequestId`；受理返回微信申请编号。查询始终使用原业务申请号，并核对响应的 `out_request_no`。超时或取消后的业务结果未知，应先查询原申请，不能自动换号重提。

`Registration` 分别建模主体证照、法人证件、联系人、店铺与结算账户。政府机关为 `2502`；社会组织需要明确的 `BusinessLicense.CertificateType`。身份证起始日期必须提供，结束日期为空表示协议的长期有效。补充图片和特殊资质在协议中是包含数组的 JSON 字符串，调用者仍使用 `string[]`。模型没有覆盖金融机构专属材料及非默认受益人列表，此类申请需独立扩展后使用。

敏感姓名、证件号、银行卡号与手机号通过本次加密材料加密，`Wechatpay-Serial` 指向同一材料。响应先验签，打款验证账户姓名和账号再由本商户私钥解密。验证截止时间按微信的日期时间格式解析。`Applyment.Status` 保留原始状态码，调用者不得把未知状态当成通过。

Automao 的 `TenantAgreementProvider` 通过现有 Feature 和插件注册消费协议。申请号绑定签约记录及租户；受理、审核和待签约不启用账户，仅 `FINISH` 创建或启用绑定该申请和发行商户的账户。`FROZEN`、`CANCELED` 停用账户，已取消的申请不能被迟到状态重新启用。状态与账户更新在本地事务中完成，网络操作位于事务外。附件使用当前发行商户上传，不复用缺少商户归属的素材编号。

Automao 当前法人及联系人只支持明确标注的身份证类型；其它证件类型拒绝提交，不能默认为身份证。机构证书类别存入 `TenantAgreement.BusinessLicenseType`，政府机关使用对应固定类别。`AgreementToken` 保存最长 256 字符的完整签约或验证链接；字段由[公共模块建表脚本](../../../../../Automao/documents/database/automao.common-mysql.sql)定义，并与[表结构设计](../../../../../Automao/documents/database/automao.common.md)一致。

<a id="merchant-onboarding-2"></a>

### 银行组件

`GetBanksAsync` 查询银行，`GetBranchesAsync` 通过银行别名编码和城市编码查询支行；分页校验偏移量和条数，即使本页为空也更新总数。开户银行编码用于结算资料，银行别名编码用于查询支行，两者不能互换。

`FindAsync` 返回 `IReadOnlyList<Bank>`，银行卡可能匹配零个或多个候选，不自动选择第一项。Web 银行识别端点成功时返回数组。银行卡加密后作为转义的查询参数发送，商户必须具备银行组件权限。

<a id="merchant-onboarding-3"></a>

### 素材上传

`Uploader.UploadAsync` 接收由框架文件系统读取的路径，拥有并释放打开的流。JPG/JPEG、PNG、BMP 和视频上限为 5 MiB，PDF 上限为 7.5 MiB；拒绝空文件、超限文件和非法文件名。视频类型保持 AVI、WMV、MPEG、MP4、MOV、MKV、FLV、F4V、M4V、RMVB。上传前按实际读取量检查上限，取消和错误路径也释放文件。

文件名及 SHA-256 放入 multipart 的 `meta`，请求签名使用同一份 meta 文本，文件部分使用对应 MIME 类型。视频进入 `merchant/media/video_upload`，图片和 PDF 进入 `merchant/media/upload`。扩展名与大小检查不代替微信对实际素材格式和内容的审核。

<a id="merchant-onboarding-4"></a>

### 官方依据与验证范围
- [平台收付通提交申请](https://pay.wechatpay.cn/doc/v3/partner/4012713017)及[查询申请](https://pay.wechatpay.cn/doc/v3/partner/4012691376)。
- [银行列表](https://pay.wechatpay.cn/doc/v3/partner/4012697583)、[银行卡识别](https://pay.wechatpay.cn/doc/v3/partner/4012697541)及[支行查询](https://pay.wechatpay.cn/doc/v3/partner/4012697673)。
- [图片与 PDF 上传](https://pay.wechatpay.cn/doc/v3/partner/4012760432)及[视频上传](https://pay.wechatpay.cn/doc/v3/partner/4012761084)。

框架协议测试使用合成密钥与报文检查敏感字段、请求身份、银行候选、分页、上传摘要及文件释放；Automao 的 `onboarding.py` 经实际插件和隔离 MySQL 验证申请查询与账户状态。它们不证明真实微信准入、材料审核和产品权限。

<a id="partner-onboarding"></a>

## 普通服务商特约商户接入

`PaymentManager.GetPartnerService(authority)` 使用普通服务商商户的签名材料、验证公钥和账号上下文。`PartnerService` 与平台收付通 `MerchantService` 分别建模；具备一类商户角色不代表具备另一产品权限。

<a id="partner-onboarding-1"></a>

### 申请与状态

`RegisterAsync(Registration)` 向 `/v3/applyment4sub/applyment/` 提交申请，返回微信申请单号。Registration 按超级管理员、主体、经营场景、结算规则、结算账户及补充材料组织为嵌套 partial 类型。字段使用官方命名映射；调用方仍须依照主体和场景提供真实、完整、有效的材料。基础必填、嵌套资料和场景关系在发送前验证；官方资质、费率、证件和媒体审核由微信支付执行。

业务申请编号只能包含 ASCII 字母、数字和下划线，最长 124 字符。`GetByRequestAsync` 按该编号查询，`GetByApplicationAsync` 按微信申请单号查询，两者都校验返回归属。申请单号以 64 位整数解析，提交返回字符串；它不能用作 sub_mchid。

`Applyment.State` 保留微信原始状态；只有 `APPLYMENT_STATE_FINISHED` 使 `IsCompleted` 为 true。签约中、开通权限中和未知状态均不能据此启用支付。完成状态必须返回特约商户号。签约链接由商户超级管理员使用，服务商不能代替本人完成身份核验或签约。

失败或结果不明时，先使用原业务申请编号查询；不自动重复提交，不生成另一业务编号绕过原单。驳回后的修订仍使用原业务编号，并由宿主保留申请与租户、服务商的关联。

<a id="partner-onboarding-2"></a>

### 敏感信息

管理员姓名、证件号码、电话和邮箱，法人/经营者与受益人的证件姓名、号码、地址，银行账户名和账号，通过已有 APIv3 加密转换器使用微信支付公钥 RSA-OAEP 加密。`Wechatpay-Serial` 对应该加密公钥。请求签名使用服务商私钥，响应先验签再解析。上传素材、银行查询继续使用同一服务商身份；模型和敏感数据不得直接返回普通客户端或日志。

<a id="partner-onboarding-3"></a>

### 支付权限与 AppID

进件、公众号/小程序第三方授权、支付产品权限及 AppID 支付绑定分别验证。第三方平台 `func_info` 不授予微信支付权限。

当前官方 AppID 管理流程通过服务商平台和对应公众/开放平台完成。服务商为自己绑定 sp_appid，为特约商户关联 sub_appid；主体和认证条件按官方规则核验，需确认的授权由对应管理员操作。本库不虚构“自动绑定完成”或“查询全部产品权限”的接口。

宿主应记录服务商商户号、特约商户号、sp_appid、适用的 sub_appid、已确认的产品、确认来源和有效状态。业务在使用前核对该关系；仅提交申请、取得商户号、填写 AppID 或完成公众号授权均不能激活支付。关系停用不应删除历史订单凭据，原订单查询和必要退款须按独立业务权限处理。

<a id="partner-onboarding-4"></a>

### 官方依据
- [提交申请](https://pay.weixin.qq.com/doc/v3/partner/4012719997)、[业务编号查询](https://pay.weixin.qq.com/doc/v3/partner/4012697168)、[申请单号查询](https://pay.weixin.qq.com/doc/v3/partner/4012697052)。
- [进件准备及签约流程](https://pay.weixin.qq.com/doc/v3/partner/4012062375)、[AppID 绑定与确认](https://pay.weixin.qq.com/doc/v3/partner/4016329059)。

<a id="partner-onboarding-5"></a>

### Automao 接入

Automao 的 `PartnerRegistry` 保存申请和明确核实的支付关系；商户端负责提交、查询和停用，平台管理端独立确认产品权限。其公开 API 当前不提供驳回资料修订/重提入口。运行配置、身份范围和退款边界见[Automao 普通服务商契约](../../../../../Automao/server/externals/wechat/docs/implementation.md#partners)。

<a id="membership"></a>

## 会员卡调用契约

本适配器使用微信支付 `marketing/membercard-open/cards` 会员卡产品，提供模板创建、查询、删除、code 预存及用户卡作废。它与“商家名片会员”的品牌接口分别建模，不自动切换产品。

<a id="membership-1"></a>

### 身份与调用

使用 `new Membership(authority, channelAccount, brandId)`，或者 `channel.GetMembership(authority, brandId)` 创建服务。`authority` 必须是有该产品权限的创建商户；公众号账户须在该商户配置的 `Accounts` 中，类型为 `Channel`。实际账号必须是服务号，并在微信侧完成商户、品牌及 AppID 关联。仅有本地配置不能证明微信权限。

创建模板必须显式提供匹配的 `AppId` 与 `Brand.BrandId`，以及品牌名称、标题、说明、有效期和官方会员素材 URL。进件素材的 media_id 不能充当会员卡 logo URL。日期采用 `DateTimeOffset`；固定区间必须有效，不能自动延长截止时间。库存 `Quantity` 为可空整数，仅系统分配 code 时可填写。

全部操作使用商户签名与 APIv3 响应验签，路径保留 `/v3/` 并转义模板和 code。查询核对模板编号、AppID 与品牌。删除、预存、作废会先查询验证模板归属，避免在同一商户下误操作其它应用或品牌的模板。调用方仍负责自身租户权限。

`AllocateAsync` 返回每个已返回 code 的 `Allocation`，保留 `SUCCESS`、`DUPLICATE`、`FAIL` 以及未来状态原值。未返回的 code 不能视为成功。仅预存模式允许调用；响应不能混入未提交的 code 或同一 code 的重复结果。

删除模板会停止其投放，并不作废已经领取的用户卡；`ObsoleteAsync` 才按模板和 code 作废用户卡，可提供失效理由。失败通过统一错误契约传播，取消、超时不会自动重放修改请求。

<a id="membership-2"></a>

### Automao 调用边界

微信会员提供者通过原有插件与 `ICardTemplateProvider` 接入。现有 `CreateAsync` 从租户/门店账户关系中选择商户，再从该商户配置中选择明确的公众号；`argument` 为 `CardTemplateProvider.CreationOptions` 的 JSON，包含 `appId`、`brand`、`logoUrl`、`backgroundUrl`。品牌对象字段为 `brand_id` 和 `display_name`。模板主字段仍来自业务模型。

提供者的模板查询、预存、删除、更新以及用户卡业务仍未实现，调用明确抛出 `NotImplementedException`。框架协议能力不代表这些业务流程可用，不会用返回 false 表示已完成处理。

<a id="membership-3"></a>

### 官方依据

[创建模板](https://pay.wechatpay.cn/doc/v3/partner/4012708314)、[查询模板](https://pay.wechatpay.cn/doc/v3/partner/4012709168)、[删除模板](https://pay.wechatpay.cn/doc/v3/partner/4012726840)、[预存 code](https://pay.wechatpay.cn/doc/v3/partner/4012709031)和[作废用户卡](https://pay.wechatpay.cn/doc/v3/partner/4012726955)分别定义本产品的操作。离线测试覆盖身份绑定、请求签名、204 验签、部分导入结果与错误响应；真实产品权限和领卡投放需要具备账号后验证。

<a id="account-credentials"></a>

## 自有账号凭据与票据

`Account.GetCredentialAsync(refresh, cancellation)` 为自有公众号和小程序调用稳定版 `POST /cgi-bin/stable_token`。账号仍由既有配置和动态 `IAccountProvider` 提供；第三方授权者令牌、组件令牌及 OAuth 用户凭据不使用本入口。

普通调用使用 `force_refresh=false`。显式 `refresh=true` 才请求强制刷新，该操作会使旧 token 失效，并受微信额度限制，不能用作每次业务请求的常规步骤。网络失败和取消后的强制刷新结果未知，不自动重放请求。普通获取仅在微信明确返回系统繁忙时做有限退避；其它错误直接传播。

<a id="account-credentials-1"></a>

### 缓存和并发

缓存由 `CredentialManager.Cache` 与 `Locker` 或原有 `/Externals/Wechat/Caching` 命名提供者解析。缺少必需服务会抛出异常。缓存键按自有账号凭据域、账号类型、AppID 以及 token/票据类型隔离；与旧 token 及第三方授权凭据分开。

缓存值是内部 JSON 信封，包含凭据及刷新修订标识；有效期由分布式缓存 TTL 承担。调用者不解析或直接写入该格式。按微信返回的剩余秒数扣除网络耗时和小幅安全余量，不把重复获取的 token 延长为固定两小时。读取缓存时也保守扣除读取耗时。

本机使用按键等待，分布式锁保护远端刷新。取锁后再次读取缓存；在等待期间有其它请求发布新修订时直接复用，包括并发强制刷新。等待结束后释放本机协调对象，不永久积累 AppID 锁。每次命中都读取共享缓存，避免保留其它节点强制刷新前的本地 token 副本。

取消贯穿缓存、等待、HTTP 和退避。取消一个等待者不取消正在刷新的请求；刷新请求自身取消后，后续调用仍可重新获取。锁丢失、缓存写入失败或非法响应均作为失败，不返回伪成功，也不缓存失败任务。

`GetTicketAsync` 支持公众号 `jsapi` 与 `wx_card`，分别缓存。刷新票据不强制刷新基础 token。返回的 `period` 是当前剩余有效期，可用于 JS-SDK 签名有效期控制。

<a id="account-credentials-2"></a>

### HTTP 配置

没有工厂时使用适配器的共享 HTTP 传输。注册了 `IHttpMessageHandlerFactory` 的宿主使用名称 `Wechat.Credentials`，必须关闭自动重定向和自动重试。框架借用工厂处理器，单次 HTTP 客户端释放时不释放共享处理器；单次获取和内部退避有 30 秒上限。

```csharp
services.AddHttpClient("Wechat.Credentials")
	.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
	{
		AllowAutoRedirect = false,
		PooledConnectionLifetime = TimeSpan.FromMinutes(5),
	});
```

不得向客户端返回 access_token、AppSecret 或票据缓存内容。基础凭据由服务器消费，Web 登录会话另由宿主签发。

<a id="account-credentials-3"></a>

### 官方依据与验证

微信当前[稳定版凭据文档](https://developers.weixin.qq.com/miniprogram/dev/server/API/mp-access-token/api_getstableaccesstoken.html)说明普通/强制刷新、返回剩余有效期及适用账号范围。旧文档路径可能跳转至分类目录。

离线测试覆盖普通/强制刷新并发合并、锁内二次读取、节点竞争、账号与票据类型隔离、剩余 TTL、等待者取消、远端取消、失败不缓存及繁忙退避。它们不验证真实 AppID 权限、IP 白名单或微信侧配额。

<a id="applet-identity"></a>

## 小程序身份与 Web 边界

<a id="applet-identity-1"></a>

### 协议与服务端身份

`Applet.LoginAsync(code, cancellation)` 调用自有小程序的 `GET /sns/jscode2session`，只消费该结果的 OpenId、可选 UnionId 与会话密钥，不调用公众号用户接口。UnionId 缺失时保持 null。`LoginResult.Secret` 和 `Account.Secret` 不参与 System.Text.Json 或 Zongsoft JSON 序列化；账号字符串只包含类型和代号。会话密钥由服务端按自己的会话生命周期保管，不放进应用身份、日志或响应。

`WechatAuthenticatorBase` 从账号提供者获取配置，不接受登录 key 中嵌入 AppSecret。应用用户查找、权限检查和会话签发由现有宿主鉴证器、质询器及凭据提供者完成。

`Applet.GetPhoneNumberAsync(phoneCode, openId, cancellation)` 使用独立手机号授权 code；传入 OpenId 时由微信验证所属用户。结果为 `PhoneInfo(PhoneNumber, PurePhoneNumber, CountryCode)`；响应必须有匹配 AppId 的 watermark 和有效字段。登录 code 与手机号 code 不能混用，不自动重试一次性 code。

协议 HTTP 使用 `IHttpMessageHandlerFactory` 的 `Wechat.Accounts` 命名处理器，借用其生命周期。处理器必须关闭重定向和自动重试；未配置时使用关闭重定向的共享连接池。基础访问凭据使用独立的 `Wechat.Credentials` 管线。

<a id="applet-identity-2"></a>

### Web 接入

- `POST /Externals/Wechat/Applets/{id}/Login?scenario={scenario}`：正文为登录 code 文本，id 与 scenario 必填，正文上限 4096 字节。通过宿主 `Authentication.AuthenticateAsync` 签发并转换应用会话，不返回微信协议 DTO。
- `POST /Externals/Wechat/Applets/{id}/PhoneNumber`：JSON 正文为 `{ "code": "手机号授权码", "openId": "可选身份" }`，返回结构化号码，要求应用身份已认证。
- 两个入口都要求宿主注册 `Wechat.Accounts` ASP.NET 授权策略；资源是所请求的 `Account`。策略须验证账号归属及调用者的租户范围，登录策略可根据宿主受信配置允许所属账号的匿名登录。策略缺失或不通过返回 403。
- 宿主须配置 `Wechat` 鉴证器、应用凭据提供者及必要质询器；只安装微信 API 插件不能签发业务会话。
- 不公开小程序用户目录或公众号/小程序 Credential HTTP 端点。服务器可继续通过核心库刷新凭据。

<a id="applet-identity-3"></a>

### Automao 身份归属

微信应用在 `Applet` 表中声明 `Provider=Wechat`、`Type=Applet` 或 `Channel`，并启用；应用代号必须唯一确定一条配置及其 TenantId、BranchId。类型缺失或不匹配不参与解析，多个匹配记录拒绝解析。登录签发前重新查询配置，不能依赖缓存对象绕过应用停用。

三个站点使用发行应用、OpenId、租户及门店确定用户；停用账户不签发身份。UnionId 保留为协议身份信息，在开放平台归属关系尚未建模前，不自动跨应用合并用户。

客户站点只接受 `phoneCode` 授权号码关联，使用当前已验证的 OpenId 兑换，并限制用户查询为应用所属租户和门店。普通 `phone` 参数不作为身份依据。大陆号码采用 PurePhoneNumber，其它国家保留 PhoneNumber。用户、消费者及微信账户在一个可串行化事务内创建；不以 Upsert 改写其它绑定，重复登录复用已有账户，重复绑定数据拒绝登录。

现有数据库需明确填充各微信应用的 Type 和归属。无账号、主体或公网回调环境时，合成传输验证仅证明本地契约，不证明微信侧实测成功。

<a id="applet-identity-4"></a>

### 官方依据
- [小程序登录 code2Session](https://developers.weixin.qq.com/miniprogram/dev/server/API/user-login/api_code2session.html)
- [手机号授权 code 兑换](https://developers.weixin.qq.com/miniprogram/dev/server/API/user-info/phone-number/api_getphonenumber.html)

<a id="channel-services"></a>

## 公众号 OAuth、JS-SDK 与消息

<a id="channel-services-1"></a>

### OAuth 身份与凭据

`ChannelAuthentication.AuthenticateAsync(code)` 通过 `/sns/oauth2/access_token` 兑换一次性网页授权 code。只有 scope 包含 `snsapi_userinfo` 才请求 `/sns/userinfo`；`snsapi_base` 只提供身份，不强行读取资料。昵称、头像和 UnionId 均可缺失，OpenId 不作为 UnionId 回退值。资料中的 OpenId 和已有 UnionId 必须与凭据一致。

`Credential` 包含 AppId、OpenId、可选 UnionId、Permission、Expiration、剩余 Period 及可选 User。AccessToken 和 RenewalToken 只在服务端使用，两种 JSON 序列化均排除它们。OAuth token 不进入基础凭据缓存。

- `RefreshAsync(credential)` 使用同一 AppId 和刷新令牌，验证返回的 OpenId/UnionId，不接受其它应用凭据。
- `ValidateAsync(credential)` 调用 `/sns/auth`，成功完成表示本次验证通过；微信错误保持 `OperationException.Reason`，不将传输失败视为无效或成功。
- `GetUserInfoAsync(credential)` 仅允许 `snsapi_userinfo`，验证返回身份，资料字段允许缺失。

code 交换不自动重试；刷新令牌到期后须重新授权。宿主在发起授权前生成并绑定 state，在回调兑换前验证 state、一次性使用状态、预期 AppId 和回跳地址白名单。核心库不持有浏览器会话，不替宿主维护 state；不能把客户端传入的回跳地址直接作为可信目标。

<a id="channel-services-2"></a>

### JS-SDK

`Channel.PostmarkAsync(url)` 接受绝对 HTTP/HTTPS 页面 URL，不接受含用户信息的 URL。签名保留原始 URL 的路径、转义形式和查询字符串，只移除第一个 `#` 及其后面的片段；不重新排序或编码查询参数。返回 SHA-1 原始字节、nonce、timestamp 和票据剩余期限。

`POST /Externals/Wechat/Channels/{id}/Postmark` 正文为页面 URL，限制 4096 字节。先通过 `Wechat.Accounts` 账号策略，再通过 `Wechat.JSSDK` 策略；后者资源为 `ChannelController.PostmarkContext(Account, Url)`。宿主必须按应用配置验证允许站点，使用 Uri 的 Scheme、Host、Port，不能用字符串前缀判断主机。缺少策略或拒绝均返回 403。允许站点仍须与微信后台 JS 接口安全域名配置相符。

Web 响应提供 AppId、NonceStr、Timestamp、Period 和小写十六进制 Signature；供 `wx.config` 使用，票据本身不返回客户端。

<a id="channel-services-3"></a>

### Web 登录、用户和模板

公众号登录入口为 `POST /Externals/Wechat/Channels/{id}/Login?scenario={scenario}`，正文为 code。与小程序共用宿主认证链和 `Wechat.Accounts` 策略，返回应用会话，不返回 OAuth 凭据。宿主授权策略或其受信回调入口须先完成上述 state 验证。

- `GET /Externals/Wechat/Channels/{id}/Users/{identifier?}`：无 identifier 时分页获取关注者 OpenId，bookmark 查询参数与 X-Bookmark 响应头承载游标；有 identifier 时获取该公众号用户信息。
- `GET /Externals/Wechat/Channels/{id}/Messager/Templates`：已选用模板列表。
- `POST /Externals/Wechat/Channels/{id}/Messager/Send/{destination}?template={template}&url={optionalUrl}`：发送模板消息，正文为模板字段对象，上限 65536 字节。

用户与消息接口要求已认证应用身份及所属账号策略；id 从明确路由绑定，不从默认公众号推断。模板字段可传平坦值或 JSON `{ "value": ... }`，数字保持数值语义；不发送过时的颜色字段。返回消息编号表示提交受理，不证明送达；微信拒绝保留错误码，不自动重发。

`ITransmitter` 的 Wechat 提供者使用 `公众号AppId:OpenId` 目的地址，channel 为 Message 或空；未知通道拒绝。发送仍等待账号提供者和 HTTP 完成并传递取消。

所有公众号协议请求共用 `Wechat.Accounts` 命名传输，禁止自动重定向和重试。用户查询、模板消息及 OAuth 分别受微信账号权限约束，小程序订阅消息不在此能力中。

<a id="channel-services-4"></a>

### 官方依据
- [服务号网页授权](https://developers.weixin.qq.com/doc/service/guide/h5/auth.html)
- [JS-SDK 签名与接口](https://developers.weixin.qq.com/doc/service/guide/h5/jssdk.html)
- [发送模板消息](https://developers.weixin.qq.com/doc/service/api/notify/template/api_sendtemplatemessage)
- [获取模板列表](https://developers.weixin.qq.com/doc/service/api/notify/template/api_getalltemplates)

<a id="platform-cryptography"></a>

## 公众平台消息密码协议

`Platforms.CryptographyUtility` 提供公众号和开放平台消息的 AES-256-CBC 与 SHA-1 协议原语，与微信支付 APIv3 的 GCM 解密分开。算法及公开测试向量来自[微信消息加解密文档](https://developers.weixin.qq.com/doc/service/guide/dev/push/encryption.html)。

<a id="platform-cryptography-1"></a>

### 使用契约

1. 接收层限制正文和字段长度，从配置确定 Token、EncodingAESKey 和预期接收方 AppId；不从未验证的报文选择任意密钥或 AppId。
2. 将收到的 `timestamp`、`nonce` 和原样 `Encrypt` 交给 `VerifySignature(token, timestamp, nonce, ciphertext, signature)`。未通过时停止处理。`Sign` 对四项按 Ordinal 排序后连接，返回小写 SHA-1 十六进制；验签采用固定时间摘要比较。
3. 验签通过后调用 `Decrypt(ciphertext, encodingAESKey, appId)`，或需要原始字节时调用 `DecryptBytes`。预期 AppId 为必需参数，必须与帧尾的 UTF-8 字节完全匹配。
4. 接收层仍负责时间窗口、重放防护、事件语义、内层身份和业务持久化；成功解密不等于业务已接收。纯密码方法不访问网络、缓存或数据库，也不自行确认回调。

`Encrypt` 接受字符串或 `ReadOnlySpan<byte>`，返回 Base64 密文。每次使用密码学随机的 16 字节前缀；消息长度采用四字节大端序，长度按字节计算。CBC 的 IV 为密钥前 16 字节，帧按 32 字节进行 PKCS#7 填充，整除时仍填充 32 字节。

EncodingAESKey 必须是没有末尾等号的 43 字符规范 Base64，解码为 32 字节。解密验证密文块长度、全部填充字节、非负且不越界的消息长度及接收方；空消息允许，空接收方不允许。字符串入口使用严格 UTF-8；任意二进制数据应使用字节入口，非法 UTF-8 不会被替换字符掩盖。

配置参数错误抛出带参数名的 `ArgumentException` 或其派生类型；非法 Base64 抛出 `FormatException`；帧、填充及接收方错误抛出 `CryptographicException`；非法 UTF-8 抛出编码异常。HTTP 接收层不向调用方暴露密码错误细节。签名格式错误或不匹配返回 `false`。

该契约不提供没有接收方检查的解密入口，也不提供重复且未实现的 CBC 密码类。临时明文与密钥缓冲在使用后清零；调用方仍负责保护配置字符串及返回的消息。

<a id="component-platform"></a>

## 第三方平台协议与接收契约

`Platforms.Component` 管理一个第三方平台的协议上下文，独立于自有公众号、小程序和支付商户。配置位于 `/Externals/Wechat/Components`，每项包括 `Name`（component_appid）、`Secret`、回调 `Token`、43 字符 `EncodingAESKey`。`ComponentManager.Find(appId)` 读取当前配置并解析宿主的 `IComponentStore`；无匹配配置返回 null，缺少存储服务时失败。配置与服务器凭据不得返回前端。

<a id="component-platform-1"></a>

### 授权事件

Gateway 的 `POST /Externals/Wechat/Components/{componentAppId}/Events` 接收安全模式 XML，接受 `text/xml` 或 `application/xml`，正文上限 256 KiB。此入口与支付、公众号代收消息入口分别配置。

接收层要求唯一的 `timestamp`、`nonce`、`msg_signature`；允许微信附带 `signature`、`encrypt_type=aes`、`openid`。时间戳与本机 UTC 相差不能超过五分钟。只使用四字段的 `msg_signature` 验签，不用三个字段的 `signature` 替代。先验签，再按可信配置的组件 AppId 解密，内层 AppId 也须一致。XML 禁用 DTD/外部实体，拒绝重复字段、嵌套字段和不支持的事件类型。

支持 `component_verify_ticket`、`authorized`、`updateauthorized`、`unauthorized`。通知 Id 由已验证的语义字段规范化后计算 SHA-256，重新加密或改变传输随机数不改变同一事件 Id。`Value` 保存票据或授权码，`PreauthorizationCode` 为授权过程关联依据，两者均不参与公开 JSON 序列化。

`IComponentStore.ReceiveAsync` 必须原子保存事件与相应的票据/授权状态变化，并按组件和事件 Id 幂等。授权撤销须立即使对应授权不可用；迟到事件不能恢复新状态，时间相同但相互冲突的授权事件应保守拒绝恢复。票据保留最新有效值。重复事件只有在已有可靠记录时才能返回 `Duplicate`，未保存返回 `Rejected` 或抛出异常。纯内存存储只适用于离线测试。

Gateway 在 `Accepted` 或 `Duplicate` 时返回 HTTP 200 和 `success`；持久化失败、未接收或四秒接收期限到期返回 503。签名/报文错误返回 400；未知组件返回 404；正文超限可能返回 413。接收层不执行需要较长时间的授权交换，可靠记录后的后续处理归宿主。授权码已经过期的事件仍须记录其状态，不循环重试已过期的授权码。

<a id="component-platform-2"></a>

### 组件令牌与授权交换

`GetCredentialAsync` 使用共享缓存和分布式锁，键域为 `Zongsoft.Wechat.Component:{componentAppId}:access-token`，不与自有账号稳定令牌共用。过期时从 `IComponentStore.GetTicketAsync` 获取同一组件最近十二小时内的验证票据，调用 `/cgi-bin/component/api_component_token`。有效期取自应答并预留刷新余量。不能使用 stable_token 获取组件令牌。

组件 HTTP 使用 `Wechat.Components` 命名处理器，关闭自动重定向，借用工厂生命周期；无自定义处理器时使用共享传输。协议请求不自动重试，取消向调用方传播。服务器错误按官方错误码传递。

`PreauthorizeAsync` 调用 `api_create_preauthcode?access_token=...`，以应答 `expires_in` 为准。`Preauthorization.GetUrl` 生成 PC 或新版 H5 授权地址，只支持公众号（1）或小程序（2），要求 HTTPS 回跳地址且无用户信息和片段；过期预授权码不能生成地址。

预授权码不是用户会话。宿主在返回授权链接前必须保存所属用户/租户、组件、预授权码、允许的回跳地址、一次性 state 和期限。回跳及授权事件均须与这条记录关联；不能依据浏览器提交的 AppId 或租户号直接建立授权关系。

`AuthorizeAsync(code, expectedAuthorizerAppId)` 通过 `api_query_auth?access_token=...` 交换授权信息，核对返回 AppId，保留实际 `func_info` 权限集。没有 API 权限的账号允许没有访问/刷新令牌；存在令牌时必须同时具备刷新令牌和有效期限。返回模型不将令牌序列化到客户端。调用方应在交换后可靠保存凭据和关联关系；业务管理入口不能直接暴露该方法，也不能在结果不明时自动重新消费一次性授权码。

<a id="component-platform-3"></a>

### 授权者凭据与委托调用

`IComponentAuthorizationStore` 提供权威授权快照与凭据条件更新，由同一个宿主存储服务实现。快照包括组件、授权者、账号类型、Enabled、实际 Permissions、GrantId、Revision 及凭据。授权、权限变更与撤销必须更换 GrantId 和 Revision；单纯令牌刷新只更换 Revision。所有读取必须反映最新撤销状态，不能让过期的业务缓存重新启用授权。

`UpdateCredentialAsync` 必须原子校验组件、授权者、Enabled 和旧 Revision，更新访问令牌、刷新令牌、期限及新 Revision。返回成功表示已可靠保存，不能只更新内存。服务器令牌不参与公开 JSON 序列化；数据库和备份须按宿主凭据存储策略保护。

授权者令牌直接读取权威存储，不另存一份容易滞后的令牌缓存。过期或强制刷新时，使用组件与授权者共同组成的锁键，合并本机并发并取得分布式租约。锁内重新读取授权；调用 `api_authorizer_token` 后验证租约，再以旧 Revision 条件保存。成功收到轮换后的刷新令牌后，持久化使用独立于调用方的取消令牌，保存后再观察迟到取消。撤销或新授权使条件更新失败，旧结果不得恢复授权。结果不明时不自动重试。

`Component.GetAccountAsync`、`GetAppletAsync`、`GetChannelAsync` 只返回可用授权者。委托 Account 的 ComponentId 参与身份比较，没有授权者 AppSecret。动态账户提供者可以返回该 Account；管理器不缓存委托实例，已有实例在每次调用时仍检查当前授权。配置中的自有账户与动态自有账户保留原缓存契约；宿主不得把已有自有账户原地改为另一租户的委托账户。

`GetAuthorizerInfoAsync` 识别公众号和普通/试用小程序，保留真实权限集与账号状态；其他账号产品不自动解释为公众号。状态 1 表示正常，缺失状态表示未知。宿主须结合状态、类型和授权请求决定是否启用，资料获取成功不等于业务已授权。

| 已有功能 | 委托权限 | 协议凭据 |
| --- | --- | --- |
| 小程序登录、手机号 | 18 | 登录使用 component_access_token；手机号使用 authorizer_access_token |
| 公众号用户 | 2 | authorizer_access_token |
| 公众号 OAuth、JS-SDK | 4 | OAuth 兑换/刷新使用组件专用路径与组件令牌；票据使用授权者令牌 |
| 模板消息 | 7 | authorizer_access_token |
| wx_card 票据 | 8 | authorizer_access_token |

OAuth 凭据同时绑定组件与授权者，不能与自有账号凭据互换。授权者票据缓存键包含组件、授权者、GrantId 和票据类型；读取缓存后再次检查授权，旧授权缓存不能在撤销或重新授权后使用。未知权限集保留但不自动授予已有功能。业务租户隔离、授权请求消费和本地解除关联仍由宿主管理。

<a id="component-platform-4"></a>

### 官方依据
- [消息推送](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/Before_Develop/message_push.html)：第三方平台仅允许安全模式和 XML，验证组件 AppId。
- [验证票据](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/Before_Develop/component_verify_ticket.html)、[授权变更](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/Before_Develop/authorize_event.html)。
- [组件令牌](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getcomponentaccesstoken.html)、[预授权码](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getpreauthcode.html)、[授权信息](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getauthorizerrefreshtoken.html)。
- [授权者令牌](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getauthorizeraccesstoken.html)、[授权者资料](https://developers.weixin.qq.com/doc/oplatform/openApi/authorization-management/api_getauthorizerinfo.html)。
- [小程序委托登录](https://developers.weixin.qq.com/doc/oplatform/openApi/miniprogram-management/login/api_thirdpartycode2session.html)、[公众号网页授权](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/Before_Develop/Official_Accounts/official_account_website_authorization.html)。
- [授权流程](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/Before_Develop/Authorization_Process_Technical_Description.html)：HTTPS 回跳、账号类型与新版 H5 地址；平台授权的正式解除由商家在微信侧操作，服务商本地停用关联不等于微信侧已撤销。

<a id="component-platform-5"></a>

### Web 管理入口

`POST /Externals/Wechat/Components/{componentAppId}/Authorizations` 接受 `Context`、`RedirectUri`、`AccountType`（Channel/Applet）及可选 `Mobile`。要求已登录身份及 NameIdentifier；宿主必须提供 `Wechat.Components` 资源策略，同时检查请求的业务范围、组件、账号类型和回跳域名。没有策略默认拒绝。`Context` 是宿主解释的业务范围，不能只因请求中填写租户号就放行。

`BeginAuthorizationAsync` 在调用预授权接口后生成 32 随机字节的 state，并加入 HTTPS 回跳地址；拒绝原地址已有 state。`IComponentStore.CreateAuthorizationAsync` 必须以唯一 state 保存请求的 Owner、Context、组件、预授权码、类型、回跳地址和期限，成功返回后才将授权 URL 和到期时间交给浏览器。保存失败返回 503，不返回链接。该方法不表示用户已经完成授权。

授权事件应通过 PreAuthCode 与这条服务端记录关联，再由宿主完成授权者信息和凭据保存。浏览器回跳只能查询或确认自己的记录；一次性 state、实际授权者、事件时序与权限变化的原子校验不可省略。微信原始授权码交换仅供服务端使用，Web 不公开返回服务器凭据的交换入口。

<a id="component-platform-6"></a>

### Automao 接入

Automao 的 `ComponentStore` 实现票据、授权请求、事件和授权者凭据持久化，通过服务注册和 Gateway 工作器装配。商户站点使用独立的租户授权管理入口，回跳地址由服务端配置；状态、密钥环、恢复边界及接口见[Automao 授权契约](../../../../../Automao/server/externals/wechat/docs/implementation.md#authorizations)。

<a id="deployment"></a>

## 微信插件运行与数据库契约

<a id="deployment-1"></a>

### 程序集和引用

主库、Web 和 Gateway 的版本为 7.10.0，必须一起使用；目标框架为 .NET 8、9、10。主库需要 Core 7.72.0，Web/Gateway 还需要 Web 7.20.0。框架基础库由宿主统一提供，不在适配器 `.deploy` 中重复部署。

Debug 构建使用当前工作树 Core/Web 的 HintPath，Web/Gateway 以 ProjectReference 引用主库。Release 分支使用上述最低版本的 NuGet 依赖，Web/Gateway 引用主库 7.10.0。未发布的版本不能由旧包替代；正式 Release 包构建须在所选源具备相应依赖后进行。源码构建与离线验证不代表 NuGet 发布或现网部署。

Automao 直接调用项目的本机引用由 `ZongsoftFrameworkPathReferenced` 和 `ZongsoftFrameworkPath` 控制；NuGet 分支同样要求主库 7.10.0。Web 部署清单使用 Web 7.10.0，Gateway 站点另外装入 Gateway 7.10.0。插件应答与业务注册节点见[Gateway](../gateway/README.zh-Hans.md)。数据库映射与业务插件跟随 Automao 自身部署清单，宿主不放置支付业务代码。

<a id="deployment-2"></a>

### 配置和公开入口

- 从 `Zongsoft.Externals.Wechat.option` 的占位模板生成环境配置；将 `https://callbacks.example.invalid/...` 替换为实际 HTTPS 地址。缺失密钥文件的模板不能作为可用商户。
- 商户签名材料、APIv2/APIv3 密钥与微信验签材料分别配置。服务商号、特约商户号、公众号/小程序 AppId 和允许关系必须明确；不能沿用同名字段推断角色。
- 登录使用应用会话，配置 `Wechat.Accounts` 资源授权；JS-SDK 另需 `Wechat.JSSDK` 站点授权。缺少策略时默认拒绝。用户、消息及手机号入口还要求已登录身份。
- 服务端凭据使用共享缓存及分布式锁；基础令牌、JS-SDK 和卡券票据的命名空间分别管理。禁止将 session_key、OAuth 令牌或商户材料放进客户端响应。
- 回调根据实际支持的产品配置明确路径，支付通知只在可靠接收后确认。第三方授权回调与支付回调是独立协议，不能共用入口或推定互相授权。

<a id="deployment-3"></a>

### 数据库结构

Automao 的[微信建表脚本](../../../../../Automao/documents/database/automao.wechat-mysql.sql)统一定义通知接收箱、组件票据、授权请求、授权事件、授权快照、普通服务商申请和支付关系七张表，字段及索引见[微信表结构设计](../../../../../Automao/documents/database/automao.wechat.md)。数据映射随 Automao 微信插件部署。

[公共模块脚本](../../../../../Automao/documents/database/automao.common-mysql.sql)定义 `TenantAgreement.BusinessLicenseType`（可空 `varchar(50)`）和 `AgreementToken`（可空 `varchar(256)`）；[支付模块脚本](../../../../../Automao/documents/database/automao.paying-mysql.sql)定义 `Paying_PaymentDetail.PaymentToken`（可空 `varchar(64)`）。对应[公共模块设计](../../../../../Automao/documents/database/automao.common.md)与[支付模块设计](../../../../../Automao/documents/database/automao.paying.md)同步维护。开发 Pod 在初始化时加载这些完整建表脚本；集成夹具只在自己的隔离库执行建表与合成业务数据写入。

<a id="deployment-4"></a>

### 验证边界

框架单元测试与本地 HTTP 测试验证协议和应用入口；Automao 夹具加载实际插件，通过合成签名应答和隔离数据库验证业务闭环，入口见[验证契约](#testing)。真实微信账号、产品权限、设备、外网回调和资金到账须独立联调，离线成功不代表已具备这些资格或已上线。

Automao 第三方授权使用绝对路径的持久化 Data Protection 密钥环，相关站点与实例共用并限制文件访问。密钥环文件没有额外的证书/DPAPI 加密；密钥环与凭据数据共同持久保存，删除密钥环会使已保存凭据无法解密。普通服务商通过独立 Authorities 白名单启用，商户站点与平台管理站点分别部署同名 Web 插件。配置模板及权限要求见 [Automao 微信模块](../../../../../Automao/server/externals/wechat/README.zh-Hans.md)。

<a id="testing"></a>

## 微信适配器验证契约

本文件定义离线测试、协议测试工程和 Automao 业务集成的运行条件，功能范围见[接口核对表](#capability-matrix)。云服务器和微信账号可在真实联调前准备，不是本地构建及离线测试的前置条件。

<a id="testing-1"></a>

### 构建与测试入口

在 `D:\Zongsoft\framework` 执行。Debug 下微信主库使用 Core 的本地程序集，Web/Gateway 还依赖 Web 的本地程序集，因此必须先构建依赖，不能用旧的 HintPath 输出代替本次源码。

```powershell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -c Debug -p:GeneratePackageOnBuild=false
dotnet build Zongsoft.Web/src/Zongsoft.Web.csproj -c Debug -p:GeneratePackageOnBuild=false
dotnet build externals/wechat/Zongsoft.Externals.Wechat.slnx -c Debug -p:GeneratePackageOnBuild=false

dotnet test externals/wechat/test/Zongsoft.Externals.Wechat.Tests.csproj -c Debug --framework net8.0 --no-build --no-restore
dotnet test externals/wechat/test/Zongsoft.Externals.Wechat.Tests.csproj -c Debug --framework net9.0 --no-build --no-restore
dotnet test externals/wechat/test/Zongsoft.Externals.Wechat.Tests.csproj -c Debug --framework net10.0 --no-build --no-restore
```

每条命令成功后才运行后续命令。项目采用 xUnit v3，通过 `Microsoft.NET.Test.Sdk` 和 Visual Studio adapter 使用 VSTest；仓库未启用 MTP runner。`--framework` 分别运行三个目标；不能把仅安装 SDK 或通过框架前滚运行视为原目标运行时验证。包版本、目标框架和运行器以实际共享配置及项目文件为准。

主库测试工程只通过 `ProjectReference` 引用微信主库；Core 使用已构建程序集的 `Reference/HintPath`，实际使用的配置、DI 和宿主抽象包显式声明。测试不引用 `Zongsoft.Plugins`，不读取生产配置、密钥和真实交易数据。

Web API 测试使用相同的三目标框架命令，项目路径为 `externals/wechat/api/test/Zongsoft.Externals.Wechat.Web.Tests.csproj`。

Gateway 测试使用相同的三目标框架命令，项目路径为 `externals/wechat/gateway/test/Zongsoft.Externals.Wechat.Gateway.Tests.csproj`。它直接装配真实控制器和本地 Kestrel，以接收结果替身隔离应答边界；协议验签在主库测试中覆盖。

<a id="testing-2"></a>

### 现有离线测试边界

离线刷脸协议测试覆盖服务商认证身份、专用扣款与查询产品、可选金额、未知状态、合同归属、敏感字段加密和解约 204。Automao `agreements.py` 通过实际插件与隔离 MySQL 验证首次签约、账户启停、重复/迟到通知、未知状态及显式解约；`PaymentProviderTest.Offline` 验证扣款业务状态和持久产品查单。详见[离线刷脸契约](#offline-face)。

| 测试 | 验证契约 | 不能据此推定 |
| --- | --- | --- |
| [基础支付](../test/AccountManagerAsyncTest.Payments.cs)与[查询](../test/AccountManagerAsyncTest.PaymentQueries.cs) | 两种角色、三个场景、应用/付款人归属、调起签名、二维码、路径编码与未知状态 | 商户真实权限、设备 SDK 和实际付款 |
| [FallbackTest](../test/FallbackTest.cs) | 原始字节验签、商户/AppID/事件约束、GCM 解密、超大报文、业务失败和取消 | 微信真实投递、宿主持久化与业务幂等 |
| [AuthorityTest](../test/AuthorityTest.cs) | 插件 XML 配置绑定、签名材料角色、商户号绑定、密钥序列化排除、借入证书所有权、公钥模式隔离和文件配置切换 | 真实商户材料已获微信认可；宿主配置热更新和插件部署已通过 |
| [证书刷新测试](../test/AccountManagerAsyncTest.Certificates.cs) | 内存 HTTP 响应验签、GCM 解密、原子轮换、提前弃用、未知序列号限流、并发刷新和调用方取消隔离 | 通用支付响应、通知管线或真实证书下载已通过 |
| [HTTP 协议测试](../test/AccountManagerAsyncTest.Http.cs) | 实际发送字节与请求签名一致、路径/查询编码、空 POST、multipart meta、204 验签、非法头/时间/签名/正文拒绝、错误码和关联 ID、银行加密头与查询 | 各支付产品 DTO 与状态语义完整、通知已验证或微信实测通过 |
| [HTTP 生命周期测试](../test/AccountManagerAsyncTest.HttpLifecycle.cs) | 超时/取消不重试、读取中断释放、正文上限、可信证书刷新后验签、JSON 内容所有权、请求目的地及重定向配置拒绝、完整快照客户端隔离 | 自定义传输处理器满足约束、Automao 插件装配或业务落库已验收 |
| [AccountManagerAsyncTest](../test/AccountManagerAsyncTest.cs) | 公众号/小程序提供者等待、Get/Find 语义、配置缓存、失败不缓存、并发返回实例及调用方取消隔离 | 微信登录、稳定令牌、分布式锁和远端权限已通过 |
| [PaymentRequestTest](../test/PaymentRequestTest.cs) | 有效人民币金额序列化为整数分，整数入口已经表示分，币种及 JSON 字段名正确 | 非法精度、负值、溢出或货币校验已经实现；完整下单报文及签名有效 |
| [退款协议测试](../test/AccountManagerAsyncTest.Refunds.cs) | 普通商户/服务商请求、金额和订单绑定、路径编码、请求受理与查询/通知字段隔离 | 实际退款到账及商户准入 |
| [RefundmentRequestTest](../test/RefundmentRequestTest.cs) | 部分退款请求保留商户订单号/退款号和原金额；应答保留退款状态、微信/商户单号及订单/用户实付金额差异 | 退款已到账、应答已验签、通知已可靠处理或业务已幂等落库 |
| [CryptographyHelperTest](../test/CryptographyHelperTest.cs) | APIv3 GCM 固定向量、UTF-8/Base64、空关联数据/明文、32 字节密钥/12 字节 nonce/16 字节标签、篡改及截断拒绝、32 个合成账户并发解密 | 通知或平台证书的来源、事件类型和接收商户已验证 |
| [ProtocolCryptographyTest](../test/ProtocolCryptographyTest.cs) | V2 MD5/HMAC-SHA256 固定签名、Ordinal/固定文化规范和并发 | 微信支付来源验证已完成 |
| [公众号签名测试](../test/AccountManagerAsyncTest.Channel.cs) | 复用账户测试上下文，通过本地缓存提供不同账户的票据，验证并发 JS-SDK SHA-1 签名 | 票据类型缓存隔离、URL 片段规范化或站点授权校验已实现 |

模型测试直接使用公开模型和 `System.Text.Json`，不复制内部 HTTP 选项，也不声称覆盖实际传输。字段依据为[JSAPI 下单](https://pay.weixin.qq.com/doc/v3/merchant/4012791856)和[申请退款](https://pay.weixin.qq.com/doc/v3/merchant/4013071036)。测试数据全部合成，`example.com` 回调字符串只用于序列化，不发送请求。

密码测试依据[微信资源解密协议](https://pay.weixin.qq.com/doc/v3/merchant/4012071382)与 [.NET AesGcm.Decrypt 契约](https://learn.microsoft.com/dotnet/api/system.security.cryptography.aesgcm.decrypt)。GCM/CBC 非空固定密文使用 Python `cryptography` 独立生成，MD5/HMAC 期望值使用 `hashlib`/`hmac` 生成；空明文向量取自 [.NET 保存的 NIST AES-256-GCM case 13](https://github.com/dotnet/runtime/blob/main/src/libraries/Common/tests/System/Security/Cryptography/AesGcmTests.cs)。运行测试不依赖 Python，也不生成真实账户密钥。内部 V2 签名使用反射绑定现有方法，不扩大生产类型可见性；调用方自建 HMAC 实例并负责释放。公众号测试缓存只响应已有票据查询，其他操作立即失败，不访问微信。

这些离线用例不替代微信 `/v3/security/echo` 实测或真实支付联调；后两者需要账号、密钥及相应测试环境。GCM 不替代 HTTP/通知签名验证；CBC 接收方校验不替代接收层的时效、重放及事件校验。

证书测试复用账户上下文中的命名 `IHttpClientFactory`；支付 HTTP 测试通过命名 `IHttpMessageHandlerFactory` 进入真实签名/验签处理器。未配置受控处理器时立即失败，不转发真实请求。证书/密钥全部在内存生成，临时配置文件在测试结束时清理。Automao 的 [AuthorityAccountTest](../../../../../Automao/server/externals/wechat/abstraction/test/AuthorityAccountTest.cs)验证两种 API 密钥独立传递、默认应用保持、同商户号配置隔离及异步文件释放。

<a id="testing-3"></a>

### 测试工程与夹具设计

主库、Web API 和回调网关的生产项目分别位于 `src`、`api/src`、`gateway/src`，各自的离线测试位于同级 `test` 目录。六个项目均加入 `Zongsoft.Externals.Wechat.slnx`，`build.cake` 的 `**/test/*.csproj` 匹配三个测试项目。

| 位置 | 被测项目与装配方式 | 核心场景 |
| --- | --- | --- |
| `test` | 主库的单元及内存协议测试 | 签名规范、密钥轮换、序列化、金额、取消、并发及资源释放 |
| `api/test` | 仅 `ProjectReference` 引用 `api/src/Zongsoft.Externals.Wechat.Web.csproj`；Core/Web/微信主库用 HintPath，ASP.NET 测试依赖显式声明 | 本地 ASP.NET Core 管线中的路由、身份作用域、响应字段及凭据不下发 |
| `gateway/test` | 仅 `ProjectReference` 引用 `gateway/src/Zongsoft.Externals.Wechat.Gateway.csproj`；其它框架程序集用 HintPath，直接装配本地 ASP.NET Core 管线 | 路由与正文限制、处理器交接、失败与重复应答；验签和解密由主库测试覆盖 |
| Automao HTTP 集成 | 实际站点与 gateway 插件宿主、数据库及业务提供者 | 插件加载、配置绑定、服务解析、首次调用、跨租户拒绝、状态持久化及幂等 |

夹具按实际被测契约逐步提供：

- 密码测试使用官方固定向量和明确标为测试用途的材料；校验错误密钥、标签、关联数据、填充和截断输入。并发使用同一协议入口，不复制实现作为期望值。
- HTTP 测试通过具有明确职责的传输边界接入内存 `HttpMessageHandler`，捕获实际请求字节并给出受控响应。未匹配请求立即失败，不转发公网；真实管线仍负责签名、验签和错误映射。
- 时间、缓存和锁使用框架现有契约或最窄的真实生产扩展点；使用受控信号推进刷新、取消和并发，不以长时间 Sleep 等待。
- 应答/通知夹具覆盖有效签名、正文篡改、未知序列号、过期时间、错误身份和处理器失败。身份验证失败时断言处理器未被调用，可靠处理失败时断言没有成功应答。
- 资源夹具记录流、内容和响应的释放；借入流与自有流分别验证。只为单个测试类服务的辅助实现使用私有嵌套类，不扩大生产成员可见性。
- 操作 `ApplicationContext.Current` 或静态提供者的测试归入禁止并发的 collection，并恢复状态；普通纯模型测试不依赖该上下文。

<a id="testing-4"></a>

### Automao 入口与依赖

数据库脚本、设计文档和 HttpYac 文件统一位于 [D:\Automao\documents](../../../../../Automao/documents)，数据库脚本目录为 [database](../../../../../Automao/documents/database)。完整 Automao 应用环境采用 [D:\Automao\server\automao.pod-mysql.yaml](../../../../../Automao/server/automao.pod-mysql.yaml)；执行前核对容器与初始化条件。

合成通知的完整插件夹具为 [Automao notifications.py](../../../../../Automao/server/externals/wechat/integration/notifications.py)，运行条件见其[说明](../../../../../Automao/server/externals/wechat/integration/README.zh-Hans.md)。夹具在独立数据库中验证实际 Gateway、支付/退款持久化、并发去重及故障恢复，并调用 HttpYac 的 `wechat-notifications.http` 请求。

请求仓库为 [D:\Automao\documents\http](../../../../../Automao/documents/http)，执行规则见[站点请求说明](../../../../../Automao/documents/http/sites/README.zh-Hans.md)。业务实现位于 [Automao/server](../../../../../Automao/server)，不在框架测试中嵌入其插件宿主。

| 场景 | 已有入口 | 执行前提与证据 |
| --- | --- | --- |
| 身份 | `sites/authentication.http` | 专用身份、密码及准确的 host/port/scenario；只从本地环境注入，不打印凭据 |
| 提供者发现与业务操作 | `sites/business/paying/payment/payment.http`；`GET /Paying/Payments/Providers`、`POST /Paying/Payments/Pay/{voucher}`、`POST /Paying/Payments/{key}/Refresh`、`POST /Paying/Payments/{key}/Cancel` | 解析 Wechat 提供者及账户依赖；支付、刷新、取消会调用提供者，不能作为默认只读批次 |
| 退款 | 同一 payment 请求文件中的退款入口，以及 `sites/business/paying/refundment/refundment.http` | 专用已支付订单、原支付归属、退款金额及业务单号；受理、处理中、终态分别核对 |
| 统计隔离 | `sites/{business,customer}/paying/total-isolation.http` | 双租户/双用户的同名凭据及期望金额；只验证统计，不发起微信支付 |
| 明细隔离 | `sites/{administration,business,customer}/paying/detail-isolation.http` | 两方各一条明细的专用数据库及明细键/金额；管理端与商户端权限分别验证 |
| 支付/退款通知 | gateway 的 `POST /Externals/Wechat/Fallback/{payment|refundment}/{merchant}` | 合成签名通知、实际业务处理器与状态落库；运行条件见[通知契约](#payment-notifications) |

支付调用链为站点 PaymentController → 支付业务服务 → `IPaymentProvider` 的 Wechat 实现 → 微信适配器。退款由退款业务服务调用 `IRefundmentProvider`；通知经 `PaymentHandler` / `RefundmentHandler` 进入 `Services.PaymentUtility.OnPaidAsync` / `Services.RefundmentUtility.OnRefundedAsync`。入口完成不等于事务与幂等已经通过验收，必须验证最终业务记录。

插件依赖为 `Automao.Paying.Providers.Wechat` → `Automao.Paying` 与 `Automao.Externals.Wechat`。gateway 插件在 `/Workbench/Externals/Wechat/Fallback/Handlers` 注册 `Payment`、`Refundment`；微信外部模块还承载刷脸签约等处理器。按实际部署核对框架 Gateway 插件、业务插件及首次服务解析，不能只检查程序集文件是否存在。

集成前落实以下条件：

1. 确定隔离的开发/测试方案、站点和 gateway 地址。站点文档默认 administration/business/customer 为 5001/5002/5003；gateway 端口以实际配置为准。
2. 数据库、Redis、所选流程需要的 daemon 可用；附件/进件上传流程另需对象存储。容器状态和端口监听只证明基础可达，不能替代认证和业务健康检查。
3. 部署目录含本次适配器及匹配的 Automao 业务程序集，`ZongsoftFrameworkPathReferenced` 与 `ZongsoftFrameworkPath` 确实选中本地新程序集。提供者还依赖已构建的 Automao Core、Common、Paying 和 Externals.Wechat，外部模块另外依赖 Security。
4. 专用测试身份和双方租户、用户、订单、支付/退款明细、账户绑定及期望金额齐备，明确只清理本次创建的数据；不采用生产数据或管理员身份代替客户权限验证。
5. 离线业务集成在实际宿主注册 `IHttpMessageHandlerFactory` 的 `Wechat.Payments` 受控传输；需要证书轮换时另提供 `IHttpClientFactory` 的 `Wechat.Certificates` 响应。它们只替换传输，真实签名/验签仍执行，且未匹配请求必须失败。保留实际插件、授权和持久化路径；传输可替换不等于完整集成环境已经具备，约束见 [HTTP 契约](#http-pipeline)。
6. 真实微信联调另需账号/商户资格、产品权限及外网可达的回调地址，按对应产品配置 HTTPS、域名或 IP 条件。账号与云服务器准备延后至此阶段；缺少条件时不执行真实交易，也不宣称联调完成。

HttpYac 按标签或请求名选取用例，禁止将 `--all` 与 `--tag` 合用。代码构建、协议单元测试、本地 HTTP 管线、Automao 插件集成和微信实测分别报告，不合并成一个“支付测试通过”。

Automao 微信支付提供者契约测试位于 `modules/paying/providers/wechat/test/Automao.Paying.Wechat.Contract.Tests.csproj`。它通过合成账户提供者和签名 HTTP 响应验证真实 PaymentProvider 的三场景结果映射，不替代实际插件宿主集成。支付抽象测试的 PaymentStateTest 与 PaymentFeatureAsyncTest 验证原状态条件、终态保护以及独立 Token 的存储映射。

付款码协议夹具见[AccountManagerAsyncTest.Tickets](../test/AccountManagerAsyncTest.Tickets.cs)，覆盖两类商户、支付中/未知状态、V2 查单、XML/签名/身份拒绝、请求取消、撤销证书与 recall 结果，以及在线刷脸凭据。Automao 的微信提供者契约测试覆盖真实提供者的付款码参数传递、超时/无效签名不误报失败及明确支付状态；支付业务契约测试覆盖取消请求的必要落库、取消提供者的等待、终态并发保护和组合支付完整授权。它们使用受控传输或数据协作者，不证明真实微信交易或实际 TLS 握手。

退款业务另有 [Automao 受控传输集成入口](../../../../../Automao/server/externals/wechat/integration/README.zh-Hans.md)：实际插件、MySQL 可串行化额度保留、显式原单重试和部分金额分配。与通知夹具串行运行，使用独立合成数据库及 Redis 数据库 14，不调用真实微信服务。

<a id="testing-5"></a>

### 进件、银行与上传

`AccountManagerAsyncTest.Merchants.cs` 检查申请号归属、敏感字段加解密、有效期、图片数组编码、银行卡多候选、空页总数、上传 MIME/摘要/meta 签名和大小限制。Automao 的 `externals/wechat/integration/onboarding.py` 与其它夹具串行运行，检查原申请查询、待签约不启用账户、完整链接存储、完成/冻结/未知状态及账户归属。夹具从当前建表脚本初始化自己的隔离库。

<a id="testing-6"></a>

### 会员卡

`AccountManagerAsyncTest.Membership.cs` 验证完整 V3 路径及请求签名、商户配置中的公众号选择、模板/AppID/品牌隔离、删除和作废的 204 验签、预存逐项结果以及未签名响应拒绝。Automao 会员提供者以定向构建验证直接调用契约；未实现的会员业务不作为可用功能验收。

<a id="testing-7"></a>

### 自有账号稳定凭据

`AccountManagerAsyncTest.Credentials.cs` 通过命名传输及缓存/锁契约替身验证本机合并、跨节点发布后重读、强制刷新去重、账号/票据域隔离、剩余 TTL、取消、锁丢失、缓存写入失败和繁忙退避。此类测试不连接微信，也不替代真实分布式缓存驱动本身的集成测试。

<a id="testing-8"></a>

### 小程序身份

主库的 `AccountManagerAsyncTest.Applet.cs` 使用合成 code2Session 和手机号响应，覆盖可选 UnionId、密钥序列化排除、应用水印、OpenId 绑定、错误与取消。`api/test` 通过环回 HTTP 和真实 ASP.NET 控制器验证资源授权、应用会话签发、匿名手机号请求拒绝及已移除的凭据/小程序用户路径。

Automao 的 `externals/wechat/integration/identity.py` 加载实际客户站点微信鉴证插件，使用隔离 MySQL 与受控微信传输验证应用/租户/门店归属、原始手机号不可信、授权号码绑定、重复登录和停用。运行前构建该站点与集成宿主，遵循其它夹具的串行执行及清理要求。用户表的手机号唯一约束仍生效：号码已属于其它租户时拒绝关联，不绕过约束创建或改绑。

<a id="testing-9"></a>

### 公众号协议

`AccountManagerAsyncTest.OAuth.cs` 覆盖 scope、缺失资料/UnionId、跨账号/身份拒绝、刷新与有效性错误、原始 URL 去片段、空用户页和模板字段/错误。Web 的 `AccountHttpTest` 使用 Zongsoft 的嵌套控制器发现器验证公众号与小程序会话、用户和消息路由账号绑定、站点策略拒绝以及实际签名 JSON。真实浏览器 state 流程、消息送达和账号权限仍需对应宿主与微信环境验证。

`PlatformCryptographyTest` 使用官方 SHA-1/CBC 固定向量和独立中文向量，覆盖错误签名、密钥、接收方、完整填充、负数与越界长度、严格 UTF-8 及二进制边界。运行时不下载官方示例，不访问微信；协议契约见[消息密码协议](#platform-cryptography)。

组件测试覆盖独立令牌键域、并发合并、过期票据、预授权有效期、目标 AppId 与实际权限集、加密 XML 的重复字段/签名/时效/接收方/限长，以及回调可靠接收应答。Web 测试验证租户与回跳域名策略、state 保存后返回链接；测试存储仅模拟契约，实际事件幂等及授权状态持久化须通过 Automao 插件集成验证。

授权者凭据验证覆盖并发刷新合并、轮换后可靠保存、迟到取消、撤销/新授权与刷新竞争、失去租约和持久化失败；委托调用覆盖组件/授权者身份、权限、类型、OAuth 凭据、票据 GrantId 隔离及小程序专用登录。

<a id="testing-10"></a>

### SaaS 授权与普通服务商

`AccountManagerAsyncTest.Components.cs` 与 `Authorizers.cs` 覆盖组件票据、授权码、实际账号资料、组件/授权者凭据隔离、权限、委托协议及撤销竞争。`Partners.cs` 验证普通服务商申请字段、全部敏感字段加密、签名材料一致、64 位申请号及查询归属；完成进件不表示已取得所有支付权限。

Automao 的 `components.py` 加载实际业务管理 API、客户鉴证器和 Gateway，验证持久化、当前租户归属、加密密钥环、并发/恢复与权限变化；`partners.py` 分别运行商户端和平台管理端，验证普通进件、平台确认、产品控制及支付/退款联动。两者复用 `D:\Automao\documents\http\sites` 中的微信管理读取请求，通过 HttpYac 验证接口结果。与既有五组夹具串行执行，详细条件和边界见[集成说明](../../../../../Automao/server/externals/wechat/integration/README.zh-Hans.md)。

这些测试替换外部微信响应和专用测试身份的权限决策，不能证明微信真实准入、第三方授权名单、商户签约、AppID 绑定或资金到账。真实验收需官方测试账号、合格支付主体及公网回调环境；未具备条件的项目不以离线成功代替。

<a id="saas-development-and-test-accounts"></a>

## SaaS 开发与测试账号

Automao 的接入目标包括两条独立能力：商户将公众号/小程序授权给微信开放平台第三方平台，以及微信支付服务商管理特约商户并发起支付。二者的身份、权限、凭据和回调分开建模，通过既有插件机制装配。

<a id="saas-development-and-test-accounts-1"></a>

### 个人开发阶段

| 账号或环境 | 个人可用范围 | 对本项目的用途与限制 |
| --- | --- | --- |
| 离线协议与 Automao 隔离测试环境 | 不依赖微信账号 | 构建、协议向量、受控响应、插件装配、租户隔离与业务状态验证；不等于微信真实联调 |
| 公众号接口测试号 | 微信扫码申请，无须先有正式公众号 | 测试后台实际开放的公众号接口；不能据测试号首页说明推定具备商户支付或全部正式服务号权限。[申请入口](https://mp.weixin.qq.com/debug/cgi-bin/sandbox?t=sandbox/login) |
| 小程序测试号 | 微信扫码取得测试账号 | 开发者工具开发、真机预览与可用接口验证；逐项核实能力，不把测试 AppID 视为支付权限。[官方说明](https://developers.weixin.qq.com/miniprogram/dev/devtools/sandbox.html)、[申请入口](https://mp.weixin.qq.com/wxamp/sandbox?doc=1) |
| 个人第三方平台测试账号 | 先注册个人主体开放平台，再创建 1 个平台型第三方平台 | 可开展授权链接、后端服务、代调用和代开发测试；不支持定制化型平台、提交审核、全网发布、服务商组件、标准模板审核和代注册小程序。[官方个人测试说明](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/getting_started/how_to_test.html) |
| 微信支付服务商号 | 官方普通服务商入驻支持企业、政府机关、事业单位、社会组织，未包含个人和个体工商户 | 无合格主体时先实现并离线验证，真实服务商及特约商户支付联调待具备准入条件；个人开放平台测试账号不能替代服务商号。[商户号申请要求](https://pay.wechatpay.cn/doc/v3/partner/4012081990) |

未全网发布的第三方平台仅接受其授权测试名单内的公众号/小程序授权；用于授权的账号类型和能力以实际控制台为准，不预先保证所有测试号都能充当授权方。现有个人公众号可用于核查其允许的授权及接口，不作为已认证服务号使用。[全网发布与测试说明](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/operation/thirdparty/publish.html)

微信支付 V2 官方仿真系统仅覆盖文档列明的付款码用例，申请仿真签名密钥仍需商户号和签名，不能视作个人无资质的通用 V3 支付沙箱。普通商户小程序支付还需认证小程序、商户产品权限及 AppID 绑定；服务商另需特约商户授权产品。[V2 仿真说明](https://pay.wechatpay.cn/doc/v2/merchant/4011984810)、[普通商户准备](https://pay.wechatpay.cn/doc/v3/merchant/4015459512)、[服务商准备](https://pay.wechatpay.cn/doc/v3/partner/4012076731)

<a id="saas-development-and-test-accounts-2"></a>

### 账号准备条件

1. 先完成离线开发与本地验证；账号申请和公网回调服务器准备延后至真实微信联调前，离线开发不以正式主体为前置条件。具体环境条件见[验证契约](#testing)。
2. 申请公众号和小程序测试号，使用开发者本人的微信扫码。账号归开发者所有，密码、验证码及 AppSecret 不进入聊天、版本库或日志。
3. 在[开放平台注册页](https://open.weixin.qq.com/wxaopen/regist/index)注册个人主体账号，再在第三方平台管理中创建平台型测试账号。准备未被开放平台、公众平台或私人微信占用的邮箱；密码设置、邮箱激活、身份信息和扫码按官方页面由本人配合完成。[注册指引](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/operation/open/create.html)
4. 为测试平台配置受控回调地址、测试授权名单和所需权限；密钥由服务器安全配置提供。授权权限和账号能力须在实际后台核定，缺失能力明确列为未联调。
5. 正式开放平台资质、全网发布以及微信支付服务商入驻另按真实主体和产品要求办理；不使用借用主体或虚构资料通过审核。

<a id="saas-development-and-test-accounts-3"></a>

### SaaS 功能边界

- 现有公众号、小程序、普通商户/服务商支付、退款、刷脸、会员及进件实现，具有各自的插件和 Automao 验证边界。
- SaaS 接入包含普通服务商特约商户关系、第三方平台授权事件与令牌、代公众号/小程序调用和 Automao 租户授权管理。已有平台收付通进件保留独立产品语义。
- 小程序模板上传、审核发布、代注册、服务商组件及其它完整代运营产品不因“支持第三方平台”自动加入；实际需求另列功能与准入条件。
- 第三方授权只允许操作所获权限集，撤销授权后停用委托调用；支付权限、AppID 绑定和特约商户归属不从公众号/小程序授权自动推导。

<a id="saas-development-and-test-accounts-4"></a>

### Automao HTTP 测试入口

请求文件位于独立文档仓库 [Automao/documents/http](../../../../../Automao/documents/http)，不是 `Automao/server/documents/http`。先读[站点请求说明](../../../../../Automao/documents/http/sites/README.zh-Hans.md)，复用 `sites/{administration,business,customer}/paying` 下的支付、退款及隔离验证请求。

已有统计和明细隔离用例不触发第三方支付；支付/退款提供者操作须按相应用例的测试条件单独执行。新增用例只补充缺失场景，不将默认只读批次改成批量资金操作。

<a id="platform-reference"></a>

## 微信平台接口与接入约束

本文以微信官方文档及微信支付官方资源库为依据，资料日期为 2026-10-22。本文记录平台契约，不表示本库已实现所列能力。各产品按账号类型、权限集、商户产品权限和应用绑定关系开放，没有覆盖微信全部业务的统一版本号。

<a id="platform-reference-1"></a>

### 文档入口与产品边界

| 产品 | 官方入口 | 边界 |
| --- | --- | --- |
| 公众号 | [公众号开发文档](https://developers.weixin.qq.com/doc/subscription/guide/) | 与服务号分别维护文档；不能推定服务号的接口权限适用于所有公众号。 |
| 服务号 | [开发指南](https://developers.weixin.qq.com/doc/service/guide/dev/)、[服务端 API](https://developers.weixin.qq.com/doc/service/api/) | 涵盖消息、菜单、素材、草稿、发布、用户、网页授权及 JS-SDK；具体权限以接口适用范围为准。 |
| 小程序 | [服务端 API](https://developers.weixin.qq.com/miniprogram/dev/server/API/) | 登录、手机号、订阅消息、交易管理等有各自的接口契约。 |
| 开放平台 | [平台介绍](https://developers.weixin.qq.com/doc/oplatform/open/intro.html)、[第三方平台 API](https://developers.weixin.qq.com/doc/oplatform/openApi/) | 开放平台绑定与第三方代开发授权是不同关系。 |
| 微信支付 | [普通商户 APIv3](https://pay.weixin.qq.com/doc/v3/merchant/4012081606)、[合作伙伴 APIv3](https://pay.weixin.qq.com/doc/v3/partner/4012081673) | 普通商户、服务商、子商户身份以及不同支付产品需要分别建模。 |

企业微信、微信小店、视频号、小游戏等属于独立业务域。开放平台将多种应用绑定到同一开放平台账号时，可以通过满足条件后返回的 UnionID 关联用户；这不代表它们共享全部接口或访问凭证。[开放平台介绍](https://developers.weixin.qq.com/doc/oplatform/open/intro.html)

旧 `offiaccount`、小程序 `OpenApiDoc` 及部分 `Third-party_Platforms/api` 文档地址会跳转到目录页或返回失效页面。引用具体接口时应使用表中入口继续定位到当前页面，不能仅凭 HTTP 200 判断旧链接仍指向原接口。

<a id="platform-reference-2"></a>

### 微信支付 APIv3

<a id="platform-reference-3"></a>

#### 认证、验签与密钥

APIv3 使用 REST、JSON、SHA256-RSA 请求签名和 AES-256-GCM 回调资源加密；官方同时保留微信支付公钥及平台证书两种验签方式，推荐公钥方式。[APIv3 概述](https://pay.weixin.qq.com/doc/v3/merchant/4012081606)

| 材料 | 用途与限制 |
| --- | --- |
| 商户 API 私钥及商户证书序列号 | 用于商户请求签名及相应的客户端调起签名。采用微信支付公钥验签不等于移除商户签名身份。 |
| APIv3 密钥 | 用于解密回调中的加密资源；不作为 RSA 请求签名私钥。 |
| 微信支付公钥及公钥 ID | 用于验证微信支付应答、回调签名，以及加密接口规定的敏感字段。公钥 ID 与商户证书序列号含义不同。 |
| 微信支付平台证书 | 存量验签方式。其公钥承担微信侧验签及敏感字段加密用途；不能与商户 API 证书混用。 |

材料角色依据：[接入模式](https://pay.weixin.qq.com/doc/v3/partner/4012081931)、[开发必要参数说明](https://pay.weixin.qq.com/doc/v3/partner/4013080340)。公钥模式及平台证书模式的选择与切换依据：[公钥切换说明](https://pay.weixin.qq.com/doc/v3/merchant/4012154180)。

应答与回调验签必须使用未经重新序列化的原始正文，以及 `Wechatpay-Timestamp`、`Wechatpay-Nonce`、`Wechatpay-Signature`、`Wechatpay-Serial`。验签串由时间戳、随机串、原始正文组成，每行及末行以 LF 结束；HTTP 204 的空正文也有对应验签规则。`WECHATPAY/SIGNTEST/` 探测签名必须按正常规则验签并拒绝失败结果，不能特殊放行。下载账单文件不适用普通 JSON 响应验签，应按接口返回的哈希信息校验文件。[微信支付公钥验签](https://pay.weixin.qq.com/doc/v3/merchant/4013053249)

敏感字段使用微信支付公钥加密，并通过 `Wechatpay-Serial` 标识对应公钥；处理字段前应按具体接口判断是否要求加密，不得把所有字符串统一加密。[敏感字段加密](https://pay.weixin.qq.com/doc/v3/merchant/4013053257)

<a id="platform-reference-4"></a>

#### 支付、退款与回调

普通商户的 JSAPI 与小程序下单使用 `/v3/pay/transactions/jsapi`，客户端调起参数属于独立契约，不能直接把下单结果作为付款结果。[JSAPI 下单](https://pay.weixin.qq.com/doc/v3/merchant/4012791856)、[小程序下单](https://pay.weixin.qq.com/doc/v3/merchant/4012791897)

普通商户模式使用 `mchid`、`appid`；服务商模式区分 `sp_mchid`、`sub_mchid`、`sp_appid`、`sub_appid` 及对应 OpenID。服务商以自己的 API 私钥签名，不能通过任意替换商户号复用普通商户身份。[接入模式](https://pay.weixin.qq.com/doc/v3/partner/4012081931)

退款接口为 `POST /v3/refund/domestic/refunds`。金额按整数分表达，使用独立的 `out_refund_no`；同一退款单号重复请求只处理同一笔退款。受理成功不等于退款完成，最终状态需要退款查询及相应通知确认。部分退款需要不同退款单号，总退款金额受原订单金额约束。[申请退款](https://pay.weixin.qq.com/doc/v3/merchant/4012552567)

支付通知可能重复投递；商户必须幂等处理并在规定时间内应答。官方通用说明要求 5 秒内应答，重复且已处理的通知直接返回成功；验签失败返回失败 HTTP 状态。`notify_url` 使用可公网访问的 HTTPS 地址且不能携带查询参数。业务结果应结合查单兜底，不能以客户端成功回调代替服务端结果。[回调通知注意事项](https://pay.weixin.qq.com/doc/v3/merchant/4012075420)、[支付签名与验签总述](https://pay.weixin.qq.com/doc/v3/merchant/4012365342)

<a id="platform-reference-5"></a>

#### 商家转账

当前商家转账同时具有“用户确认收款”和“用户授权免确认”两种模式。官方产品说明将旧企业付款、红包及 `/v3/transfer/batches` 归入需要升级的产品；仅有 `/v3/` 前缀不能证明使用的是当前产品契约。[商家转账产品说明](https://pay.weixin.qq.com/doc/v3/merchant/4015273741)

| 场景 | 当前接口与状态约束 |
| --- | --- |
| 用户确认收款 | `POST /v3/fund-app/mch-transfer/transfer-bills`。创建成功且处于 `WAIT_USER_CONFIRM` 时返回 `package_info`，用于调起确认收款页。 |
| 转账同时申请免确认授权 | `/v3/fund-app/mch-transfer/transfer-bills/pre-transfer-with-authorization`。转账与授权状态均需处理。 |
| 单独申请免确认授权 | `/v3/fund-app/mch-transfer/user-confirm-authorization`。授权申请、用户确认及授权生效构成独立生命周期。 |
| 已授权后转账 | `/v3/fund-app/mch-transfer/transfer-bills/transfer`。按同步结果及查单规则判断结果，此模式没有转账结果异步通知。 |

接口对应及模式差异依据：[商家转账模式与接口表](https://pay.weixin.qq.com/doc/v3/merchant/4015273741)。

用户确认模式需要 `transfer_scene_id`、场景报备信息、正确的 AppID/OpenID 绑定及按规则加密的姓名。`ACCEPTED`、`PROCESSING`、`WAIT_USER_CONFIRM`、`TRANSFERING`、`CANCELING` 是非终态；`SUCCESS`、`FAIL`、`CANCELLED` 是终态。HTTP 200、创建转账单成功、前端成功展示确认页均不等于到账。[发起转账](https://pay.weixin.qq.com/doc/v3/merchant/4012716434)、[JSAPI 调起确认收款](https://pay.weixin.qq.com/doc/v3/merchant/4012716430)

免确认收款授权应保存商户授权单号和微信授权单号，授权可以被用户或商户解除；授权结果通知与资金转账结果是不同事件。已授权后转账必须检查订单状态，网络失败或未知错误不能直接换商户单号重试。[发起免确认授权](https://pay.weixin.qq.com/doc/v3/merchant/4015901167)、[授权结果通知](https://pay.weixin.qq.com/doc/v3/merchant/4014512908)、[用户授权后转账](https://pay.weixin.qq.com/doc/v3/merchant/4014399371)

商家转账要求配置接口请求来源 IP；产品权限、场景准入和应用绑定需要在商户侧完成。[商家转账开发指引](https://pay.weixin.qq.com/doc/v3/merchant/4012715211)

<a id="platform-reference-6"></a>

#### 付款码与 SDK 边界

当前普通商户和合作伙伴文档中的“付款码支付（V2）”均明确指向 V2 接口。已实现的对应功能在官方没有更新版本时继续采用 V2；不能因为其它支付产品已采用 APIv3 就删除该能力或编造 V3 付款码端点。升级到新版的判断必须对应同一产品、商户角色及配套流程，文档目录中的 `/doc/v3/` 本身不是接口版本依据。[普通商户付款码支付](https://pay.weixin.qq.com/doc/v3/merchant/4012647301)、[合作伙伴付款码支付](https://pay.weixin.qq.com/doc/v3/partner/4012851192)

微信支付官方 GitHub 组织列出 Java、Go、PHP 库及相关工具；当前公开目录未列出官方 .NET SDK。不能把第三方 .NET 库描述为微信官方 SDK。新版商家转账官方文档采用工具类与示例代码接入方式，而不是保证旧 SDK 覆盖新产品。[官方仓库](https://github.com/wechatpay-apiv3)、[商家转账接入说明](https://pay.weixin.qq.com/doc/v3/merchant/4015273741)

<a id="platform-reference-7"></a>

### 公众号与服务号

<a id="platform-reference-8"></a>

#### 凭证与网页授权

`getStableAccessToken` 是接口英文名，实际地址为 `POST /cgi-bin/stable_token`。它与 `/cgi-bin/token` 的凭证隔离，官方推荐使用稳定版。普通模式在有效期内复用 token；必须依据响应 `expires_in` 缓存，不能每次调用后固定延长两小时。`force_refresh=true` 会使先前 token 失效并受次数限制。该接口不支持第三方平台调用，因此不能替代 `component_access_token` 或 `authorizer_access_token`。[稳定版接口调用凭据](https://developers.weixin.qq.com/doc/service/api/base/api_getstableaccesstoken.html)

网页 OAuth 使用 `/sns/oauth2/access_token`、`/sns/oauth2/refresh_token`、`/sns/auth`、`/sns/userinfo`。网页授权 token 与服务端基础接口 token 不同。AppSecret 和 OAuth token 只保存在服务器；不得返回客户端。授权 code 一次性使用，五分钟未使用失效；OAuth refresh token 的有效期为 30 天。[微信网页授权](https://developers.weixin.qq.com/doc/service/guide/h5/auth.html)

网页授权主要适用于已认证服务号，文档列出已认证政府、事业单位和媒体公众号的特例；不能对任意公众号、小程序或开放平台 AppID 开启同一网页授权流程。`snsapi_base` 主要取得 OpenID，`snsapi_userinfo` 受用户授权及能力规则约束。用户信息撤回、注销和资料变更属于需要处理的事件。[网页授权适用范围与事件](https://developers.weixin.qq.com/doc/service/guide/h5/auth.html)

JS-SDK 使用服务端缓存的 `jsapi_ticket` 生成签名；参与字段为随机串、票据、时间戳和去掉 `#` 及后续片段的页面 URL，按官方算法进行 SHA-1。JS-SDK 权限签名与支付 RSA 签名属于不同协议，不能因支付采用 APIv3 就统一更换算法。[JS-SDK](https://developers.weixin.qq.com/doc/service/guide/h5/jssdk.html)

<a id="platform-reference-9"></a>

#### 消息与回调

服务号仍分别开放模板消息、订阅通知、一次性订阅消息、客服消息、群发消息等能力。不能把所有消息统一替换成小程序 `subscribe/send`；发送端点、模板定义、用户授权及权限限制需要按产品区分。[服务端 API 目录](https://developers.weixin.qq.com/doc/service/api/)

服务号 URL 接入验证使用 GET 查询参数验签后返回 `echostr`；消息安全模式使用 `msg_signature`，参与计算的是 Token、时间戳、随机串及 `Encrypt`。验签后按 AES-256-CBC、微信规定的 32 字节填充规则解密，并校验正文末尾的 AppID；被动回复有相应加密应答要求。支付回调的 RSA/AES-GCM 不能复用到此协议。[消息推送接入](https://developers.weixin.qq.com/doc/service/guide/dev/push/)、[消息加解密](https://developers.weixin.qq.com/doc/service/guide/dev/push/encryption.html)

服务号推送介绍页写明数据格式仅 XML，而加解密页同时展示 XML 与 JSON 示例。两页存在粒度差异，具体账号配置与具体事件文档是最终依据，不能仅从通用示例推断每种服务号事件均支持 JSON。[推送配置](https://developers.weixin.qq.com/doc/service/guide/dev/push/)、[加解密示例](https://developers.weixin.qq.com/doc/service/guide/dev/push/encryption.html)

<a id="platform-reference-10"></a>

### 小程序

| 能力 | 当前契约 |
| --- | --- |
| 登录 | 客户端 `wx.login` 获取 code，由服务器调用 `GET /sns/jscode2session`。响应包含 OpenID、session_key，UnionID 按开放平台绑定条件返回。 |
| 手机号 | `POST /wxa/business/getuserphonenumber`，请求传手机号授权 code；code 只能使用一次且有效期五分钟。支持可选 `openid`，提供时校验其与 code 的绑定。 |
| 订阅消息 | `POST /cgi-bin/message/subscribe/send`，使用模板 ID、用户 OpenID、模板数据和小程序页面；第三方代调用须具备对应权限集。 |

接口依据：[登录凭证校验](https://developers.weixin.qq.com/miniprogram/dev/server/API/user-login/api_code2session.html)、[获取手机号](https://developers.weixin.qq.com/miniprogram/dev/server/API/user-info/phone-number/api_getphonenumber.html)、[发送订阅消息](https://developers.weixin.qq.com/miniprogram/dev/server/API/mp-message-management/subscribe-message/api_sendmessage.html)。

`session_key` 是用户数据签名与加解密材料，官方明确不应向小程序下发，也不应对外提供。应用服务器根据微信身份创建自己的登录态。手机号授权 code 与登录 code 对应不同接口，不应互换。[小程序登录](https://developers.weixin.qq.com/miniprogram/dev/framework/open-ability/login.html)、[手机号接口](https://developers.weixin.qq.com/miniprogram/dev/server/API/user-info/phone-number/api_getphonenumber.html)

订阅消息需要用户先取得相应下发授权；一次性订阅、长期订阅和设备订阅有各自入口。订阅弹窗结果、取消订阅和下发结果属于独立事件；模板字段有类型和长度约束，发送失败不能被统一解释为 token 过期。[订阅消息指南](https://developers.weixin.qq.com/miniprogram/dev/framework/open-ability/subscribe-message.html)、[发送接口限制与错误码](https://developers.weixin.qq.com/miniprogram/dev/server/API/mp-message-management/subscribe-message/api_sendmessage.html)

<a id="platform-reference-11"></a>

### 第三方开放平台

账号绑定到开放平台用于同主体应用之间的 UnionID 关联；账号授权给第三方平台用于代开发、代运营。服务商不能仅因为代运营多个商家，就假定所有商家的用户共享 UnionID。[第三方平台基本概念](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/getting_started/terminology_introduce)

| 凭证或授权步骤 | 当前契约 |
| --- | --- |
| `component_verify_ticket` | 微信每十分钟推送的票据，有效期十二小时，是取得平台 token 的输入。经对应事件协议验证后保存最近有效值，接收端返回 `success`。 |
| `component_access_token` | `POST /cgi-bin/component/api_component_token`，输入第三方平台 AppID、AppSecret、verify ticket；有效期两小时，按过期时间管理。 |
| 预授权与用户授权 | 取得预授权码并引导商家完成授权，取得 `authorization_code`。 |
| 授权信息与刷新令牌 | `POST /cgi-bin/component/api_query_auth` 换取授权信息、权限集和 `authorizer_refresh_token`；重新授权会使之前的刷新令牌失效。 |
| `authorizer_access_token` | `POST /cgi-bin/component/api_authorizer_token`，以 component token 和授权方刷新令牌获取；按授权账号分别缓存。 |

来源：[验证票据](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/Before_Develop/component_verify_ticket.html)、[component token](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getcomponentaccesstoken.html)、[获取预授权码](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getpreauthcode.html)、[授权信息](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getauthorizerrefreshtoken.html)、[授权账号 token](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getauthorizeraccesstoken.html)。

当前文档对 `api_query_auth` 和 `api_authorizer_token` 的查询参数写作 `access_token`，允许值为 `component_access_token`；参数名、凭证类型和账号作用域需要分别校验。授权账号可以只授予部分权限，不能把第三方平台声明的全部权限视作已获授权。[授权信息及权限集](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getauthorizerrefreshtoken.html)

<a id="platform-reference-12"></a>

### 证据适用范围

上述结论来自公开的官方文档正文及微信支付官方同步资源；不包含真实商户、公众号或小程序账号的权限验证，也不证明具体账号已开通产品。金额上限、调用额度、收费、类目准入及新字段应在落地接口时按当前页面和实际账号再次确认。

网页授权文档顶部明确性别与地区信息已停止返回，但同页旧字段表仍保留相应项目；数据模型不得依赖这些字段必有值。回调 XML/JSON 差异见前述消息章节。官方文档的这类内部不一致应按限制更严格的声明和实际账号接口能力处理，不通过第三方文章补成无依据的保证。[网页授权说明](https://developers.weixin.qq.com/doc/service/guide/h5/auth.html)
