# Upgrading Web 服务端文档

本目录保存 `Zongsoft.Upgrading.Web` 服务端第一阶段的产品定义、接口设计、数据模型与执行计划。

## 文档索引

- [产品需求定义](requirements.md)
- [Web API 设计](api.md)
- [数据模型设计](models.md)
- [执行任务清单](planning.md)

## 已确认原则

1. 第一版只实现 Web API，不实现管理后台 UI。
2. 数据库表名称与实体类名称一致，采用单数形式；对应 API 路径资源名采用复数形式。
3. 数据增删改查服务原则上继承 `Zongsoft.Web` 的 `ServiceController`。
4. Fetch 决策等协议型接口使用专门 Controller，并遵守 RESTful 规范。
5. `Release` 包含 `Mode` 属性，用于表达升级部署模式。
6. `Release.Mode` 仅包含两种语义：默认在客户端程序重启时部署；尽快执行部署。
7. Release 元数据保存到 MySQL 或 SQLite，发布包 `.zip` 通过 `Zongsoft.IO` 虚拟文件系统保存到 S3。
8. 上传 Release 的 `.zip` 包后，更新 `Release.Path` 为 `Zongsoft.IO` 虚拟文件系统完整路径。
9. Fetch 请求为 `POST /upgrading/upgrader/{name}/{edition?}`，QueryString 保留现有参数，Body 始终为 JSON 键值对。
10. 升级控制条件保持简单够用。
