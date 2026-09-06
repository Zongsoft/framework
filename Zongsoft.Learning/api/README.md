# Zongsoft.Learning.Web Machine Learning Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Learning.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Learning.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**L**earning.**W**eb](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Learning/api) is a sub-plugin of the machine learning plugin library for the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides _**W**eb_ plugin support for the machine learning feature set.

The current package provides a controller for reading the runtime machine-learning pipeline catalog in development and administration interfaces. It does not train models, upload datasets, run predictions or persist pipelines. Controller discovery and HTTP route mapping are separate steps; the current source does not provide a directly accessible default route.

## Installation and Deployment

```shell
dotnet add package Zongsoft.Learning.Web
```

Deploy `Zongsoft.Learning.Web.plugin` in a plugin-aware web host. It depends on `Zongsoft.Learning`; load the core plugin and all assemblies that register pipeline catalogs or estimators before querying the endpoint.

🚨 The current source has a build compatibility issue with Core: the `PropertyInfo` parameter in `TextFileLoaderSettings.Populate` does not match the base class's `MemberInfo` signature, producing `CS0115` in a targeted build. Until this is fixed or mutually compatible versions are selected, this page is not a verified deployment tutorial. This documentation review did not change implementation code or run training.

## Endpoint

[PipelineController](Controllers/PipelineController.cs) declares the `MachineLearning` area and `[HttpGet]`, but neither `[Route]` nor Zongsoft's `[ControllerName]`. The default Plugins.Web host calls `MapControllers()`; an Area alone does not generate a URL. The action is intended to return:

- `Catalogs`: the catalog groups currently registered in `Pipeline.Catalog`;
- `Estimators`: the registered estimator/component descriptors.

Do not assume that `GET /MachineLearning/Pipelines`, or its singular form, is available. An application adopting this controller must explicitly supply an appropriate MVC routing convention at composition time, or expose the required catalog view through its own routed controller. Inspect the host's endpoint collection and send an HTTP request after startup. If the endpoint is absent from OpenAPI, check route metadata before concluding that plugin loading failed.

> 💡 A missing estimator usually means its assembly or service registration was not loaded; this Web package does not scan arbitrary directories or install ML.NET components by itself.

## Security and Stability

🚨 Catalog metadata can reveal model capabilities and implementation details. Protect the endpoint in non-development environments and do not expose configuration secrets inside estimator descriptors.

The response is a snapshot of process-local registrations. Ordering and descriptor details are discovery metadata, not a stable public interchange schema. Cache only if the host's plugin set is immutable for the cache lifetime.

## Related Resources

- [Machine-learning concepts and core plugin](../README.md)
- [Zongsoft.Web conventions](../../Zongsoft.Web/README.md)
- [ML.NET documentation](https://learn.microsoft.com/dotnet/machine-learning/)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

Use a Plugins.Web host and deploy the corresponding main adapter listed below. Service/controller discovery runs during host initialization; do not put business logic into Program.cs. Verify the configured endpoint and authorization before accepting requests.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Learning.Web` | [Zongsoft.Learning.Web.plugin](Zongsoft.Learning.Web.plugin) |
| File copying and dependencies | [Zongsoft.Learning.Web.deploy](Zongsoft.Learning.Web.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft learning]
nuget:Zongsoft.Learning

[plugins zongsoft learning web]
nuget:Zongsoft.Learning.Web
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Learning.Web.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
