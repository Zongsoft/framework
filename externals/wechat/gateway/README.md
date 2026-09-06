# Zongsoft.Externals.Wechat.Gateway

[English](README.md) | [简体中文](README.zh-Hans.md)

## Purpose and Boundary

This package receives HTTP callbacks and dispatches them to named framework handlers. It complements the [WeChat client adapter](../README.md) and differs from the [Web API package](../api/README.md), which exposes application-facing management operations.

A **callback** is an inbound provider request, potentially retried after timeout. Its route name is only a dispatch key, not proof of sender identity. This gateway does not automatically verify every WeChat product's signature or decrypt every payload.

## Installation and Hosting

```shell
dotnet add package Zongsoft.Externals.Wechat.Gateway
```

Load the gateway plugin and its Wechat dependency in a [plugin-aware Web host](../../../Zongsoft.Plugins.Web/README.md). Deploy the assembly and `.plugin` according to the included `.deploy` file. Under the framework route conventions the endpoint is `POST /Externals/Wechat/Fallback/{name}/{key?}`.

Configure the account, certificate and callback secrets using the parent adapter's options. The gateway does not provide GET URL-verification handlers automatically.

## Handler Registration

The following application composition helper registers an already constructed handler. That handler must implement the service-specific verification and response protocol:

```csharp
using Zongsoft.Components;
using Zongsoft.Externals.Wechat.Gateway;

static void RegisterCallback(string name, IHandler handler)
{
	ArgumentException.ThrowIfNullOrEmpty(name);
	ArgumentNullException.ThrowIfNull(handler);
	FallbackExecutor.Instance.Handlers[name] = handler;
}
```

Register once during startup, before requests arrive; the dictionary is not a concurrent runtime registry. The executor passes the request body stream and request parameters to the selected handler. Keep body consumption within the request lifetime; snapshot a bounded payload before queuing background work.

## Results and Delivery

A non-null result becomes `200 OK`; null becomes `204 No Content`. OperationException reasons map Unfound to 404, Unsupported to 400, Unprocessed to 422 and Unsatisfied to 412; other reasons produce 500. Check that this result format matches the chosen WeChat callback protocol; it is not a universal acknowledgment format.

💡 Test duplicate notifications and retry after a lost response. Make business writes idempotent using a verified provider event identifier.

## Security and Verification

🚨 Validate signatures over the required original bytes, check timestamps/nonces and replay windows, decrypt only after required verification, bound request size, and use TLS. Never log secrets or full payment/user payloads. The controller does not supply these protections for your handler.

Use local signed fixtures for success, bad signature, expired timestamp, duplicate event, malformed body and handler failure. No real WeChat request is needed for these checks. Implementation routes are in [FallbackExecutor](FallbackExecutor.cs), [controller](Controllers/FallbackController.cs) and the [external adapter skill](../../SKILL.md).

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../../Zongsoft.Plugins/README.md).

Use a Plugins.Web host and deploy the corresponding main adapter listed below. Service/controller discovery runs during host initialization; do not put business logic into Program.cs. Verify the configured endpoint and authorization before accepting requests.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Wechat.Gateway` | [Zongsoft.Externals.Wechat.Gateway.plugin](Zongsoft.Externals.Wechat.Gateway.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Wechat.Gateway.deploy](Zongsoft.Externals.Wechat.Gateway.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals wechat]
nuget:Zongsoft.Externals.Wechat

[plugins zongsoft externals wechat gateway]
nuget:Zongsoft.Externals.Wechat.Gateway
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Wechat.Gateway.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
