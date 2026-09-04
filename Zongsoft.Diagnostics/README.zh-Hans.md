# Zongsoft.Diagnostics 诊断插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Diagnostics)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Diagnostics)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**D**iagnostics](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Diagnostics) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的诊断插件库，提供了基于 [_**O**pen**T**elemetry_](https://opentelemetry.io) 诊断遥测的功能集。

通过 [Zongsoft.Diagnostics.option](src/Zongsoft.Diagnostics.option) 配置文件默认定义了 _**O**pen**T**elemetry_ 标准的指标 _(Metric)_、跟踪 _(Trace)_ 导出器，以及 [_**P**rometheus_](https://prometheus.io) _指标_ 导出器 和 [_**Z**ipkin_](https://zipkin.io) _跟踪_ 导出器。

## 内置 OpenTelemetry 协议

[`proto`](proto) 目录是指向上游 [OpenTelemetry Protocol 仓库](https://github.com/open-telemetry/opentelemetry-proto)的 Git 子模块，其中包含 [OTLP 协议规范](proto/docs/specification.md)以及对应的语言无关接口类型（[`.proto` 文件](proto/opentelemetry/proto)）。该子模块必须保持干净；协议说明和本项目专用指引应维护在本 README 中，不得写入上游工作树。

### 语言无关接口类型

Proto 文件可以作为 Git 子模块使用，也可以复制到使用方项目中直接构建。OpenTelemetry 的各语言客户端库会把编译产物发布到 Maven 等中央仓库。修改上游定义前必须遵循 [OpenTelemetry Proto 贡献指南](proto/CONTRIBUTING.md)。

### OTLP/JSON

OTLP/JSON 在线路表示方面的附加要求参见 [JSON Protobuf 编码规范](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/otlp.md#json-protobuf-encoding)。

### 生成 gRPC 客户端库

上游仓库使用 `make gen-${LANGUAGE}` 生成原始 gRPC 客户端库，目前支持：

- cpp
- csharp
- go
- java
- objc
- openapi（Swagger）
- php
- python
- ruby

### 成熟度

从 1.0.0 开始，发行版仍可能包含下表中标记为不稳定（alpha 或 beta）的组件。

| 组件 | Binary Protobuf 成熟度 | JSON 成熟度 |
| --- | --- | --- |
| common/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| resource/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| metrics/\*<br>collector/metrics/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| trace/\*<br>collector/trace/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| logs/\*<br>collector/logs/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| profiles/\*<br>collector/profiles/* | Development | [Development](proto/docs/specification.md#json-protobuf-encoding) |

成熟度定义参见[版本与稳定性](https://github.com/open-telemetry/opentelemetry-specification/blob/a08d1f92f62acd4aafe4dfaa04ae7bf28600d49e/specification/versioning-and-stability.md)。

### 稳定性保证

标记为 `Stable` 的组件保证已有字段类型、编号和名称，服务及包名，方法名、参数、返回类型和调用种类，消息与枚举符号，包名与目录结构，以及现有 `optional` 或 `repeated` 声明不会发生不兼容变更，也不会删除已有符号。

允许以下兼容性增量变更：

- 向现有消息新增字段。
- 新增消息或枚举。
- 新增枚举选项。
- 向现有 `oneof` 字段新增选项。
- 新增服务。
- 向现有服务新增方法。

所有增量变更都必须说明分别实现变更前后协议版本的发送方与接收方如何互操作。

### 实验

全新实验组件应隔离在 `development` 子目录中，并使用 `Development`、`Alpha`、`Beta` 或 `Release Candidate` 成熟度标识。未被稳定组件引用的实验组件可以在实验结束时删除；成功的实验必须经过审查后才能标记为 `Stable`，并自此遵守完整的稳定性保证。

稳定组件中新增的实验字段或消息必须保持兼容，并明确标记为非稳定状态。如果实验失败，这些字段或消息仍须保留，只能标记为弃用；发送方通常应将其留空，接收方必须持续容忍空值。

### 生成代码

上游项目不保证任何特定 Proto 代码生成器所生成代码的稳定性。

### 上游维护者与批准者

- 维护者：[OpenTelemetry Technical Committee](https://github.com/open-telemetry/community/blob/main/community-members.md#technical-committee)
- 批准者：[OpenTelemetry Specification Sponsors](https://github.com/open-telemetry/community/blob/main/community-members.md#specifications-and-proto)

角色说明参见 OpenTelemetry 社区的[成员指南](https://github.com/open-telemetry/community/blob/main/guides/contributor/membership.md#maintainer)。
