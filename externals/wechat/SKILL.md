---
name: zongsoft-wechat
description: 修改、审查或排查 Zongsoft 微信扩展库的账户凭据、公众号、小程序、开放平台、支付协议、Web API 与回调网关，并追踪 Automao 调用方适配；不用于小程序前端页面开发。
---

# Zongsoft 微信适配工作流

## 定位与资料

1. 阅读 [AGENTS.md](AGENTS.md)、[README.zh-Hans.md](README.zh-Hans.md)、[解决方案](Zongsoft.Externals.Wechat.slnx)及受影响项目文件，先确定改动属于主库、Web API 还是回调网关。通用外部适配规则见[上级技能](../SKILL.md)。
2. 修改接口字段、签名、加解密、令牌或业务状态时，从[官方接口参考](docs/platform-reference.md)定位对应官方文档，核实产品、账号/商户角色和当前权限。资料日期不等于接口永久有效；具体实现以当前官方契约为准。
3. 执行升级任务时阅读[微信生态接入设计](docs/modernization-design.zh-Hans.md)。先从源码与 Automao 调用列出已实现功能，再逐项记录现用接口、官方替代接口及依据、目标版本、配套流程和验收条件；有新版则只支持新版，没有新版则继续采用现有版本。该文档定义目标契约，README 和源码定义当前支持范围；未实现的候选能力不自动纳入升级范围。
4. 公开类型、配置、插件入口或回调协议发生变化时，按下文 Automao 路径追踪到消费端与部署端，明确受影响文件、目标行为和验收条件后实施。

## 代码入口

| 工作内容 | 入口与追踪顺序 |
| --- | --- |
| 账户与身份 | `IAccountProvider` → `AppletManager` / `ChannelManager` → `Applet` / `Channel` → `Security/WechatAuthenticatorBase` 与 API 控制器 |
| 凭据与消息 | `CredentialManager`、`ChannelAuthentication`、`ChannelMessager`、`UserProvider`、`Transmitter`；分别核对基础凭据、网页授权与消息产品 |
| 商户与支付 | `Authority*`、`Options/Authority*` → `Paying/PaymentManager*`、`MerchantService`、`Uploader` → `Paying/HttpClientFactory` / `HttpUtility` |
| 密码与序列化 | `CertificateUtility`、`CryptographyHelper`、`Json`；网络取证书应先于同步转换完成 |
| 支付回调 | `gateway/Controllers/FallbackController` → `FallbackExecutor` → `Paying/FallbackHandlerBase` → 支付/退款及宿主业务处理器 |
| 开放平台 | `Platforms/CryptographyUtility` 是消息加解密入口；组件及授权者服务是否完整，应从实际实现核实 |
| 插件与部署 | 三项目的 `.csproj`、`.plugin`、`.deploy`，以及主库 `.option`；同时追踪 Automao 的注册节点与部署模板 |

## 协议与实现约束

- 账户获取遵循 [AGENTS.md](AGENTS.md) 的异步契约。凭据刷新另外检查缓存键、有效期、并发刷新、分布式锁及取消传播；基础 token、OAuth token、组件 token、授权者 token、小程序会话和各类票据分别建模。
- 支付请求签名、微信应答/通知验签、敏感字段加密和资源解密分别核对密钥角色。验签使用原始正文；在业务分派前确认签名、时间窗口、身份与资源算法。默认网关分派和资源解密本身不构成验签保证。
- 追踪回调从请求头和原始字节到业务状态写入的完整链路。微信库处理协议身份，Automao 校验订单归属、金额、币种及幂等状态；成功应答须与可靠接收或业务处理结果一致。
- 支付按具体产品和商户角色选择版本：已有官方新版的功能迁移后移除被替代入口；无官方新版的付款码等已实现功能继续采用现有版本。核对 Automao 的付款码、刷脸、客户端调起及状态转换分支，以及查单、撤销、退款和通知的配套要求；不能按类名含有 Compatibility 或协议使用 V2/XML 就整块删除，也不能伪造 V3 端点或给已升级功能保留自动降级。
- APIv2 与 APIv3 按具体接口隔离签名、密钥、序列化及回调处理；只有没有保留功能引用时才移除公共协议代码。公众号/开放平台的 XML、SHA-1 和 AES-CBC 属于独立协议。平台证书与微信支付公钥是 APIv3 的接入机制，不能仅按 APIv2/APIv3 的版本号决定删除证书支持。
- 金额使用明确单位和币种；十进制转换检查精度与溢出。查单、创建支付、退款及转账按各自语义重试，状态不明时保留原业务单号；通知重复与乱序不能重复产生业务副作用。
- 小程序登录协议结果与应用登录结果分开，`session_key` 留在服务端。公众号用户接口、小程序用户身份和开放平台 UnionID 关联分别核对适用范围，不通过共享类型假定能力相同。
- 本地签名、转换与解密保持同步计算；网络、文件、缓存与提供者查询使用异步入口。明确 HTTP 应答、流、密码器和缓存实例的所有权，不共享可变加密状态。

## Automao 下游适配

本项目唯一的下游适配对象是 Automao，当前后端工作树为 [Automao/server](../../../../Automao/server)。以该工作树中的项目与配置为依据，不从同级 `automao.server` 目录推断当前契约。工作树不可用时说明无法核验的范围，不转而扫描其它业务仓库。

进入 Automao 前读取其 [AGENTS.md](../../../../Automao/server/AGENTS.md)、目标目录就近约定及[异步调用约定](../../../../Automao/server/docs/zongsoft-async-adaptation.md)。只修改本次契约变更涉及的调用方，业务状态、认证及订单规则仍归 Automao 所属模块。

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
4. 同步实际受影响的 `.csproj`、插件节点、配置模板和部署项。需要维护调用约束时更新 Automao 已有的对应实现文档；不把其异步约定文档当作通用任务进度清单。
5. 验收覆盖受影响的登录、付款、退款、回调或会员流程；测试方式遵循各仓库约定。宿主环境未就绪时记录缺少的依赖，不执行真实交易或部署来替代离线核查。

## 官方 Skills 的使用边界

- 支付文档检索和接入工作可使用环境中可用的 `wechatpay-payment-integration`，来源为[微信支付官方仓库](https://github.com/wechatpay-apiv3/wechatpay-skills)。以实际官方接口为依据，.NET 适配由本仓库维护。
- `wxa-skills-generate`、`wxa-skills-validate` 面向小程序 AI 开发模式的前端工程；只有涉及该前端任务时才使用，不作为本库构建依赖。
- 文档检索、资源同步和离线验证不代表授权调用真实订单接口；执行范围遵循用户要求与项目操作边界。

## 验证与交付

- 文档修改检查 frontmatter、相对链接、CRLF 和差异；记录最终契约及当前限制，不保存过程日志。
- 代码修改先构建受影响项目，公共契约变更再验证 Automao 直接下游。多目标框架按仓库要求分别验证；先生成 `HintPath` 引用的依赖程序集。
- 协议验证覆盖签名原文、非法签名、密钥轮换、解密失败、并发隔离、金额边界、重复/乱序通知、取消及资源释放。测试工程引用方式和断言约束遵循项目 [AGENTS.md](AGENTS.md) 与仓库约定。
- README 仅公布实际具备的功能和约束；实现边界写入本技能，目标设计和官方资料分别维护在已链接文档中。交付时说明未验证的宿主或真实微信环境。
