# Zongsoft.Diagnostics.Protocols.Server Samples

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

This sample is a plugin for the OpenTelemetry gRPC server integration. It registers `MetricHandler` at `/Workbench/Diagnostics/Telemetry/Listener/Metrics`; every received meter collection is dumped to the host terminal.

Unlike the executable samples, this project must be loaded by a Zongsoft application that already hosts `Zongsoft.Diagnostics.Protocols.Server`.

## Build and Load

Build the sample from the repository root:

```shell
dotnet build Zongsoft.Diagnostics/protocols/server/samples/Zongsoft.Diagnostics.Protocols.Server.Samples.csproj
```

Deploy the generated assembly together with `Zongsoft.Diagnostics.Protocols.Server.Samples.plugin`, then add the sample plugin to the host after the `Zongsoft.Diagnostics.Protocols.Server` dependency. The packaged `.deploy` file lists the artifacts required by a plugin deployment.

## Verify

1. Start the Zongsoft host with the diagnostics protocol server and this sample plugin loaded.
2. Configure an OpenTelemetry client or collector to export metrics to the host's OTLP gRPC endpoint.
3. Produce application metrics.
4. Confirm that `MetricHandler` writes each received meter and its contents to the terminal.

If no output appears, verify the OTLP endpoint, transport security, server plugin activation, and the `/Workbench/Diagnostics/Telemetry/Listener/Metrics` registration path.
