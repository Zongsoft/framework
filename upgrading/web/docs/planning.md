# 执行任务清单

## 阶段一：模型与共享协议

1. 已实现 `Release.Mode` 属性。
2. 已定义升级部署模式枚举：
	- `Default`：默认模式，在客户端程序重启时进行升级部署。
	- `Immediate`：尽快执行升级部署，不等待下次程序重启。
3. Release XML 读写逻辑需要持续确认 `mode` 属性兼容性。
4. 旧 release 缺少 `mode` 时默认值为 `Default`。

## 阶段二：数据服务

1. 已提供 MySQL、PostgreSQL 和 SQLite 兼容表结构。
2. 已实现 `Application` 数据服务和 `/Upgrading/Applications` CRUD API。
3. 已实现 `ApplicationEdition` 数据服务和 `/Upgrading/Applications/{applicationId}/Editions` 子资源 CRUD API。
4. 已实现 `Release` 数据服务和 `/Upgrading/Releases` CRUD API。
5. 已实现 `ReleaseProperty` 数据服务和 Release 属性父子资源 API。
6. 已实现 `ReleaseExecutor` 数据服务和 Release 执行器父子资源 API。
7. 已实现 `Instance` 数据服务和 `/Upgrading/Instances` CRUD API。
8. 已实现 `ReleasePublishing` 数据服务，并支持 Release 与 Instance 两个访问维度。
9. Release 数据服务当前使用 `EvaluatorName`、`EvaluatorSetting` 表达升级评估条件。

原则：

- 数据库物理表名使用 `Upgrading_` 前缀。
- 实体类名采用单数形式。
- API 路径资源名采用复数形式。
- 纯数据增删改查服务从 `ServiceController` 或 `SubserviceController` 继承。

## 阶段三：Release 发布包上传

1. 已在 ReleaseController 中支持上传 `.zip` 包。
2. 当前上传路径为 `POST /Upgrading/Releases/{id}/Upload?overwrite=true`。
3. 同时支持 `POST /Upgrading/Releases/Upload/{id}?overwrite=true`。
4. 通过 `Zongsoft.IO` 虚拟文件系统保存 zip。
5. 上传成功后更新 `Release.Path` 为虚拟文件系统文件地址。
6. 上传成功后自动维护 `Size` 和 `Checksum`。
7. 存储根路径来自 `/Upgrading/Settings` 下 `server` 设置中的 `storage`。

## 阶段四：Manifest 导入与 Application 自动维护

1. 已通过 `POST /Upgrading/Releases/Import?format=manifest` 支持 `.manifest` 导入。
2. 导入时解析 Release 基础字段、属性集和执行器集。
3. 创建 Release 时，根据 `Name` 自动创建 Application。
4. 创建 Release 时，根据 `Name + Edition` 自动创建 ApplicationEdition。
5. 保留 Application 和 ApplicationEdition 的 API 供手动维护。
6. 用 ApplicationEdition 的 `Licensed` 字段表达是否授权。

## 阶段五：Upgrader Fetch 决策

1. 已实现 `GET /Upgrading/Upgrader/{name}/{edition?}`。
2. 保留 QueryString 参数兼容：
	- `Platform`
	- `Architecture`
	- `CurrentlyVersion`
	- `UpgradingVersion`
3. QueryString 与 Headers 会合并为评估参数集合。
4. 候选 Release 当前按发布状态、平台、架构和版本名筛选。
5. 已实现内置 `Default` 评估器，按键值设置匹配请求参数。
6. 已实现 `GET /Upgrading/Upgrader/Evaluators` 查询可用评估器。
7. 后续如需 License、Instance 自动登记或 ReleasePublishing 自动更新，可在 Upgrader 流程中继续扩展。

## 阶段六：Release 子资源

1. 已实现 Release 属性集父子资源 API：
	- `/Upgrading/Releases/{releaseId}/Properties`
2. 已实现 Release 执行器集父子资源 API：
	- `/Upgrading/Releases/{releaseId}/Executors`
3. 已实现 Release 发布状态父子资源 API：
	- `/Upgrading/Releases/{releaseId}/Publishings`
	- `/Upgrading/Releases/Publishings/Query`
4. 已实现 Instance 发布状态父子资源 API：
	- `/Upgrading/Instances/{instanceId}/Publishings`
	- `/Upgrading/Instances/Publishings/Query`
5. ReleaseProperty 写入 Release XML 的 `<properties>` 节点需要通过协议转换持续验证。
6. ReleaseExecutor 写入 Release XML 的 `<executors>` 节点需要通过协议转换持续验证。

## 阶段七：验收与测试

1. 验证 Release 可以 CRUD。
2. 验证 Release `.zip` 可以上传到 VFS/S3。
3. 验证上传成功后 `Release.Path`、`Release.Size`、`Release.Checksum` 更新正确。
4. 验证 `.manifest` 可以导入 Release、ReleaseProperty 和 ReleaseExecutor。
5. 验证创建 Release 时自动维护 Application。
6. 验证创建 Release 时自动维护 ApplicationEdition。
7. 验证 Fetch 可以按 QueryString 和 Header 参数触发 `Default` 评估器。
8. 验证 ReleasePublishing 可以记录实例上的发布升级状态。
9. 验证返回 Release 集合包含 `mode` 语义。
10. 验证客户端能按 `Release.Mode` 执行部署模式。

## 待确认

1. `Release.Path` 与 `Download.Url` 的最终职责边界。
2. 是否需要在 Upgrader Fetch 中自动维护 Instance。
3. 是否需要在 Upgrader Fetch 中自动创建或更新 ReleasePublishing。
4. 是否需要在 Upgrader Fetch 中直接执行 ApplicationEdition.Licensed 的 License 校验。
5. `dotnet-upgrade publish --channel:web` 是否与第一版同步实现。
