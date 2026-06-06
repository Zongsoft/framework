---
name: web
description: 处理 upgrading/web 下的 Zongsoft.Upgrading.Web 包管理器。用于修改或审查发布仓库 API、应用/版本分支/发布/实例服务、manifest 导入、包上传与存储、发布状态、升级器发现端点、评估器注册、数据库映射/脚本、HTTP 文档，或 Zongsoft 自动升级 Web 模块打包相关工作。
---

# Zongsoft Upgrading Web

## 入手位置

修改行为前，优先读取最小且有用的文件集合：

- `README.md`：了解公开的包管理器契约、配置、工作流和 API 示例。
- `docs/index.md`、`docs/api.md`、`docs/models.md`、`docs/requirements.md`、`docs/planning.md`：了解设计意图。
- `docs/http/*.http`：修改路由、请求/响应负载或管理流程时读取。
- `Controllers/UpgraderController.cs`：客户端发现端点和评估器元数据端点。
- `Controllers/ReleaseController.cs`：manifest 导入和包上传路由。
- `Services/ReleaseService.cs`：导入、上传、校验、应用/版本分支同步以及子服务。
- `Services/ApplicationService.cs`、`Services/InstanceService.cs`：修改服务控制器行为时读取。
- `Upgrader.cs`：发布过滤逻辑，以及转换为共享客户端 `Release` 模型的逻辑。
- `Evaluator*.cs`、`Module.cs`：评估器契约和注册。
- `Settings.cs`、`Zongsoft.Upgrading.Web.option`：存储和数据库设置。
- `Zongsoft.Upgrading.Web.csproj`：包产物、链接的 `.shared` 文件、插件/选项打包、文档生成设置。
- `../database/zongsoft.upgrading-*.sql`：修改数据库结构假设时读取。
- `../.shared/*.cs`：触碰共享发布、manifest、执行器、平台、架构或工具契约时读取。

## 核心契约

保持 Web 包管理器承担这些职责：

1. 存储应用、版本分支、发布、发布属性、发布执行器、实例和发布状态元数据。
2. 导入由 `dotnet-upgrade pack` 生成的 `.manifest` 文件。
3. 将已有发布的包内容上传到配置的 Zongsoft 文件系统存储。
4. 在上传时重新计算并持久化包大小和校验和。
5. 通过 Zongsoft 服务控制器约定暴露管理服务。
6. 返回可被 `Zongsoft.Upgrading.Upgrader` 消费的发现响应。
7. 返回发布前应用可选的评估器检查。
8. 让包产物（`.deploy`、`.plugin`、`.option`、映射文件、README）与 `Zongsoft.Upgrading.Web.csproj` 保持一致。

## 跨项目契约

将 `../.shared` 类型视为 tool、web 包管理器、upgrader 和 deployer 共享的协议对象。修改 `Manifest`、`Release`、`Executor`、`ReleaseKind`、`Platform`、`Architecture`、reader/writer 代码或 checksum/runtime 辅助逻辑时，通常需要检查另外三个子项目。

保持 Web 导入流程兼容 `Zongsoft.Tools.Upgrader` 输出的 manifest，并保持发现响应兼容 upgrader 中的 `Fetcher.Web` 和 `Downloader.Web`。

## 发现规则

保持升级器端点兼容：

```http
GET /Upgrading/Upgrader/{name}/{edition?}?Platform=Windows&Architecture=X64&CurrentlyVersion=1.0.0
```

返回的发布必须满足：

- 可见；
- 已发布；
- 未废弃；
- 应用名称、平台和架构匹配；
- 匹配请求的版本分支，或者当版本分支为空或 `_` 时匹配无版本分支发布；
- 提供 `CurrentlyVersion` 时，发布版本必须更新；
- 提供 `UpgradingVersion` 时，发布版本不能高于它；
- 关联存在的包路径并且包大小为正数；
- 当发布配置了评估器时，必须通过该评估器。

将查询参数和请求头视为评估器输入。除非任务明确要改变评估器输入，否则保留客户端升级器发送的硬件指纹字段。

## 管理规则

修改 manifest 导入时：

- 保留应用和版本分支的 upsert/同步行为。
- 保留 manifest 中的发布属性和执行器。
- 保持校验足够严格，拒绝不完整的发布身份、runtime 或包元数据。

修改包上传时：

- 保留基于发布身份生成存储路径的规则：

```text
{storage}/{lowercase-release-name}/{release-name}[-edition]@{version}_{runtime}
```

- 从上传后的存储内容重新计算大小和校验和。
- 保持存储提供器 URL 兼容配置的 Zongsoft 文件系统提供器，例如 `zfs.s3:/...`。

修改发布状态时，保持兼容 tool 的 Web 发布流程：

1. 使用 `format=manifest` POST 导入 manifest。
2. 将包内容上传到每个导入的发布。
3. PATCH 或更新 `Published` 为 `true`。

## 评估器

在 `Module.Current.Evaluators` 中注册自定义评估器。保持评估器名称大小写不敏感，并且只返回配置评估器执行成功的发布。

通过以下端点暴露评估器元数据：

```http
GET /Upgrading/Upgrader/Evaluators
```

新增评估器行为时，保持逻辑确定且尽量无副作用；评估器在发现期间运行，不应修改发布元数据。

## 文档

行为或 API 变化时同步更新文档：

- 更新 `README.md`，说明公开安装、配置或工作流变化。
- 更新 `docs/api.md`、`docs/models.md` 或 `docs/requirements.md`，说明契约变化。
- 更新 `docs/http/*.http`，说明端点、请求形状或状态流程变化。
- 修改面向用户的说明时，同步中文 README。

## 验证

优先执行聚焦验证：

```shell
dotnet build Zongsoft.Upgrading.Web.slnx
```

API 变化时，在本地开发实例可用的情况下，至少验证发现端点、manifest 导入、包上传和发布状态更新流程。数据库结构变化时，除构建外还要验证受影响的 SQL 脚本和映射文件。
