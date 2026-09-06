# Zongsoft.Learning

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Learning)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Learning)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Zongsoft.Learning**](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Learning) is a machine-learning plugin library for the [Zongsoft](https://github.com/Zongsoft/framework) application framework. It is built on [ML.NET](https://dotnet.microsoft.com/apps/machinelearning-ai/ml-dotnet) and aims to provide a plugin-based foundation for describing, discovering, and assembling machine-learning pipelines in Zongsoft applications.

The current codebase mainly establishes the core abstractions and extension points for datasets, pipeline steps, trainers, and ML.NET component registration.

## Packages

| Package | Description |
| ------- | ----------- |
| `Zongsoft.Learning` | Core machine-learning plugin library. |
| `Zongsoft.Learning.Web` | Optional web plugin that exposes machine-learning metadata for web applications. |

## Current Scope

The current implementation includes:

- Dataset and dataset-field abstractions.
- Pipeline and trainer-step abstractions.
- A runtime catalog for registered ML.NET pipeline components.
- A text-file dataset loader.
- A small set of ML.NET transform and trainer builders.
- Plugin metadata and configuration-setting drivers for integration with the Zongsoft framework.
- A preliminary database schema document for future persistence work.

The project will continue to evolve toward richer model training, pipeline management, model storage, and web-facing capabilities.

## Repository Layout

| Path | Description |
| ---- | ----------- |
| `src/` | Core `Zongsoft.Learning` plugin library. |
| `api/` | Optional `Zongsoft.Learning.Web` plugin library. |
| `database/` | Draft database schema notes. |
| `build.cake` | Build automation script. |

## Build

Restore and build the solution:

```powershell
dotnet restore Zongsoft.Learning.slnx
dotnet build Zongsoft.Learning.slnx
```

## License

Zongsoft.Learning is released under the GNU Lesser General Public License. See the repository license for details.

## Concepts for Application Developers

An **IDataView** is ML.NET's tabular data access abstraction, often evaluated lazily. An **estimator** describes a transformation or training step; fitting it produces a **transformer**. A pipeline composes steps, while a catalog describes which steps an application can choose. Reading the catalog is not training a model.

This package currently registers LightGbm regression, column concatenation, one-hot encoding and one-hot hash encoding. Other catalog categories may be empty; a category name does not establish a working trainer.

## Installation and Safe First Example

```shell
dotnet add package Zongsoft.Learning
```

Load `Zongsoft.Learning.plugin` in a plugin host to populate `Pipeline.Catalog`. A standalone metadata inspection needs no model call or training data:

```csharp
using Zongsoft.Learning;

static void PrintCatalog(EstimatorDescriptorCatalog catalog)
{
	foreach(var estimator in catalog.Estimators)
		Console.WriteLine(estimator.Name);

	foreach(var child in catalog.Catalogs)
		PrintCatalog(child);
}

PrintCatalog(Pipeline.Catalog);
```

Without plugin initialization an empty catalog is expected. In a configured host it lists registered estimator names. The [Web package](api/README.md) exposes the same metadata.

## Data Loading and Extension Points

`Dataset.Settings` configures a loader; `DatasetLoader.GetLoader` resolves a named `IDatasetLoader`. The built-in `TextFile` loader creates ML.NET TextLoader options from [its settings](src/Data/TextFileLoader.Settings.cs). Set the file path and column options explicitly: declaring Dataset.Fields does not by itself build those options.

Implement `IEstimatorBuilder` for a new step and register its descriptor in the appropriate catalog. Keep loading, pipeline construction, fitting, evaluation and model storage as distinct application operations.

## Current Limitations

🚨 The current `Pipeline.Build` implementation does not correctly accumulate subsequent steps into the returned estimator chain. Do not use it as a complete multi-step training workflow. An unknown estimator is not converted into a friendly validation error, and an empty pipeline can return null. These are current implementation limits, not supported behavior to rely on.

The database notes are a draft, not an installed training-task repository. There is no complete model lifecycle or training HTTP API. Loaded data can be enumerated later, so keep files available for the actual reading period and validate input size/column types.

See [ML.NET concepts](https://learn.microsoft.com/dotnet/machine-learning/resources/glossary) for terminology and [SKILL.md](SKILL.md) for implementation constraints.

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../Zongsoft.Plugins/README.md).

The manifest populates `Pipeline.Catalog` and registers loader/estimator settings drivers. Inspect the catalog after host initialization; loading the plugin does not train a model or install a persistence service.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Learning` | [Zongsoft.Learning.plugin](src/Zongsoft.Learning.plugin) |
| File copying and dependencies | [Zongsoft.Learning.deploy](src/Zongsoft.Learning.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft learning]
nuget:Zongsoft.Learning
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Learning.option`, `Zongsoft.Learning.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
