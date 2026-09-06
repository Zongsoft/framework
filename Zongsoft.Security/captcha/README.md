# Zongsoft.Security.Captcha Captcha Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Security.Captcha)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Security.Captcha)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**S**ecurity.**C**aptcha](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Security/captcha) is a sub-plugin of the security plugin library for the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides captcha-related functionality.

The built-in `Authencode` scheme generates a PNG challenge, stores the answer in `IDistributedCache`, and exchanges a correct answer for a short-lived, one-time confirmation token. It integrates with `ICaptcha`, the Security Web controller, and the `X-Captcha` response header.

## Installation and Requirements

```shell
dotnet add package Zongsoft.Security.Captcha
```

Deploy `Zongsoft.Security.Captcha.plugin`; it depends on the main Security plugin. The host must provide `IDistributedCache`. In a multi-instance deployment the cache must be shared, otherwise a challenge issued by one node cannot be verified by another.

## Browser/API Flow

1. Request the `Authencode` scheme through the Security CAPTCHA endpoint.
2. Display the returned `image/png`; read its challenge token from the `X-Captcha` header.
3. Submit the answer as `token:code` (an equals sign is also accepted by the provider).
4. Receive a confirmation token and pass it to the protected security workflow.
5. The final verification consumes the confirmation and original challenge entries.

The challenge expires after 10 minutes and a successful answer creates a confirmation valid for 5 minutes. Confirmation verification removes both cache entries, making the confirmation single-use.

> 💡 `AuthencodeImager.Generate(code, width, height)` can generate the image directly, but application flows should normally use `ICaptcha` so caching, formatting, and scheme matching remain consistent.

## Security and Accessibility

🚨 A visual CAPTCHA is only one abuse-control signal. Apply endpoint rate limits and monitoring, use TLS, do not log answers or tokens, and do not treat a CAPTCHA as identity proof. Provide an accessible alternative for users who cannot solve an image challenge.

Image rendering uses ImageSharp and its font/drawing packages. Verify that fonts are available and that image dimensions, proxy/header rules, and cache expiry behave consistently in the target environment.

## References

- CAPTCHA
	- [English](https://en.wikipedia.org/wiki/CAPTCHA)
	- [Chinese](https://zh.wikipedia.org/wiki/CAPTCHA)

- [Security API plugin](../api/README.md)
- [Distributed caching in ASP.NET Core](https://learn.microsoft.com/aspnet/core/performance/caching/distributed)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

Use a Web host with Security configured and a registered distributed-cache provider. Service discovery exposes Authencode and its formatter; do not register a second challenge implementation with the same scheme inadvertently.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Security.Captcha` | [Zongsoft.Security.Captcha.plugin](Zongsoft.Security.Captcha.plugin) |
| File copying and dependencies | [Zongsoft.Security.Captcha.deploy](Zongsoft.Security.Captcha.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft security]
nuget:Zongsoft.Security

[plugins zongsoft security captcha]
nuget:Zongsoft.Security.Captcha
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Security.Captcha.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
