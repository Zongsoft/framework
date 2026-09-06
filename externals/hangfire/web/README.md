# Zongsoft.Externals.Hangfire.Web Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Hangfire.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Hangfire.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.Hangfire.Web` integrates the [Hangfire Dashboard](https://docs.hangfire.io/en/latest/configuration/using-dashboard.html) with a Zongsoft ASP.NET Core host.

Loading `Zongsoft.Externals.Hangfire.Web.plugin` registers Hangfire services and calls `UseHangfireDashboard()` during application initialization. The default Hangfire Dashboard route is therefore available to the host after a `JobStorage` has been configured.

This package only supplies the web-host integration. Use the [core Hangfire plugin](..) for scheduling and background processing, and add an appropriate storage package such as the [Redis adapter](../storages/redis). Configure Dashboard authorization and routing according to the hosting application's security requirements; do not expose it publicly without access controls.


## Installation and Startup

```shell
dotnet add package Zongsoft.Externals.Hangfire.Web
```

The service registration calls `AddHangfire`; the application initializer calls parameterless `UseHangfireDashboard()`, which uses Hangfire's default dashboard path and options. A valid global `JobStorage` must be registered before the middleware handles requests.

> 💡 This adapter currently provides no option for changing the dashboard path or filters. Customize the host directly when deployment needs non-default Dashboard options.

🚨 The default Hangfire Dashboard authorization behavior is not a substitute for an application security review. Verify remote-access rules, add explicit authorization filters, protect cookies against CSRF, and avoid exposing arguments or exception details to unauthorized users.

Test the dashboard together with the selected storage and worker: confirm server heartbeat, queue/job views, retry/delete actions, and access denial for unauthenticated callers.

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../../Zongsoft.Plugins/README.md).

Use a Plugins.Web host and deploy the corresponding main adapter listed below. Service/controller discovery runs during host initialization; do not put business logic into Program.cs. Verify the configured endpoint and authorization before accepting requests.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Hangfire.Web` | [Zongsoft.Externals.Hangfire.Web.plugin](Zongsoft.Externals.Hangfire.Web.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Hangfire.Web.deploy](Zongsoft.Externals.Hangfire.Web.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals hangfire]
nuget:Zongsoft.Externals.Hangfire

[plugins zongsoft externals hangfire web]
nuget:Zongsoft.Externals.Hangfire.Web
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Hangfire.Web.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
