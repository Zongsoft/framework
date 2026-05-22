# Zongsoft.Upgrading.Web 产品需求定义

## 背景

现有 `upgrader` 客户端只能基于应用名称、版本名、版本号、平台和架构进行本地过滤。这个能力适合基础升级，但不能表达更细的升级控制，例如指定机器、指定 IP、指定网卡物理地址、指定 Site、指定环境或 License 状态。

`Zongsoft.Upgrading.Web` 的职责是作为服务端升级决策中心，接收客户端画像，结合 Release 元数据、License 状态和升级控制条件，返回适合该客户端的可升级发布集。发布包的 `.zip` 文件由 `Zongsoft.IO` 虚拟文件系统保存到 S3，服务端数据库只保存发布元数据、发布包路径和升级控制相关信息。

## 目标

1. 提供兼容现有升级模型的 Web API 服务端。
2. 支持通过 POST Fetch 上传客户端画像。
3. 支持基于 License、InstanceCode、IP、MAC、Edition、Site、环境、硬件和配置的升级控制。
4. 支持 Release 元数据入库，支持 MySQL 和 SQLite。
5. 支持发布包 `.zip` 通过 `Zongsoft.IO` 虚拟文件系统保存到 S3。
6. 支持返回现有客户端可解析的 Release XML 集合。
7. 支持维护应用、应用版本名、实例、发布状态关系、发布、发布属性和发布执行器。

## 非目标

1. 第一版不实现管理后台 UI，后续另建纯前端项目。
2. 本项目不实现通用认证授权体系，发布接口鉴权依赖宿主中的 `Zongsoft.Security` 和 `Zongsoft.Security.Web`。
3. 发布包二进制内容仅保存到 `Zongsoft.IO` 虚拟文件系统。
4. 第一版实体范围不包含 Package。

## 核心用户

1. 发布管理员：上传发布包、维护发布信息、发布或废弃版本、配置升级控制条件。
2. 技术运维人员：维护实例、查看发布在实例上的升级状态和失败信息。
3. upgrader 客户端：发起 Fetch 请求、获取可升级发布集、使用发布属性中的 `Download.Url` 下载升级包、按 `Release.Mode` 执行部署。

## 关键概念

### Application

应用表由系统自动维护，也提供 API 供手动维护。创建 Release 记录时，服务端根据发布信息自动补齐对应的 Application 记录。

应用是否需要 License 授权由子表 ApplicationEdition 的 `Licenced` 字段表达。

### ApplicationEdition

ApplicationEdition 是 Application 的子表，表示某应用的版本名配置。它同样由系统自动维护，也提供 API 供手动维护。

核心字段包括：

- 应用标识。
- Edition 名称。
- `Licenced`：是否需要 License 授权。
- 标题和描述。
- 启用状态。

### Release

发布元数据以现有 `Release` 类为基础，并包含 `Mode` 属性表示升级部署模式。`Mode` 是固定字段，不放入 `Properties`，以确保客户端和服务端具有一致语义。

`Mode` 建议枚举值：

- `Default`：默认模式，在客户端程序重启时进行升级部署。
- `Immediate`：尽快执行升级部署，不等待下次程序重启。

Release 的发布控制字段：

- `Deprecated`：是否废弃。
- `Published`：是否已发布，可被升级决策使用。
- `Visible`：是否对查询和管理场景可见。
- `FilterName`：过滤器名称。
- `FilterData`：过滤器数据。
- `FilterSetting`：过滤器设置。

### ReleaseProperty

Release 扩展属性采用主从表结构，表名为 `ReleaseProperty`。每条属性记录归属于一个 Release。

服务端返回给客户端的发布集中，每个 Release 的扩展属性应包含 `Download.Url`。该属性指向升级包的下载地址，通常是可访问的 S3 存储对象公共地址。

### ReleaseExecutor

Release 执行器采用主从表结构，表名为 `ReleaseExecutor`。每条执行器记录归属于一个 Release。

### Instance

Instance 表示安装了应用的客户端实例。其唯一索引是客户端机器唯一编号 `InstanceCode`，实例还包含名称、标签、描述和配置信息等。

### ReleasePublishing

ReleasePublishing 表示某个发布在某个实例上的升级发布状态，主键为 `ReleaseId + InstanceId`。

发布状态建议值：

- `Fetch`
- `Downloading`
- `Downloaded`
- `Upgrading`
- `Upgraded`
- `Completed`

### Client Profile

客户端画像由 Fetch 请求 Body 上传。Body 始终为键值对 JSON，便于服务端统一处理和升级控制匹配。

典型键包括：

- `Site`
- `Environment`
- `License.*`
- `Configuration.*`
- `Hardware.Identifier`
- `Hardware.Mainboard.*`
- `Hardware.Processors.*`
- `Hardware.Memories.*`
- `Hardware.Storages.*`
- `Hardware.Networks.*`
- `Network.IP`
- `Network.MACs`

硬件结构参考 `Zongsoft.IO.Hardwares.HardwareProfile`，其核心语义包含 `Identifier`、`Mainboard`、`Processors`、`Memories`、`Storages`、`Networks` 和 `Devices`。

## 业务流程

### 发布流程

1. 管理端或工具创建 Release 元数据。
2. 管理端或工具通过 `/upgrading/releases` 对应的 ReleaseController 上传 `.zip` 包。
3. 服务端通过 `Zongsoft.IO` 虚拟文件系统保存 zip 到 S3。
4. 上传成功后更新 `Release.Path` 为虚拟文件系统完整路径。
5. 服务端校验 zip 大小和校验码。
6. 服务端根据 Release 信息自动维护 Application 和 ApplicationEdition。
7. 管理员设置 `Published`、`Visible` 和 `Deprecated`。

### Fetch 决策流程

1. 客户端发起 `POST /upgrading/upgrader/{name}/{edition?}`。
2. QueryString 提供平台、架构、当前版本和目标版本等基础参数。
3. Body 以键值对 JSON 提供 License、Site、环境、配置和硬件画像。
4. 服务端登记或更新 Instance。
5. 服务端为返回给该实例的发布登记或更新 ReleasePublishing。
6. 服务端根据 ApplicationEdition 的 `Licenced` 判断是否需要 License。
7. 服务端查询候选 Release。
8. 服务端应用升级控制条件。
9. 服务端为每个返回的 Release 附加 `Download.Url` 扩展属性。
10. 服务端返回 Release XML 集合。

### Download 流程

1. 客户端从 Release 扩展属性 `Download.Url` 获取下载地址。
2. 下载地址通常指向可访问的 S3 存储对象公共地址。
3. 客户端下载、升级和部署过程中，服务端维护 ReleasePublishing 的状态。

## 验收标准

1. Release 支持标准 CRUD，并支持上传 `.zip` 包。
2. 上传 `.zip` 成功后，`Release.Path` 更新为 `Zongsoft.IO` 虚拟文件系统完整路径。
3. Release 元数据可以写入 MySQL 或 SQLite。
4. 创建 Release 时可以自动维护 Application 和 ApplicationEdition。
5. Fetch 请求可以携带键值对 JSON。
6. 升级控制可以按 InstanceCode、IP、MAC、Edition、Site、环境命中。
7. ApplicationEdition 标记为需要 License 时，License 过期客户端不能获取升级发布。
8. 返回 Release XML 可被客户端解析。
9. `Release.Mode` 可以写入、读取、入库、输出到 Release XML。
10. Instance 和 ReleasePublishing 能反映客户端安装实例及其升级发布状态。

