# Zongsoft.Upgrading.Web API 设计

## 设计原则

1. 遵循 `D:\Zongsoft\guidelines\zongsoft.rest-api.guidelines.md`。
2. 数据库表名与实体类名一致，采用单数形式。
3. API 路径资源名采用复数形式。
4. 资源型 CRUD API 使用 `ServiceController`。
5. Fetch 决策等协议型 API 使用专门 Controller。
6. 错误响应采用 Problem Details 风格。
7. API 版本建议通过请求头承载，例如 `API-Version: 1.0`。

## CRUD 资源 API

下列资源原则上使用 `ServiceController` 暴露标准 CRUD 能力：

| 实体/表 | 路径 | 说明 |
| --- | --- | --- |
| Application | `/upgrading/applications` | 应用定义 |
| ApplicationEdition | `/upgrading/applications/editions` | 应用版本名定义，Application 的子资源 |
| Release | `/upgrading/releases` | 发布元数据和发布包上传下载 |
| ReleaseProperty | `/upgrading/releases/properties` | Release 扩展属性集合入口 |
| ReleaseExecutor | `/upgrading/releases/executors` | Release 执行器集合入口 |
| Instance | `/upgrading/instances` | 客户端安装实例 |
| ReleasePublishing | `/upgrading/releases/publishings` | 发布与实例的升级状态关系 |

标准行为：

- `GET /resources`：查询资源集。
- `GET /resources/{id}`：获取单个资源。
- `POST /resources`：创建资源。
- `PUT /resources/{id}`：更新资源。
- `PATCH /resources/{id}`：部分更新资源。
- `DELETE /resources/{id}`：执行 DELETE 操作。
- `POST /resources/query`：复杂查询。

## Release 上传和下载

`/upgrading/releases` 对应 ReleaseController，并从 `ServiceController` 继承。它除了标准增删改查，还承担 Release 发布包 `.zip` 文件的上传和下载。

上传发布包：

```http
PUT /upgrading/releases/{id}/content
Content-Type: application/zip
```

上传成功后：

1. zip 写入 `Zongsoft.IO` 虚拟文件系统。
2. `Release.Path` 更新为虚拟文件系统完整路径。
3. 根据 zip 文件更新或校验 `Size`、`Checksum` 等字段。

下载发布包：

```http
GET /upgrading/releases/{id}/content
```

说明：

- 下载返回的是该 Release 的 `.zip` 包内容。
- 客户端升级首选使用 Release 扩展属性 `Download.Url` 指向的下载地址。
- 该 content 下载接口主要用于管理、调试或非公开存储场景。

## Release 子资源 API

Release 与属性集、执行器集采用父子资源表达方式，参考 `Zongsoft.Security` 中 roles 与 roles-members 的 HTTP 示例。

属性集：

```http
GET /upgrading/releases/{releaseId}/properties
PUT /upgrading/releases/{releaseId}/properties
POST /upgrading/releases/{releaseId}/properties
DELETE /upgrading/releases/{releaseId}/properties
```

单个属性：

```http
GET /upgrading/releases/{releaseId}/property/{name}
PUT /upgrading/releases/{releaseId}/property/{name}
DELETE /upgrading/releases/{releaseId}/property/{name}
```

执行器集：

```http
GET /upgrading/releases/{releaseId}/executors
PUT /upgrading/releases/{releaseId}/executors
POST /upgrading/releases/{releaseId}/executors
DELETE /upgrading/releases/{releaseId}/executors
```

单个执行器：

```http
GET /upgrading/releases/{releaseId}/executor/{event}
PUT /upgrading/releases/{releaseId}/executor/{event}
DELETE /upgrading/releases/{releaseId}/executor/{event}
```

集合入口：

```http
GET /upgrading/releases/properties
GET /upgrading/releases/executors
```

## Upgrader Fetch API

升级客户端通过该接口获得可升级的发布集：

```http
POST /upgrading/upgrader/{name}/{edition?}?Platform={platform}&Architecture={architecture}&CurrentlyVersion={version}&UpgradingVersion={version}
Content-Type: application/json
Accept: application/xml
```

说明：

- `{name}` 为应用名称。
- `{edition}` 为版本名，可省略。
- QueryString 保留现有客户端参数。
- Body 始终为键值对 JSON，便于服务端统一处理。
- 响应为 Release XML 集合。
- 每个返回的 Release 扩展属性中应包含 `Download.Url`。

请求 Body 示例：

```json
{
  "InstanceCode": "CLIENT-MACHINE-UNIQUE-CODE",
  "Site": "default",
  "Environment": "Production",
  "License.Code": "LICENSE-CODE",
  "License.Type": "Commercial",
  "License.Expiration": "2026-12-31T23:59:59+08:00",
  "Configuration.Tenant": "demo",
  "Configuration.Channel": "stable",
  "Network.IP": "192.168.1.20",
  "Network.MACs": "00-11-22-33-44-55,00-11-22-33-44-66",
  "Hardware.Identifier": "MD5-HARDWARE-ID",
  "Hardware.Mainboard.Code": "BOARD-0",
  "Hardware.Mainboard.Model": "MODEL"
}
```

响应：

- `200 OK`：返回 Release XML 集合。
- `204 No Content`：无可升级发布。
- `400 Bad Request`：请求参数错误。
- `403 Forbidden`：License 过期、无效或升级控制拒绝。
- `404 Not Found`：应用不存在。
- `422 Unprocessable Entity`：请求语义无法处理，例如版本号非法。

响应 XML 要求：

1. 可以返回 `<release>` 或 `<releases>`。
2. Release 必须包含 `mode` 属性。
3. Release 的扩展属性必须包含 `Download.Url`，除非该 Release 暂不可下载。

## Manifest Import API

如果仍需支持 `.manifest` 导入，可提供专门接口：

```http
POST /upgrading/manifests/import
Content-Type: application/xml
```

职责：

1. 解析 `.manifest`。
2. 生成或更新 Release 元数据。
3. 同步维护 Application 和 ApplicationEdition。
4. 校验字段完整性。
5. 返回导入结果。

响应：

- `200 OK`：导入成功。
- `201 Created`：创建了新的 Release。
- `400 Bad Request`：XML 无法解析。
- `409 Conflict`：Release 唯一约束冲突。
- `422 Unprocessable Entity`：manifest 语义错误。

## Release 发布动作 API

Release 是否可用于升级由字段表达，优先使用标准 PATCH 更新：

```http
PATCH /upgrading/releases/{id}
```

示例：

```json
{
  "Published": true,
  "Visible": true,
  "Deprecated": false
}
```

如果需要动作式接口，可保留：

```http
POST /upgrading/releases/{id}/publish
POST /upgrading/releases/{id}/deprecate
```

## ReleasePublishing 状态 API

客户端或服务端可维护发布在指定实例上的升级状态：

```http
PATCH /upgrading/releases/{releaseId}/publishings/{instanceId}
```

示例：

```json
{
  "Status": "Downloaded",
  "Message": "",
  "Timestamp": "2026-05-22T16:30:00+08:00",
  "Description": "The package has been downloaded."
}
```

也可以按实例查看相关发布状态：

```http
GET /upgrading/instances/{instanceId}/publishings
```

## Problem Details 示例

```json
{
  "type": "LicenseExpired",
  "title": "License has expired.",
  "detail": "The client license expired at 2026-01-01T00:00:00+08:00."
}
```

