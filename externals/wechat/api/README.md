# Zongsoft.Externals.Wechat.Web Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Wechat.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Wechat.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.Wechat.Web` exposes the services of the [Zongsoft WeChat integration](..) as ASP.NET Core Web APIs. It is intended for Zongsoft web hosts that need a consistent HTTP surface for WeChat applets, official-account channels, payments, and certificates.

## API Areas

- Applet application-session login and structured phone-number retrieval.
- Official-account user, message-template, authentication, and postmark operations.
- Bank and branch lookup, including bank-card identification.
- WeChat verification-material metadata retrieval and temporary media-file upload.

Load `Zongsoft.Externals.Wechat.Web.plugin` together with `Zongsoft.Externals.Wechat`. The controllers use the `Externals/Wechat` area and resolve configured applets, channels, payment authorities, and related services from the core plugin; only the endpoints required by the host should be exposed publicly.

## Installation and Configuration Boundary

```shell
dotnet add package Zongsoft.Externals.Wechat.Web
```

This package contains controllers only. Account identifiers, applet/channel credentials, merchant/payment authorities, certificates, HTTP clients and token caching are configured by the [core adapter](../README.md). Generate OpenAPI from the host to see exact routes, verbs and payloads after framework conventions are applied.

## Typical Applet Flow

1. The client obtains a temporary login code from WeChat.
2. The host exchanges it through `Applet.LoginAsync` and uses OpenID and, when available, UnionID to identify the application user.
3. The host issues its own application session and keeps the WeChat session key on the server.
4. Access-credential retrieval and refresh are restricted to the owning server-side service.

`POST Applets/{id}/Login?scenario={scenario}` accepts a plain-text login code and issues an application session through the host authenticator, challengers, and credential provider. `POST Applets/{id}/PhoneNumber` accepts `{ "code": "phone authorization code", "openId": "optional identity" }` and returns the full number, national number, and country code.

Both require the host's `Wechat.Accounts` authorization policy, with the requested `Account` as resource, to validate caller and tenant ownership; a missing policy returns 403. Phone retrieval also requires an authenticated application identity. There are no applet user-directory or applet/channel Credential HTTP endpoints. Protocol secrets are excluded from serialization. See the [identity contract](../docs/implementation.md#applet-identity).

Channel operations similarly resolve a named channel before reading users, sending template messages or performing authentication. Bank/certificate/file controllers delegate to the configured payment/platform services.

`Certificates/{code?}` looks up a public-key ID or platform-certificate serial number for the default merchant; omitting it selects the current encryption material. The result contains `VerificationKey` identity, mode, and validity metadata, without merchant private keys or certificate files. See the [credential contract](../docs/implementation.md#merchant-credentials) for loading and refresh conditions. The host should coordinate token and certificate refresh.

## Security and Side Effects

🚨 Login codes, phone data, OpenID/UnionID, payment certificates and media files are sensitive. Require TLS, authenticate the application's own caller, isolate account/tenant names, validate upload type and size, and redact secrets and personal data from logs.

Several operations call external WeChat APIs and can send messages or mutate remote state. Apply rate limits and idempotency where the provider supports it; distinguish provider rejection from transient transport failure before retrying.

## Related Resources

- [Core WeChat integration](../README.md)
- [Callback gateway](../gateway/README.md)
- [WeChat developer documentation](https://developers.weixin.qq.com/doc/)
- [External adapter implementation guidance](../../SKILL.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../../Zongsoft.Plugins/README.md).

Use a Plugins.Web host and deploy the corresponding main adapter listed below. Service/controller discovery runs during host initialization; do not put business logic into Program.cs. Verify the configured endpoint and authorization before accepting requests.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Wechat.Web` | [Zongsoft.Externals.Wechat.Web.plugin](src/Zongsoft.Externals.Wechat.Web.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Wechat.Web.deploy](src/Zongsoft.Externals.Wechat.Web.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals wechat]
nuget:Zongsoft.Externals.Wechat@7.10.0

[plugins zongsoft externals wechat web]
nuget:Zongsoft.Externals.Wechat.Web@7.10.0
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Wechat.Web.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

`Channels/{id}/Login` also returns a host application session. Postmark requires an additional `Wechat.JSSDK` site policy and returns a hexadecimal Signature. User and message endpoints require authenticated callers and account authorization, with an explicit id route. See the [channel service contract](../docs/implementation.md#channel-services) for routes, payloads, and host-owned state validation.

## Third-party authorization requests

Authenticated callers can use `POST Components/{componentAppId}/Authorizations` with `Context`, `RedirectUri`, `AccountType` and optional `Mobile`. The host must configure `Wechat.Components` to authorize the business scope, component, account type and redirect site, and register a durable `IComponentStore`. The response contains an authorization URL and expiry only after the server saves its owner-bound state. It does not establish a tenant association or expose component/authorizer credentials. See the [component contract](../docs/implementation.md#component-platform).
