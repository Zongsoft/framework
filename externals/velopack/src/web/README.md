# Zongsoft.Externals.Velopack Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Velopack.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Velopack.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.Velopack.Web` publishes Velopack asset-feed metadata from a Zongsoft web host. `VelopackFileScanner` recursively reads `releases.*.json` files from the application's `releases` directory, and `VelopackController` returns their assets as a `VelopackAssetFeed`.

## Installation and Feed Layout

```shell
dotnet add package Zongsoft.Externals.Velopack.Web
```

Deploy the Web plugin and place feed JSON plus the package files referenced by it under the host's managed release location. The metadata endpoint is:

```http
GET /Velopack/Releases?id=MyApplication
Accept: application/json
```

The optional `id` filters `PackageId`. The current `os` and `arch` query branches do not filter results, and the optional route `name` is not used by the scanner. Do not document or depend on those parameters until the implementation changes.

> 💡 The scanner aggregates every matching JSON feed recursively. Keep release directories isolated by host or package id to avoid unintentionally mixing channels.

## Publishing and Security

Generate feeds and packages with a compatible Velopack toolchain, publish files atomically, and verify every URL from the same perspective as a client. Configure static-file/object-storage serving separately for package binaries.

🚨 Update feeds are a software-supply-chain boundary. Require HTTPS, strong publisher access control, signed packages, immutable release artifacts, audit logs and safe cache rules. Never allow unauthenticated upload into the scanned directory.

Large directory trees are scanned per request and JSON is read into memory. Bound file count/size, use caching at an appropriate layer, and monitor malformed feeds and duplicate assets.

## References

Velopack documentation
> https://docs.velopack.io

Velopack reference
> https://docs.velopack.io/reference

Velopack source code
> https://github.com/velopack/velopack

- [Client updater](../../README.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../../../Zongsoft.Plugins/README.md).

Use a Web host and configure a dedicated releases directory. The release-list endpoint reads files; it does not create, sign or publish update packages.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Velopack.Web` | [Zongsoft.Externals.Velopack.Web.plugin](Zongsoft.Externals.Velopack.Web.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Velopack.Web.deploy](Zongsoft.Externals.Velopack.Web.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals velopack web]
nuget:Zongsoft.Externals.Velopack.Web
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Velopack.Web.option`, `Zongsoft.Externals.Velopack.Web.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
