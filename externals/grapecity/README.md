# Zongsoft.Externals.Grapecity Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Grapecity)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Grapecity)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**E**xternals.**G**rapecity](https://github.com/Zongsoft/framework/tree/main/externals/grapecity) adapts [GrapeCity ActiveReports](https://developer.mescius.com/activereportsnet) to the reporting abstractions of the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) framework.

## Features

- Resolves Zongsoft report descriptors, data sources, parameters, and data models for ActiveReports.
- Loads report definitions from the framework's report locators and data-model providers.
- Supplies designer resource and theme integration for ActiveReports-based report design.
- Works with the companion [web package](api) to host report viewer and designer endpoints.

Load `Zongsoft.Externals.Grapecity.plugin` after `Zongsoft.Reporting`. ActiveReports is commercially licensed software; ensure that the required runtime and designer licenses are available in the target environment.

## Installation and Opening a Definition

```shell
dotnet add package Zongsoft.Externals.Grapecity
```

```csharp
using Zongsoft.Externals.Grapecity.Reporting;

using var report = Report.Open("reports/sales.rdlx");
Console.WriteLine($"{report.Name}: {report.Type}");
report.Parameters["Title"].Value = "Monthly sales";
```

`Report.Open` accepts a framework file path, stream, or `IReportDescriptor`. It wraps an ActiveReports `PageReport`, classifies the first report item as fixed-page (`FPL`) or continuous-page (`CPL`), and projects ActiveReports parameters/data sources into Zongsoft reporting contracts.

## Data and Designer Resources

ActiveReports data-source definitions become `ReportDataSource`/`ReportDataModel`; a registered `IReportDataLoader` must turn those models into runtime data. `ResourceService` and `ThemeResolver` connect report locators, images, templates and themes to the ActiveReports designer.

> 💡 Keep credentials out of RDLX definitions. Let a trusted `IReportDataLoader` map logical source names and parameterized schemas to environment-specific data access.

## Current Limits

The current adapter can open, inspect, edit and save report definitions, and supports the companion viewer/designer middleware. Its direct `IReport.Render*` methods throw `NotImplementedException`, `Export*` has no implementation, and assigning `IReport.Locator` is not implemented. Use the [Web integration](api/README.md) for the implemented runtime path and test required export behavior before adopting it.

🚨 ActiveReports templates may contain expressions and data queries. Load trusted definitions only, authorize underlying data, restrict resources and output paths, bound report size/runtime, and comply with MESCIUS/GrapeCity runtime and designer licensing.

## Related Resources

- [Zongsoft.Reporting contracts](../../Zongsoft.Reporting/README.md)
- [ActiveReports documentation](https://developer.mescius.com/activereportsnet/docs/introduction/getstarted)
- [Web viewer/designer integration](api/README.md)
- [External adapter implementation guidance](../SKILL.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

Deploy Reporting and the licensed ActiveReports runtime as well. The adapter's source `.deploy` lists its own artifacts but not the complete commercial runtime; verify the selected package's dependencies and licensing separately.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Grapecity` | [Zongsoft.Externals.Grapecity.plugin](src/Zongsoft.Externals.Grapecity.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Grapecity.deploy](src/Zongsoft.Externals.Grapecity.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft reporting]
nuget:Zongsoft.Reporting

[plugins zongsoft externals grapecity]
nuget:Zongsoft.Externals.Grapecity
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Grapecity.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
