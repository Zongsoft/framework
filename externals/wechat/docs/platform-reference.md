# 微信平台接口与接入约束

本文以微信官方文档及微信支付官方资源库为依据，资料日期为 2026-10-22。本文记录平台契约，不表示本库已实现所列能力。各产品按账号类型、权限集、商户产品权限和应用绑定关系开放，没有覆盖微信全部业务的统一版本号。

## 文档入口与产品边界

| 产品 | 官方入口 | 边界 |
| --- | --- | --- |
| 公众号 | [公众号开发文档](https://developers.weixin.qq.com/doc/subscription/guide/) | 与服务号分别维护文档；不能推定服务号的接口权限适用于所有公众号。 |
| 服务号 | [开发指南](https://developers.weixin.qq.com/doc/service/guide/dev/)、[服务端 API](https://developers.weixin.qq.com/doc/service/api/) | 涵盖消息、菜单、素材、草稿、发布、用户、网页授权及 JS-SDK；具体权限以接口适用范围为准。 |
| 小程序 | [服务端 API](https://developers.weixin.qq.com/miniprogram/dev/server/API/) | 登录、手机号、订阅消息、交易管理等有各自的接口契约。 |
| 开放平台 | [平台介绍](https://developers.weixin.qq.com/doc/oplatform/open/intro.html)、[第三方平台 API](https://developers.weixin.qq.com/doc/oplatform/openApi/) | 开放平台绑定与第三方代开发授权是不同关系。 |
| 微信支付 | [普通商户 APIv3](https://pay.weixin.qq.com/doc/v3/merchant/4012081606)、[合作伙伴 APIv3](https://pay.weixin.qq.com/doc/v3/partner/4012081673) | 普通商户、服务商、子商户身份以及不同支付产品需要分别建模。 |

企业微信、微信小店、视频号、小游戏等属于独立业务域。开放平台将多种应用绑定到同一开放平台账号时，可以通过满足条件后返回的 UnionID 关联用户；这不代表它们共享全部接口或访问凭证。[开放平台介绍](https://developers.weixin.qq.com/doc/oplatform/open/intro.html)

旧 `offiaccount`、小程序 `OpenApiDoc` 及部分 `Third-party_Platforms/api` 文档地址会跳转到目录页或返回失效页面。引用具体接口时应使用表中入口继续定位到当前页面，不能仅凭 HTTP 200 判断旧链接仍指向原接口。

## 微信支付 APIv3

### 认证、验签与密钥

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

### 支付、退款与回调

普通商户的 JSAPI 与小程序下单使用 `/v3/pay/transactions/jsapi`，客户端调起参数属于独立契约，不能直接把下单结果作为付款结果。[JSAPI 下单](https://pay.weixin.qq.com/doc/v3/merchant/4012791856)、[小程序下单](https://pay.weixin.qq.com/doc/v3/merchant/4012791897)

普通商户模式使用 `mchid`、`appid`；服务商模式区分 `sp_mchid`、`sub_mchid`、`sp_appid`、`sub_appid` 及对应 OpenID。服务商以自己的 API 私钥签名，不能通过任意替换商户号复用普通商户身份。[接入模式](https://pay.weixin.qq.com/doc/v3/partner/4012081931)

退款接口为 `POST /v3/refund/domestic/refunds`。金额按整数分表达，使用独立的 `out_refund_no`；同一退款单号重复请求只处理同一笔退款。受理成功不等于退款完成，最终状态需要退款查询及相应通知确认。部分退款需要不同退款单号，总退款金额受原订单金额约束。[申请退款](https://pay.weixin.qq.com/doc/v3/merchant/4012552567)

支付通知可能重复投递；商户必须幂等处理并在规定时间内应答。官方通用说明要求 5 秒内应答，重复且已处理的通知直接返回成功；验签失败返回失败 HTTP 状态。`notify_url` 使用可公网访问的 HTTPS 地址且不能携带查询参数。业务结果应结合查单兜底，不能以客户端成功回调代替服务端结果。[回调通知注意事项](https://pay.weixin.qq.com/doc/v3/merchant/4012075420)、[支付签名与验签总述](https://pay.weixin.qq.com/doc/v3/merchant/4012365342)

### 商家转账

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

### 付款码与 SDK 边界

当前普通商户和合作伙伴文档中的“付款码支付（V2）”均明确指向 V2 接口。已实现的对应功能在官方没有更新版本时继续采用 V2；不能因为其它支付产品已采用 APIv3 就删除该能力或编造 V3 付款码端点。升级到新版的判断必须对应同一产品、商户角色及配套流程，文档目录中的 `/doc/v3/` 本身不是接口版本依据。[普通商户付款码支付](https://pay.weixin.qq.com/doc/v3/merchant/4012647301)、[合作伙伴付款码支付](https://pay.weixin.qq.com/doc/v3/partner/4012851192)

微信支付官方 GitHub 组织列出 Java、Go、PHP 库及相关工具；当前公开目录未列出官方 .NET SDK。不能把第三方 .NET 库描述为微信官方 SDK。新版商家转账官方文档采用工具类与示例代码接入方式，而不是保证旧 SDK 覆盖新产品。[官方仓库](https://github.com/wechatpay-apiv3)、[商家转账接入说明](https://pay.weixin.qq.com/doc/v3/merchant/4015273741)

## 公众号与服务号

### 凭证与网页授权

`getStableAccessToken` 是接口英文名，实际地址为 `POST /cgi-bin/stable_token`。它与 `/cgi-bin/token` 的凭证隔离，官方推荐使用稳定版。普通模式在有效期内复用 token；必须依据响应 `expires_in` 缓存，不能每次调用后固定延长两小时。`force_refresh=true` 会使先前 token 失效并受次数限制。该接口不支持第三方平台调用，因此不能替代 `component_access_token` 或 `authorizer_access_token`。[稳定版接口调用凭据](https://developers.weixin.qq.com/doc/service/api/base/api_getstableaccesstoken.html)

网页 OAuth 使用 `/sns/oauth2/access_token`、`/sns/oauth2/refresh_token`、`/sns/auth`、`/sns/userinfo`。网页授权 token 与服务端基础接口 token 不同。AppSecret 和 OAuth token 只保存在服务器；不得返回客户端。授权 code 一次性使用，五分钟未使用失效；OAuth refresh token 的有效期为 30 天。[微信网页授权](https://developers.weixin.qq.com/doc/service/guide/h5/auth.html)

网页授权主要适用于已认证服务号，文档列出已认证政府、事业单位和媒体公众号的特例；不能对任意公众号、小程序或开放平台 AppID 开启同一网页授权流程。`snsapi_base` 主要取得 OpenID，`snsapi_userinfo` 受用户授权及能力规则约束。用户信息撤回、注销和资料变更属于需要处理的事件。[网页授权适用范围与事件](https://developers.weixin.qq.com/doc/service/guide/h5/auth.html)

JS-SDK 使用服务端缓存的 `jsapi_ticket` 生成签名；参与字段为随机串、票据、时间戳和去掉 `#` 及后续片段的页面 URL，按官方算法进行 SHA-1。JS-SDK 权限签名与支付 RSA 签名属于不同协议，不能因支付采用 APIv3 就统一更换算法。[JS-SDK](https://developers.weixin.qq.com/doc/service/guide/h5/jssdk.html)

### 消息与回调

服务号仍分别开放模板消息、订阅通知、一次性订阅消息、客服消息、群发消息等能力。不能把所有消息统一替换成小程序 `subscribe/send`；发送端点、模板定义、用户授权及权限限制需要按产品区分。[服务端 API 目录](https://developers.weixin.qq.com/doc/service/api/)

服务号 URL 接入验证使用 GET 查询参数验签后返回 `echostr`；消息安全模式使用 `msg_signature`，参与计算的是 Token、时间戳、随机串及 `Encrypt`。验签后按 AES-256-CBC、微信规定的 32 字节填充规则解密，并校验正文末尾的 AppID；被动回复有相应加密应答要求。支付回调的 RSA/AES-GCM 不能复用到此协议。[消息推送接入](https://developers.weixin.qq.com/doc/service/guide/dev/push/)、[消息加解密](https://developers.weixin.qq.com/doc/service/guide/dev/push/encryption.html)

服务号推送介绍页写明数据格式仅 XML，而加解密页同时展示 XML 与 JSON 示例。两页存在粒度差异，具体账号配置与具体事件文档是最终依据，不能仅从通用示例推断每种服务号事件均支持 JSON。[推送配置](https://developers.weixin.qq.com/doc/service/guide/dev/push/)、[加解密示例](https://developers.weixin.qq.com/doc/service/guide/dev/push/encryption.html)

## 小程序

| 能力 | 当前契约 |
| --- | --- |
| 登录 | 客户端 `wx.login` 获取 code，由服务器调用 `GET /sns/jscode2session`。响应包含 OpenID、session_key，UnionID 按开放平台绑定条件返回。 |
| 手机号 | `POST /wxa/business/getuserphonenumber`，请求传手机号授权 code；code 只能使用一次且有效期五分钟。支持可选 `openid`，提供时校验其与 code 的绑定。 |
| 订阅消息 | `POST /cgi-bin/message/subscribe/send`，使用模板 ID、用户 OpenID、模板数据和小程序页面；第三方代调用须具备对应权限集。 |

接口依据：[登录凭证校验](https://developers.weixin.qq.com/miniprogram/dev/server/API/user-login/api_code2session.html)、[获取手机号](https://developers.weixin.qq.com/miniprogram/dev/server/API/user-info/phone-number/api_getphonenumber.html)、[发送订阅消息](https://developers.weixin.qq.com/miniprogram/dev/server/API/mp-message-management/subscribe-message/api_sendmessage.html)。

`session_key` 是用户数据签名与加解密材料，官方明确不应向小程序下发，也不应对外提供。应用服务器根据微信身份创建自己的登录态。手机号授权 code 与登录 code 对应不同接口，不应互换。[小程序登录](https://developers.weixin.qq.com/miniprogram/dev/framework/open-ability/login.html)、[手机号接口](https://developers.weixin.qq.com/miniprogram/dev/server/API/user-info/phone-number/api_getphonenumber.html)

订阅消息需要用户先取得相应下发授权；一次性订阅、长期订阅和设备订阅有各自入口。订阅弹窗结果、取消订阅和下发结果属于独立事件；模板字段有类型和长度约束，发送失败不能被统一解释为 token 过期。[订阅消息指南](https://developers.weixin.qq.com/miniprogram/dev/framework/open-ability/subscribe-message.html)、[发送接口限制与错误码](https://developers.weixin.qq.com/miniprogram/dev/server/API/mp-message-management/subscribe-message/api_sendmessage.html)

## 第三方开放平台

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

## 证据适用范围

上述结论来自公开的官方文档正文及微信支付官方同步资源；不包含真实商户、公众号或小程序账号的权限验证，也不证明具体账号已开通产品。金额上限、调用额度、收费、类目准入及新字段应在落地接口时按当前页面和实际账号再次确认。

网页授权文档顶部明确性别与地区信息已停止返回，但同页旧字段表仍保留相应项目；数据模型不得依赖这些字段必有值。回调 XML/JSON 差异见前述消息章节。官方文档的这类内部不一致应按限制更严格的声明和实际账号接口能力处理，不通过第三方文章补成无依据的保证。[网页授权说明](https://developers.weixin.qq.com/doc/service/guide/h5/auth.html)
