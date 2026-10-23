---
name: zongsoft-wechat
description: 修改、审查或排查 Zongsoft 微信扩展库的账户凭据、公众号、小程序、开放平台、支付协议、Web API 与回调网关，并追踪 Automao 调用方适配；不用于小程序前端页面开发。
---

# Zongsoft 微信适配工作流

## 定位与资料

1. 阅读 [AGENTS.md](AGENTS.md)、[README.zh-Hans.md](README.zh-Hans.md)、[解决方案](Zongsoft.Externals.Wechat.slnx)及受影响项目文件，先确定改动属于主库、Web API 还是回调网关。遵循 `D:\Zongsoft\guidelines` 的[开发规范](../../../guidelines/zongsoft.csharp.guidelines.md)及[协作流程](../../../guidelines/AGENTS.md)；通用外部适配规则见[上级技能](../SKILL.md)。
2. 修改接口字段、签名、加解密、令牌或业务状态时，从[官方接口参考](docs/implementation.zh-Hans.md#platform-reference)定位对应官方文档，核实产品、账号/商户角色和当前权限。资料日期不等于接口永久有效；具体实现以当前官方契约为准。
3. 执行升级任务时阅读[微信生态接入设计](.plan.md#modernization-design)。先从源码与 Automao 调用列出已实现功能，再逐项记录现用接口、官方替代接口及依据、目标版本、配套流程和验收条件；有新版则只支持新版，没有新版则继续采用现有版本。该计划集中维护设计取舍、实施清单及验收状态；当前契约集中在双语 implementation 文档，未实现的候选能力不自动纳入升级范围。
4. 公开类型、配置、插件入口或回调协议发生变化时，按下文 Automao 路径追踪到消费端与部署端，明确受影响文件、目标行为和验收条件后实施。

功能与协议对应关系见[功能与接口核对表](docs/implementation.zh-Hans.md#capability-matrix)，本地构建、测试覆盖与集成准备条件见[验证契约](docs/implementation.zh-Hans.md#testing)；本次重构的实施顺序、逐项验收和自主执行约定见[实施计划](.plan.md#refactoring-plan)。

SaaS 范围包含第三方平台公众号/小程序授权管理及微信支付服务商特约商户接入，分阶段实现；[账号与测试边界](docs/implementation.zh-Hans.md#saas-development-and-test-accounts)区分个人测试能力和真实支付准入。

## 代码入口

主库、Web API 和回调网关分别位于 `src`、`api/src`、`gateway/src`，对应测试位于 `test`、`api/test`、`gateway/test`。各项目的 `.csproj`、`.plugin` 和 `.deploy` 与源码同目录，双语 README 留在项目根目录。`build.cake` 通过 `**/test/*.csproj` 发现三个测试项目。

| 工作内容 | 入口与追踪顺序 |
| --- | --- |
| 账户与身份 | `IAccountProvider` → `AppletManager` / `ChannelManager` → `Applet` / `Channel` → `Security/WechatAuthenticatorBase` 与 API 控制器 |
| 凭据与消息 | `CredentialManager`、`ChannelAuthentication`、`ChannelMessager`、`UserProvider`、`Transmitter`；分别核对基础凭据、网页授权与消息产品 |
| 商户与支付 | `Authority*`、`Options/Authority*` → `Paying/PaymentManager*`、`MerchantService`、`Uploader` → `Paying/HttpClientFactory` / `HttpUtility` |
| 密码与序列化 | `CertificateUtility`、`CryptographyHelper`、`Json`；网络取证书应先于同步转换完成 |
| 支付回调 | `gateway/src/Controllers/FallbackController` → `FallbackExecutor` → `Paying/FallbackHandlerBase` → 支付/退款及宿主业务处理器 |
| 开放平台 | `Platforms/CryptographyUtility` → `Component` / `ComponentManager` → `IComponentStore` / `IComponentAuthorizationStore`；协议、权威授权快照、租户关系分别管理 |
| 插件与部署 | 三项目的 `.csproj`、`.plugin`、`.deploy`，以及主库 `.option`；同时追踪 Automao 的注册节点与部署模板 |

商户材料按[商户与密钥契约](docs/implementation.zh-Hans.md#merchant-credentials)组织：`Authority.SigningCredential`、`VerificationKey`、`Verifier` 使用嵌套 partial 文件；`AuthorityUtility` 异步加载显式文件，`PaymentManager` 消费完整快照。证书下载先验证已有信任，再解密并原子替换。

APIv3 支付传输按 [HTTP 契约](docs/implementation.zh-Hans.md#http-pipeline)组织：`HttpClientFactory` 按请求/响应拆分 partial，内部处理器先签名，再通过 `IHttpMessageHandlerFactory` 的 `Wechat.Payments` 传输，读取原始字节验签后交给 `HttpUtility` 解析。复用完整商户快照，不只缓存签名凭据。命名传输必须关闭重定向和自动重试；主库借用工厂处理器，不接管其生命周期。银行、进件、上传及会员卡使用同一管线；V2/XML 和通知处理采用各自协议。

## 协议与实现约束

- 保持现有插件、服务与配置装配模式；从消费契约追踪服务注册、模块归属、插件节点及生命周期。职责划分遵守 SOLID，紧密关联的子服务优先嵌套并按需用 `partial` 分文件，方法内部辅助逻辑使用局部函数；避免无必要的公共类型和独立注册项。
- 账户获取遵循 [AGENTS.md](AGENTS.md) 的异步契约。凭据刷新另外检查缓存键、有效期、并发刷新、分布式锁及取消传播；基础 token、OAuth token、组件 token、授权者 token、小程序会话和各类票据分别建模。
- 支付请求签名、微信应答/通知验签、敏感字段加密和资源解密分别核对密钥角色。验签使用原始正文；在业务分派前确认签名、时间窗口、身份与资源算法。默认网关分派和资源解密本身不构成验签保证。
- 接收接口、明确产品路由、可靠应答及 Automao 接收箱状态见[通知契约](docs/implementation.zh-Hans.md#payment-notifications)。网络入口使用 `IFallbackHandler.ReceiveAsync`，强类型 `HandleAsync` 只供可信本地调用。
- 追踪回调从请求头和原始字节到业务状态写入的完整链路。微信库处理协议身份，Automao 校验订单归属、金额、币种及幂等状态；成功应答须与可靠接收或业务处理结果一致。
- 离线刷脸按[离线刷脸与签约契约](docs/implementation.zh-Hans.md#offline-face)组织专用扣款/查单与 Contracts partial；不能复用普通支付查询。Automao 签约通知查询当前状态，条件更新协议、账户与解约记录，不凭未知状态启用账户。
- 退款按[订单退款契约](docs/implementation.zh-Hans.md#refunds)组织嵌套 partial：Request 验证身份与金额，Result 区分受理结果、查询和通知字段。Automao 在事务中保留额度，事务结束后发起请求；结果不明保持待确认，显式重试使用原单号。
- 基础支付按[基础支付契约](docs/implementation.zh-Hans.md#basic-payments)组织嵌套 partial：Request 负责身份与产品字段，Query 校验订单归属，Result 生成同一请求身份的客户端参数。APP 身份使用 AccountType.Application；登录服务仍仅处理 Applet/Channel。
- 支付按具体产品和商户角色选择版本：已有官方新版的功能迁移后移除被替代入口；无官方新版的付款码等已实现功能继续采用现有版本。核对 Automao 的付款码、刷脸、客户端调起及状态转换分支，以及查单、撤销、退款和通知的配套要求；不能按类名含有 Compatibility 或协议使用 V2/XML 就整块删除，也不能伪造 V3 端点或给已升级功能保留自动降级。
- APIv2 与 APIv3 按具体接口隔离签名、密钥、序列化及回调处理；只有没有保留功能引用时才移除公共协议代码。公众号/开放平台的 XML、SHA-1 和 AES-CBC 属于独立协议。平台证书与微信支付公钥是 APIv3 的接入机制，不能仅按 APIv2/APIv3 的版本号决定删除证书支持。
- 金额、商户标识、预支付结果和状态遵循[支付模型契约](docs/implementation.zh-Hans.md#payment-models)。金额使用明确单位和币种；十进制转换检查精度与溢出。查单、创建支付、退款及转账按各自语义重试，状态不明时保留原业务单号；通知重复与乱序不能重复产生业务副作用。
- 小程序登录协议结果与应用登录结果分开，`session_key` 留在服务端。公众号用户接口、小程序用户身份和开放平台 UnionID 关联分别核对适用范围，不通过共享类型假定能力相同。
- 本地签名、转换与解密保持同步计算；网络、文件、缓存与提供者查询使用异步入口。明确 HTTP 应答、流、密码器和缓存实例的所有权，不共享可变加密状态。

`CryptographyHelper.Decrypt1` 和 `Decrypt2` 的字符串/字节数组入口共用原生 `AesGcm` 实现。字符串密钥、nonce 和关联数据按 UTF-8 编码，密文按 Base64 解码；密文包含末尾 16 字节标签。密钥必须为 32 字节，nonce 必须为 12 字节，关联数据的 null 与空值均表示无关联数据。缺失必需字节数组抛出 `ArgumentNullException`，长度不符抛出带参数名的 `ArgumentException`，认证失败抛出 `AuthenticationTagMismatchException`。解密成功仅证明 GCM 认证通过，调用方仍须按支付通知或平台证书流程完成来源验证。

## Automao 下游适配

本项目唯一的下游适配对象是 Automao，当前后端工作树为 [Automao/server](../../../../Automao/server)。以该工作树中的项目与配置为依据，不从同级 `automao.server` 目录推断当前契约。工作树不可用时说明无法核验的范围，不转而扫描其它业务仓库。

进入 Automao 前读取其 [AGENTS.md](../../../../Automao/server/AGENTS.md)、目标目录就近约定及[后端实现约定](../../../../Automao/server/docs/implementation.md)。只修改本次契约变更涉及的调用方，业务状态、认证及订单规则仍归 Automao 所属模块。

HTTP 请求位于独立文档仓库 [Automao/documents/http](../../../../Automao/documents/http)，执行前阅读[站点请求说明](../../../../Automao/documents/http/sites/README.zh-Hans.md)，按标签或请求名选定场景；支付统计隔离测试不等于真实支付联调。

| 变更触发点 | Automao 核查位置 |
| --- | --- |
| 账户、商户、证书或认证契约 | [externals/wechat](../../../../Automao/server/externals/wechat)：账户提供者、`AuthorityAccountProvider`、`CertificateExtension`、认证基类与站点实现；附件上传和刷脸协议处理器也在此处 |
| 支付请求、结果、签名或退款契约 | [modules/paying/providers/wechat](../../../../Automao/server/modules/paying/providers/wechat)：`PaymentProvider`、`RefundmentProvider`、`Utility`、`PaymentHandler`、`RefundmentHandler` |
| 会员/卡券与相关依赖 | [modules/membership/providers/wechat](../../../../Automao/server/modules/membership/providers/wechat)：提供者、项目引用及 gateway 插件；先核实方法实现，不能把占位方法视为已支持产品 |
| 回调路由或处理器注册 | 上述三个目录中的 `*-gateway.plugin`，以及 [gateway 宿主](../../../../Automao/server/hosting/web/gateway)；核对 `/Workbench/Externals/Wechat/Fallback/Handlers` 等实际节点 |
| 包版本、配置或资源部署 | [hosting/web/web.deploy](../../../../Automao/server/hosting/web/web.deploy)、[Web 业务部署](../../../../Automao/server/hosting/.deploy/web.deploy)、[daemon 业务部署](../../../../Automao/server/hosting/.deploy/daemon.deploy)及其引用的微信配置模板 |

适配时逐项核对：

1. 搜索框架公开类型及成员的直接引用，再沿 Automao 提供者、处理器和插件注册追踪业务调用；不能只查项目引用。
2. 保持普通商户/服务商、租户/门店账户、用户 OpenId 和订单身份的绑定。调整结果 DTO 时，同时检查支付完成判定、客户端调起参数和退款状态转换。
3. 检查 `ZongsoftFrameworkPathReferenced` / `ZongsoftFrameworkPath` 控制的本机程序集引用，以及 NuGet 回退版本。先构建框架和所需 Automao 依赖，再构建受影响的提供者；成功引用旧程序集不能作为适配验证。
4. 同步实际受影响的 `.csproj`、插件节点、配置模板和部署项。调用约束写入 Automao 对应的实现文档，只描述当前契约与约束。
5. 验收覆盖受影响的登录、付款、退款、回调或会员流程；测试方式遵循各仓库约定。Automao 支付业务集成测试使用实际插件宿主、服务提供者和 gateway 注册，检查业务状态落库，不用手工装配替代插件集成证据；框架单元测试和本地协议测试仍不依赖插件宿主。先核对测试环境、端口、数据隔离与清理范围，环境未就绪时明确缺少的条件。

## 官方 Skills 的使用边界

- 支付文档检索和接入工作可使用环境中可用的 `wechatpay-payment-integration`，来源为[微信支付官方仓库](https://github.com/wechatpay-apiv3/wechatpay-skills)。以实际官方接口为依据，.NET 适配由本仓库维护。
- `wxa-skills-generate`、`wxa-skills-validate` 面向小程序 AI 开发模式的前端工程；只有涉及该前端任务时才使用，不作为本库构建依赖。
- 文档检索、资源同步和离线验证不代表授权调用真实订单接口；执行范围遵循用户要求与项目操作边界。

## 验证与交付

- 文档修改检查 frontmatter、相对链接、CRLF 和差异；记录最终契约及当前限制，不保存过程日志。
- 代码修改先构建受影响项目，公共契约变更再验证 Automao 直接下游。多目标框架按仓库要求分别验证；先生成 `HintPath` 引用的依赖程序集。
- 协议验证覆盖签名原文、非法签名、密钥轮换、解密失败、并发隔离、金额边界、重复/乱序通知、取消及资源释放。测试工程引用方式和断言约束遵循项目 [AGENTS.md](AGENTS.md) 与仓库约定。
- README 仅公布实际具备的功能和约束；当前实现与官方资料集中在 `docs/implementation.md` / `docs/implementation.zh-Hans.md`，本技能维护工作流，任务与验收清单在 `.plan.md` 中维护。代码、SQL 和文档直接描述当前契约，不维护历史版本兼容或升迁过程；SQL 变动同步表结构设计文档。交付时说明未验证的宿主或真实微信环境。

## 付款码协议边界

`PaymentService.Tickets` 是付款码及在线刷脸的 V2 产品子服务；请求/查单/撤销与订单解析按嵌套 partial 类组织。`HttpClientFactory.Xml` 只承载 V2 签名、限长 XML、验签及双向证书传输；不与 V3 JSON 处理器混用。TicketRequest 不持有业务处理器，Automao 根据返回状态等待业务持久化；取消流程位于支付服务基类，站点保留权限与租户约束。公开用法及具名传输要求见[付款码契约](docs/implementation.zh-Hans.md#ticket-payments)。

平台收付通进件按[进件契约](docs/implementation.zh-Hans.md#merchant-onboarding)组织：模型与刷脸合同使用不同 partial；提交绑定业务申请号，查询验证归属并用商户私钥解密验证账号。银行识别保留全部候选。上传只缓冲有界内容，素材随发行商户隔离。Automao 状态与账户写入必须原子完成，未确认及未知状态不能启用商户。

会员模板、协议模型和操作分别放在 `Membership` 的 partial 文件中。商户、公众号和品牌为实例上下文；查询结果必须匹配模板与应用归属，修改前验证归属。预存结果保留逐项状态，未知模式不能回退为系统分配。

自有账号凭据按[凭据契约](docs/implementation.zh-Hans.md#account-credentials)实现。协调与 HTTP 获取分别放在 `CredentialManager` partial；分布式缓存信封使用修订标识合并并发强制刷新，TTL 不重复扩展。不要恢复本地 token 副本或混用票据类型；第三方令牌使用独立域。`Wechat.Credentials` 命名处理器关闭重定向和重试，借用工厂生命周期。

小程序协议、Web 会话及 Automao 账号归属按[身份契约](docs/implementation.zh-Hans.md#applet-identity)实现。`AccountHttpClient` 借用 `Wechat.Accounts` 处理器；登录与手机号 code 不混用。Web 登录复用宿主认证链并执行资源授权；不得恢复返回服务端密钥的端点。Automao 在签发前重新检查应用归属和启用状态，不从请求手机号推断用户身份。

`AppletManager` 和 `ChannelManager` 优先使用显式 Provider，未指定时解析宿主注册的 `IAccountProvider`。保留已完成实例缓存，业务签发仍需校验当前归属与启用状态。

公众号协议见[服务契约](docs/implementation.zh-Hans.md#channel-services)。OAuth 流程和模型分别放在 ChannelAuthentication partial；OAuth 令牌不进入基础缓存，刷新结果必须匹配应用和身份。所有账号协议使用 `AccountHttpClient`；没有 CredentialManager.Http 旁路。JS-SDK 保留原始 URL，仅移除片段，Web 额外执行站点策略。用户/消息控制器明确绑定路由账号并授权，ITransmitter 只接受 Message 通道。

公众平台 CBC 与 SHA-1 分别位于 `Platforms.CryptographyUtility` partial。接收层必须遵循[密码协议](docs/implementation.zh-Hans.md#platform-cryptography)，先验签再解密，提供可信配置的预期 AppId；时效、重放及持久确认属于接收服务。不能恢复仅输出 AppId 而不验证接收方的入口。

## 版本与发行

三程序集使用 7.10.0，最低依赖、Debug/Release 引用分支、部署和数据库要求见[部署契约](docs/implementation.zh-Hans.md#deployment)。正式 Release 发行为显式授权操作；主包须先在所选 NuGet 源可还原，再构建依赖它的 Web/Gateway 包。`build.cake` 的 `pack` 任务接受 `--edition=Release` 与 `--project=<csproj>`；不指定项目会处理整个解决方案。普通构建关闭 `GeneratePackageOnBuild`，不得为了通过 Release 验证而用旧版主包替代。

第三方组件按[组件契约](docs/implementation.zh-Hans.md#component-platform)分为配置、消息解析、协议交换及模型 partial。IComponentStore 是宿主可靠接收扩展点；Gateway 只验证与交接，不能在控制器中落入租户业务。组件与自有账号复用内部凭据协调流程，但键域与传输名称分开。

普通服务商进件使用 `PaymentManager.PartnerService`，按[普通服务商契约](docs/implementation.zh-Hans.md#partner-onboarding)独立维护资料、查询与状态；不能复用收付通申请模型或从第三方平台权限推定支付权限。

Automao SaaS 存储由 `ComponentStore` 和 `PartnerRegistry` 分别负责，Gateway 工作器消费可靠授权事件，商户端管理与平台支付权限确认使用各自站点插件。修改时同步 Components/Partners 映射、独立 SQL、配置模板和 HttpYac 请求；业务边界与恢复契约见 [Automao 微信模块](../../../../Automao/server/externals/wechat/README.zh-Hans.md)。不能让框架依赖 Automao 数据库，也不能在宿主启动代码实现租户授权。
