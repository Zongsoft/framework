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

## Background and Key Code

OTLP is OpenTelemetry's transport protocol. This sample consumes already-converted framework meter collections, not raw protobuf bytes. [MetricHandler.cs](MetricHandler.cs) derives from a typed HandlerBase and prints entries; it contains no database storage or retry queue.

The host must support gRPC over HTTP/2. Use a local test endpoint and synthetic metrics without user identifiers. Source generation depends on the checked-out proto submodule, which must remain unmodified.

## Expected Scope and Cleanup

A printed meter proves that this request reached the sample handler. It does not prove lossless conversion of all metric types/resources, nor persistence. Review the [server limitations](../README.md) before using it to evaluate a receiver.

After testing, stop the exporter first, then stop the host and remove only this sample's assembly/manifest from the test host's plugin directory if no longer needed. Protect redirected console output; no sample-owned database requires cleanup.

💡 No console output can also mean that no matching metric was emitted. Start with one explicitly recorded synthetic measurement before investigating batches.
