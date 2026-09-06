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

- Applet login, phone-number retrieval, user lookup, and access-credential refresh.
- Official-account credential, user, message-template, authentication, and postmark operations.
- Bank and branch lookup, including bank-card identification.
- Platform certificate retrieval and temporary media-file upload.

Load `Zongsoft.Externals.Wechat.Web.plugin` together with `Zongsoft.Externals.Wechat`. The controllers use the `Externals/Wechat` area and resolve configured applets, channels, payment authorities, and related services from the core plugin; only the endpoints required by the host should be exposed publicly.

## Installation and Configuration Boundary

```shell
dotnet add package Zongsoft.Externals.Wechat.Web
```

This package contains controllers only. Account identifiers, applet/channel credentials, merchant/payment authorities, certificates, HTTP clients and token caching are configured by the [core adapter](../README.md). Generate OpenAPI from the host to see exact routes, verbs and payloads after framework conventions are applied.

## Typical Applet Flow

1. The client obtains a temporary login code from WeChat.
2. Call the configured applet endpoint to exchange it through `Applet`/`UserProvider`.
3. Use the returned application identity according to the host's authentication design.
4. Refresh access credentials through the credential endpoint only when required by the owning service.

Channel operations similarly resolve a named channel before reading users, sending template messages or performing authentication. Bank/certificate/file controllers delegate to the configured payment/platform services.

> 💡 WeChat access tokens and platform certificates have their own expiry and refresh rules. Use the core managers and cache; do not have every controller consumer implement independent refresh loops.

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
| `Zongsoft.Externals.Wechat.Web` | [Zongsoft.Externals.Wechat.Web.plugin](Zongsoft.Externals.Wechat.Web.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Wechat.Web.deploy](Zongsoft.Externals.Wechat.Web.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals wechat]
nuget:Zongsoft.Externals.Wechat

[plugins zongsoft externals wechat web]
nuget:Zongsoft.Externals.Wechat.Web
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Wechat.Web.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
