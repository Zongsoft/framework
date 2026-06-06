---
name: tool
description: 处理 upgrading/tool 下的 Zongsoft.Tools.Upgrader dotnet-upgrade 命令行工具。用于修改或审查 pack、checksum、publish 命令，发布 manifest 生成，ZIP 条目选择和重复处理，变量替换，执行器选项，runtime 命名，Amazon S3 或 Web 发布，本地化命令资源，NuGet 工具打包，或 Zongsoft 自动升级 tool 文档。
---

# Zongsoft Tools Upgrader

## 入手位置

修改行为前，优先读取最小且有用的文件集合：

- `README.md`：了解公开命令语法、选项、示例和发布流程。
- `Program.cs`：命令注册和命令行表达式处理。
- `Packager.Pack.cs`：`pack` 选项、变量替换、条目选择、排除规则、runtime 命名和 manifest 生成。
- `Packager.Checksum.cs`：校验和重算规则和 manifest 配对。
- `Packager.Publish.cs`：`amazon.s3` 和 `web` 发布流程。
- `Packager.cs`：ZIP 写入、重复条目处理和共享 packager 辅助逻辑。
- `Normalizer.cs`：`$(Variable)` 和 `%Variable%` 替换。
- `AmazonS3.cs`：S3 兼容上传行为。
- `Dumper.cs`：修改诊断输出时读取。
- `Properties/Resources*.resx`：修改面向用户的命令消息时读取。
- `Zongsoft.Tools.Upgrader.csproj`：`dotnet-upgrade` 工具打包、目标框架、链接的 `.shared` 文件和包元数据。
- `../.shared/*.cs`：触碰发布、manifest、执行器、平台、架构、runtime、checksum 或 reader/writer 契约时读取。

## 核心契约

保持工具围绕三个子命令：

```shell
dotnet-upgrade pack [options...] [arguments...]
dotnet-upgrade checksum [options] <package-file...>
dotnet-upgrade publish [options] <package-file...>
```

对 `pack`，保留以下顺序：

1. 解析必需身份选项：`name`、`version`、`platform` 和 `framework`。
2. 先从命令行选项标准化变量，再从环境变量标准化变量。
3. 根据平台和架构解析 `Runtime` 和 `RuntimeIdentifier`。
4. 验证源路径和输出路径。
5. 将 `--exclude` 规则应用到整目录打包和基于参数的打包。
6. 写入包 ZIP 条目；遇到重复条目时警告并跳过。
7. 计算校验和，并在包旁边写入 `.manifest`。

对 `checksum`，接受 `.zip`、`.manifest` 或无扩展名的包名，定位配对 manifest，重新计算校验和并更新 manifest。

对 `publish`，保持 `amazon.s3`、`s3` 和 `web` 通道别名兼容已有命令行。

## 跨项目契约

此工具生成的 manifest 和包会被 `Zongsoft.Upgrading.Web`、`Zongsoft.Upgrading.Upgrader` 和 `Zongsoft.Upgrading.Deployer` 消费。

修改 `Manifest`、`Release`、`ReleaseKind`、`Executor`、runtime 命名、checksum 算法、包路径约定或 XML reader/writer 行为，可能破坏整个升级流水线。此类契约变化时检查其它子项目。

## 选项和条目规则

保留 Zongsoft 命令行工具使用的选项解析风格，包括冒号形式：

```shell
--name:Zongsoft.Daemon
--version:1.1.0
--source:"D:\build\$(compilation)\$(framework)"
```

包参数规则：

- 相对路径按 `--source` 的相对路径处理。
- 允许绝对路径。
- 允许文件名部分使用 `*` 和 `?` 通配符。
- 使用 `source:target` 在包内重命名或重定位条目。
- 将空目标、`~` 和 `/` 视为包根目录。
- 保留重复 ZIP 条目行为：警告并跳过，不要静默覆盖。

执行器选项保留 `--executor.<name>@<event>:"command"` 格式。缺少事件名的执行器定义应警告并忽略。保持执行器名称兼容共享 deployer 命令：`Copy`、`Move`、`Link`、`Delete`。

## 发布说明

Web 发布时，保持 URL 标准化兼容指向站点根、`/Upgrading`、`/Upgrading/Upgrader` 或 `/Upgrading/Releases` 的输入。流程是导入 manifest、为每个导入的发布上传包，然后将发布标记为已发布。

Amazon S3 发布时，保持包和 manifest 上传路径兼容配置的 `--destination` 和发布文件名。

不要记录来自 `--secret`、`--access`、`--authorization` 或 `--credential` 的密钥值。

## 文档

命令行为变化时同步更新文档：

- 更新 `README.md` 和 `README-zh_CN.md`，说明公开命令语法、选项语义、示例或发布行为。
- 修改面向用户的消息时，同时更新 `Properties/Resources.resx` 和本地化资源文件。
- 保持示例兼容 `Zongsoft.Tools.Upgrader.csproj` 支持的目标框架。

## 验证

优先执行聚焦验证：

```shell
dotnet build Zongsoft.Tools.Upgrader.slnx
```

命令变化时，针对临时输出目录运行代表性 CLI 命令：

- 一个覆盖变量替换和包参数规则的 `pack` 命令；
- 一个会更新配对 `.manifest` 的 `checksum` 命令；
- 当凭据或本地测试端点可用时，运行受影响的发布路径。

Web 发布变化时，在本地 Web 包管理器可用的情况下验证 manifest 导入、包上传和发布状态更新。S3 变化时，优先使用本地 S3 兼容端点，避免直接触碰真实存储。
