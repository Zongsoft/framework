# Zongsoft.Security.Web Security Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Security.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Security.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**S**ecurity.**W**eb](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Security/api) is a sub-plugin of the security plugin library for the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides _**W**eb_ plugin support for the security feature set.

It publishes the framework security services as ASP.NET Core controllers: sign-in/sign-out/renewal, secret verification, CAPTCHA issue/verification, users, roles, memberships, passwords, and privileges. The package is an HTTP projection of [Zongsoft.Security](../README.md), not an independent identity store.

## Installation and Hosting

```shell
dotnet add package Zongsoft.Security.Web
```

Deploy `Zongsoft.Security.Web.plugin` with a plugin-aware web host. Its manifest depends on `Zongsoft.Security`; the core security plugin, data mapping, database schema, credential authentication, and authorization services must already be available.

## Endpoint Groups

| Controller | Responsibility |
| --- | --- |
| `AuthenticationController` | sign in by scheme/key, sign out, renew credentials, issue and verify secrets |
| `CaptchaController` | issue and verify an `ICaptcha` scheme |
| `UserController` | user lifecycle, contact verification, password and membership operations |
| `RoleController` | role lifecycle, members, parents, and inherited relationships |
| nested privilege controllers | direct and filtering privileges for users or roles |
| `AuthorizationController` | inspect authorization schemes and evaluated privileges |

Routes are determined by framework MVC conventions. Use the checked-in [HTTP request examples](../docs/http/) or generated OpenAPI metadata to discover the exact route and payload shape for the deployed version.

## Typical Flow

1. Issue a CAPTCHA or out-of-band secret when the selected policy requires it.
2. Sign in through an authenticator scheme and capture the returned credential.
3. Send that credential through the configured authentication handler.
4. Authorize each operation; renew or sign out according to credential lifetime.

> 💡 The `scheme`, `scenario`, and verification channel are domain values resolved by registered services. Do not invent client-side values without checking the host configuration.

🚨 These controllers mutate identity and privilege state. Require TLS, apply rate limits, restrict administrative operations, prevent cross-tenant identifiers, validate antiforgery needs for browser credentials, and never expose raw password or secret payloads in logs.

## Responses and Cancellation

Controllers use standard HTTP status codes, framework pagination, and cancellation tokens. Bulk or reset operations can replace memberships or privileges; clients must distinguish additive operations from `reset=true`. Cancellation is not proof that a server-side mutation was rolled back—use idempotency and reread state where retries are possible.

## Related Resources

- [Security model and configuration](../README.md)
- [API reference](../docs/Zongsoft.Security-api.md)
- [HTTP examples](../docs/http/)
- [Zongsoft.Web conventions](../../Zongsoft.Web/README.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

Use a Plugins.Web host and deploy the corresponding main adapter listed below. Service/controller discovery runs during host initialization; do not put business logic into Program.cs. Verify the configured endpoint and authorization before accepting requests.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Security.Web` | [Zongsoft.Security.Web.plugin](Zongsoft.Security.Web.plugin) |
| File copying and dependencies | [Zongsoft.Security.Web.deploy](Zongsoft.Security.Web.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft security]
nuget:Zongsoft.Security

[plugins zongsoft security web]
nuget:Zongsoft.Security.Web
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Security.Web.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
