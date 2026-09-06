# Zongsoft.Externals.Amazon Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Amazon)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Amazon)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**E**xternals.**A**mazon](https://github.com/Zongsoft/framework/tree/main/externals/amazon) integrates Amazon Web Services with the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) framework. The current package focuses on Amazon S3 and exposes object storage through Zongsoft's file-system abstraction.

## S3 File System

The plugin registers `S3FileSystem` under `/Workbench/FileSystem`. It supports file and directory operations on S3 buckets, uses the `amazon.s3` connection-setting driver, and addresses resources with the `zfs.s3` scheme. AWS regions, service endpoints, access keys, and secret keys are supplied through the packaged option file or the host configuration.

Load `Zongsoft.Externals.Amazon.plugin` and configure `/Externals/Amazon/ConnectionSettings` before resolving the file system. The driver also accepts S3-compatible endpoints when a custom server address is configured. See the [tests](test) for file-system examples.

## Installation and Configuration

```shell
dotnet add package Zongsoft.Externals.Amazon
```

```xml
<option path="/Externals/Amazon">
	<connectionSettings default="production">
		<connectionSetting connectionSetting.name="production"
		                   driver="amazon.s3"
		                   value="region=us-east-1;accessKey=REPLACE_ME;secretKey=REPLACE_ME" />
	</connectionSettings>
</option>
```

Settings include `server`/`url`, `region`, `client`, `timeout`, `accessKey`, `secretKey`, and `accountId`. A custom `server` enables path-style addressing for compatible endpoints.

## File-System Paths

The first path segment is the bucket and the remainder is the object key:

```csharp
using Zongsoft.IO;

var path = "zfs.s3:/application-assets/manuals/start.pdf";
await using(var stream = await FileSystem.File.OpenAsync(path, FileMode.OpenOrCreate))
{
	await source.CopyToAsync(stream, cancellationToken);
}

var info = await FileSystem.File.GetInfoAsync(path, cancellationToken);
```

Directory operations emulate hierarchy through object-key prefixes; S3 itself has no real directories. Renames/copies may require copying objects, and listing is paged by the remote service.

> 💡 Prefer workload identities or an external credential provider where available. If keys must be configured, inject them at deployment time rather than committing the option file.

🚨 S3 writes and deletes affect remote durable data. Apply least-privilege bucket policies, encryption, versioning and lifecycle rules; validate bucket/key input to prevent tenant crossover. S3-compatible services can differ in metadata, append, consistency, multipart, and URL behavior.

Dispose opened streams promptly. Integration tests make real network calls and mutate a configured disposable bucket; they must remain opt-in.

## Related Resources

- [Amazon S3 documentation](https://docs.aws.amazon.com/s3/)
- [File-system tests](test)
- [External adapter implementation guidance](../SKILL.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

The manifest mounts the S3 file-system provider. Configure `/Externals/Amazon/ConnectionSettings`, then use `zfs.s3` paths through the framework file system; a package reference alone does not register that scheme.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Amazon` | [Zongsoft.Externals.Amazon.plugin](src/Zongsoft.Externals.Amazon.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Amazon.deploy](src/Zongsoft.Externals.Amazon.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals amazon]
nuget:Zongsoft.Externals.Amazon
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Amazon.plugin`, `Zongsoft.Externals.Amazon.option`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
