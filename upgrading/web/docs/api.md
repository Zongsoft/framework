# Zongsoft.Upgrading.Web API 说明

## 设计原则

1. 遵循 `D:\Zongsoft\guidelines\zongsoft.rest-api.guidelines.md`。
2. 数据库物理表名使用 `Upgrading_` 前缀，实体类名采用单数形式。
3. API 路径资源名采用复数形式。
4. 资源型 CRUD API 使用 `ServiceController` 或 `SubserviceController`。
5. 升级客户端获取发布集使用专门的 `UpgraderController`。
6. API 版本建议通过请求头承载，例如 `API-Version: 1.0`。

## CRUD 资源 API

下列资源使用数据服务控制器暴露标准 CRUD 能力：

| 实体/表 | 路径 | 说明 |
| --- | --- | --- |
| Application | `/Upgrading/Applications` | 应用定义 |
| ApplicationEdition | `/Upgrading/Applications/{applicationId}/Editions` | 应用版本名定义，Application 的子资源 |
| Release | `/Upgrading/Releases` | 发布元数据和发布包上传 |
| ReleaseProperty | `/Upgrading/Releases/{releaseId}/Properties` | Release 扩展属性 |
| ReleaseExecutor | `/Upgrading/Releases/{releaseId}/Executors` | Release 执行器 |
| Instance | `/Upgrading/Instances` | 客户端安装实例 |
| ReleasePublishing | `/Upgrading/Releases/{releaseId}/Publishings` | 发布与实例的升级状态关系 |
| ReleasePublishing | `/Upgrading/Instances/{instanceId}/Publishings` | 实例维度的升级状态关系 |

标准行为：

- `GET /resources`：查询资源集。
- `GET /resources/{id}`：获取单个资源。
- `POST /resources`：创建资源。
- `PUT /resources`：新增或更新资源。
- `PATCH /resources/{id}`：部分更新资源。
- `DELETE /resources/{id}`：删除资源。
- `POST /resources/Query`：复杂查询，仅适用于定义了查询条件的服务。
- `POST /resources/Count`：按条件统计数量。
- `POST /resources/Exists`：按条件判断是否存在。
- `POST /resources/Import`：导入文件。

## Release 上传

`/Upgrading/Releases` 对应 `ReleaseController`，除了标准增删改查，还提供发布包 `.zip` 上传和 `.manifest` 导入。

上传发布包：

```http
POST /Upgrading/Releases/{id}/Upload?overwrite=true
Content-Type: multipart/form-data
```

也支持等价路径：

```http
POST /Upgrading/Releases/Upload/{id}?overwrite=true
Content-Type: multipart/form-data
```

上传成功后：

1. 服务器根据 `/Upgrading/Settings` 下 `server` 设置的 `storage` 值确定保存根路径，当前默认值为 `zfs.s3:/upgrading/releases`。
2. 文件写入 `Zongsoft.IO` 虚拟文件系统。
3. `Release.Path` 更新为虚拟文件系统文件地址。
4. `Release.Size` 更新为上传文件大小。
5. `Release.Checksum` 根据已保存文件重新计算。
6. 如果数据库更新失败，已经写入的文件会被删除。

当前服务端没有实现独立的 `GET /content` 下载接口。升级客户端通过 Fetch 返回的 `Release.Path` 或发布属性中的下载地址获取安装包。

## Manifest 导入

`.manifest` 文件通过 Release 的导入接口上传：

```http
POST /Upgrading/Releases/Import?format=manifest
Content-Type: multipart/form-data
```

导入职责：

1. 调用 `Release.LoadAsync` 解析 `.manifest`。
2. 生成 Release 元数据。
3. 导入 manifest 中的属性集和执行器集。
4. 写入 Release 后自动同步 Application 和 ApplicationEdition。
5. 返回成功导入的 Release 集合；没有导入内容时返回 `204 No Content`。

## Application 自动同步

创建 Release 或导入 manifest 时，`ReleaseService` 会根据 `Release.Name` 自动维护应用信息：

1. 如果 Application 已存在，并且 Release.Edition 非空且不是 `_`，则尝试创建对应 ApplicationEdition。
2. 如果 Application 不存在，则创建 Application。
3. 如果 Release.Edition 非空且不是 `_`，则同时创建 ApplicationEdition。
4. 自动同步使用忽略约束冲突的写入方式，避免重复发布时打断流程。

## Upgrader Fetch API

