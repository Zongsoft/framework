---
name: upgrader
description: 处理 upgrading/upgrader 下的 Zongsoft.Upgrading.Upgrader 客户端组件。用于修改或审查自动升级发现、File/Web fetcher、发布选择、包下载与校验和验证、解压、.deployment 交接生成、worker 启动调度、deployer 启动行为、upgrader plugin/option/deploy 产物、执行器注册，或 Zongsoft 自动升级 upgrader 文档。
---

# Zongsoft Upgrading Upgrader

## 入手位置

修改行为前，优先读取最小且有用的文件集合：

- `README.md`：了解公开升级工作流、配置、通道和部署交接。
- `Upgrader.cs`：`UpgradeAsync`、worker 调度、并发保护、部署描述文件创建和关闭流程。
- `Fetcher.cs`、`Fetcher.File.cs`、`Fetcher.Web.cs`：发布发现、URL 构造、硬件指纹参数和选择逻辑。
- `Downloader.cs`、`Downloader.File.cs`、`Downloader.Web.cs`：包获取、本地复用、大小检查和校验和行为。
- `Extractor.cs`：将包解压到临时 `.app` 目录。
- `Upgrader.Launcher.cs`：deployer 进程启动细节和平台处理。
- `Executor.Initializer.cs`：内置执行器命令注册。
- `Zongsoft.Upgrading.Upgrader.option`、`.plugin`、`.deploy`：默认配置、启动注册和包产物。
- `Zongsoft.Upgrading.Upgrader.csproj`：链接的 `.shared` 文件、包产物和文档生成设置。
- `../.shared/*.cs`：触碰发布、manifest、部署描述文件、执行器、平台、架构或工具契约时读取。

## 核心契约

保留以下客户端流程：

1. 如果部署描述文件已存在，则报告升级准备成功。
2. 从配置的 `File` 或 `Web` 通道获取候选发布。
3. 按应用名称、版本分支、平台、架构、当前版本、可选目标版本和废弃状态过滤发布。
4. 如果存在完整发布，选择最新的完整发布作为主干，然后按版本升序追加更新的增量发布。
5. 将每个选中的包下载到应用专用临时目录。
6. 当发布元数据提供大小和校验和时，验证包大小和校验和。
7. 将包解压到临时 `.app` 目录。
8. 在应用目录中保存 `.deployment` 描述文件。
9. 从 `{ApplicationPath}/.deployer` 启动 `Zongsoft.Upgrading.Deployer`，然后关闭宿主。

## 跨项目契约

保持 upgrader 兼容：

- `Zongsoft.Tools.Upgrader` 生成的 manifest；
- `Zongsoft.Upgrading.Web` 返回的发现和下载响应；
- `Zongsoft.Upgrading.Deployer` 消费的 `.deployment` 文件；
- `../.shared/commands` 下实现的共享执行器命令。

修改 `Manifest`、`Release`、`ReleaseKind`、`Deployer`、`Executor`、`Platform`、`Architecture`、reader/writer 代码、checksum 辅助逻辑或 runtime 命名时，通常需要检查 tool、web 包管理器和 deployer。

## 通道说明

修改 `File` 通道时，保持 manifest 发现顺序兼容文档：

```text
{root}/{application-name}/*.manifest
{root}/{application-type}/{application-name}/*.manifest
{root}/{application-type}/{application-name}*.manifest
{root}/{application-name}*.manifest
```

修改 `Web` 通道时，保持请求兼容 Web 包管理器端点：

```text
{base-url}/{application-name}/{edition}?Name=...&Edition=...&Platform=...&Architecture=...&CurrentlyVersion=...
```

保留或扩展请求构造时包含硬件指纹参数；服务端评估器可能依赖这些字段。

## 实现规则

- 保持 `File` 和 `Web` 通道名称大小写不敏感，并兼容 `/Upgrading` 下的连接设置名称。
- 发布匹配必须严于包可用性：下载前先匹配应用身份和 runtime。
- 保持完整发布加增量发布链有序且确定。
- 在解压和部署交接前完成校验和验证。
- 只有当大小和校验和仍然匹配时，才能复用已下载的本地包文件。
- 保持 `.deployment` 创建为 deployer 继续执行的唯一信号。
- 保持 Linux deployer 启动足够脱离当前进程，使其能在宿主关闭后继续运行。
- 保留 interlocked 升级标志，避免并发 worker tick 阻塞或重入。
- 保持 plugin/option/deploy 产物与 NuGet 包布局一致。
- 发布名称必须匹配运行时 `Application.ApplicationName`。验证宿主升级时，先读部署目录 `.version`、启动代码或 upgrader/deployer 日志中的 `app.name`，不要只依赖打包脚本中的历史默认名称。
- 全量部署可能清理应用根目录下的 `logs/`，导致 upgrader 发现、下载、解压和启动 deployer 的成功日志在部署后消失；集成验证需要在观察循环中把 upgrader 日志快照复制到外部 `.artifacts`。

## 常见任务

修改发布选择时，测试只有增量发布、一个完整发布、多个完整发布，以及指定 `UpgradingVersion` 的组合。

修改下载行为时，验证本地文件复用、缺失文件下载、校验和不匹配和大小不匹配路径。

修改部署交接时，同时更新 upgrader 文档和 deployer 参数预期。

修改启动调度时，保留取消处理并避免并发运行多个升级准备流程。

## 文档

行为变化时同步更新文档：

- 更新 `README.md` 和 `README-zh_CN.md`，说明公开工作流、配置、通道或交接变化。
- 包安装行为变化时，更新 option/plugin/deploy 产物。
- 如果请求形状、查询名称或硬件评估器输入变化，同步更新 Web 文档。

## 验证

优先执行聚焦验证：

```shell
dotnet build Zongsoft.Upgrading.Upgrader.slnx
```

行为变化时，如果周边解决方案有可用测试，添加或运行发布过滤、下载器校验路径、包解压、worker 调度和部署描述文件生成相关测试。Web 通道变化时，在本地包管理器可用的情况下验证文档化的 `/Upgrading/Upgrader` 端点。
