# 执行任务清单

## 阶段一：模型与共享协议

1. 实现 `Release.Mode` 属性。
2. 定义升级部署模式枚举，建议值为：
   - `Default`：默认模式，在客户端程序重启时进行升级部署。
   - `Immediate`：尽快执行升级部署，不等待下次程序重启。
3. 更新 Release XML 写入逻辑，输出 `mode` 属性。
4. 更新 Release XML 读取逻辑，解析 `mode` 属性。
5. 更新客户端 Fetch 结果处理逻辑，使客户端识别 `Release.Mode`。
6. 确认旧 release 缺少 `mode` 时的默认值为 `Default`。

## 阶段二：数据服务

1. 设计 MySQL 和 SQLite 兼容表结构。
2. 实现 `Application` 数据服务和 `/upgrading/applications` CRUD API。
3. 实现 `ApplicationEdition` 数据服务和 `/upgrading/applications/editions` 子资源 CRUD API。
4. 实现 `Release` 数据服务和 `/upgrading/releases` CRUD API。
5. 实现 `ReleaseProperty` 数据服务和 Release 属性父子资源 API。
6. 实现 `ReleaseExecutor` 数据服务和 Release 执行器父子资源 API。
7. 实现 `Instance` 数据服务和 `/upgrading/instances` CRUD API。
8. 实现 `ReleasePublishing` 数据服务和 `/upgrading/releases/publishings` 子资源 CRUD API。
9. Release 数据服务需覆盖 `FilterName`、`FilterData`、`FilterSetting` 三个过滤器文本字段。

原则：

- 数据库表名与实体类名一致，采用单数形式。
- API 路径资源名采用复数形式。
- 纯数据增删改查服务从 `ServiceController` 继承。

## 阶段三：Release 发布包上传

1. 在 ReleaseController 中支持上传 `.zip` 包。
2. 上传路径建议为 `PUT /upgrading/releases/{id}/content`。
3. 通过 `Zongsoft.IO` 虚拟文件系统保存 zip 到 S3。
4. 上传成功后更新 `Release.Path` 为虚拟文件系统完整文件路径。
5. 校验 zip 大小与校验码。
6. 自动维护或校验 Release 的 `Size`、`Checksum`。
7. 维护 Release 的 `Published`、`Visible`、`Deprecated` 字段。

## 阶段四：Application 自动维护

1. 创建 Release 时，根据 `Name` 自动创建或更新 Application。
2. 创建 Release 时，根据 `Name + Edition` 自动创建或更新 ApplicationEdition。
3. 保留 Application 和 ApplicationEdition 的 API 供手动维护。
4. 用 ApplicationEdition 的 `Licenced` 字段表达是否需要 License 授权。

## 阶段五：Upgrader Fetch 决策

1. 实现 `POST /upgrading/upgrader/{name}/{edition?}`。
2. 保留 QueryString 参数兼容：
   - `Platform`
   - `Architecture`
   - `CurrentlyVersion`
   - `UpgradingVersion`
3. Body 始终按键值对 JSON 处理。
4. 根据 Body 中的 `InstanceCode` 查找或创建 Instance。
5. 更新 Instance 的名称、标签、描述和 Profile 等信息。
6. 根据 ApplicationEdition.Licenced 判断是否校验 License。
7. 实现候选 Release 查询。
8. 实现升级控制条件匹配。
9. 为返回给该实例的 Release 创建或更新 ReleasePublishing。
10. 为返回的 Release 附加 `Download.Url` 属性。
11. 返回 Release XML 集合。

## 阶段六：Release 子资源

1. 实现 Release 属性集父子资源 API：
   - `/upgrading/releases/{releaseId}/properties`
   - `/upgrading/releases/{releaseId}/property/{name}`
   - `/upgrading/releases/properties`
2. 实现 Release 执行器集父子资源 API：
   - `/upgrading/releases/{releaseId}/executors`
   - `/upgrading/releases/{releaseId}/executor/{event}`
   - `/upgrading/releases/executors`
3. ReleaseProperty 写入 Release XML 的 `<properties>` 节点。
4. ReleaseExecutor 写入 Release XML 的 `<executors>` 节点。
5. 确保 `Download.Url` 属性可参与 Release XML 输出。

## 阶段七：ReleasePublishing 发布状态

1. 设计 ReleasePublishing 状态更新 API。
2. 支持状态：
   - `Fetch`
   - `Downloading`
   - `Downloaded`
   - `Upgrading`
   - `Upgraded`
   - `Completed`
3. 记录失败消息。
4. 记录更新时间。
5. 支持按实例、发布、应用、Edition、版本查询发布状态。

## 阶段八：验收与测试

1. 验证 Release 可以 CRUD。
2. 验证 Release `.zip` 可以上传到 VFS/S3。
3. 验证上传成功后 `Release.Path` 更新为虚拟文件系统完整路径。
4. 验证创建 Release 时自动维护 Application。
5. 验证创建 Release 时自动维护 ApplicationEdition。
6. 验证 Fetch 可以携带键值对 JSON。
7. 验证 Instance 可以按 `InstanceCode` 自动维护。
8. 验证 ReleasePublishing 可以记录实例上的发布升级状态。
9. 验证 InstanceCode、IP、MAC、Edition、Site、环境升级控制命中。
10. 验证 ApplicationEdition.Licenced 为真时，License 过期会拒绝升级。
11. 验证返回 Release XML 包含 `mode` 属性。
12. 验证返回 Release XML 包含 `Download.Url` 扩展属性。
13. 验证客户端能按 `Release.Mode` 执行部署模式。

## 待架构确认

1. `Release.Mode` 枚举类型名称。
2. `Release.Mode` 枚举值最终命名。
3. ApplicationEdition 的 License 字段拼写采用 `Licenced` 还是 `Licensed`。
4. Tags 第一版使用文本、JSON 还是独立表。
5. Release 属性和执行器父子资源的最终 Controller 路由映射。
6. `Download.Url` 的生成规则和是否需要签名 URL。
7. `dotnet-upgrade publish --channel:web` 是否与第一版同步实现。

