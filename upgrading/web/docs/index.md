# Upgrading Web 服务端文档

本目录保存 `Zongsoft.Upgrading.Web` 服务端第一阶段的产品定义、接口说明、数据模型与执行计划。

## 文档索引

- [产品需求说明](requirements.md)
- [Web API 说明](api.md)
- [数据模型设计](models.md)
- [执行任务清单](planning.md)
- [HTTP 请求示例](http/)

## 已确认原则

1. 第一版只实现 Web API，不实现管理后台 UI。
2. 数据库物理表名使用 `Upgrading_` 前缀；实体类名采用单数形式；对应 API 路径资源名采用复数形式。
3. 数据增删改查服务原则上继承 `Zongsoft.Web` 的 `ServiceController` 或 `SubserviceController`。
4. Fetch 使用 `GET /Upgrading/Upgrader/{name}/{edition?}`，QueryString 与 Headers 共同作为评估参数来源。
5. `Release` 包含 `Mode` 属性，用于表达升级部署模式。
6. `Release.Mode` 仅包含两种语义：默认在客户端程序重启时部署；尽快执行部署。
7. Release 元数据保存到数据库，发布包 `.zip` 通过 `Zongsoft.IO` 虚拟文件系统保存。
8. 上传 Release 的 `.zip` 包后，更新 `Release.Path`、`Release.Size` 和 `Release.Checksum`。
9. `.manifest` 文件通过 `POST /Upgrading/Releases/Import?format=manifest` 导入。
10. 升级控制第一版通过 `EvaluatorName` 与 `EvaluatorSetting` 实现轻量评估。
