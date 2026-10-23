# Zongsoft.Externals.Wechat Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Wechat)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Wechat)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Current capabilities and limitations

This library provides selected REST wrappers for mini program login and phone-number exchange, official account OAuth and template messages, payments, and refunds. Payment implementations include both APIv3 and APIv2/XML paths; this is not a complete SDK for all WeChat products.

APIv3 HTTP calls for payments, refunds, face payment, merchant onboarding, banking, uploads and membership cards sign requests and verify the original response bytes, signature headers, and timestamp window before parsing. Empty 204 responses also require verification. Unsigned gateway errors remain failures, and financial requests are not automatically retried. See the [HTTP contract](docs/implementation.md#http-pipeline) for timeouts, error codes, transport configuration, and resource ownership. Membership calls explicitly bind an eligible merchant, service account and brand.

Payment, refund and offline face agreement notifications use explicit product routes. The adapter verifies original bytes, timestamps, events, merchant identity and application bindings before authenticated decryption. The host validates business ownership, amounts, currency and durable idempotency, acknowledging only completed or reliably accepted work. See the [Gateway](gateway/README.md) and [notification contract](docs/implementation.md#payment-notifications).

Web login endpoints issue application sessions through the host authenticator without returning WeChat server tokens or mini program `session_key` values. Hosts configure account resource authorization; phone, user and messaging operations also require an authenticated identity, and JS-SDK signing requires a site allowlist. See the [Web API](api/README.md).

Third-party components support verification-ticket events, authorization-change reception, component tokens, preauthorization and server-side authorization exchange. Hosts provide durable storage and tenant association rules; authorizer tokens are refreshed and existing account operations are delegated according to the actual granted permissions. See the [component contract](docs/implementation.md#component-platform).

Payments and refunds use integer CNY cents; decimal inputs reject precision loss and overflow. Payment results distinguish prepayment tokens, Native QR contents and actual orders. Unknown states require confirmation by query. See the [payment model contract](docs/implementation.md#payment-models).

See the [basic payment contract](docs/implementation.md#basic-payments) for JSAPI/mini program, APP and Native identity configuration, order results, client invocation parameters and query rules.

Payment codes, online face credentials, V2 queries/reversals, and Automao state handling are described in the [payment-code contract](docs/implementation.md#ticket-payments).

Refund acceptance and settlement are separate states. See [refunds](docs/implementation.md#refunds) for original-order queries, partial refunds, merchant bindings and host-side cumulative limits.

See the [offline face and agreement contract](docs/implementation.md#offline-face) for device authentication, dedicated payment queries, presign and authorization tokens, and termination. A service provider, sub-merchant and relevant product permissions are required.

## Runtime requirements

Use version 7.10.0 of the core adapter, Web and Gateway together. Supported runtimes are .NET 8, 9 and 10; minimum host dependencies are Core 7.72.0 and Web 7.20.0 for HTTP endpoints. See the [deployment contract](docs/implementation.md#deployment) for package availability, authorization and pending-transaction requirements.

## Merchant credentials

Load merchants with `await AuthorityUtility.GetAuthorityAsync(name, cancellation)`. Configure the signing identity, `ApiV2Key`, `ApiV3Key`, and WeChat verification material separately. Both explicit public-key and platform-certificate modes are supported. File loading supports cancellation and rejects missing or mismatched material. See the [credential contract and configuration examples](docs/implementation.md#merchant-credentials) for rotation and host refresh requirements. Certificate downloads are verified with existing trusted material before updating the certificate set.

## Product coverage

| Capability | Supported scope |
| --- | --- |
| JSAPI / mini program, APP, Native payments | APIv3 direct merchants and service providers; original-order query and close |
| Payment codes / online face payment | Product-specific APIv2 submission, query and reversal; no automatic V2 fallback for APIv3 products |
| Refunds | APIv3 application, query and notifications; acceptance and settlement are separate |
| Offline face payment | Dedicated service-provider payment and agreement APIs; device SDK is external |
| Merchant onboarding | Separate platform ecommerce and ordinary service-provider submerchant applications and queries |
| Banking / media | Multiple bank candidates, branch queries and bounded image/PDF/video uploads |
| Membership | Merchant + channel + brand template/code operations; not the complete card product |
| Mini program identity | code2Session, authorized phone exchange and host-issued sessions |
| Official accounts | OAuth, JS-SDK signatures, follower queries and template messages |
| Basic credentials | Stable tokens for owned accounts, separate JS-SDK/card tickets and distributed coordination |
| Open platform | SHA-1/CBC, component events, preauthorization, authorization exchange, authorizer tokens and delegated calls; host persistence is required |

Product eligibility and account permissions remain prerequisites. See the [capability matrix](docs/implementation.md#capability-matrix) for exact interfaces and limitations.

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

The manifest exposes the Wechat components and services. Keep account, certificate and payment settings under application-owned options. API and callback gateway plugins are optional, separate host contributions.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Wechat` | [Zongsoft.Externals.Wechat.plugin](src/Zongsoft.Externals.Wechat.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Wechat.deploy](src/Zongsoft.Externals.Wechat.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals wechat]
nuget:Zongsoft.Externals.Wechat@7.10.0
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Wechat.plugin`, `Zongsoft.Externals.Wechat.option`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

## Account lookup

Implement `IAccountProvider.GetAccountAsync` to supply dynamic accounts. Use `await AppletManager.GetAppletAsync(code, cancellation)` or `await ChannelManager.GetChannelAsync(code, cancellation)` to obtain an applet or channel. Use `FindAppletAsync` or `FindChannelAsync` and check for null when absence is allowed. Lookups support cancellation and reuse existing instances.

## Onboarding and media

`PartnerService` submits and queries ordinary service-provider submerchant applications. Signing, payment products and AppID binding require separate confirmation; see the [partner contract](docs/implementation.md#partner-onboarding).

`MerchantService.RegisterAsync/GetAsync` submits platform ecommerce submerchant applications and queries the original business request ID. Acceptance does not enable payments. Bank identification returns a candidate array. Image, PDF and video uploads enforce format and size limits. The caller needs the corresponding product permissions; see the [onboarding, banking and media contract](docs/implementation.md#merchant-onboarding).

## Membership cards

Use `new Membership(authority, channelAccount, brandId)` for existing template and code operations. The channel must be a service account associated with the brand, and the merchant needs product permission. Deposits return individual code results. See the [membership contract](docs/implementation.md#membership) for supported operations and Automao limitations.

## Account credentials and tickets

Self-owned channels and applets obtain stable tokens through `GetCredentialAsync`. Ordinary calls reuse shared cache entries; request forced refresh only when necessary. Channel `jsapi` and `wx_card` tickets use separate cache entries. Cancellation and errors propagate. Hosts provide distributed caching, locking and a named HTTP transport with redirects disabled; see [credential configuration](docs/implementation.md#account-credentials).

See the [applet identity contract](docs/implementation.md#applet-identity) for structured phone data, server-only secrets, and host session integration.

See the [channel service contract](docs/implementation.md#channel-services) for OAuth scopes and refresh, JS-SDK site authorization, and user/message routes.

Public-platform messages support CBC encryption/decryption with required receiver validation and SHA-1 signature verification, for both text and binary payloads. See the [cryptographic contract](docs/implementation.md#platform-cryptography) for call order, errors and receiver responsibilities.
