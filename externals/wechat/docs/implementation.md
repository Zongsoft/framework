# Zongsoft.Externals.Wechat implementation

[简体中文](implementation.zh-Hans.md) · [Project overview](../README.md) · [Refactoring tasks and plan](../.plan.md)

This document defines the current implementation, protocol contracts, plugin composition and verification boundaries. Task status and outstanding live validation belong exclusively to `.plan.md`. An official API reference does not imply that this library implements every product or that a particular account has permission to use it.

## Contents

- [Architecture and project boundaries](#architecture)
- [Capabilities and interface map](#capability-matrix)
- [Merchant credentials and keys](#merchant-credentials)
- [APIv3 HTTP pipeline](#http-pipeline)
- [Amounts, identifiers and results](#payment-models)
- [Basic APIv3 payments](#basic-payments)
- [Payment codes and online face payment](#ticket-payments)
- [Refunds](#refunds)
- [Offline face payment and agreements](#offline-face)
- [Payment notifications](#payment-notifications)
- [Platform ecommerce onboarding, banks and media](#merchant-onboarding)
- [Ordinary service-provider onboarding](#partner-onboarding)
- [Membership cards](#membership)
- [Owned-account credentials and tickets](#account-credentials)
- [Mini program identity and Web boundaries](#applet-identity)
- [Official-account OAuth, JS-SDK and messaging](#channel-services)
- [Public-platform message cryptography](#platform-cryptography)
- [Third-party platform protocols](#component-platform)
- [Runtime composition and database schema](#deployment)
- [Verification](#testing)
- [SaaS accounts and test environments](#saas-development-and-test-accounts)
- [Official platform references and eligibility](#platform-reference)

<a id="architecture"></a>

## Architecture and project boundaries

The adapter follows [project instructions](../AGENTS.md) and the [Zongsoft development guidelines](../../../../guidelines/zongsoft.csharp.guidelines.md). Existing plugins, module containers, providers and handler extension points compose its services. Hosts provide startup and hosting; this library owns WeChat protocols, while Automao owns tenants, business orders, amount checks, idempotency and persistence. Follow SOLID, cohesive responsibilities, narrow visibility and low coupling. Do not add WeChat-specific Core interfaces or widen production visibility for tests.

| Production project | Test project | Responsibility |
| --- | --- | --- |
| [src](../src/Zongsoft.Externals.Wechat.csproj) | [test](../test/Zongsoft.Externals.Wechat.Tests.csproj) | Accounts, merchants, credentials, cryptography, transports, product services and notification parsing |
| [api/src](../api/src/Zongsoft.Externals.Wechat.Web.csproj) | [api/test](../api/test/Zongsoft.Externals.Wechat.Web.Tests.csproj) | Host authorization, application sessions, management endpoints and response mapping |
| [gateway/src](../gateway/src/Zongsoft.Externals.Wechat.Gateway.csproj) | [gateway/test](../gateway/test/Zongsoft.Externals.Wechat.Gateway.Tests.csproj) | Incoming WeChat protocols, durable acceptance handoff and protocol responses |

Project files, `.plugin` and `.deploy` manifests reside with source; the main library also provides `.option`. Bilingual READMEs remain at each project root. The [solution](../Zongsoft.Externals.Wechat.slnx) contains all six projects, and `build.cake` discovers tests through `**/test/*.csproj`. The host supplies Core, Web, Plugins and Plugins.Web; adapter deployment manifests do not copy them again.

Use nested classes for closely related services, `partial` files for stable implementation responsibilities, and local functions for method-specific helpers; use `static` when no capture is needed. Public types, plugin entries and configuration changes require synchronized projects, manifests, resources, READMEs and direct Automao consumers. [SKILL.md](../SKILL.md) describes navigation and maintenance workflows.

`IAccountProvider.GetAccountAsync` retrieves accounts asynchronously. `Get…Async` requires a result, while `Find…Async` permits null; cancellation is the last parameter. Configured and dynamic owned accounts reuse completed instances, without caching failed or unfinished tasks. Delegated accounts are not instance-cached and check current authorization on every use. Local parsing, signing, cryptography and conversion of prepared objects remain synchronous and do not hide network, database or file waits.

APIv3 resource decryption creates and disposes a native `AesGcm` per operation. It requires a 32-byte key, 12-byte nonce and trailing 16-byte authentication tag. Strings use UTF-8/Base64; null and empty associated data are equivalent, and empty plaintext is valid. Missing required byte arrays throw `ArgumentNullException`, invalid lengths throw `ArgumentException` with parameter names, and authentication failure throws `AuthenticationTagMismatchException`. Successful GCM authentication does not replace source verification. V2 MD5/HMAC, public-platform SHA-1/CBC and upload SHA-256 retain their protocol-specific roles. Mutable cryptographic objects are not shared; callers own borrowed hash instances. The main library and deployment manifests do not depend on BouncyCastle.

<a id="capability-matrix"></a>

## Capabilities and interface map

This map connects actual source entry points with their protocols and constraints. Code, DTOs, enum values and documentation do not establish account eligibility or successful runtime validation. Paths are relative to this project; use method names rather than fixed line numbers. Recheck fields, errors and related operations when changing an interface. [Verification](#testing) separates local evidence from live WeChat validation; task status is in the [plan](../.plan.md).

<a id="capability-matrix-1"></a>

### Features and versions

| Source entry | Protocol or behavior | Contract |
| --- | --- | --- |
| `IAccountProvider`, `AppletManager`, `ChannelManager` | Async lookup; required Get and nullable Find; completed-instance reuse | Preserve cancellation, configuration initialization and isolation; never share unfinished lookup tasks |
| `CredentialManager.GetCredentialAsync` | `POST /cgi-bin/stable_token` | Owned accounts only; shared cache, revision-aware refresh, remaining TTL and cancellation |
| `GetTicketAsync`, `Channel.PostmarkAsync` | `/cgi-bin/ticket/getticket`, separate jsapi/wx_card tickets, SHA-1 | Account/type isolation, fragment-free original URL and host site policy |
| `Applet.LoginAsync` and authenticators | `/sns/jscode2session` | OpenID, optional UnionID and server-only session key; no official-account profile lookup |
| `Applet.GetPhoneNumberAsync` | `/wxa/business/getuserphonenumber` | Independent phone code, structured `PhoneInfo`, optional OpenID binding |
| `ChannelAuthentication` | OAuth code exchange, optional profile, refresh and validation | Scope-aware requests, optional profile data, host-owned state, no token serialization |
| `UserProvider`, `ChannelMessager`, `Transmitter` | Official-account followers, profiles and templates | Account permissions apply; these are not mini program user management or subscription messaging |
| Direct/Broker `PaymentService` | Role-specific V3 JSAPI, APP, Native, query and close | Bind merchant and application; no automatic V2 fallback |
| `PaymentResult.Parameters` | JSAPI/APP launch parameters; Native `CodeUrl` | Launch success is not settlement; confirm with server queries or verified notifications |
| `PaymentService.Tickets` | V2 submission, query and mutual-TLS reversal | Verify XML signatures and order context; refunds use the existing V3 service |
| `AuthenticateOnlineAsync` | `payapp.weixin.qq.com/face/get_wxpayface_authinfo` | Device initialization followed by the payment-code query/reversal flow |
| `AuthenticateOfflineAsync`, `PayOfflineTicketAsync` | `/v3/offlineface/authinfo`, `/v3/offlineface/transactions` | Dedicated product, `GetOfflineAsync`, optional amounts and explicit identity checks |
| `MerchantService.Contracts` | `offlinefacemch/tokens`, signatory query, `offlineface/contracts/presign`, contract query/revoke | Separate authorization and agreement lifecycles; unknown status is not signed |
| `RefundmentService`, `RefundmentHandlerBase` | `/v3/refund/domestic/refunds` application/query and notifications | Roles, amounts, original identifiers and separate response/notification fields |
| `Authority*`, `CertificateUtility` | Explicit signing and API keys; public-key/certificate modes; atomic trusted refresh | Host supplies initial trust and periodic refresh |
| `HttpClientFactory`, `HttpUtility`, `Json` | Request signing, sensitive-field encryption, verification before JSON parsing | Includes membership; notifications use a separate receiver; unsigned gateway errors remain failures |
| Gateway, `FallbackHandlerBase` | Explicit routes, raw-byte verification, authenticated decryption and reliable handoff | Host owns durable idempotency and business recovery |
| `MerchantService.RegisterAsync/GetAsync` | `/v3/ecommerce/applyments/` and original-request lookup | Platform ecommerce onboarding, separate from ordinary partner onboarding |
| `BankUtility` | `capital/capitallhh/banks/*` | Bank/branch pagination, all card-identification candidates, encrypted and escaped queries |
| `Paying.Uploader` | `merchant/media/upload`, `merchant/media/video_upload` | Bounded reads, meta signing, SHA-256 and resource ownership |
| `Membership.TemplateService` | `marketing/membercard-open/cards` | Merchant/channel/brand binding, per-code outcomes and ownership checks before mutation |
| `Platforms.CryptographyUtility` | SHA-1, AES-CBC, 32-byte padding and expected AppID | Verify before decrypting; strict UTF-8 and byte-oriented payload support |
| `api/src` controllers | Login, preauthorization, phones, users, templates, banks, verification metadata and media | Host authorization and tenant scope; no server-credential HTTP endpoints |

`Scenario.Web` is JSAPI. `Scenario.Mobile` does not implement H5. A `profit_sharing` field is not a profit-sharing service. Transfers, bill downloads and mini program subscription messages remain independent extensions. Component/authorizer credentials, delegated calls and ordinary partner management follow their respective sections below; live account permissions remain separate prerequisites.

<a id="capability-matrix-2"></a>

### Payment roles and supporting operations

The host is `https://api.mch.weixin.qq.com`. In this table only, D means `/v3/pay/transactions`, P means `/v3/pay/partner/transactions`, and R means `/v3/refund/domestic/refunds`.

| Product/role | Submission | Query, close/reversal and refund | Automao boundary |
| --- | --- | --- | --- |
| Direct JSAPI/mini program | `POST D/jsapi` | `GET D/out-trade-no/{out_trade_no}` or `D/id/{transaction_id}`; `POST D/out-trade-no/{out_trade_no}/close`; R | `PaymentProvider` and `RefundmentProvider`; AppID/OpenID pairing and server-confirmed final state |
| Partner JSAPI/mini program | `POST P/jsapi` | Corresponding P query/close paths; R with `sub_mchid` | Distinguish service-provider and submerchant app/payer identities |
| Direct/partner APP | `POST D/app` or `P/app` | Role-specific query/close; R | Separate APP launch parameters and signature |
| Direct/partner Native | `POST D/native` or `P/native` | Role-specific query/close; R | `code_url` is neither a prepayment identifier nor a completed order |
| Direct/partner payment code | `POST /pay/micropay` | V2 `/pay/orderquery`, `/secapi/pay/reverse`; V3 refunds under the rules below | `USERPAYING` and timeout remain uncertain; reversal is not close |
| Online face payment | `get_wxpayface_authinfo` | Device face_code then the payment-code flow; role-specific R | `PaymentRecognizer`/`PaymentProvider`; device SDK is outside framework tests |
| Partner offline face payment | Dedicated offlineface auth/payment paths | Dedicated query retains `business_product_id`; agreement lifecycle is separate | Persist product, merchant, organization, user and device identity; live K12 eligibility remains unverified |
| Direct/partner refund | `POST R`, `GET R/{out_refund_no}` | Preserve `sub_mchid` and original refund identifier; separate application/query/notification status | Atomic cumulative limits, verified outcomes and terminal-state protection |

Roles, AppIDs and versions come from explicit order/account context, not certificate presence or HTTP 200. Other products retain their own permissions and identity requirements.

<a id="capability-matrix-3"></a>

#### V2 payment codes and refund selection

1. V2 payment codes use `micropay` → `orderquery` → optional `reverse`. Reversal needs the merchant mutual-TLS certificate; query does not. Both require protocol signing and response validation. V3 close cannot replace reversal.
2. Official V2 refund/query endpoints `/secapi/pay/refund` and `/pay/refundquery` also exist; only the former needs mutual TLS. This library implements V3 refunds, not those alternative endpoints.
3. Official direct/partner FAQs allow cross-version payment and refund but discourage it. The project retains V3 refunds with the required V2 query/reversal flow for domestic ordinary merchants/providers. This is not a promise for platform ecommerce, special products or arbitrary historical orders.
4. Explicit V3 `notify_url` matches the JSON/GCM gateway. Merchant-console refunds or some omitted-URL configurations can produce V2/XML notifications; use query compensation or a separate receiver, never the V3 parser.
5. Synthetic tests check identity and state continuity. Actual cross-version transactions, merchant material and permissions require live validation.

<a id="capability-matrix-4"></a>

### SaaS capabilities

Third-party authorization includes component configuration, tickets, events, preauthorization and code exchange. Component tokens use shared caching; authorizer tokens use authoritative persistence and revision-conditional refresh. Delegated calls require actual granted permissions and stop after revocation. `PartnerService` handles ordinary submerchant applications independently of ecommerce onboarding. Automao `ComponentStore` and `PartnerRegistry` persist these two relationships through plugins, with tenant management and platform confirmation authorized separately. Code upload, review/release, registration on behalf of merchants and provider components are outside this scope.

<a id="capability-matrix-5"></a>

### Automao adaptation map

Paths are relative to [Automao/server](../../../../../Automao/server). Trace member consumers as well as project references.

| Area | Files/locations | Required checks |
| --- | --- | --- |
| Accounts and merchant material | `externals/wechat/abstraction/src/AccountProvider.cs`, `AuthorityAccountProvider.cs`, `AuthorityAccountSetting.cs`, `CertificateExtension.cs`, `Utility.cs` | Account/branch origin, provider/submerchant pairing, snapshots and rotation |
| Login | `WechatAuthenticatorBase.cs`, `AccountVerifier.cs`, `sites/{administration,business,customer}/src/WechatAuthenticator.cs` | Optional UnionID, no server secrets, account/tenant scope |
| Payments/refunds | `modules/paying/providers/wechat/src/PaymentProvider.cs`, `RefundmentProvider.cs`, `Utility.cs`, `PaymentHandler.cs`, `RefundmentHandler.cs` | Launch results, amounts, original identifiers, atomic state and duplicate/out-of-order handling |
| Agreements/onboarding | `TenantAgreementProvider.cs`, `UserAgreementProvider.cs`, `UserRevocationProvider.cs`, `OfflineFaceAgreementHandler.cs`, `AttachmentSynchronizer.cs` | Product role, sensitive fields, dedicated queries and attachment ownership |
| Membership | `modules/membership/providers/wechat/src/CardProvider.cs`, `CardTemplateProvider.cs`, `Utility.cs` | Explicit `NotImplementedException` boundaries remain |
| Callback composition | The corresponding `*-gateway.plugin` files and `hosting/web/gateway` | Handler registration, protocol routes and rejection before business dispatch |
| References/deployment | `.csproj`, `hosting/web/web.deploy`, `hosting/.deploy/{web,daemon}.deploy` | Current local assemblies, synchronized NuGet fallback and manifests |

Calling `OnPaidAsync` or `OnRefundedAsync` alone does not demonstrate transactional validation or idempotency; inspect the business service and its persisted results.

<a id="capability-matrix-6"></a>

### Official sources

The official payment skill stores documents under `wechatpay-payment-integration/assets/微信支付官网文档`, including `APIv3/{普通商户,合作伙伴}/支付产品/{产品}/API列表/`, V2 payment-code APIs, and product-specific FAQs. Document IDs locate matching Markdown resources. Face payment and membership require their corresponding official web references when absent locally.

Use the sources in each product section and the [platform reference](#platform-reference). Key version decisions are supported by [direct payment codes](https://pay.weixin.qq.com/doc/v3/merchant/4012647301), [partner payment codes](https://pay.weixin.qq.com/doc/v3/partner/4012851192), [direct cross-version FAQ](https://pay.weixin.qq.com/doc/v3/merchant/4012791869), [partner cross-version FAQ](https://pay.weixin.qq.com/doc/v3/partner/4013334850), [direct refund FAQ](https://pay.weixin.qq.com/doc/v3/merchant/4013071200) and [partner refund FAQ](https://pay.weixin.qq.com/doc/v3/partner/4013080629).

Official pages have inconsistencies: the K12 query URL uses `sub_mchid` while a table spells it `sup_mchid`; OAuth restrictions differ from older field tables. Follow the explicit interface contract and verify behavior, without claiming live support from contradictory reference text.

<a id="merchant-credentials"></a>

## Merchant credentials and keys

Merchant material belongs to the main adapter and is composed through existing `.option`, `AuthorityUtility` and providers. No Core interface or plugin registration node is added. Application AppSecret, merchant API keys and WeChat verification material have separate roles.

<a id="merchant-credentials-1"></a>

### Material roles

| Member | Purpose and constraints |
| --- | --- |
| `Authority.Code` | Signing merchant number; partner calls sign as the service provider and pass the submerchant separately |
| `Authority.Signing` | `SigningCredential` binds merchant number, hexadecimal merchant certificate serial, RSA-2048 private key and validity; a `PUB_KEY_ID_…` is not a merchant serial |
| `Authority.ApiV2Key` | Independent V2 signing key; when present, exactly 32 single-byte characters |
| `Authority.ApiV3Key` | Independent GCM resource-decryption key; when present, exactly 32 single-byte characters |
| `Authority.Verification` | Snapshot-local `Verifier` with explicit PublicKey or PlatformCertificate mode; no mixed collection |
| `VerificationKey` | WeChat RSA public key, identifier and validity; signature verification and OAEP-SHA1 field encryption, never a private key |

A submerchant identity may omit material it does not use; operations requiring it still enforce its presence. No generic `Secret` is inferred as either API key. `IAuthority`/`Authority` JSON excludes API keys; configuration objects remain sensitive and must not be returned or logged.

<a id="merchant-credentials-2"></a>

### Explicit file configuration

This illustrative fragment belongs inside `<options><option path="/Externals/Wechat">` in host options. Prepare real material through a trusted channel; these placeholders cannot make payments.

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

Application bindings remain in `apps`. `privateKeyFile` is merchant private-key PEM; optional `certificateFile` is the matching merchant certificate. Loading checks the serial and key pair and uses certificate validity. Without the certificate file, the material provider owns private-key validity and serial correctness. Paths use the framework filesystem and host rules, without directory scanning or filename inference.

For platform-certificate mode, `key.name` must exactly match the PEM serial. The currently valid certificate with the latest effective time is selected for encryption; do not set `encryptionKeyId`. Public-key mode may omit that setting for a single key, but requires it when several keys exist.

```xml
<verification mode="PlatformCertificate">
	<keys>
		<key key.name="PLATFORM_CERTIFICATE_SERIAL" file="./certificates/wechat/platform.pem" />
	</keys>
</verification>
```

<a id="merchant-credentials-3"></a>

### Loading, updates and ownership

```csharp
var authority = await AuthorityUtility.GetAuthorityAsync("merchant", cancellation);
var payment = Zongsoft.Externals.Wechat.Paying.PaymentManager.Get(authority);
var key = await authority.GetCertificateAsync("PUB_KEY_ID_EXAMPLE", cancellation);
```

`GetAuthorityAsync` requires configuration; `FindAuthorityAsync` may return null. Providers can use `CreateAsync(AuthorityOptions, cancellation)`. All file reads and validation complete before a snapshot is returned, with no global cache of incomplete or failed material. Custom callback lookup overrides `FallbackHandlerBase.GetAuthorityAsync(code, cancellation)` for asynchronous I/O; synchronous `IServiceProvider<IAuthority>` extensions return prepared in-memory objects only.

Authority lookup does not cache file reads. Shared read-only file access permits concurrent loading. Hosts can retain complete snapshots and replace references only after fully loading and validating new ones. In-flight operations retain a consistent merchant, serial and key. Use versioned paths for multi-file updates and publish configuration afterward. The existing mutable `Accounts` collection must not be modified concurrently after publication.

`PaymentManager.Get(master, subsidiary)` takes prepared `IAuthority` objects and does not synchronously resolve merchant strings. Managers are not globally cached by merchant number. V3 clients use weak caching by complete snapshot object identity, binding signing and verification material; consumers borrow them and do not dispose them.

`SigningCredential.FromCertificate` copies an already loaded, exportable Core `ICertificate` private key without file I/O or ownership transfer. The caller retains the original certificate. Nonexportable system keys cannot be copied; use explicit private-key PEM and public merchant-certificate configuration instead of changing export policies. Snapshots hold encoded managed key data; each operation creates/disposes its RSA object, with no long-lived native RSA/X.509 handle. Managed material is collected with the snapshot, without an immediate-zeroing guarantee. Loaders/downloaders release owned streams, responses, clients and cryptographic objects.

<a id="merchant-credentials-4"></a>

### Trust, rotation and refresh

- Public-key mode uses configured `PUB_KEY_ID_…`/PEM pairs only, does not download platform certificates, and returns null for unknown IDs. Replacement is a trusted configuration update.
- Initial platform certificates must be obtained outside the application through a trusted channel such as official tools. Without a valid trusted certificate, refresh fails; downloaded material cannot authenticate itself.
- `RefreshCertificatesAsync` signs `/v3/certificates`. An existing trusted, valid certificate verifies raw response bytes and a five-minute timestamp window before APIv3 decryption. Invalid headers, unknown signing serials, bad signatures/algorithms/ciphertext, duplicate or mismatched serials, and a changed public key under the same serial are rejected.
- Replace the trust set only after the entire batch validates. Validity is the intersection of X.509 validity and API `effective_time`/`expire_time`; earlier API retirement wins, and removed certificates are not retained.
- Each snapshot coalesces refreshes, with at least one minute between attempts. The cooldown reuses the previous task outcome, including failure. Unknown serials do not cause download loops. Caller cancellation stops only that wait; downloading has its own 30-second deadline.
- `GetCertificateAsync` returns a `VerificationKey` for an exact identifier, or the encryption material when omitted. Certificate mode refreshes after ten hours, when material has under 36 hours left, or for an unknown serial. An unknown serial after successful refresh returns null; refresh errors propagate. Expired material cannot be used.
- No background timer is built in. Long-lived hosts retain snapshots and explicitly refresh more often than every twelve hours, observing failures. If all trust expires, bootstrap again through a trusted channel; repeatedly rereading unchanged configuration is insufficient.

Certificate download uses `IHttpClientFactory` client `Wechat.Certificates`, or the adapter's pooled transport. Its body limit is 4 MiB and batch limit is 32 certificates. Payment response verification and notification verification remain separate contracts; material validity alone does not establish business completion.

<a id="merchant-credentials-5"></a>

### Official sources

See [public-key verification](https://pay.weixin.qq.com/doc/v3/merchant/4013053249), [platform-certificate verification](https://pay.weixin.qq.com/doc/v3/merchant/4013053420), [certificate download](https://pay.weixin.qq.com/doc/v3/merchant/4012551764) and [resource decryption](https://pay.weixin.qq.com/doc/v3/merchant/4012071382).

V2 reversal additionally needs a full matching merchant X.509 certificate and private key. `Signing.CertificateFile` loads the public certificate with the private key; `FromCertificate` can also copy loaded X.509 material. A private key alone can sign V3 requests but cannot perform mutual-TLS reversal. Automao `SigningCertificate` stores the public merchant PEM and combines it with the existing RSA provider's loaded private key, validating serial, key pair and validity; never store the private key in that field.

<a id="http-pipeline"></a>

## APIv3 HTTP pipeline

`Paying.HttpClientFactory.GetHttpClient(IAuthority)` covers V3 `PaymentManager`, `MerchantService`, `BankUtility`, `Paying.Uploader` and `Membership`. It requires matching merchant signing and WeChat verification material. V2/XML, account protocols and notification receivers are separate pipelines.

<a id="http-pipeline-1"></a>

### Requests and signing

- Clients are weakly cached by complete merchant snapshot object identity, even when merchant numbers or signing material match. Borrowers neither dispose the client nor mutate its properties/default headers during concurrent operations.
- Requests permit only HTTPS port 443 on `api.mch.weixin.qq.com` or `api2.mch.weixin.qq.com`, a `/v3/` path, and no user information or fragment. The default base is `https://api.mch.weixin.qq.com/v3/`.
- Sign the actual method, escaped path/query, Unix seconds, nonce and original body bytes, separated and terminated by LF. Serialize/buffer ordinary bodies once and send the same bytes. POST without a request object has an empty body.
- Multipart requests contain exactly one JSON `meta` part; a quoted part name is allowed. Only its bytes are buffered/signed, not the file or multipart boundary. Upload methods own file reads and multipart disposal; product limits and digests are separate requirements.
- `Wechatpay-Serial` is a request header, not a content header. Sensitive-field encryption explicitly selects the matching WeChat public-key ID or platform-certificate serial, or uses the current encryption material when omitted. Unknown material fails immediately. Merchant signing identity belongs in `Authorization`, not in this header.

<a id="http-pipeline-2"></a>

### Response verification

Read and verify raw bytes before JSON parsing. Both declared and actual body lengths are limited to 4 MiB. All successful responses, including empty 204 responses, require unique, nonempty `Wechatpay-Serial`, `Wechatpay-Timestamp`, `Wechatpay-Nonce` and `Wechatpay-Signature` headers. Optional `Wechatpay-Signature-Type` must be `WECHATPAY2-SHA256-RSA2048`. Timestamps may differ from UTC by at most five minutes; hosts must synchronize clocks.

Verify timestamp, nonce and original body bytes, never reconstructed JSON. Missing/duplicate headers, invalid Base64, unknown keys, expired timestamps, tampering and invalid `WECHATPAY/SIGNTEST/` probes fail cryptographic verification. Public-key mode cannot discover unknown keys. Certificate mode may refresh using existing trust and then verify the response without resending the business request.

An unsuccessful response with no `Wechatpay-*` headers can only produce an unverified HTTP failure; its JSON `code` is not trusted. Any such header requires complete verification, with no partial-header downgrade. Valid signatures and HTTP success still require product-state and business checks.

<a id="http-pipeline-3"></a>

### Errors, timeouts and retries

| Condition | Result |
| --- | --- |
| Verified non-success response with nonempty string `code` | `OperationException.Reason` is that code; a string `message` may describe it |
| Unsigned gateway failure or verified error without a valid code | `HTTP_` plus numeric status, such as `HTTP_502`; no HTML/unverified body in the exception |
| Verified success with invalid target JSON | `INVALID_RESPONSE`, retaining the inner `JsonException` |
| Invalid signature/material/time or oversized body | `CryptographicException`, never business success |
| Caller cancellation | Cancellation propagates; no business retry |
| Client timeout | Standard `HttpClient` timeout cancellation; no inference of transaction success/failure |

Protocol, HTTP-status and JSON exceptions include integer `StatusCode` and Boolean `ResponseVerified` in `Exception.Data`; a valid single `Request-ID` becomes `RequestId`. This ID is not signed and is for diagnosis, not authorization or deduplication. Logs selectively record those fields without whole payloads, encrypted values or signatures.

The default 30-second timeout includes signing preparation, transport, reading and verification waits. A shared certificate refresh has its own lifecycle; caller cancellation stops only its wait. There are no automatic redirects or business retries. Resolve uncertain outcomes through the original business identifier, not global POST replay.

<a id="http-pipeline-4"></a>

### Host transport and ownership

Nested handlers implement signing/verification; `partial` files separate request, response and result parsing. Existing manifests/providers/services remain the composition mechanism.

With `IHttpMessageHandlerFactory`, each send borrows the current `Wechat.Payments` handler and disposes only its own `HttpMessageInvoker` wrapper. The factory owns the handler. Disable redirects/retries and do not modify signed methods, URIs or bodies. Underlying `SocketsHttpHandler`/`HttpClientHandler` instances allowing redirects cause `InvalidOperationException` before sending; a generic default factory is insufficient. Custom transports have the same obligations.

Without a factory, the shared `SocketsHttpHandler` disables redirects and uses a five-minute connection lifetime. `Wechat.Certificates` remains a separate `IHttpClientFactory` path to avoid recursive trust bootstrap.

High-level `HttpUtility.GetAsync/PostAsync`, `BankUtility` and `Uploader` dispose their own requests, content and responses. Direct client consumers own requests/responses they create. `GetResultAsync`/`EnsureResultAsync` borrow responses and do not dispose them for callers. The pipeline reads/disposes original response streams/content and replaces successful content with verified bytes; verification, pre-parse read and status failures dispose the response. The owning high-level method handles cleanup after parsing failures.

<a id="http-pipeline-5"></a>

### Verification and official sources

In-memory tests cover exact bytes, encoding, empty bodies, multipart, 204, bad signatures/time, errors, rotation, cancellation, timeout, limits and disposal. They do not establish account eligibility or a complete Automao business flow. Bill downloads are outside this JSON pipeline and require their own digest checks.

Sources: [query signing](https://pay.weixin.qq.com/doc/v3/merchant/4012365337), [upload meta signing](https://pay.weixin.qq.com/doc/v3/merchant/4012365335), [verification and empty responses](https://pay.weixin.qq.com/doc/v3/merchant/4013053249), [HTTP statuses](https://pay.weixin.qq.com/doc/v3/merchant/4012081717), [Request-ID and basic rules](https://pay.weixin.qq.com/doc/v3/merchant/4012081709), [Wechatpay-Serial](https://pay.weixin.qq.com/doc/v3/merchant/4012365346).

<a id="payment-models"></a>

## Amounts, identifiers and results

<a id="payment-models-1"></a>

### Amounts and currency

Domestic payment/refund requests use integer CNY fen. Integer constructors already take fen; decimal constructors take yuan and require exact conversion without truncation or rounding. Empty currency defaults to CNY, case may be normalized, and other currencies are rejected.

Payments must exceed zero and cannot exceed `int.MaxValue` fen (`21474836.47` yuan). Refund/original amounts must be positive, and a refund cannot exceed the original total. Zero, negative and out-of-range amounts throw `ArgumentOutOfRangeException`; sub-fen precision and unsupported currency throw `ArgumentException`. Amount request structs are readonly; default structs cannot be assigned as a request's `Amount`.

Original order totals, payer cash totals and refunds retain distinct meanings. Order totals do not determine discounts or merchant settlement. Concurrent cumulative partial-refund limits belong to the host, not one DTO.

<a id="payment-models-2"></a>

### Identifiers and payment creation results

Merchant, submerchant, merchant-order and WeChat transaction identifiers remain strings, preserving leading zeroes. `PayAsync`/`PayV3Async` return `PaymentService.PaymentResult`:

| Member | Meaning |
| --- | --- |
| `PrepaymentId` | Accepted JSAPI/mini program or APP prepayment credential, not transaction ID |
| `CodeUrl` | Native QR content, not a prepayment identifier |
| `Order` | Actual payment-code/offline-face order; inspect its `Status` |

Prepayment and actual-order results are mutually exclusive. Missing `prepay_id` for JSAPI/APP, missing Native `code_url`, or mixed responses are rejected. Missing signing material does not trigger V2 fallback. Queries return `PaymentOrder`; `Identifier` means `transaction_id` only.

Automao maps `PaymentProviderResult.Token` to `PaymentDetail.PaymentToken` and `Identifier` to `PaymentCode`. Native QR content is extension data, not a transaction ID. Accepted prepayments remain Pending with zero paid/received amount; only a confirmed successful actual order is successful. Other providers without a separate Token can still use their Identifier as the payment credential.

<a id="payment-models-3"></a>

### Status and out-of-order updates

`PaymentUtility.GetStatus` maps absent/unrecognized values to `PaymentStatus.Unknown`, retaining the meanings of `NOTPAY`, `USERPAYING`, `SUCCESS`, `REFUND`, `CLOSED`, `REVOKED` and `PAYERROR`. `RefundmentUtility` maps `ABNORMAL` to distinct `RefundmentStatus.Abnormal`, not success or permanent failure; unknown values remain Unknown. Investigate/query an abnormal refund instead of creating another one blindly.

Automao retains unknown payment/refund and abnormal refund states as Pending, preserving raw protocol states in DTOs/notifications. Use original identifiers to resolve uncertainty. Aggregate updates use the previously observed database state as a condition, so older snapshots cannot overwrite completed states. A failed conditional update cannot masquerade as successful local mutation. Terminal corrections require separate reconciliation.

<a id="payment-models-4"></a>

### Official sources

See [JSAPI amounts and prepayment](https://pay.weixin.qq.com/doc/v3/merchant/4012791856), [refund acceptance](https://pay.weixin.qq.com/doc/v3/merchant/4012791862) and [order lookup](https://pay.weixin.qq.com/doc/v3/merchant/4012791859). Product scope is in the [interface map](#capability-matrix), and durable reception in [notifications](#payment-notifications).

`PaymentProviderResult.Status` preserves Pending/Succeed/Failed/Cancelled. `Successful` is true only for Succeed; the default result is Pending. Payment-code timeout or signature/message failure does not prove refusal; query the original order.

<a id="basic-payments"></a>

## Basic APIv3 payments

<a id="basic-payments-1"></a>

### Scenarios and roles

`PaymentManager.Get(authority)` uses a direct merchant; `Get(master, subsidiary)` uses an ordinary provider and submerchant. Supported scenarios are `Web` (JSAPI/mini program), `App` and `Native`. Undefined enum values are rejected and H5 is not implemented.

| Scenario | Direct creation | Partner creation | Result |
| --- | --- | --- | --- |
| Web | `/v3/pay/transactions/jsapi` | `/v3/pay/partner/transactions/jsapi` | Prepayment ID and JSAPI parameters |
| App | `/v3/pay/transactions/app` | `/v3/pay/partner/transactions/app` | Prepayment ID and APP parameters |
| Native | `/v3/pay/transactions/native` | `/v3/pay/partner/transactions/native` | QR content; no client signature parameters |

Signing and verification material are mandatory. Payment-code/online-face V2 and offline-face dedicated V3 flows cannot be selected by changing a basic-payment scenario.

<a id="basic-payments-2"></a>

### Applications and payers

Use the bound service's `Request.Create(appId, voucher, amount, currency, payer, description)`. The AppID belongs to the bound merchant/submerchant configuration; omission selects the provider/direct merchant's default application. JSAPI OpenID must belong to that selected AppID. Submerchant apps send `sub_appid/sub_openid`; provider apps send `sp_appid/sp_openid`. A configured submerchant default must not silently change payer ownership. APP and Native omit payer objects and do not require an Automao OpenID lookup.

Mobile apps use `AccountType.Application` or `Account.Application(appId)`. This identity is for payments, without account-login APIs or a login Secret:

```xml
<apps default="wx-mobile-app-id">
	<app app.name="wx-mobile-app-id" type="application" />
</apps>
```

Channel/applet bindings still reference their account collections. APP requires an application account; Web requires a channel/applet; Native permits any bound type. A provider using a submerchant mobile AppID also needs an unambiguous mobile application of its own. If its default has the wrong type, select only a unique matching app, never guess among several.

Requests require a business order number, description, valid amount and absolute HTTP(S) notification URL without user information or fragments. Set `FallbackUrl` explicitly or `/Externals/Wechat/Fallback/Url`, for example `https://callback.example.test/Externals/Wechat/Fallback/{0}/{1}`. Do not fabricate payer IP when no scene is provided; scene fields require valid `Place.DeviceIp`.

<a id="basic-payments-3"></a>

### Client parameters

`PaymentResult.Parameters` is a readonly string dictionary signed with the same merchant key as creation:

| Scenario | Fields |
| --- | --- |
| Web | `appId`, `timeStamp`, `nonceStr`, `package=prepay_id=…`, `signType=RSA`, `paySign` |
| App | `appId`, `partnerId`, `prepayId`, `packageValue=Sign=WXPay`, `nonceStr`, `timeStamp`, `sign` |

The fourth signing line is `prepay_id=…` for JSAPI and the raw prepay ID for APP. Every line ends in LF; signatures use RSA-SHA256/Base64. Partner mode always signs with the provider key; client appId and APP partnerId follow the selected provider/submerchant pairing. iOS callers map `packageValue` to SDK `package`.

Native `CodeUrl` is raw QR content, not a transaction identifier or signing credential. Client success still requires a server query or verified notification.

<a id="basic-payments-4"></a>

### Query, close and uncertain outcomes

`GetAsync(voucher)` queries by merchant order number; `GetCompletedAsync(transactionId)` queries by WeChat transaction ID. Neither method name guarantees a successful payment. `CancelAsync(voucher)` uses the role-specific V3 close endpoint. Escape path identifiers and query parameters separately; merchant fields come from the bound context. Responses must match the requested identifier, merchant, configured app, submerchant and amount/currency, even when correctly signed.

No automatic money-request POST retry is performed. Timeout/network failure may follow acceptance: retain the original order and query its product. Unknown `trade_state` retains its raw value and maps to Unknown. Closing is not payment-code reversal.

<a id="basic-payments-5"></a>

### Automao

The provider keeps client parameter names in extension results and uses `codeUrl` for Native. Prepayment Token and actual PaymentCode are distinct; acceptance remains Pending. `Paying_PaymentDetail.PaymentToken` is nullable `varchar(64)`, defined in the [Paying creation script](../../../../../Automao/documents/database/automao.paying-mysql.sql) and [schema design](../../../../../Automao/documents/database/automao.paying.md). Synthetic signatures, mappings and persistence do not establish eligibility or real payment success.

<a id="basic-payments-6"></a>

### Official sources

[Direct JSAPI](https://pay.weixin.qq.com/doc/v3/merchant/4012791856), [partner JSAPI](https://pay.weixin.qq.com/doc/v3/partner/4012738519), [direct APP](https://pay.weixin.qq.com/doc/v3/merchant/4013070347), [partner APP](https://pay.weixin.qq.com/doc/v3/partner/4013080231), [direct Native](https://pay.weixin.qq.com/doc/v3/merchant/4012791877), [partner Native](https://pay.weixin.qq.com/doc/v3/partner/4012738659), [APP parameters](https://pay.weixin.qq.com/doc/v3/partner/4013080233), [APP signing](https://pay.weixin.qq.com/doc/v3/partner/4012365868), [JSAPI signing](https://pay.weixin.qq.com/doc/v3/partner/4012365867).

<a id="ticket-payments"></a>

## Payment codes and online face payment

Direct and partner payment-code/online-face flows use V2 XML: `/pay/micropay`, `/pay/orderquery` and `/secapi/pay/reverse`. Basic JSAPI/APP/Native and dedicated offline-face products use their own services.

<a id="ticket-payments-1"></a>

### Submission and results

Create requests with `manager.Payment.Request.Ticket(appId, voucher, amount, currency, ticketCode, description)` and set `Place.DeviceIp` to the real terminal IP, not a fabricated loopback address. Decimal amounts are yuan, exactly convertible to positive CNY fen. Bind the correct merchant app; selecting a submerchant app includes both the provider app and `sub_appid`.

`await manager.Payment.PayAsync(request, Scenario.Web, cancellation)` returns `PaymentResult.Order`, without a prepayment credential or background business callback. Await it, then persist confirmed completion or uncertainty.

- Confirm success only after transport/business success, valid trade type and matching order, merchant, app and amount. Transaction ID is separate from merchant order number.
- `USERPAYING` remains in progress. System, bank and unknown errors remain uncertain, with no invented paid amount or transaction ID.
- Explicit expired/invalid payment code, unsupported card or insufficient balance maps to failure; closed/reversed orders retain their terminal state.
- Timeout, cancellation, bad signature and parsing failure do not prove a failed debit. Retain the original order; never retry debit under a new identifier.

<a id="ticket-payments-2"></a>

### Query and reversal

`Payment.Tickets.GetAsync(appId, voucher, cancellation)` queries by merchant order number; `GetCompletedAsync(appId, identifier, cancellation)` by transaction ID. Inspect `trade_state`, not query `result_code=SUCCESS`; unknown states stay unknown.

`ReverseAsync(appId, voucher, cancellation)` returns `ReversalResult`: `Succeeded` confirms reversal, `RequiresRetry` represents `recall=Y`, and `ErrorCode` retains the verified business error. Transport exceptions propagate. Retry only the same reversal, never the debit. Unpaid reversed orders cannot subsequently be paid; reversal can return funds already debited.

The library does not replay requests or poll indefinitely. Hosts query original orders, bound polling and durably retain compensation work. Official guidance suggests querying about five seconds after system errors, every ten seconds during user payment, considering reversal after about 45 seconds of uncertainty, and waiting at least 15 seconds between debit and reversal. See [direct](https://pay.wechatpay.cn/doc/v2/merchant/4011937125) and [partner](https://pay.wechatpay.cn/doc/v2/partner/4011941293) guidance.

<a id="ticket-payments-3"></a>

### V2 protocol and certificates

V2 uses independent `ApiV2Key`, not the V3 private key. Sort fields ordinally, omit `sign`, null and empty strings, and format numbers invariantly. MD5 and HMAC-SHA256 are supported. XML is escaped on write; reads are limited to 2 MiB and reject DTDs, nested/duplicate fields, namespaces and extra root nodes.

Transport-success responses require the same signing algorithm as the request, followed by merchant/app/product checks. Transport failure produces an exception, not a definite transaction terminal state. The pipeline releases requests/responses and performs no redirect, debit retry or unawaitable thread-pool business callback.

Reversal requires matching merchant X.509 certificate/private key, serial and validity. `SigningCredential` can take certificate PEM, `Signing.CertificateFile` loads it, and `FromCertificate` copies loaded exportable X.509 material. Private key plus serial alone cannot perform mutual TLS. Providers use their own certificate, not the submerchant's.

Factory transports are `Wechat.Payments.Xml` for ordinary V2 and `Wechat.Payments.Xml.{merchantNumber}.{certificateSerial}` for mutual TLS. The host configures the latter's matching client certificate including its private key and disables redirects/retries for both. Generic factory defaults are insufficient. Without a factory, redirects are disabled and reversal owns/disposes its TLS handler and certificate. Custom transports preserve identity and lifetime requirements.

<a id="ticket-payments-4"></a>

### Online face initialization

`AuthenticateOnlineAsync` calls `https://payapp.weixin.qq.com/face/get_wxpayface_authinfo` with device, store, raw data and the selected merchant/app. Verify XML signatures/identity and require authinfo plus positive expiry when supplied. This endpoint has no payment-code `result_code` contract and must not share that parser. authinfo is bound to the same device/merchant; obtaining it is not payment success. See [face payment](https://pay.wechatpay.cn/wiki/doc/wxfacepay/develop/windows/facepay.html).

<a id="ticket-payments-5"></a>

### Automao business integration

`ClientAddress` supplies terminal IP and `Device` supplies device number; IP is not authorization evidence. A ticket request's `Argument` is the payment code, not a JSAPI/APP scenario enum.

The provider's explicit Status controls success. Timeout/protocol failures remain Pending for original-order lookup. On caller cancellation, payment business code first uses an independent cancellation scope for necessary uncertain-state persistence, skips not-yet-started providers, then propagates cancellation.

Cancellation endpoints validate Cancel permission and site query scope. Combined payments must be completely visible. Use the same V2 product for lookup/reversal; `recall=Y` or failed reversal cannot mark an order cancelled. Confirmed changes conditionally update only None/Pending details before aggregation, preserving concurrently completed states. Clients refresh by the original key instead of creating another debit.

After a provider result arrives, persistence and terminal handling do not use the caller token. Cancellation prevents unstarted payments and propagates after confirmed results are saved; zero-amount payments retain original cancellation behavior. Synthetic tests validate material binding and transport choice, not real TLS acceptance, debits, devices or eligibility.

<a id="refunds"></a>

## Refunds

`PaymentManager.Refundment` uses domestic direct/ordinary-provider APIv3 refunds and the verified [HTTP pipeline](#http-pipeline). V2 payment-code orders use this service under the [cross-version selection rules](#capability-matrix-3); actual product eligibility and cross-version transactions require live validation.

<a id="refunds-1"></a>

### Application, query and notification

Construct requests through the bound manager's `Request.Create`. Preserve original payment number, merchant refund number, original total, refund amount and currency on retries. Provide valid `FallbackUrl`, explicitly or from the configured template; reject query strings, user information and fragments.

`RefundAsync` returns `RefundmentResult`; `Order` is the acceptance-time snapshot, not settlement. Confirm final status through `GetAsync` or a verified notification. Provider requests carry `sub_mchid` and use provider signing material for both application and query.

Application/query responses use `status`; notification resources use `RefundmentNotification.refund_status`. Do not fall back between them. Responses need not contain merchant fields: their context comes from the signed request, and any additional returned identity fields must match it. Notifications derive identity from the decrypted message and validate it before dispatch.

All amounts are integer CNY fen. Original total, payer cash, refund total and cash refund remain distinct. Requested refund must be positive and no greater than the original total. Reject mismatched identifiers or amounts.

<a id="refunds-2"></a>

### Automao states and retries

- Acceptance, network/timeout/protocol uncertainty and possibly submitted cancellations remain Pending.
- `PROCESSING`, `ABNORMAL` and unknown states remain pending; preserve abnormal status in explanatory data for investigation. Only query/notification SUCCESS confirms success, and CLOSED confirms closure.
- A positive business amount requests that partial refund; zero requests the remaining balance. Failed/closed details do not reserve capacity; unstarted/pending details conservatively do. Multiple business payments create separate refunds: retain identifiers after partial failure, without promising cross-payment financial atomicity.
- Explicit retry by refund ID returns a failed detail to Pending. Queries/notifications do not reopen failed terminal states automatically. Query an already attempted original refund first; only confirmed absence permits resubmission with the same number and amount. No automatic refund POST replay occurs.
- The provider validates refund, original payment, merchant, store and amount. Combined payments account for all business members of the same WeChat transaction, rather than using one member's amount as the channel order total.
- Before a money request, a serializable transaction counts other unstarted, pending, successful and uncertain refunds and reserves this detail as Pending. Send only after commit; submissions inside an outer transaction are rejected. Timeout does not release reservations, and reservation conflicts prevent sending.
- Conditional writes protect terminal states for queries, application results and notifications. Only the updater that advances the aggregate terminal state emits completion. Inbox business keys include refund number and protocol status, allowing abnormal-to-success transitions while preventing duplicate same-state processing and late nonterminal regression.

Merchant-console refunds may have separate identifiers or V2 notifications. The Gateway accepts configured V3 notifications only. Compensate through `GetAsync(merchantRefundNumber)` after the host associates the original business order; unknown business refunds are not created or transferred automatically. External refunds also require reconciliation; local reservations cannot prove their absence.

<a id="refunds-3"></a>

### Official sources

[Direct refund](https://pay.weixin.qq.com/doc/v3/merchant/4013071036), [partner refund](https://pay.weixin.qq.com/doc/v3/partner/4013080625), [partner query](https://pay.weixin.qq.com/doc/v3/partner/4013080626) and [partner notification](https://pay.weixin.qq.com/doc/v3/partner/4013080241). Official skill resources include the corresponding documents under `APIv3/{普通商户,合作伙伴}/支付产品/订单退款/API列表/`; the notification reference also appears under APP payment APIs.

<a id="offline-face"></a>

## Offline face payment and agreements

This product requires provider/submerchant context and explicit eligibility. It follows [K12/offline-face APIs](https://pay.wechatpay.cn/wiki/doc/wxfacepay/develop/k12-development-guidelines.html) and [integration requirements](https://pay.wechatpay.cn/wiki/doc/wxfacepay/develop/access-specifications.html). The library supplies no device SDK; synthetic protocol tests do not establish live eligibility.

<a id="offline-face-1"></a>

### Authentication and debit

`PaymentManager.Get(serviceProvider, subsidiary).Payment.AuthenticateOfflineAsync` binds provider app, submerchant, organization and device, returning material for that identity. Device IDs are 1–32 ASCII letters/digits/underscores; raw device data is at most 2048 UTF-8 bytes. Direct-merchant context is unsupported.

Use the business-parameter overload of `Request.Ticket`. K12 product is 2 with scenes 3–6; group dining is 11 with scenes 124–126. Preserve selected app/submerchant and send CNY fen, authorization code, description, attachment data and device IP to `/v3/offlineface/transactions`. Retain the original identifier after uncertainty; no automatic debit replay.

`Payment.GetOfflineAsync(voucher, product)` uses the dedicated query with `sp_mchid`, `sub_mchid` and `business_product_id`. `OfflineOrder` retains `trade_state_description`, debt state, supplemental-debit transaction ID and error type; paid amount comes from `amount.pay_total`. Success requires transaction ID and complete amount data; pending/unknown states may lack amounts. HTTP 200 is not completion. Merchant, app and business order must match the request.

Automao `face.offline`/`offline` stores `PaymentChannel` as `K12` or `Groupon`, optionally followed by `:scene`; defaults are 4 and 125. Queries use the persisted product. Without a corresponding reversal API, cancellation throws `NotSupportedException` rather than cancelling locally and ignoring the remote debit.

<a id="offline-face-2"></a>

### Agreement services

| `MerchantService.Contracts` entry | Purpose and binding |
| --- | --- |
| `AuthenticateAsync` | WEBSESSION credential bound to organization and merchant user number |
| `ApplyAsync` | K12/GROUP_DINING presign credential; optional identity, with complete bank identity for bank-restricted mode; encrypted fields match the header material |
| `GetSignatoryAsync` | Organization/user lookup; preserve user, face-collection and contract states |
| `GetContractAsync` | Specific contract bound to merchant and selected application |
| `RevokeAsync` | Organization/WeChat-face-user termination, including verification of empty 204 responses |

Use authorization/presign credentials within official validity; they are not durable identities. Unknown user/contract states remain Unknown. Automao's current user application returns WEBSESSION; presign and bank modes are independent library APIs.

<a id="offline-face-3"></a>

### Automao state and notifications

Accounts link the local agreement, user, tenant/organization/store and WeChat organization. Enable only when the contract is signed, the user normal and face collection complete. Unknown results preserve state and report uncertainty. Cancelled local agreements cannot be revived by refresh; a terminated contract cannot be reenabled by late signed results.

After signature verification and authenticated decryption, notifications validate merchant, app, organization and user. Match contract first; an initial signature may match a unique unbound pending record. After durable receipt, query current agreement state. An old notification cannot rebind a changed contract, and ambiguous ownership fails rather than selecting the first record.

Agreement, account enablement and confirmed termination commit together, conditioned on observed state/contract. Before remote revocation, validate the original contract and persist a pending revocation; end the local transaction before sending. A verified remote success must complete critical local writes despite late caller cancellation. Uncertainty retains the original revocation for refresh; completed revocations are not resubmitted. Explicit and notification flows share synchronization logic.

[Automao fixtures](../../../../../Automao/server/externals/wechat/integration/README.md) cover actual plugins, isolated MySQL and synthetic signed transport; live state changes, devices and permissions require an eligible environment.

<a id="payment-notifications"></a>

## Payment notifications

<a id="payment-notifications-1"></a>

### Protocol and identity

Payment notifications have three explicit POST routes; component events use their own endpoint:

| Route | Events | Original resource type |
| --- | --- | --- |
| `/Externals/Wechat/Fallback/payment/{merchant}` | `TRANSACTION.SUCCESS` | `transaction` |
| `/Externals/Wechat/Fallback/refundment/{merchant}` | `REFUND.SUCCESS`, `REFUND.ABNORMAL`, `REFUND.CLOSED` | `refund` |
| `/Externals/Wechat/Fallback/offlinefaceagreement/{merchant}` | `FACEPAY.USER_STATE_CHANGE` | `facepay` |

Merchant is a digits-only direct/provider number without role suffix. Reject query parameters; require JSON and at most 2 MiB. Configuration matches `Code` uniquely; dynamic hosts may override the handler's asynchronous merchant lookup.

Require unique serial, timestamp, nonce and signature headers, a five-minute timestamp window, and optional signature type `WECHATPAY2-SHA256-RSA2048`. Verify raw bytes using explicitly trusted keys/certificates before event parsing and AES-256-GCM authentication/decryption.

Reject duplicate JSON properties, unknown events/algorithms and mixed merchant identities. Direct mode uses `mchid`; provider mode uses `sp_mchid` plus required `sub_mchid`; offline face uses `mch_id`. Payment/face AppIDs must be in the configured account set. Refund notifications have no AppID, so the host checks original-payment ownership. Refund state comes from `refund_status`.

<a id="payment-notifications-2"></a>

### Acceptance extension points

`IFallbackHandler.ReceiveAsync(FallbackRequest, CancellationToken)` accepts raw body and headers. The one/two-generic-parameter `FallbackHandlerBase` forms validate the protocol and hand immutable `FallbackNotification` plus the parsed product object to `OnReceiveAsync`. Hosts validate order, organization, user, amount and currency, then complete business work or durably store the notification.

| Result | Meaning |
| --- | --- |
| `Completed` | Business work completed |
| `Accepted` | Durably accepted; host owns subsequent processing/recovery |
| `Duplicate` | The same business notification was already durably accepted |
| `None` | Not reliably accepted |

The first three return empty HTTP 204. Unaccepted, failed persistence, handler exceptions or deadline expiry return 503; malformed protocol input returns 400 with generic JSON `code=FAIL`. The Gateway passes a four-second cancellation deadline; handlers must respect it. Unknown routes return 404. A disconnect after acceptance can still cause redelivery, requiring persistent deduplication.

Typed `HandleAsync(TRequest, ...)` is only for trusted internal objects, such as validated payment-code results; it does not authenticate network notifications. Never deserialize arbitrary HTTP and bypass `ReceiveAsync`. The caller owns `FallbackRequest.Body`; handlers do not close it or retain it across requests.

At startup, register handlers under `/Workbench/Externals/Wechat/Fallback/Handlers` using Payment, Refundment and OfflineFaceAgreement keys matching `Kind`. Do not mutate registration while serving. Protocol code has no database, tenant or business-state dependency.

<a id="payment-notifications-3"></a>

### Durable reception in Automao

`NotificationReceiver`, mapping `Wechat.Notification` and table `Wechat_Notification` implement the inbox. Initialize it with `D:\Automao\documents\database\automao.wechat-mysql.sql` before running Gateway. The external WeChat Gateway plugin contributes the worker, and payment providers register `INotificationProcessor`; startup code contains no payment business logic.

Merchant, submerchant and product scope deduplication. Notification ID and business key each have unique constraints, plus body fingerprints. Matching duplicates succeed; conflicting content fails. Payment keys use business order, refund keys use refund number plus protocol state, and face keys use agreement number plus notification time.

Payment acceptance checks environment, receiving merchant, amount and currency. Refund acceptance checks original payment, transaction and amount, using business CNY constraints when the message omits currency. Face acceptance checks agreement/user/organization; it must query current contract state before changing accounts.

| Inbox status | Meaning and recovery |
| --- | --- |
| `0` | Pending; claimable after restart |
| `1` | Claimed; processing or requiring investigation after interruption |
| `2` | Completed |
| `3` | Failed; `Failure` retains an error code or exception type |

Database conditional updates allow one worker to claim a record. Payment business includes post-write credits, delayed tasks and events without one common transaction boundary. Therefore 1/3 are not replayed automatically: investigate payment/refund state and side effects before recovery. Do not bulk-reset them to 0 or equate HTTP 204 with business completion. Durable storage and deduplication do not promise exactly-once execution across external systems. Restrict access/retention for notification bodies and omit them, signatures and secrets from logs.

<a id="payment-notifications-4"></a>

### Verification

`FallbackTest` uses independent RSA/GCM fixtures for verification, identity and event rejection. `gateway/test` directly assembles local ASP.NET Core to check responses, without a plugin host. Actual Automao plugin fixtures reside in `externals/wechat/integration`. Synthetic callbacks demonstrate local protocol/persistence behavior, not real delivery or eligibility.

<a id="merchant-onboarding"></a>

## Platform ecommerce onboarding, banks and media

`PaymentManager.GetMerchantService(authority).RegisterAsync/GetAsync` implement platform ecommerce secondary-merchant onboarding, requiring that product's permission. They do not implement ordinary-provider onboarding; upload success or acceptance does not activate payments.

<a id="merchant-onboarding-1"></a>

### Applications and state

Submit with merchant business request ID `Registration.RequestId`; acceptance returns a WeChat application ID. Query by the original business ID and validate returned `out_request_no`. Timeout/cancellation leaves an unknown outcome: query before resubmitting, without changing IDs automatically.

`Registration` separates licenses, legal-representative identity, contact, shop and settlement account. Government category is `2502`; social organizations require explicit `BusinessLicense.CertificateType`. Identity-card start date is mandatory; null end means long-term validity. Supplementary pictures/qualifications use JSON-array strings on the wire but `string[]` for callers. Financial-institution-specific material and nondefault beneficiary lists need independent extensions.

Encrypt sensitive names, IDs, account numbers and phones with the same material identified by `Wechatpay-Serial`. Verify responses before decrypting verification-account name/number with the merchant private key. Parse verification deadlines using the specified date-time format. Preserve raw `Applyment.Status`; unknown never means approved.

Automao `TenantAgreementProvider` uses existing Features/plugins. Bind application, tenant and agreement; acceptance/review/pending signature do not enable accounts. Only FINISH creates/enables an account bound to that application and issuing merchant. FROZEN/CANCELED disable it; late states cannot revive cancelled applications. Commit state/account updates together; perform network work outside transactions. Upload attachments under the current issuing merchant, without reusing unscoped media IDs.

Automao currently supports explicitly identified identity cards for legal representatives/contacts and rejects other document types. Organization certificate class is stored in `TenantAgreement.BusinessLicenseType`, with the appropriate fixed government class. `AgreementToken` stores the complete signing or verification URL, up to 256 characters. The [Common schema script](../../../../../Automao/documents/database/automao.common-mysql.sql) defines these fields alongside the [schema design](../../../../../Automao/documents/database/automao.common.md).

<a id="merchant-onboarding-2"></a>

### Bank components

`GetBanksAsync` lists banks; `GetBranchesAsync` uses bank alias and city codes. Validate paging offset/count and update total even on empty pages. Settlement bank codes and branch-query alias codes are distinct.

`FindAsync` returns `IReadOnlyList<Bank>` with zero or more candidates, never an implicit first choice. The Web endpoint returns an array. Encrypt card numbers before escaping them into query parameters; bank-component permission is required.

<a id="merchant-onboarding-3"></a>

### Media uploads

`Uploader.UploadAsync` reads framework-filesystem paths and owns/disposes the stream. JPG/JPEG, PNG, BMP and video are limited to 5 MiB; PDF to 7.5 MiB. Reject empty/oversized files and invalid names. Video types are AVI, WMV, MPEG, MP4, MOV, MKV, FLV, F4V, M4V and RMVB. Enforce limits on bytes actually read, including cancellation/error cleanup.

Multipart meta carries filename and SHA-256; sign those exact meta bytes and use the appropriate file MIME type. Video uses `merchant/media/video_upload`; images/PDF use `merchant/media/upload`. Extension/size validation does not replace WeChat content inspection.

<a id="merchant-onboarding-4"></a>

### Sources and verification boundary

[Application](https://pay.wechatpay.cn/doc/v3/partner/4012713017), [query](https://pay.wechatpay.cn/doc/v3/partner/4012691376), [banks](https://pay.wechatpay.cn/doc/v3/partner/4012697583), [card identification](https://pay.wechatpay.cn/doc/v3/partner/4012697541), [branches](https://pay.wechatpay.cn/doc/v3/partner/4012697673), [images/PDF](https://pay.wechatpay.cn/doc/v3/partner/4012760432), [video](https://pay.wechatpay.cn/doc/v3/partner/4012761084).

Framework tests use synthetic material to check encryption, identity, candidates, paging, digests and release. Automao `onboarding.py` checks actual plugins and isolated MySQL application/account state; neither proves eligibility or document approval.

<a id="partner-onboarding"></a>

## Ordinary service-provider onboarding

`PaymentManager.GetPartnerService(authority)` uses ordinary-provider signing, verification and account context. `PartnerService` and ecommerce `MerchantService` are separate products; one role does not confer another product's permissions.

<a id="partner-onboarding-1"></a>

### Applications and status

`RegisterAsync(Registration)` posts to `/v3/applyment4sub/applyment/` and returns the WeChat application number. Nested partial models represent administrator, subject, business scene, settlement rules/account and supplements using official field mappings. Validate required fields, nested information and scene relationships before sending; the caller supplies truthful complete documents, while WeChat performs eligibility, rates, identity and media review.

Business application IDs contain ASCII letters/digits/underscores and at most 124 characters. `GetByRequestAsync` and `GetByApplicationAsync` query by business or WeChat application ID and validate response ownership. Application numbers parse as 64-bit integers and submission returns a string; they are not sub_mchid.

Preserve raw `Applyment.State`. Only `APPLYMENT_STATE_FINISHED` makes `IsCompleted` true and requires a submerchant number. Signing, permission activation and unknown states cannot enable payments. The merchant administrator must personally complete identity checks/signing; the provider cannot substitute for that person.

Query the original business ID after failure/uncertainty; no automatic duplicate submission or new ID. Revisions after rejection retain the original business ID and host tenant/provider association.

<a id="partner-onboarding-2"></a>

### Sensitive information

Administrator name, identification, phone/email; legal representative/operator/beneficiary identity names, numbers and addresses; and settlement account names/numbers use the existing V3 RSA-OAEP field converters. `Wechatpay-Serial` identifies the encryption key; requests use provider signing and responses verify before parsing. Upload/bank calls retain that provider identity. Do not expose models or sensitive data to ordinary clients/logs.

<a id="partner-onboarding-3"></a>

### Payment permissions and AppID

Onboarding, third-party account authorization, payment products and payment AppID bindings are independently verified. Third-party `func_info` grants no payment rights.

Official AppID management occurs through provider and public/open-platform consoles. Bind provider sp_appid and associate submerchant sub_appid under applicable entity/verification rules; required approvals belong to the corresponding administrators. The library does not invent an automatic binding-completion or all-products-permission API.

Hosts retain provider/submerchant numbers, sp_appid, applicable sub_appid, confirmed products, confirmation source and effective state. Check them before use. Submission, obtaining a merchant number, entering an AppID or authorizing an official account alone cannot activate payments. Disabling a relationship preserves historical order credentials; original-order queries and necessary refunds use independent business permissions.

<a id="partner-onboarding-4"></a>

### Official sources

[Submit](https://pay.weixin.qq.com/doc/v3/partner/4012719997), [business-ID query](https://pay.weixin.qq.com/doc/v3/partner/4012697168), [application-ID query](https://pay.weixin.qq.com/doc/v3/partner/4012697052), [preparation/signing](https://pay.weixin.qq.com/doc/v3/partner/4012062375), [AppID binding](https://pay.weixin.qq.com/doc/v3/partner/4016329059).

<a id="partner-onboarding-5"></a>

### Automao integration

`PartnerRegistry` stores applications and explicitly verified payment relationships. The business site submits, queries and disables; administration independently confirms products. Public APIs do not yet provide rejected-material revision/resubmission. Configuration, identity scope and refund boundaries are defined in the [Automao partner contract](../../../../../Automao/server/externals/wechat/docs/implementation.md#partners).

<a id="membership"></a>

## Membership cards

The adapter implements template create/query/delete, code allocation and user-card invalidation for `marketing/membercard-open/cards`. It is separate from merchant-business-card brand membership, with no automatic product substitution.

<a id="membership-1"></a>

### Identity and operations

Construct `new Membership(authority, channelAccount, brandId)` or `channel.GetMembership(authority, brandId)`. The issuing merchant must have product permission. The channel must be in its Accounts, have Channel type, and actually be a service account with WeChat merchant/brand/AppID association. Local configuration does not establish permission.

Template creation requires matching AppId/Brand.BrandId, brand name, title, description, validity and official membership-media URLs. Onboarding media_id is not a logo URL. Use `DateTimeOffset`, valid fixed intervals and no automatic expiry extension. Nullable integer Quantity is allowed only for system-assigned codes.

All calls use merchant signing and V3 response verification, preserve `/v3/` and escape template/code identifiers. Queries validate template, app and brand. Delete, allocation and invalidation first query ownership, preventing changes to another app/brand's template under the same merchant. Hosts still enforce their own tenant permissions.

`AllocateAsync` returns per-code Allocation with SUCCESS, DUPLICATE, FAIL or future raw status. Missing codes are not successes. Only predeposit mode permits it; reject unexpected codes or duplicate returned entries. Deleting a template stops distribution but does not invalidate already claimed cards; `ObsoleteAsync` invalidates a card by template/code and optional reason. Errors use the common contract, without mutation replay on cancellation/timeout.

<a id="membership-2"></a>

### Automao boundaries

Existing plugins expose `ICardTemplateProvider`. Current `CreateAsync` selects a merchant through tenant/store accounts, then an explicit configured channel. Its argument is JSON `CardTemplateProvider.CreationOptions` with appId, brand, logoUrl and backgroundUrl; brand fields are `brand_id` and `display_name`. Main template fields come from business models.

Provider query, allocation, deletion, update and user-card operations remain unimplemented and explicitly throw `NotImplementedException`, rather than returning false as though handled. Library protocol support does not make those business flows available.

<a id="membership-3"></a>

### Official sources

[Create](https://pay.wechatpay.cn/doc/v3/partner/4012708314), [query](https://pay.wechatpay.cn/doc/v3/partner/4012709168), [delete](https://pay.wechatpay.cn/doc/v3/partner/4012726840), [allocate](https://pay.wechatpay.cn/doc/v3/partner/4012709031) and [invalidate](https://pay.wechatpay.cn/doc/v3/partner/4012726955). Offline checks cover identities, signing, 204 verification, partial imports and errors; live eligibility and distribution require accounts.

<a id="account-credentials"></a>

## Owned-account credentials and tickets

`Account.GetCredentialAsync(refresh, cancellation)` calls stable `POST /cgi-bin/stable_token` for owned official accounts and mini programs supplied by existing configuration or `IAccountProvider`. Component/authorizer tokens and user OAuth credentials use separate APIs.

Ordinary requests use `force_refresh=false`; explicit refresh=true forces rotation, invalidating the old token and consuming limited quota. It is not a per-request routine. Failed/cancelled forced refresh has an uncertain outcome and is not replayed automatically. Ordinary acquisition has bounded backoff only for an explicit WeChat system-busy response; other failures propagate.

<a id="account-credentials-1"></a>

### Caching and concurrency

Resolve cache/lock from `CredentialManager.Cache`/`Locker` or the existing `/Externals/Wechat/Caching` named provider; missing required services fail. Keys isolate owned credentials, account type, AppID and token/ticket type, separately from delegated domains.

An internal JSON envelope holds credential and refresh revision; the distributed cache TTL owns expiry. Consumers neither parse nor write this format. Deduct network time and a small safety margin from the returned remaining lifetime, and conservatively deduct read time. Repeated acquisition cannot extend an existing token to another fixed two hours.

Local keyed waiting and a distributed lock protect refresh. Reread after locking; reuse a newer revision published during waiting, including concurrent forced refresh. Release local coordination objects rather than accumulating AppID locks. Every hit reads shared cache, with no stale local token copy after another node forces rotation.

Cancellation reaches cache, waits, HTTP and backoff. Cancelling a waiter does not cancel the active refresher. After a refresher cancels, later calls can retry acquisition. Lost lease, cache-write failure and malformed response fail rather than returning false success or caching failed tasks.

Channel `GetTicketAsync` separately caches `jsapi` and `wx_card`. Ticket refresh does not force token refresh. Returned period is remaining validity and can bound JS-SDK signatures.

<a id="account-credentials-2"></a>

### HTTP configuration

Without a factory use the shared adapter transport. With `IHttpMessageHandlerFactory`, configure `Wechat.Credentials` with no redirects or automatic retries. The adapter borrows the handler; disposing each client does not dispose the shared handler. Acquisition and internal backoff share a 30-second bound.

```csharp
services.AddHttpClient("Wechat.Credentials")
	.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
	{
		AllowAutoRedirect = false,
		PooledConnectionLifetime = TimeSpan.FromMinutes(5),
	});
```

Never return access_token, AppSecret or ticket-cache contents to clients. Host application sessions are issued separately.

<a id="account-credentials-3"></a>

### Sources and verification

The [stable-token specification](https://developers.weixin.qq.com/miniprogram/dev/server/API/mp-access-token/api_getstableaccesstoken.html) defines ordinary/forced modes, remaining expiry and eligible accounts; old links may redirect to categories. Offline tests cover refresh coalescing, lock rereads, competing nodes, account/ticket isolation, remaining TTL, waiter/refresher cancellation, failures and busy backoff. They do not establish AppID rights, IP allowlisting or remote quota.

<a id="applet-identity"></a>

## Mini program identity and Web boundaries

<a id="applet-identity-1"></a>

### Protocol and server identity

`Applet.LoginAsync(code, cancellation)` calls owned-account `GET /sns/jscode2session` and consumes OpenId, optional UnionId and session key, without official-account profile queries. Missing UnionId remains null. System.Text.Json and Zongsoft JSON exclude `LoginResult.Secret` and `Account.Secret`; account string formatting contains only type/code. The host protects session keys under its own session lifecycle, never in application identities, logs or responses.

`WechatAuthenticatorBase` resolves configured accounts rather than accepting AppSecret embedded in a login key. Existing host authenticators, challengers and credential providers own user lookup, authorization and session issuance.

`GetPhoneNumberAsync(phoneCode, openId, cancellation)` consumes an independent phone-authorization code; supplied OpenId is validated by WeChat. `PhoneInfo` contains PhoneNumber, PurePhoneNumber and CountryCode. Require matching AppId watermark and valid fields. Login/phone codes are not interchangeable and are not automatically replayed.

Protocols borrow `Wechat.Accounts` from `IHttpMessageHandlerFactory`, with redirects/retries disabled; otherwise use a shared no-redirect pool. Basic tokens use separate `Wechat.Credentials` transport.

<a id="applet-identity-2"></a>

### Web integration

- `POST /Externals/Wechat/Applets/{id}/Login?scenario={scenario}` takes plain-text login code, requires id/scenario and limits the body to 4096 bytes. Host `Authentication.AuthenticateAsync` issues/maps an application session rather than returning the protocol DTO.
- `POST /Externals/Wechat/Applets/{id}/PhoneNumber` takes JSON `{ "code": "phone authorization code", "openId": "optional identity" }` and returns structured numbers; it requires an authenticated application identity.
- Both require host ASP.NET policy `Wechat.Accounts`, with the requested Account as resource. Validate account/tenant ownership; trusted host configuration may allow anonymous login for an owned account. Missing/denied policy returns 403.
- Configure the Wechat authenticator, application credential provider and required challengers; installing the API plugin alone cannot issue business sessions.
- No mini program user-directory or channel/applet Credential HTTP endpoints exist. Server code can refresh credentials through the core library.

<a id="applet-identity-3"></a>

### Automao ownership

The Applet table declares enabled `Provider=Wechat` and Type=Applet/Channel. A code must uniquely resolve configuration with TenantId/BranchId. Missing/mismatched types are excluded; ambiguous matches fail. Requery before login issuance so a cached object cannot bypass application disablement.

All three sites identify users by issuing app, OpenId, tenant and branch; disabled accounts receive no identity. UnionId remains protocol information and does not merge users across apps without a modeled open-platform relationship.

Customer login associates phones only through phoneCode redeemed against the currently verified OpenId, and restricts user lookup to the app's tenant/branch. Plain phone input is untrusted. Mainland numbers use PurePhoneNumber; other countries retain PhoneNumber. User, consumer and WeChat account creation occurs in one serializable transaction, without Upsert overwriting other bindings. Repeated login reuses accounts; duplicate bindings reject login.

Existing databases must populate app Type and ownership explicitly. Synthetic transport validates local behavior, not live WeChat access.

<a id="applet-identity-4"></a>

### Official sources

[code2Session](https://developers.weixin.qq.com/miniprogram/dev/server/API/user-login/api_code2session.html) and [phone-code exchange](https://developers.weixin.qq.com/miniprogram/dev/server/API/user-info/phone-number/api_getphonenumber.html).

<a id="channel-services"></a>

## Official-account OAuth, JS-SDK and messaging

<a id="channel-services-1"></a>

### OAuth identity and credentials

`ChannelAuthentication.AuthenticateAsync(code)` exchanges one-time code at `/sns/oauth2/access_token`. Only `snsapi_userinfo` permits `/sns/userinfo`; `snsapi_base` obtains identity without forcing profile retrieval. Nickname, avatar and UnionId may be absent; OpenId is never a UnionId substitute. Profile OpenId and any existing UnionId must match the credential.

Credential contains AppId, OpenId, optional UnionId, Permission, Expiration, remaining Period and optional User. AccessToken/RenewalToken remain server-only and are excluded by both JSON serializers. OAuth tokens do not enter basic-token cache.

- `RefreshAsync(credential)` uses the same AppId/refresh token and validates returned identity; reject other-app credentials.
- `ValidateAsync` calls `/sns/auth`. Successful completion means validation passed; retain WeChat `OperationException.Reason`, without turning transport failure into valid or invalid identity.
- `GetUserInfoAsync` requires userinfo scope, validates identity and allows missing profile fields.

No code-exchange replay occurs; expired refresh tokens require reauthorization. The host creates/binds state before authorization and checks state, one-time use, expected AppId and redirect allowlist before exchange. The library has no browser session and does not own state. Client redirect input is not inherently trusted.

<a id="channel-services-2"></a>

### JS-SDK

`Channel.PostmarkAsync(url)` accepts absolute HTTP/HTTPS page URLs without user information. Preserve original escaped path/query and remove only the first # and following fragment; do not reorder/reencode queries. Return raw SHA-1 bytes, nonce, timestamp and remaining ticket lifetime.

`POST /Externals/Wechat/Channels/{id}/Postmark` accepts at most 4096 bytes of URL text. Apply `Wechat.Accounts`, then `Wechat.JSSDK`, whose resource is `ChannelController.PostmarkContext(Account, Url)`. The host checks configured sites using URI Scheme/Host/Port, never host-prefix matching. Missing/denied policy returns 403. Allowed sites must also match WeChat's configured JS interface domains.

Web returns AppId, NonceStr, Timestamp, Period and lowercase hexadecimal Signature for wx.config; it does not return the ticket.

<a id="channel-services-3"></a>

### Web login, users and templates

`POST /Externals/Wechat/Channels/{id}/Login?scenario={scenario}` takes code and uses the host authentication chain/account policy, returning an application session, not OAuth credentials. The policy or trusted callback entry must already validate state.

| Endpoint | Behavior |
| --- | --- |
| `GET /Externals/Wechat/Channels/{id}/Users/{identifier?}` | Follower OpenId pagination without identifier, with bookmark query/X-Bookmark response header; profile lookup when identifier is present |
| `GET /Externals/Wechat/Channels/{id}/Messager/Templates` | Selected templates |
| `POST /Externals/Wechat/Channels/{id}/Messager/Send/{destination}?template={template}&url={optionalUrl}` | Template fields in a body limited to 65536 bytes |

User/message endpoints require authenticated identity and owning-account policy. Bind explicit id from the route rather than a default channel. Fields accept flat values or JSON `{ "value": ... }`, preserving numeric semantics and omitting obsolete color data. Message ID means submission acceptance, not delivery. Retain provider errors without automatic resending.

Wechat `ITransmitter` uses `officialAccountAppId:OpenId` destinations and Message or empty channel; reject unknown channels. Await account lookup and HTTP with cancellation. These protocols use `Wechat.Accounts` without redirects/retries; OAuth, followers and templates each require their own account permissions. Mini program subscription messages are not included.

<a id="channel-services-4"></a>

### Official sources

[OAuth](https://developers.weixin.qq.com/doc/service/guide/h5/auth.html), [JS-SDK](https://developers.weixin.qq.com/doc/service/guide/h5/jssdk.html), [template sending](https://developers.weixin.qq.com/doc/service/api/notify/template/api_sendtemplatemessage) and [template listing](https://developers.weixin.qq.com/doc/service/api/notify/template/api_getalltemplates).

<a id="platform-cryptography"></a>

## Public-platform message cryptography

`Platforms.CryptographyUtility` implements public/open-platform AES-256-CBC and SHA-1 independently of payment APIv3 GCM. Algorithms and public vectors follow the [message encryption specification](https://developers.weixin.qq.com/doc/service/guide/dev/push/encryption.html).

<a id="platform-cryptography-1"></a>

### Usage contract

1. The receiver limits body/field sizes and selects Token, EncodingAESKey and expected AppId from trusted configuration, not unverified message-selected keys.
2. Pass original timestamp, nonce and Encrypt to `VerifySignature(token, timestamp, nonce, ciphertext, signature)` and stop on failure. Sign ordinally sorts the four fields and returns lowercase SHA-1 hex; verification compares digests in fixed time.
3. After verification, use `Decrypt(ciphertext, encodingAESKey, appId)` or `DecryptBytes`. Expected AppId is required and must byte-match the frame's UTF-8 receiver suffix.
4. The receiver still owns timestamp/replay protection, event semantics, inner identity and persistence. Decryption does not acknowledge business acceptance. Cryptographic methods access no network, cache or database and do not acknowledge callbacks.

Encrypt accepts string or ReadOnlySpan<byte>, returns Base64, and uses a fresh cryptographic 16-byte prefix. Message length is a four-byte big-endian byte count. IV is the first 16 key bytes; PKCS#7 padding uses a 32-byte block, adding a full block on exact alignment.

EncodingAESKey is canonical 43-character Base64 without trailing equals and decodes to 32 bytes. Validate ciphertext block length, every padding byte, nonnegative in-bounds message length and receiver. Empty messages are allowed, empty receivers are not. String APIs use strict UTF-8; use bytes for arbitrary binary messages, without replacement-character masking.

Invalid configuration raises ArgumentException/derived types with parameter names; invalid Base64 raises FormatException; bad frames/padding/receiver raise CryptographicException; invalid UTF-8 raises encoding errors. HTTP responses do not disclose cryptographic detail. Malformed/mismatched signatures return false.

There is no receiver-unchecked decrypt API or duplicate unimplemented CBC class. Temporary plaintext/key buffers are cleared after use; callers still protect configuration strings and returned messages.

<a id="component-platform"></a>

## Third-party platform protocols

`Platforms.Component` owns one component context separately from owned accounts and payment merchants. `/Externals/Wechat/Components` configures Name (component_appid), Secret, callback Token and a 43-character EncodingAESKey. `ComponentManager.Find(appId)` reads current configuration and resolves host `IComponentStore`; unknown configuration returns null, missing storage fails. Never return configuration/server credentials to clients.

<a id="component-platform-1"></a>

### Authorization events

`POST /Externals/Wechat/Components/{componentAppId}/Events` accepts secure-mode XML (`text/xml` or `application/xml`) up to 256 KiB. Configure this independently of payment and delegated business-message routes.

Require unique timestamp, nonce and msg_signature; permit signature, encrypt_type=aes and openid. Enforce five-minute UTC skew. Use the four-field msg_signature, not the three-field signature substitute. Verify before decrypting against the configured component AppId, and check inner AppId too. Disable DTD/external entities and reject duplicate/nested fields or unsupported events.

Supported events are component_verify_ticket, authorized, updateauthorized and unauthorized. SHA-256 of canonical verified semantic fields creates the event ID, unchanged by reencryption or transport nonce changes. Value holds ticket/code; PreauthorizationCode correlates the grant. Both are excluded from public JSON.

`IComponentStore.ReceiveAsync` atomically persists the event and ticket/authorization changes, idempotently by component/event ID. Revocation immediately disables authorization. Late events cannot restore newer state; equally timed conflicting events conservatively reject restoration. Retain the latest valid ticket. Duplicate is valid only after a durable existing record; otherwise return Rejected or fail. In-memory stores are test-only.

Accepted/Duplicate returns HTTP 200 with success; persistence failure, rejection or the four-second deadline returns 503. Signature/message errors return 400, unknown components 404, and size limits may return 413. Defer slow code exchange until after durable receipt. Expired authorization-code events still record state; do not retry expired codes indefinitely.

<a id="component-platform-2"></a>

### Component tokens and authorization exchange

`GetCredentialAsync` uses shared cache/lock domain `Zongsoft.Wechat.Component:{componentAppId}:access-token`. On expiry, obtain the same component's ticket from within the last twelve hours through `IComponentStore.GetTicketAsync`, then call `/cgi-bin/component/api_component_token`. Use returned expiry with a refresh margin. stable_token cannot obtain component credentials.

`Wechat.Components` transport borrows factory ownership and disables redirects; without custom transport use the shared handler. No automatic protocol replay occurs. Cancellation and official error codes propagate.

`PreauthorizeAsync` calls `api_create_preauthcode?access_token=...` and honors expires_in. `Preauthorization.GetUrl` builds PC/new H5 authorization URLs for official accounts (1) or mini programs (2), requiring HTTPS redirect without user information/fragment and an unexpired preauthorization code.

Before returning a URL, persist owner/tenant, component, preauthorization code, allowed redirect, one-time state and expiry. Browser callbacks and authorization events correlate with that record; browser AppId/tenant input cannot establish a relationship by itself.

`AuthorizeAsync(code, expectedAuthorizerAppId)` calls `api_query_auth?access_token=...`, checks returned AppId and retains actual func_info. Accounts without API permission may have no tokens; a returned access token requires its refresh token and expiry. Models do not serialize tokens to clients. Hosts durably persist credentials/relations after exchange. Management APIs do not directly expose this server exchange or replay uncertain one-time codes.

<a id="component-platform-3"></a>

### Authorizer credentials and delegated calls

The same host store implements `IComponentAuthorizationStore` for authoritative snapshots and conditional updates. Snapshots contain component, authorizer, type, Enabled, actual Permissions, GrantId, Revision and credentials. Grants, permission changes and revocations change both GrantId and Revision; token refresh changes only Revision. Reads must reflect current revocation, without stale business-cache reactivation.

`UpdateCredentialAsync` atomically checks component, authorizer, Enabled and old Revision before saving access/refresh tokens, expiry and new Revision. Success means durable persistence, not an in-memory update. JSON excludes server tokens; protect databases/backups under the host credential-storage policy.

Read tokens directly from authoritative storage rather than another stale token cache. Expired/forced refresh uses component/authorizer lock keys, local coalescing and a distributed lease. Reread within the lock; after `api_authorizer_token`, validate the lease and conditionally save using the old revision. Once rotated credentials arrive, persistence uses cancellation independent of the caller, then observes late cancellation. Revocation/new grants make the update fail; stale results cannot restore authorization. Unknown outcomes are not retried automatically.

`Component.GetAccountAsync/GetAppletAsync/GetChannelAsync` return only available authorizers. Delegated Account identity includes ComponentId and has no authorizer AppSecret. Dynamic providers may return it; managers do not cache delegated instances, and existing instances recheck authorization per call. Owned accounts retain their cache contract; never mutate an owned account into another tenant's delegated account.

`GetAuthorizerInfoAsync` identifies official accounts and ordinary/trial mini programs, preserving permissions and account state. Other products are not assumed to be official accounts. Status 1 means normal; missing means unknown. Hosts combine status/type with the original authorization request; fetching a profile is not business authorization.

| Existing feature | Permission | Credential |
| --- | --- | --- |
| Mini program login/phone | 18 | Component token for login; authorizer token for phone |
| Official-account users | 2 | Authorizer token |
| Official-account OAuth/JS-SDK | 4 | Component-specific OAuth exchange/refresh and component token; authorizer token for tickets |
| Template messages | 7 | Authorizer token |
| wx_card ticket | 8 | Authorizer token |

OAuth credentials bind both component and authorizer and cannot interchange with owned-account credentials. Ticket cache keys contain component, authorizer, GrantId and ticket type; recheck authorization after cache reads so old grants cannot survive revocation/reauthorization. Preserve unknown permissions without granting existing features. Hosts own tenant isolation, request consumption and local unlinking.

<a id="component-platform-4"></a>

### Official sources

[Secure message push](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/Before_Develop/message_push.html), [tickets](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/Before_Develop/component_verify_ticket.html), [grant events](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/Before_Develop/authorize_event.html), [component token](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getcomponentaccesstoken.html), [preauthorization code](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getpreauthcode.html), [authorization exchange](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getauthorizerrefreshtoken.html), [authorizer token](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getauthorizeraccesstoken.html), [authorizer profile](https://developers.weixin.qq.com/doc/oplatform/openApi/authorization-management/api_getauthorizerinfo.html), [delegated mini program login](https://developers.weixin.qq.com/doc/oplatform/openApi/miniprogram-management/login/api_thirdpartycode2session.html), [delegated OAuth](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/Before_Develop/Official_Accounts/official_account_website_authorization.html) and [authorization flow](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/Before_Develop/Authorization_Process_Technical_Description.html). Local disablement is not formal WeChat-side revocation by the merchant.

<a id="component-platform-5"></a>

### Web management entry

`POST /Externals/Wechat/Components/{componentAppId}/Authorizations` accepts Context, RedirectUri, AccountType (Channel/Applet) and optional Mobile. Require authenticated identity and NameIdentifier. Host resource policy `Wechat.Components` checks business context, component, type and redirect site; missing policy denies access. Context is host-interpreted scope, not automatically trusted tenant input.

`BeginAuthorizationAsync` obtains preauthorization, generates 32 random state bytes and adds state to the HTTPS redirect, rejecting an existing state parameter. `IComponentStore.CreateAuthorizationAsync` persists unique state with Owner, Context, component, preauthorization code, type, redirect and expiry. Return URL/expiry only after success; persistence failure returns 503 without a URL. This is not completed authorization.

Events correlate by PreAuthCode with the server request, then hosts persist authorizer information/credentials. Browser returns can only query/confirm their own record. Atomically validate one-time state, actual authorizer, event ordering and permission changes. No Web exchange endpoint exposes server credentials.

<a id="component-platform-6"></a>

### Automao integration

`ComponentStore` persists tickets, requests, events and authorizer credentials through registered services/Gateway workers. The business site has tenant-scoped management and server-configured redirects. See [Automao authorization](../../../../../Automao/server/externals/wechat/docs/implementation.md#authorizations) for state, key rings, recovery and endpoints.

<a id="deployment"></a>

## Runtime composition and database schema

<a id="deployment-1"></a>

### Assemblies and references

The main, Web and Gateway packages use version 7.10.0 together and target .NET 8/9/10. Core 7.72.0 is required; Web/Gateway additionally require Web 7.20.0. Hosts provide foundational assemblies rather than duplicating them in adapter deployment manifests.

Debug uses current-tree Core/Web HintPaths and Web/Gateway ProjectReferences to the main library. Release uses those minimum NuGet versions and main package 7.10.0. An old package cannot substitute for an unpublished dependency; Release package builds require the selected source to contain those versions. Source builds/offline checks are not NuGet publication or deployment.

Automao local references are controlled by `ZongsoftFrameworkPathReferenced`/`ZongsoftFrameworkPath`; NuGet fallback requires main 7.10.0 too. Web deploys Web 7.10.0; Gateway adds Gateway 7.10.0. [Gateway documentation](../gateway/README.md) defines registration. Automao owns its mappings/business manifests; host startup contains no payment logic.

<a id="deployment-2"></a>

### Configuration and public entries

- Generate environment settings from the `.option` template and replace `https://callbacks.example.invalid/...` with real HTTPS endpoints. A template with missing key files is not a usable merchant.
- Configure signing identity, V2/V3 keys and WeChat verification separately, with explicit provider, submerchant, app and permitted relationships.
- Login returns host sessions and requires `Wechat.Accounts`; JS-SDK also requires `Wechat.JSSDK`. Missing policies deny access. Users, messages and phone operations additionally require login.
- Server credentials use shared cache/locks and separate basic/jsapi/card domains. Never serialize session_key, OAuth tokens or merchant material to clients.
- Use explicit callback product routes and acknowledge only reliable acceptance. Third-party authorization and payment callbacks are independent protocols and grants.

<a id="deployment-3"></a>

### Database schema

Automao's [WeChat schema script](../../../../../Automao/documents/database/automao.wechat-mysql.sql) defines seven tables: the notification inbox, component tickets, authorization requests, authorization events, authorization snapshots, ordinary partner applications and payment bindings. See the [WeChat schema design](../../../../../Automao/documents/database/automao.wechat.md) for fields and indexes. Data mappings are deployed with the Automao WeChat plugin.

The [Common script](../../../../../Automao/documents/database/automao.common-mysql.sql) defines nullable `TenantAgreement.BusinessLicenseType` as `varchar(50)` and `AgreementToken` as `varchar(256)`. The [Paying script](../../../../../Automao/documents/database/automao.paying-mysql.sql) defines nullable `Paying_PaymentDetail.PaymentToken` as `varchar(64)`. Keep the corresponding [Common](../../../../../Automao/documents/database/automao.common.md) and [Paying](../../../../../Automao/documents/database/automao.paying.md) schema designs synchronized. Development Pods load these complete scripts during initialization; integration fixtures create tables and synthetic business data only in their own isolated databases.

<a id="deployment-4"></a>

### Verification boundary

Framework/local HTTP tests cover protocols and endpoints; Automao fixtures use actual plugins, synthetic signed responses and isolated persistence. Accounts, product rights, devices, public callbacks and settlement require separate live validation.

Automao third-party authorization uses an absolute persistent Data Protection key-ring directory shared by the corresponding sites/instances with restricted file access. Files have no additional certificate/DPAPI encryption. Persist the key ring together with credential data; deleting it prevents decryption of stored credentials. Ordinary partners require an independent Authorities allowlist; business/admin sites deploy their corresponding Web plugins of the same name. See [Automao WeChat](../../../../../Automao/server/externals/wechat/README.md).

<a id="testing"></a>

## Verification

This contract defines offline protocols and Automao business integration; see the [capability matrix](#capability-matrix) for scope. Accounts and cloud servers are not prerequisites for offline development.

<a id="testing-1"></a>

### Build and test entry points

From `D:\Zongsoft\framework`, build local Core/Web dependencies before the WeChat solution; stale HintPath outputs cannot validate current source.

```powershell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -c Debug -p:GeneratePackageOnBuild=false
dotnet build Zongsoft.Web/src/Zongsoft.Web.csproj -c Debug -p:GeneratePackageOnBuild=false
dotnet build externals/wechat/Zongsoft.Externals.Wechat.slnx -c Debug -p:GeneratePackageOnBuild=false

dotnet test externals/wechat/test/Zongsoft.Externals.Wechat.Tests.csproj -c Debug --framework net8.0 --no-build --no-restore
dotnet test externals/wechat/test/Zongsoft.Externals.Wechat.Tests.csproj -c Debug --framework net9.0 --no-build --no-restore
dotnet test externals/wechat/test/Zongsoft.Externals.Wechat.Tests.csproj -c Debug --framework net10.0 --no-build --no-restore
```

Continue only after each command succeeds. xUnit v3 uses VSTest through Microsoft.NET.Test.Sdk and the Visual Studio adapter; MTP is not enabled. Each framework needs its actual runtime; SDK installation or roll-forward is not equivalent validation. Shared configuration/project files are authoritative for versions and runner.

Each test project ProjectReferences only its own production project, with other framework libraries referenced through built HintPaths and explicit third-party dependencies. Tests do not reference Plugins or read production configuration/credentials. Run the same three-framework commands for `externals/wechat/api/test/Zongsoft.Externals.Wechat.Web.Tests.csproj` and `externals/wechat/gateway/test/Zongsoft.Externals.Wechat.Gateway.Tests.csproj`. Gateway tests assemble actual controllers/local Kestrel with controlled acceptance collaborators; main-library tests cover cryptographic verification.

<a id="testing-2"></a>

### Existing offline coverage

| Test | Contract demonstrated | Not established |
| --- | --- | --- |
| [Payments](../test/AccountManagerAsyncTest.Payments.cs), [queries](../test/AccountManagerAsyncTest.PaymentQueries.cs) | Both roles/three scenarios, payer/app identity, launch signing, QR, encoding and unknown states | Live permission, devices or payment |
| [FallbackTest](../test/FallbackTest.cs) | Raw signatures, merchant/app/events, GCM, limits, failure and cancellation | Delivery or host business persistence |
| [AuthorityTest](../test/AuthorityTest.cs) | XML option binding, material roles, identity, secret exclusion, borrowed ownership and file rotation | Remote recognition or host deployment |
| [Certificates](../test/AccountManagerAsyncTest.Certificates.cs) | Verified/encrypted refresh, atomic rotation, earlier retirement, rate limits and cancellation isolation | General response/notification/live download coverage |
| [HTTP](../test/AccountManagerAsyncTest.Http.cs) | Exact signing bytes, encoding, empty POST, multipart, 204, invalid inputs/errors and bank headers | All product state or live behavior |
| [HTTP lifecycle](../test/AccountManagerAsyncTest.HttpLifecycle.cs) | No retry, disposal, limits, trusted refresh, content ownership, destinations and snapshots | Arbitrary custom handlers or business persistence |
| [AccountManagerAsyncTest](../test/AccountManagerAsyncTest.cs) | Provider waits, Get/Find, completed caching, failure/cancellation/concurrency | Actual login, remote permissions or distributed infrastructure |
| [PaymentRequestTest](../test/PaymentRequestTest.cs) | Valid integer-fen serialization, integer units and currency fields | All invalid-input or transport behavior from this test alone |
| [Refunds](../test/AccountManagerAsyncTest.Refunds.cs) | Roles, amounts, ownership, encoding and response/notification distinction | Settlement |
| [RefundmentRequestTest](../test/RefundmentRequestTest.cs) | Identifiers and original/payer/refund amount distinctions | Verification or durable receipt |
| [CryptographyHelperTest](../test/CryptographyHelperTest.cs) | Fixed GCM/UTF-8/Base64 vectors, empty inputs, key/nonce/tag lengths, tampering/truncation and 32 synthetic accounts | Proven notification/certificate source |
| [ProtocolCryptographyTest](../test/ProtocolCryptographyTest.cs) | V2 MD5/HMAC vectors, ordinal/invariant formatting and concurrency | Proven payment source |
| [Channel signing](../test/AccountManagerAsyncTest.Channel.cs) | Concurrent SHA-1 against cached account tickets | Full site policy or ticket lifecycle |

Models use public DTOs and System.Text.Json rather than copies of internal HTTP configuration. Synthetic callback strings are never sent. Nonempty GCM/CBC vectors were independently generated with Python cryptography, hash expectations with hashlib/hmac, and empty plaintext follows [.NET's NIST AES-256-GCM case 13](https://github.com/dotnet/runtime/blob/main/src/libraries/Common/tests/System/Security/Cryptography/AesGcmTests.cs). Tests do not require Python or runtime downloads. Internal V2 signing is exercised through reflection without widening production visibility; callers own HMAC objects.

Certificate tests use named IHttpClientFactory; payment tests use named IHttpMessageHandlerFactory and execute real signing/verification. Unmatched requests fail instead of reaching WeChat. Keys are generated in memory and temporary files cleaned. [Automao AuthorityAccountTest](../../../../../Automao/server/externals/wechat/abstraction/test/AuthorityAccountTest.cs) checks separate API keys, default app, snapshot isolation and asynchronous file release. GCM authentication is not source verification; CBC receiver checks are not timestamp/replay protection. Offline tests do not replace `/v3/security/echo` or real transactions.

Offline-face tests cover provider identity, dedicated products/queries, optional amounts, unknown state, contracts, encryption and 204. Automao agreements.py checks initial/repeated/late signatures, account status and revocation; PaymentProviderTest.Offline verifies persisted product selection.

<a id="testing-3"></a>

### Test projects and fixtures

Production/test siblings are src/test, api/src–api/test and gateway/src–gateway/test. The solution contains all six; the Cake test glob matches three test projects. Core tests cover models/protocols; API tests cover actual local ASP.NET routes, policy and secret exclusion; Gateway tests cover body/handler/response boundaries with protocol verification in core tests. Automao separately verifies actual host/plugin/persistence integration.

Use fixed independent crypto vectors; check wrong keys/tags/data/padding/truncation and concurrent calls through the same public protocol. Controlled handlers capture actual bytes and reject unmatched requests. Time/cache/lock tests use existing narrow contracts and synchronization signals, not long sleeps. Assert no handler call after failed identity and no success acknowledgment after failed reliable acceptance. Track stream/content/response ownership; private nested helpers do not justify public production members. Tests mutating ApplicationContext.Current/static providers use nonparallel collections and restore state. Plain model tests do not require that context.

<a id="testing-4"></a>

### Automao entry points and dependencies

Scripts, database design and HttpYac requests live in [D:\Automao\documents](../../../../../Automao/documents), with [database scripts](../../../../../Automao/documents/database). The application environment is [automao.pod-mysql.yaml](../../../../../Automao/server/automao.pod-mysql.yaml). [notifications.py](../../../../../Automao/server/externals/wechat/integration/notifications.py) uses actual Gateway/plugins and isolated databases to verify persistence, concurrent deduplication and recovery, including `wechat-notifications.http`. Read the [fixture](../../../../../Automao/server/externals/wechat/integration/README.md) and [site-request instructions](../../../../../Automao/documents/http/sites/README.zh-Hans.md) first.

| Scenario | Entry | Evidence/prerequisites |
| --- | --- | --- |
| Authentication | `sites/authentication.http` | Dedicated identity and exact host/port/scenario; inject secrets locally |
| Provider/business operations | `sites/business/paying/payment/payment.http`; `GET /Paying/Payments/Providers`, `POST /Paying/Payments/Pay/{voucher}`, `POST /Paying/Payments/{key}/Refresh`, `POST /Paying/Payments/{key}/Cancel` | Resolve real provider/accounts; provider mutations are not default read-only requests |
| Refund | Same payment file and `sites/business/paying/refundment/refundment.http` | Dedicated paid order, original ownership, amount/identifier and acceptance/final-state distinction |
| Total isolation | `sites/{business,customer}/paying/total-isolation.http` | Two tenants/users with same-named credentials and expected amounts; no payment |
| Detail isolation | `sites/{administration,business,customer}/paying/detail-isolation.http` | Dedicated records/keys and site-specific permissions |
| Notifications | Gateway `/Externals/Wechat/Fallback/{payment|refundment}/{merchant}` | Synthetic signed data, real handlers and persisted results |

Trace site PaymentController → business service → Wechat IPaymentProvider → adapter; refunds use IRefundmentProvider; notifications call OnPaidAsync/OnRefundedAsync through real handlers. Verify final records, not merely endpoint completion. `Automao.Paying.Providers.Wechat` depends on Paying and Externals.Wechat; its Gateway plugin registers Payment/Refundment and the external module adds face handlers.

Before integration, establish isolated host/tenant/orders and exact sites (documented defaults: administration/business/customer 5001/5002/5003; Gateway from actual configuration). Database, Redis and required daemon must be available; uploads additionally need object storage. Ports/containers alone are not authenticated health checks. Confirm current local adapter/business outputs via framework-path properties and build Automao Core, Common, Paying, Externals.Wechat and Security as required. Use dedicated identities and scope cleanup to this run, without production data or admin substitution for customer permissions.

Controlled `Wechat.Payments` and optional `Wechat.Certificates` replace only transport; real signing, plugins, authorization and persistence remain. Live checks additionally require accounts, merchant/product eligibility and public callbacks configured for the relevant HTTPS/domain/IP rules. Missing prerequisites cannot be replaced by synthetic success. Select HttpYac cases by tag or name; never combine --all and --tag. Report builds, protocol tests, local HTTP, Automao plugins and live WeChat separately.

`modules/paying/providers/wechat/test/Automao.Paying.Wechat.Contract.Tests.csproj` checks the real provider with synthetic accounts/responses; it is not a plugin-host test. PaymentStateTest/PaymentFeatureAsyncTest cover original-state conditions, terminal protection and Token mapping. Ticket tests cover roles, pending/unknown outcomes, V2 XML/signature/identity rejection, cancellation, reversal certificate/recall and face initialization. Automao adds argument propagation, uncertainty, necessary cancellation persistence, awaited providers, combined-payment authorization and terminal races. Refund fixtures verify serializable reservations, original-order retry and partial allocation through actual plugins. Run fixtures serially with isolated databases and Redis database 14; no WeChat calls or real TLS proof.

<a id="testing-5"></a>

### Onboarding, banks and media

`AccountManagerAsyncTest.Merchants.cs` checks application ownership, sensitive fields, dates, JSON arrays, multiple bank candidates, empty-page totals, MIME/digests/meta and size limits. Serial Automao onboarding.py checks original lookup, pending signature, complete links, finish/freeze/unknown state and ownership; the fixture initializes its isolated database from the current schema scripts.

<a id="testing-6"></a>

### Membership

`AccountManagerAsyncTest.Membership.cs` checks V3 paths/signatures, merchant channel selection, template/app/brand isolation, 204, individual allocation outcomes and unsigned-response rejection. Targeted Automao provider builds validate direct calls; unimplemented business operations are not accepted as available.

<a id="testing-7"></a>

### Owned stable credentials

`AccountManagerAsyncTest.Credentials.cs` covers local coalescing, rereads after competing publication, forced-refresh deduplication, namespaces, remaining TTL, cancellation, lost leases, failed cache writes and busy backoff. It does not replace actual distributed-cache-driver integration.

<a id="testing-8"></a>

### Mini program identity

`AccountManagerAsyncTest.Applet.cs` covers optional UnionId, secret exclusion, watermark/OpenId binding, errors and cancellation. api/test exercises real loopback controllers for account policies, host sessions, anonymous phone rejection and absent credential/user-directory routes. Automao identity.py loads the real customer authenticator with isolated MySQL and controlled responses, checking app/tenant/store ownership, untrusted raw phone input, verified association, repeated login and disabled accounts/apps. Build that site and integration host first. Existing phone uniqueness remains enforced: cross-tenant conflicts cannot create or rebind users.

<a id="testing-9"></a>

### Official-account protocols

OAuth.cs covers scope, missing profiles/UnionId, identity rejection, refresh/validation errors, original URL fragments, empty follower pages and template fields/errors. AccountHttpTest uses Zongsoft nested-controller discovery for sessions, routes, account/site policies and signature JSON. Browser state, delivery and live permissions require separate host/WeChat validation.

PlatformCryptographyTest uses official SHA-1/CBC and independent Chinese vectors for signatures, keys, receivers, full padding, negative/out-of-range lengths, strict UTF-8 and binary messages. Component tests cover token domains, concurrent refresh, expired tickets/codes, AppID/permissions, XML duplicates/signature/time/receiver/limits and durable-result responses. Web tests return URLs only after saved state; actual idempotency requires Automao storage. Authorizer tests cover durable rotation, late cancellation, revoke/new-grant races, lease loss, persistence failure, permissions/types, OAuth and GrantId ticket isolation.

<a id="testing-10"></a>

### SaaS authorization and ordinary providers

Components.cs/Authorizers.cs cover tickets/codes, real account metadata, isolated credentials, permissions, delegation and revocation races. Partners.cs checks nested fields, all encrypted sensitive fields, consistent signing material, 64-bit IDs and query ownership; onboarding completion is not all-product permission.

Automao components.py loads actual business APIs, customer authentication and Gateway to test tenancy, encrypted persistence/key rings, concurrency, restart/recovery and changed permissions. partners.py runs business/admin plugins separately and checks ordinary applications, platform confirmation, product gating and payment/refund links. They reuse WeChat management reads under `D:\Automao\documents\http\sites` through HttpYac, serially with the five other suites. Controlled WeChat responses and dedicated test permission decisions do not establish real test lists, merchant signatures, AppID binding or settlement.

<a id="saas-development-and-test-accounts"></a>

## SaaS accounts and test environments

Automao combines two independently authorized relationships: merchants delegate official accounts/mini programs to a third-party platform, and a payment service provider manages submerchants. Keep identities, permissions, credentials and callbacks separate within existing plugins. The [plan](../.plan.md) owns sequencing and status.

<a id="saas-development-and-test-accounts-1"></a>

### Individual development

| Account/environment | Individual access | Scope and limits |
| --- | --- | --- |
| Offline protocols and isolated Automao | No WeChat account needed | Builds, vectors, controlled responses, plugins, tenants and persistence; not live integration |
| Official-account API test account | Developer WeChat QR registration, without a formal official account first | Only console-enabled APIs; not payment/all verified-service-account permissions. [Apply](https://mp.weixin.qq.com/debug/cgi-bin/sandbox?t=sandbox/login) |
| Mini program test account | Developer WeChat QR registration | Tools, device preview and allowed APIs; test AppID is not payment permission. [Guide](https://developers.weixin.qq.com/miniprogram/dev/devtools/sandbox.html), [apply](https://mp.weixin.qq.com/wxamp/sandbox?doc=1) |
| Individual third-party test platform | Register an individual open-platform account, then one platform-type third-party test platform | Authorization/server/delegation testing; no customized platform, review/global publication, provider components, standard-template review or mini program registration on behalf of others. [Rules](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/getting_started/how_to_test.html) |
| Payment service provider | Official ordinary-provider admission lists companies, government, institutions and social organizations, excluding individuals/sole proprietors | An eligible entity and product rights are required for live provider/submerchant payments; open-platform testing is no substitute. [Requirements](https://pay.wechatpay.cn/doc/v3/partner/4012081990) |

Unpublished platforms accept only accounts on their authorization test list; inspect actual console eligibility rather than assuming every test account can authorize. A personal official account is not a verified service account. See [publication/testing](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/operation/thirdparty/publish.html).

The V2 simulator covers documented payment-code cases and still requires a merchant number/signature to obtain its test key. It is not a credential-free individual V3 sandbox. Direct mini program payment needs a verified mini program, merchant product rights and AppID binding; partners also need submerchant product authorization. See [V2 simulation](https://pay.wechatpay.cn/doc/v2/merchant/4011984810), [direct preparation](https://pay.wechatpay.cn/doc/v3/merchant/4015459512) and [partner preparation](https://pay.wechatpay.cn/doc/v3/partner/4012076731).

<a id="saas-development-and-test-accounts-2"></a>

### Account preparation requirements

Complete offline work first; account applications and public callback hosting can wait until live validation. Developers use their own WeChat for test-account QR steps and retain account ownership; passwords, codes and AppSecret never enter chat, source control or logs.

[Open-platform registration](https://open.weixin.qq.com/wxaopen/regist/index) needs an eligible unused email, password setup, email activation, identity information and personal QR participation under the [registration guide](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/operation/open/create.html). Configure controlled callbacks, test lists and permissions, with secrets in server configuration. Verify actual console capabilities and explicitly leave missing ones unvalidated. Formal qualification, publication and payment-provider admission use real entity/product requirements, not borrowed identities or invented material.

<a id="saas-development-and-test-accounts-3"></a>

### SaaS feature boundaries

Existing account, payment, refund, face, membership and ecommerce services retain their individual validation limits. SaaS includes ordinary submerchant relationships, third-party events/tokens, delegated calls and tenant management, with ecommerce remaining separate. Template upload, review/release, registration on behalf of others, provider components and complete agency operations need independent scope and eligibility. Delegated calls use granted permissions and stop after revocation; account grants do not imply payment rights, AppID bindings or submerchant ownership.

<a id="saas-development-and-test-accounts-4"></a>

### Automao HTTP entry points

Requests reside in [Automao/documents/http](../../../../../Automao/documents/http), not server/documents/http. Read [site instructions](../../../../../Automao/documents/http/sites/README.zh-Hans.md) and reuse payment/refund/isolation requests under sites/{administration,business,customer}/paying. Existing total/detail tests do not trigger third-party payments; provider operations have their own prerequisites. Add only missing cases and do not turn a default read-only batch into financial operations.

<a id="platform-reference"></a>

## Official platform references and eligibility

The source reference audit is dated 2026-10-22. This section describes official platform contracts and product boundaries, including products not implemented here. Account type, granted permissions, merchant products and application bindings control availability; there is no single version spanning all WeChat products.

<a id="platform-reference-1"></a>

### Documentation entry points and products

| Product | Official entry | Boundary |
| --- | --- | --- |
| Subscription accounts | [Documentation](https://developers.weixin.qq.com/doc/subscription/guide/) | Service-account permissions do not apply to every official account |
| Service accounts | [Guide](https://developers.weixin.qq.com/doc/service/guide/dev/), [APIs](https://developers.weixin.qq.com/doc/service/api/) | Messages, menus, media, drafts, publishing, users, OAuth and JS-SDK have separate eligibility |
| Mini programs | [Server APIs](https://developers.weixin.qq.com/miniprogram/dev/server/API/) | Login, phone, subscriptions and transaction management have separate contracts |
| Open platform | [Introduction](https://developers.weixin.qq.com/doc/oplatform/open/intro.html), [third-party APIs](https://developers.weixin.qq.com/doc/oplatform/openApi/) | Binding applications is different from granting a third-party platform access |
| Payments | [Direct V3](https://pay.weixin.qq.com/doc/v3/merchant/4012081606), [partner V3](https://pay.weixin.qq.com/doc/v3/partner/4012081673) | Model direct merchants, providers, submerchants and products separately |

WeCom, WeChat stores, Channels and mini games are independent domains. Open-platform app binding may enable conditional UnionID association; it does not share all APIs or tokens. Legacy offiaccount, mini program OpenApiDoc and some Third-party_Platforms/api URLs redirect to categories or disappear. Locate the actual current interface from these entries; HTTP 200 alone is not evidence that an old URL still documents it.

<a id="platform-reference-2"></a>

### WeChat Pay APIv3

<a id="platform-reference-3"></a>

#### Authentication, verification and keys

V3 uses REST/JSON, SHA256-RSA request signatures and AES-256-GCM encrypted callback resources. Official guidance supports public-key and platform-certificate verification and recommends public keys. Merchant private key/certificate serial sign requests and applicable client launches; APIv3 key decrypts resources; WeChat public key/ID or platform certificate verifies WeChat and encrypts specified sensitive fields. Public-key mode does not remove merchant signing identity. See [integration modes](https://pay.weixin.qq.com/doc/v3/partner/4012081931), [required parameters](https://pay.weixin.qq.com/doc/v3/partner/4013080340) and [public-key switching](https://pay.weixin.qq.com/doc/v3/merchant/4012154180).

Verify raw, unreconstructed response/notification bodies with timestamp, nonce, signature and serial headers. Timestamp, nonce and body are LF-separated and LF-terminated, including empty 204 bodies. SIGNTEST probes must fail normal verification rather than receive an exception. Bill-file downloads use returned hashes instead of ordinary JSON response verification. [Verification rules](https://pay.weixin.qq.com/doc/v3/merchant/4013053249)

Encrypt only fields required by each API using WeChat material identified by Wechatpay-Serial, not every string indiscriminately. [Sensitive fields](https://pay.weixin.qq.com/doc/v3/merchant/4013053257)

<a id="platform-reference-4"></a>

#### Payments, refunds and callbacks

Direct JSAPI/mini program creates at `/v3/pay/transactions/jsapi`; client launch is a separate contract from creation and settlement. Direct mode uses mchid/appid; partner mode uses sp_mchid/sub_mchid/sp_appid/sub_appid and the corresponding payer identity, signing with provider material rather than replacing merchant numbers arbitrarily. [JSAPI](https://pay.weixin.qq.com/doc/v3/merchant/4012791856), [mini program](https://pay.weixin.qq.com/doc/v3/merchant/4012791897)

`POST /v3/refund/domestic/refunds` uses integer fen and independent out_refund_no. Reusing that number addresses the same refund; partial refunds need distinct numbers and cumulative original-amount bounds. Acceptance is not settlement, which requires queries/notifications. [Refund specification](https://pay.weixin.qq.com/doc/v3/merchant/4012552567)

Notifications can repeat. Official general guidance requires an answer within five seconds, successful acknowledgment for processed duplicates, and failure HTTP status for invalid signatures. notify_url is public HTTPS without query parameters. Queries backstop notification delivery; client success is not authoritative. [Callback requirements](https://pay.weixin.qq.com/doc/v3/merchant/4012075420), [signature overview](https://pay.weixin.qq.com/doc/v3/merchant/4012365342)

<a id="platform-reference-5"></a>

#### Merchant transfers

Transfers are an independent, unimplemented extension. Official references distinguish per-transfer recipient confirmation from previously authorized confirmation-free receipt; older enterprise payments, red packets and `/v3/transfer/batches` require product-specific upgrades. A /v3 prefix alone does not identify the current product. [Modes and interfaces](https://pay.weixin.qq.com/doc/v3/merchant/4015273741)

| Mode | Interface and outcome |
| --- | --- |
| Recipient confirms each transfer | `POST /v3/fund-app/mch-transfer/transfer-bills`; WAIT_USER_CONFIRM supplies package_info for the confirmation UI |
| Transfer plus authorization request | `/v3/fund-app/mch-transfer/transfer-bills/pre-transfer-with-authorization`; track both lifecycles |
| Separate authorization request | `/v3/fund-app/mch-transfer/user-confirm-authorization`; application, confirmation and activation are distinct |
| Transfer after authorization | `/v3/fund-app/mch-transfer/transfer-bills/transfer`; inspect synchronous/query state, with no asynchronous transfer-result notification for this mode |

Confirmation mode requires transfer_scene_id, reported scene data, AppID/OpenID binding and required name encryption. ACCEPTED, PROCESSING, WAIT_USER_CONFIRM, TRANSFERING and CANCELING are nonterminal; SUCCESS, FAIL and CANCELLED are terminal. HTTP 200, order creation and showing the UI are not settlement. [Initiation](https://pay.weixin.qq.com/doc/v3/merchant/4012716434), [JSAPI confirmation](https://pay.weixin.qq.com/doc/v3/merchant/4012716430)

Retain merchant/WeChat authorization identifiers. Users or merchants may revoke authorization; authorization events are separate from transfer outcomes. Unknown network/error results cannot justify a new merchant order number. [Authorization](https://pay.weixin.qq.com/doc/v3/merchant/4015901167), [authorization events](https://pay.weixin.qq.com/doc/v3/merchant/4014512908), [authorized transfers](https://pay.weixin.qq.com/doc/v3/merchant/4014399371). Request-source IP, product/scene rights and app binding require merchant-side setup. [Development guide](https://pay.weixin.qq.com/doc/v3/merchant/4012715211)

<a id="platform-reference-6"></a>

#### Payment codes and SDK boundaries

Official direct/partner payment-code pages explicitly specify V2. Preserve that implementation until the same product/role has a replacement; never invent V3 endpoints or delete a supported product merely because others use V3. A /doc/v3 documentation directory is not a protocol version. [Direct product](https://pay.weixin.qq.com/doc/v3/merchant/4012647301), [partner product](https://pay.weixin.qq.com/doc/v3/partner/4012851192)

The referenced [official GitHub catalog](https://github.com/wechatpay-apiv3) lists Java, Go, PHP and tools, not an official .NET SDK. This project's .NET adapter is maintained here. New transfer examples/tool classes do not establish support in an older SDK.

<a id="platform-reference-7"></a>

### Official and service accounts

<a id="platform-reference-8"></a>

#### Tokens and OAuth

getStableAccessToken is the API name; its endpoint is POST /cgi-bin/stable_token, isolated from /cgi-bin/token. Ordinary mode reuses valid tokens; expires_in is remaining validity and cannot be reset to two hours on every call. force_refresh invalidates previous tokens and has quotas. Third-party platforms cannot replace component/authorizer credentials with stable_token. [Stable tokens](https://developers.weixin.qq.com/doc/service/api/base/api_getstableaccesstoken.html)

OAuth uses /sns/oauth2/access_token, /sns/oauth2/refresh_token, /sns/auth and /sns/userinfo, separately from basic tokens. AppSecret and OAuth tokens stay server-side. Codes are single-use and expire after five minutes; refresh tokens last 30 days. Applicability is mainly verified service accounts, with documented verified government/institution/media exceptions, not arbitrary accounts/AppIDs. Base scope obtains identity; userinfo follows consent and rights. Handle profile withdrawal, account closure and profile changes. [OAuth](https://developers.weixin.qq.com/doc/service/guide/h5/auth.html)

JS-SDK signs nonce, cached jsapi_ticket, timestamp and page URL without the # fragment using SHA-1. It is independent of payment RSA signatures. [JS-SDK](https://developers.weixin.qq.com/doc/service/guide/h5/jssdk.html)

<a id="platform-reference-9"></a>

#### Messaging and callbacks

Template messages, subscription notices, one-time subscriptions, customer-service and mass messages have distinct endpoints, templates, consent and permissions. Mini program subscribe/send is not their universal replacement. [Service API catalog](https://developers.weixin.qq.com/doc/service/api/)

Service-account URL verification uses GET signature validation and echostr. Secure messages use msg_signature over Token, timestamp, nonce and Encrypt, then AES-256-CBC, WeChat 32-byte padding and trailing AppID validation; passive replies have their own encryption requirements. Payment RSA/GCM cannot substitute. [Push integration](https://developers.weixin.qq.com/doc/service/guide/dev/push/), [encryption](https://developers.weixin.qq.com/doc/service/guide/dev/push/encryption.html)

The push overview specifies XML, while generic encryption examples include XML and JSON. Follow the specific event and actual account configuration, without assuming every event accepts JSON. These reference protocols do not add an implemented general message receiver to the library.

<a id="platform-reference-10"></a>

### Mini programs

| Capability | Official contract |
| --- | --- |
| Login | wx.login code → GET /sns/jscode2session; OpenID/session_key and conditional UnionID |
| Phone | POST /wxa/business/getuserphonenumber; distinct single-use five-minute phone code, with optional OpenID binding |
| Subscription message | POST /cgi-bin/message/subscribe/send; template, OpenID, typed data and page; delegated use requires the relevant permission; not implemented by this adapter |

[Login API](https://developers.weixin.qq.com/miniprogram/dev/server/API/user-login/api_code2session.html), [phone API](https://developers.weixin.qq.com/miniprogram/dev/server/API/user-info/phone-number/api_getphonenumber.html), [subscription sending](https://developers.weixin.qq.com/miniprogram/dev/server/API/mp-message-management/subscribe-message/api_sendmessage.html). Keep session_key server-side and issue application-owned login state. Login/phone codes are not interchangeable. [Login guide](https://developers.weixin.qq.com/miniprogram/dev/framework/open-ability/login.html)

Subscription messages need user grants; one-time, long-term and device subscriptions have separate entries. Consent dialogs, unsubscribe events and delivery results differ, and typed template fields have limits. A sending failure does not always mean token expiry. [Subscription guide](https://developers.weixin.qq.com/miniprogram/dev/framework/open-ability/subscribe-message.html)

<a id="platform-reference-11"></a>

### Third-party open platform

Open-platform binding enables conditional same-entity UnionID association; third-party authorization enables delegated development/operation. Serving several merchants does not make their users share UnionID. [Terminology](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/getting_started/terminology_introduce)

| Stage | Official contract |
| --- | --- |
| component_verify_ticket | Pushed every ten minutes, valid twelve hours; verify, persist latest valid ticket and return success |
| component_access_token | POST /cgi-bin/component/api_component_token with component AppID, Secret and ticket; two-hour lifetime governed by expiry |
| Preauthorization | Obtain code, guide merchant authorization and receive authorization_code |
| Authorization exchange | POST /cgi-bin/component/api_query_auth returns actual grants and authorizer_refresh_token; reauthorization invalidates the previous refresh token |
| Authorizer token | POST /cgi-bin/component/api_authorizer_token using component token and that authorizer's refresh token, isolated per account |

See [tickets](https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/Before_Develop/component_verify_ticket.html), [component token](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getcomponentaccesstoken.html), [preauthorization](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getpreauthcode.html), [grant exchange](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getauthorizerrefreshtoken.html) and [authorizer token](https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getauthorizeraccesstoken.html). api_query_auth/api_authorizer_token use a query parameter named access_token whose value is component_access_token. Validate parameter name, credential kind and account scope separately. Declared platform permissions are not necessarily all granted by an account; the implementation uses authoritative authorizer storage as described above.

<a id="platform-reference-12"></a>

### Evidence boundaries

These references are public documentation/official synchronized resources, not account-permission checks. Reconfirm limits, quota, fees, categories and new fields against the relevant page and real account when implementing an API. The OAuth page states gender/region are no longer returned while an old field table still lists them; models cannot require them. Apply the stricter explicit restriction and actual API capability rather than filling gaps with third-party claims.
