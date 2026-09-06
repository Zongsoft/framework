# Zongsoft.Externals.Aliyun Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Aliyun)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Aliyun)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**E**xternals.**A**liyun](https://github.com/Zongsoft/framework/tree/main/externals/aliyun) integrates selected [Alibaba Cloud](https://www.alibabacloud.com/) services with the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) framework.

## Features

- Exposes OSS buckets through the Zongsoft file-system abstraction and the `zfs.oss` scheme.
- Provides queue and topic access for Alibaba Cloud Message Service.
- Supplies an MQTT connection-setting driver for the framework's messaging services.
- Supports SMS and voice calls through the telecom transmitter and commands.
- Supports mobile push notifications and the corresponding command integration.

Load `Zongsoft.Externals.Aliyun.plugin` and configure `/Externals/Aliyun` with the required service center, certificate, bucket, messaging, telecom, or push application settings. The packaged [option file](src/Zongsoft.Externals.Aliyun.option) documents the configuration hierarchy and placeholder values.

## Installation and Service Selection

```shell
dotnet add package Zongsoft.Externals.Aliyun
```

The `general` section selects a default region/service center and whether intranet endpoints are used. Named certificates contain an Access Key ID and secret. Each OSS bucket, messaging queue/topic, telecom template, or push application can override region and certificate selection.

```xml
<option path="/Externals/Aliyun">
	<general name="Shenzhen" intranet="false">
		<certificates default="main">
			<certificate certificate.name="main"
			             code="REPLACE_ACCESS_KEY_ID"
			             secret="REPLACE_ACCESS_KEY_SECRET" />
		</certificates>
	</general>
</option>
```

> 💡 Prefer instance/workload credentials when the deployment supports them. When static keys are unavoidable, inject them from a secret provider and use separate least-privilege credentials per service boundary.

## Application Workflows

- Address OSS objects through the registered `zfs.oss` file system; bucket and key semantics follow object storage rather than a local disk.
- Resolve the Aliyun queue/topic provider through Zongsoft messaging contracts, then publish, subscribe, and acknowledge using the shared message model.
- Resolve `ITransmitter` for SMS or use `Phone.Send`; voice calls use `Phone.Call`. Template names map to configured provider template codes.
- Use `Pushing.Send` or `PushingSender` for configured mobile applications and target types.

The library signs HTTP requests with `HttpAuthenticator` and selects public or internal service centers from configuration. Provider error payloads are surfaced as `AliyunException`; capture correlation data, not credentials, when diagnosing them.

## Operational Boundaries

🚨 Telecom, push, storage, and messaging calls can incur cost or cause irreversible external effects. Validate recipients and object paths, rate-limit operations, configure retries only for idempotent calls, and keep tests opt-in against dedicated cloud resources.

Cloud APIs impose quotas, payload limits, region constraints, eventual consistency, template approval, and callback requirements. The adapter does not remove those constraints; consult the relevant [Alibaba Cloud documentation](https://www.alibabacloud.com/help/) for the enabled service.

## Related Resources

- [Complete option hierarchy](src/Zongsoft.Externals.Aliyun.option)
- [Callback gateway](gateway/README.md)
- [External adapter implementation guidance](../SKILL.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

The manifest registers Alibaba Cloud connection drivers, file-system providers and phone/message commands. Keep certificates and service settings in deployment-owned options; resolving a provider does not authorize cloud calls.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Aliyun` | [Zongsoft.Externals.Aliyun.plugin](src/Zongsoft.Externals.Aliyun.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Aliyun.deploy](src/Zongsoft.Externals.Aliyun.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals aliyun]
nuget:Zongsoft.Externals.Aliyun
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Aliyun.plugin`, `Zongsoft.Externals.Aliyun.option`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
