# Zongsoft.Diagnostics Diagnostics Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Diagnostics)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Diagnostics)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**D**iagnostics](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Diagnostics) is the diagnostics plugin library for the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides diagnostics and telemetry features based on [_**O**pen**T**elemetry_](https://opentelemetry.io).

The [Zongsoft.Diagnostics.option](src/Zongsoft.Diagnostics.option) configuration file defines the default _**O**pen**T**elemetry_ metric and trace exporters, together with the [_**P**rometheus_](https://prometheus.io) metric exporter and the [_**Z**ipkin_](https://zipkin.io) trace exporter.

## Bundled OpenTelemetry Protocol

The [`proto`](proto) directory is a Git submodule pinned to the upstream [OpenTelemetry Protocol repository](https://github.com/open-telemetry/opentelemetry-proto). It contains the [OTLP protocol specification](proto/docs/specification.md) and the corresponding language-independent interface types ([`.proto` files](proto/opentelemetry/proto)). Keep this submodule clean: protocol documentation and project-specific guidance belong in this README rather than in the upstream working tree.

### Language-Independent Interface Types

The Proto files can be consumed as a Git submodule or copied and built directly in a consumer project. OpenTelemetry client libraries publish compiled artifacts to central repositories such as Maven. Changes to the upstream definitions must follow the [OpenTelemetry Proto contribution guidelines](proto/CONTRIBUTING.md).

### OTLP/JSON

Additional requirements for the OTLP/JSON wire representation are defined by the [JSON Protobuf encoding specification](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/otlp.md#json-protobuf-encoding).

### Generating gRPC Client Libraries

The upstream repository generates raw gRPC client libraries with `make gen-${LANGUAGE}`. Supported targets currently include:

- cpp
- csharp
- go
- java
- objc
- openapi (Swagger)
- php
- python
- ruby

### Maturity

Releases numbered 1.0.0 and later may still contain unstable alpha or beta components, as indicated below.

| Component | Binary Protobuf maturity | JSON maturity |
| --- | --- | --- |
| common/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| resource/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| metrics/\*<br>collector/metrics/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| trace/\*<br>collector/trace/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| logs/\*<br>collector/logs/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| profiles/\*<br>collector/profiles/* | Development | [Development](proto/docs/specification.md#json-protobuf-encoding) |

See [Versioning and Stability](https://github.com/open-telemetry/opentelemetry-specification/blob/a08d1f92f62acd4aafe4dfaa04ae7bf28600d49e/specification/versioning-and-stability.md) for the definition of maturity levels.

### Stability Guarantees

Components marked `Stable` guarantee that existing field types, numbers, and names; service and package names; method names, parameters, return types, and invocation kinds; message and enum symbols; package names and directory structure; and existing `optional` or `repeated` declarations will not change incompatibly. Existing symbols will not be deleted.

Compatible additive changes may introduce:

- New fields in existing messages.
- New messages or enums.
- New enum choices.
- New choices in existing `oneof` fields.
- New services.
- New methods in existing services.

Every additive change must explain how senders and receivers implemented against protocol versions from before and after the change interoperate.

### Experiments

New experimental components should be isolated in a `development` subdirectory and use `Development`, `Alpha`, `Beta`, or `Release Candidate` maturity labels. Experimental components that are not referenced by a stable component may be removed when an experiment ends. Successful experiments require review before they become `Stable` and inherit the full stability guarantees.

Experimental fields or messages added to stable components must remain compatible and be clearly marked as non-stable. If an experiment is abandoned, those fields or messages must remain in place and may only be deprecated; senders should normally leave them empty, and receivers must continue to tolerate empty values.

### Generated Code

The upstream project does not guarantee the stability of code produced by any particular Proto code generator.

### Upstream Maintainers and Approvers

- Maintainers: [OpenTelemetry Technical Committee](https://github.com/open-telemetry/community/blob/main/community-members.md#technical-committee)
- Approvers: [OpenTelemetry Specification Sponsors](https://github.com/open-telemetry/community/blob/main/community-members.md#specifications-and-proto)

See the OpenTelemetry community [membership guide](https://github.com/open-telemetry/community/blob/main/guides/contributor/membership.md#maintainer) for role details.
