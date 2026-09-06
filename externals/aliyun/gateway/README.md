# Zongsoft.Externals.Aliyun.Gateway Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Aliyun.Gateway)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Aliyun.Gateway)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.Aliyun.Gateway` is an ASP.NET Core callback gateway for Alibaba Cloud services. It receives service callbacks and dispatches each request body and its parameters to a named Zongsoft `IHandler`.

## Routing and Handlers

The gateway exposes `POST /Externals/Aliyun/Fallback/{name}/{key?}`. The `{name}` segment selects a handler in `FallbackExecutor.Instance.Handlers`; the optional key and all request parameters are passed through the execution context. A handler result is returned as the response, while an empty result produces `204 No Content`.

Load `Zongsoft.Externals.Aliyun.Gateway.plugin` in a Zongsoft web host and register the required callback handlers. Validate callback signatures and restrict access according to the Alibaba Cloud service that owns each endpoint.

## Installation and Handler Registration

```shell
dotnet add package Zongsoft.Externals.Aliyun.Gateway
```

Register stable handler names in the plugin tree used by `FallbackExecutor`; `{name}` must match one of them. Handlers receive the request stream as the argument and route/query values through `Parameters`. Read the body once, honor cancellation, and return a value compatible with the callback protocol.

The current gateway manifest binds its `Handlers` node to the executor itself, not its dictionary. Expose the executor's `Handlers` property before appending entries to that collection. The consumer manifest must depend on `Zongsoft.Externals.Aliyun.Gateway` and list the application assembly implementing the handler.

```xml
<extension path="/Workbench/Externals/Aliyun/Fallback/Handlers">
	<expose name="Items" value="{path:../@Handlers}" />
</extension>
<extension path="/Workbench/Externals/Aliyun/Fallback/Handlers/Items">
	<object name="Notification" type="MyCompany.Aliyun.NotificationHandler, MyCompany.Aliyun" />
</extension>
```

> 💡 Acknowledge quickly when the provider retries on timeout. Move long-running idempotent work to a durable internal queue after signature validation.

🚨 The route itself does not prove the caller is Alibaba Cloud. Validate the service-specific signature, timestamp/nonce and replay window before processing the payload. Enforce payload limits, use TLS, avoid logging secrets or personal data, and make duplicate delivery safe.

## Verification

Use the provider's callback verification tool or a locally signed fixture to test accepted, invalid-signature, expired, duplicate, malformed, empty-result and handler-failure cases. Do not point tests at production callback URLs.

## Related Resources

- [Aliyun integration](../README.md)
- [ASP.NET Core request body guidance](https://learn.microsoft.com/aspnet/core/fundamentals/use-http-context)
- [External adapter implementation guidance](../../SKILL.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../../Zongsoft.Plugins/README.md).

Use a Plugins.Web host and deploy the corresponding main adapter listed below. Service/controller discovery runs during host initialization; do not put business logic into Program.cs. Verify the configured endpoint and authorization before accepting requests.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Aliyun.Gateway` | [Zongsoft.Externals.Aliyun.Gateway.plugin](Zongsoft.Externals.Aliyun.Gateway.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Aliyun.Gateway.deploy](Zongsoft.Externals.Aliyun.Gateway.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals aliyun]
nuget:Zongsoft.Externals.Aliyun

[plugins zongsoft externals aliyun gateway]
nuget:Zongsoft.Externals.Aliyun.Gateway
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Aliyun.Gateway.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