升级客户端通过该接口获得可升级的发布集：

```http
GET /Upgrading/Upgrader/{name}/{edition?}?Platform={platform}&Architecture={architecture}&CurrentlyVersion={version}&UpgradingVersion={version}
Accept: application/xml
```

说明：

- `{name}` 为应用名称，不能为空。
- `{edition}` 为版本名，可省略；空值或 `_` 表示无版本名。
- `Platform` 和 `Architecture` 为当前必需的筛选参数。
- `CurrentlyVersion` 与 `UpgradingVersion` 会进入评估参数集合，供评估器使用。
- 服务端会把 QueryString 和请求头合并成评估参数集合，键名不区分大小写。
- 响应为 Release 集合，具体格式由 ASP.NET Core 内容协商决定；升级客户端通常使用 XML。

当前候选发布至少满足：

- `Name` 等于路由中的应用名称。
- `Published = true`。
- `Visible = true`。
- `Deprecated = false`。
- `Platform` 匹配请求参数。
- `Architecture` 匹配请求参数。
- `Edition` 匹配路由版本名；无版本名时匹配空值或 `_`。

响应：

- `200 OK`：返回 Release 集合，无可升级发布时为空集合。
- `400 Bad Request`：应用名称等必需参数错误。

## Evaluator API

可用评估器列表：

```http
GET /Upgrading/Upgrader/Evaluators
```

默认评估器名为 `Default`。当 Release 设置了 `EvaluatorName` 且模块中存在同名评估器时，服务端会调用评估器。默认评估器会把 `EvaluatorSetting` 解析成键值设置，并要求每个键都能在 QueryString 或请求头参数中找到同名且同值的参数。

示例：

```text
EvaluatorName = Default
EvaluatorSetting = Site=demo; Environment=Production
```

满足示例的 Fetch 请求需要携带 `Site=demo` 与 `Environment=Production`，可放在 QueryString 或请求头中。

## Release 子资源 API

属性集：

```http
GET /Upgrading/Releases/{releaseId}/Properties
POST /Upgrading/Releases/{releaseId}/Properties
PUT /Upgrading/Releases/{releaseId}/Properties
PATCH /Upgrading/Releases/{releaseId}/Properties
DELETE /Upgrading/Releases/{releaseId}/Properties
```

执行器集：

```http
GET /Upgrading/Releases/{releaseId}/Executors
POST /Upgrading/Releases/{releaseId}/Executors
PUT /Upgrading/Releases/{releaseId}/Executors
PATCH /Upgrading/Releases/{releaseId}/Executors
DELETE /Upgrading/Releases/{releaseId}/Executors
```

发布状态集：

```http
GET /Upgrading/Releases/{releaseId}/Publishings
POST /Upgrading/Releases/{releaseId}/Publishings
PUT /Upgrading/Releases/{releaseId}/Publishings
PATCH /Upgrading/Releases/{releaseId}/Publishings
DELETE /Upgrading/Releases/{releaseId}/Publishings
```

也可以从实例维度维护发布状态：

```http
GET /Upgrading/Instances/{instanceId}/Publishings
POST /Upgrading/Instances/{instanceId}/Publishings
PUT /Upgrading/Instances/{instanceId}/Publishings
PATCH /Upgrading/Instances/{instanceId}/Publishings
DELETE /Upgrading/Instances/{instanceId}/Publishings
```

## Release 发布动作

发布可见性和可升级状态由字段表达，使用标准 PATCH 更新：

```http
PATCH /Upgrading/Releases/{id}
Content-Type: application/json
```

示例：

```json
{
	"Published": true,
	"Visible": true,
	"Deprecated": false
}
```

## ReleasePublishing 状态

发布状态记录表示某个发布在某个实例上的升级状态：

```http
POST /Upgrading/Releases/{releaseId}/Publishings
Content-Type: application/json
```

示例：

```json
[
	{
		"InstanceId": 1,
		"Status": "Downloaded",
		"Message": "",
		"Description": "The package has been downloaded."
	}
]
```

也可以按实例写入：

```http
POST /Upgrading/Instances/{instanceId}/Publishings
Content-Type: application/json
```

```json
[
	{
		"ReleaseId": 1,
		"Status": "Downloaded",
		"Message": "",
		"Description": "The package has been downloaded."
	}
]
```

## Problem Details 示例

```json
{
	"type": "InvalidRelease",
	"title": "Invalid release metadata.",
	"detail": "The release version is required."
}
```
