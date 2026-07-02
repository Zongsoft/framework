# Zongsoft.Learning

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Learning)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Learning)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README-zh_CN.md)

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
