# Zongsoft.Reporting Reporting Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Reporting)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Reporting)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**R**eporting](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Reporting) is the reporting plugin library for the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides the infrastructure required for report development.

This package defines provider-neutral contracts for locating report definitions, resolving a vendor format, loading data, binding parameters, rendering, exporting, storing resources, and archiving results. It does not include a complete report designer or rendering engine; a provider adapter must implement those format-specific operations.

## Reporting Vocabulary

| Concept | Contract | Purpose |
| --- | --- | --- |
| definition | `IReportDescriptor` | identifies a template and opens its source stream |
| report | `IReport` | provider-backed document with parameters, data locator, render and export operations |
| resource | `IReportResource` | named embedded/external asset used by a report |
| data model | `IReportDataModel` | describes source, schema, paging, and provider settings |
| locator/loader | `IReportDataLocator`, `IReportDataLoader` | selects a loader and materializes report data |
| repository/archive | `IReportRepository`, `IReportArchiveLocator` | persists definitions or finds generated output |

Rendering produces the report's presentation format; exporting produces a selected interchange/output format. Exact options and supported formats belong to the provider.

## Installation

```shell
dotnet add package Zongsoft.Reporting
```

Also install a reporting provider that implements `IReportResourceResolver` and the concrete `IReport`. Register its locators and repository with the application service container.

## Definition Example

The framework's [FileReportDescriptor](src/ReportDescriptor.cs) is a concrete reference for opening a definition through the framework file system. Its `Open` implementation is:

```csharp
public Stream Open()
{
	return Zongsoft.IO.FileSystem.File.Open(this.FilePath, FileMode.Open, FileAccess.Read);
}
```

This is a source excerpt, not a bundled report demo. Neither this package nor the Grapecity adapter includes a ready-to-run report definition. Supply a trusted definition supported by your deployed provider; its format, data sources, and parameters must come from that actual definition.

A descriptor only opens the definition; it does not instantiate a report. Use the provider's report-opening API (for example, [Grapecity's `Report.Open`](../externals/grapecity/README.md)) to obtain an `IReport`. `IReportResourceResolver.Resolve` instead produces an `IReportResource` for an asset such as an image; it is not a report factory. Only call rendering/export operations that the chosen provider actually implements.

> 💡 Keep business queries behind `IReportDataLoader`. A template should name a data model and parameters, not embed deployment credentials or unrestricted ad-hoc access.

## Lifecycle and Limits

Callers own streams they open or pass unless a concrete provider documents otherwise. Rendering can be CPU-, memory-, and I/O-intensive; bound input size, row count, paging, output size, fonts/images, and execution time. Use a background job for long reports.

🚨 Treat report templates as active, privileged input when a provider supports expressions or scripts. Only load trusted definitions, constrain file/resource resolution, parameterize data access, sanitize exported filenames, and authorize both the definition and its underlying data.

The built-in `ReportDataLocator` is only a service placeholder in the current implementation and does not select a loader. Applications must provide a working locator/loader or a provider package; do not assume installing this contract package enables data-bound reports by itself.

## Related Resources

- [Public reporting contracts](src/)
- [Zongsoft.Data](../Zongsoft.Data/README.md)
- [Implementation guidance](SKILL.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../Zongsoft.Plugins/README.md).

Service scanning registers the built-in data loader/locator. A concrete report engine and an application data service are still required; the current locator is incomplete, as noted above.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Reporting` | [Zongsoft.Reporting.plugin](src/Zongsoft.Reporting.plugin) |
| File copying and dependencies | [Zongsoft.Reporting.deploy](src/Zongsoft.Reporting.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft reporting]
nuget:Zongsoft.Reporting
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Reporting.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
