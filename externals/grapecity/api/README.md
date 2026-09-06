# Zongsoft.Externals.Grapecity.Web Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Grapecity.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Grapecity.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.Grapecity.Web` hosts the [GrapeCity ActiveReports](https://developer.mescius.com/activereportsnet) viewer and designer integration for Zongsoft web applications. It connects ActiveReports HTTP services to the report locators, templates, data sources, and data models supplied by `Zongsoft.Reporting` and `Zongsoft.Externals.Grapecity`.

## Web Endpoints

The package provides endpoints for listing reports, serving report definitions to the viewer, and supplying designer resources, templates, thumbnails, themes, and data models. Report discovery starts at `GET /Grapecity/Reporting/Reports`, while template endpoints are rooted at `/Grapecity/Reporting/Templates`.

Load `Zongsoft.Externals.Grapecity.Web.plugin` after the core GrapeCity and reporting plugins. Configure authentication and authorization in the hosting application before exposing viewer or designer endpoints.

## Installation and Startup

```shell
dotnet add package Zongsoft.Externals.Grapecity.Web
```

The registered `IApplicationInitializer<IApplicationBuilder>` calls `UseReporting` and `UseDesigner` with the shared `/Grapecity/Reporting` prefix, compression, a custom report store, and a data-source callback. A configured `IReportDataLoader` is required for data-bound reports; `IReportLocator` implementations supply definitions.

```http
GET /Grapecity/Reporting/Reports
Accept: application/json
```

That endpoint returns report names from all registered locators or `204` when none exist. ActiveReports viewer/designer routes under the same prefix are owned by the vendor middleware; use its client packages and documentation for request formats.

> 💡 Load order matters: reporting contracts, the core GrapeCity adapter, report/data providers, and then this Web plugin. Missing reports usually indicate locator or plugin discovery, not a browser rendering problem.

## Security and Operations

🚨 A designer can read definitions/resources and may save modified templates through its store. Separate viewer and designer authorization, disable design access for ordinary users, validate report keys, constrain uploads/resources, and apply antiforgery rules appropriate to the host's credentials.

Report execution can be expensive. Bound rows, output size, render time and concurrency; keep secrets out of definitions and logs. The package versions target a specific ActiveReports API generation, so upgrade core and Web vendor packages together and retest the browser client.

## Related Resources

- [Core adapter and current limits](../README.md)
- [Zongsoft.Reporting](../../../Zongsoft.Reporting/README.md)
- [ActiveReports Web integration](https://developer.mescius.com/activereportsnet/docs/introduction/getstarted)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../../Zongsoft.Plugins/README.md).

Use a Plugins.Web host and deploy the corresponding main adapter listed below. Service/controller discovery runs during host initialization; do not put business logic into Program.cs. Verify the configured endpoint and authorization before accepting requests.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Grapecity.Web` | [Zongsoft.Externals.Grapecity.Web.plugin](Zongsoft.Externals.Grapecity.Web.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Grapecity.Web.deploy](Zongsoft.Externals.Grapecity.Web.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft reporting]
nuget:Zongsoft.Reporting

[plugins zongsoft externals grapecity]
nuget:Zongsoft.Externals.Grapecity

[plugins zongsoft externals grapecity web]
nuget:Zongsoft.Externals.Grapecity.Web
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Grapecity.Web.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
