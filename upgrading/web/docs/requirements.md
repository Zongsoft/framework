# Zongsoft.Upgrading.Web 产品需求说明

## 背景

`Zongsoft.Upgrading.Web` 是自动升级插件库的 Web 服务端。它负责保存发布元数据、接收发布包上传、导入 `.manifest` 文件，并为升级客户端返回匹配当前运行环境的 Release 集合。

发布包 `.zip` 文件由 `Zongsoft.IO` 虚拟文件系统保存，服务端数据库只保存发布元数据、文件路径、包大小、校验码和升级状态相关信息。

## 目标

1. 提供兼容现有升级模型的 Web API 服务端。
2. 支持维护应用、应用版本名、实例、发布、发布属性、发布执行器和发布状态。
3. 支持 Release 元数据入库，覆盖 MySQL、PostgreSQL 和 SQLite。
4. 支持发布包 `.zip` 通过 `Zongsoft.IO` 虚拟文件系统保存。
5. 支持上传 `.manifest` 文件并导入 Release 元数据。
6. 支持创建 Release 时自动维护 Application 和 ApplicationEdition。
7. 支持升级客户端通过 GET Fetch 获取可升级发布集。
8. 支持通过评估器对候选 Release 做轻量升级控制。

## 非目标

1. 第一版不实现管理后台 UI，后续另建纯前端项目。
2. 本项目不实现通用认证授权体系，发布接口鉴权依赖宿主中的 `Zongsoft.Security` 和 `Zongsoft.Security.Web`。
3. 发布包二进制内容仅保存到 `Zongsoft.IO` 虚拟文件系统。
4. 第一版实体范围不包含 Package。

## 核心用户

1. 发布管理员：上传发布包、导入 manifest、维护发布信息、发布或废弃版本、配置升级评估条件。
2. 技术运维人员：维护实例、查看发布在实例上的升级状态和失败信息。
3. upgrader 客户端：发起 Fetch 请求、获取可升级发布集、下载升级包、按 `Release.Mode` 执行部署。

## 关键概念

### Application

应用表由系统自动维护，也提供 API 供手动维护。创建 Release 或导入 `.manifest` 时，服务端根据发布信息自动补齐对应的 Application 记录。

应用是否需要 License 授权由子表 ApplicationEdition 的 `Licensed` 字段表达。

### ApplicationEdition

ApplicationEdition 是 Application 的子表，表示某应用的版本名配置。它同样由系统自动维护，也提供 API 供手动维护。

核心字段包括：

- 应用编号。
- Edition 名称。
- `Licensed`：是否授权。
- 标题和描述。
- 启用状态。

### Release

发布元数据以现有 `Release` 类为基础，并包含 `Mode` 属性表示升级部署模式。`Mode` 是固定字段，不放入 `Properties`，以确保客户端和服务端具有一致语义。

`Mode` 枚举值：

- `Default`：默认模式，在客户端程序重启时进行升级部署。
- `Immediate`：尽快执行升级部署，不等待下次程序重启。

Release 的发布控制字段：

- `Deprecated`：是否废弃。
- `Published`：是否已发布，可被升级决策使用。
- `Visible`：是否对查询和管理场景可见。
- `EvaluatorName`：评估器名称。
- `EvaluatorSetting`：评估器设置。

### Evaluator

Evaluator 用于对候选 Release 做附加判断。当前内置 `Default` 评估器，它会把 `EvaluatorSetting` 解析为键值设置，并要求 Fetch 请求的 QueryString 或 Headers 中存在同名同值的参数。

### ReleaseProperty

Release 扩展属性采用主从表结构，表名为 `ReleaseProperty`。每条属性记录归属于一个 Release。

服务端可以通过 `Release.Path` 返回安装包地址，也可以通过扩展属性保存额外下载地址，例如 `Download.Url`。

### ReleaseExecutor

Release 执行器采用主从表结构，表名为 `ReleaseExecutor`。每条执行器记录归属于一个 Release。

### Instance

Instance 表示安装了应用的客户端实例。其唯一索引是客户端机器唯一编号 `InstanceCode`，实例还包含名称、标签、描述和配置信息等。

### ReleasePublishing

ReleasePublishing 表示某个发布在某个实例上的升级发布状态，主键为 `ReleaseId + InstanceId`。

发布状态值：

- `Fetch`
- `Downloading`
- `Downloaded`
- `Upgrading`
- `Upgraded`
- `Completed`

## 业务流程

### 发布流程

1. 管理端或工具创建 Release 元数据，或上传 `.manifest` 由服务端导入 Release 元数据。
2. 服务端根据 Release 信息自动维护 Application 和 ApplicationEdition。
3. 管理端或工具通过 `/Upgrading/Releases/{id}/Upload` 上传 `.zip` 包。
4. 服务端通过 `Zongsoft.IO` 虚拟文件系统保存 zip。
5. 上传成功后更新 `Release.Path`、`Release.Size` 和 `Release.Checksum`。
6. 管理员设置 `Published`、`Visible` 和 `Deprecated`。
7. 如需灰度或条件控制，管理员设置 `EvaluatorName` 和 `EvaluatorSetting`。

### Fetch 流程

1. 客户端发起 `GET /Upgrading/Upgrader/{name}/{edition?}`。
2. QueryString 提供平台、架构、当前版本、目标版本等参数。
3. 服务端把 QueryString 与 Headers 合并为评估参数集合。
4. 服务端查询满足发布状态、平台、架构和版本名的候选 Release。
5. 如果 Release 指定了评估器，则调用评估器判断是否返回。
6. 服务端把模型转换为客户端升级协议中的 Release 对象。
7. 如果 `Release.Path` 不为空，则解析为 `Zongsoft.IO` 可访问 URL。
8. 服务端返回 Release 集合。

### 状态维护流程

1. 客户端或管理端通过 Instance API 维护安装实例。
2. 客户端或管理端通过 ReleasePublishing API 维护发布在实例上的状态。
3. 发布状态可按 Release 或 Instance 两个维度查询与写入。

## 验收标准

1. Release 支持标准 CRUD，并支持上传 `.zip` 包。
2. 上传 `.zip` 成功后，`Release.Path`、`Release.Size` 和 `Release.Checksum` 被更新。
3. Release 元数据可以写入 MySQL、PostgreSQL 或 SQLite。
4. 创建 Release 或导入 `.manifest` 时可以自动维护 Application。
5. 创建 Release 或导入 `.manifest` 时可以自动维护 ApplicationEdition。
6. Fetch 可以按 Name、Edition、Platform、Architecture、Published、Visible、Deprecated 筛选 Release。
7. Fetch 可以使用内置 `Default` 评估器按 QueryString 或 Header 参数做附加判断。
8. 返回 Release 集合可被客户端解析。
9. `Release.Mode` 可以写入、读取、入库并输出给客户端。
10. Instance 和 ReleasePublishing 能反映客户端安装实例及其升级发布状态。
