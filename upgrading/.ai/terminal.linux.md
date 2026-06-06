# Linux/WSL Terminal 升级验证工作流

先阅读 `.ai/common.md`，再执行本场景。本文只记录 Linux/WSL Terminal 的差异项，通用约束、发布节奏、异常处理和报告格式以 common 为准。

## 场景目标

- 平台：Linux/WSL shell，不使用 Windows PowerShell、CMD 或 Windows 原生命令执行构建、部署和验证。
- 宿主程序：`/Zongsoft/hosting/terminal`。
- 升级身份：`Zongsoft.Terminal`。
- 托管方式：terminal 进程；可用 `tmux`、`screen`、后台进程或项目约定方式保持运行。
- 验证轮次：至少两轮，示例版本 `1.0.0.1` -> `1.0.0.2`。

升级必须证明 terminal 能发现、下载、解压、调用 `Zongsoft.Upgrading.Deployer`、完成部署，并通过日志、命令输出或可观测标记确认新版本生效。

## 必读和假设

执行前先声明：

- WSL 发行版、内核、架构、实际仓库路径、目标框架、发布通道、部署目录。
- 包名是否仍为 `Zongsoft.Terminal`；除非源码或配置明确支持插件级升级，不要用 `Zongsoft.Commands` 作为默认发布身份。
- 如果仓库位于 Windows 盘符下，先确认实际挂载路径，例如 `D:\Zongsoft\framework\upgrading` -> `/mnt/d/Zongsoft/framework/upgrading`。

先读：

- `upgrading/tool/SKILL.md`
- `upgrading/upgrader/SKILL.md`
- `upgrading/deployer/SKILL.md`

## 关键路径

| 用途 | 路径 |
| --- | --- |
| Terminal 宿主 | `/Zongsoft/hosting/terminal` |
| Upgrader 插件 | `/Zongsoft/framework/upgrading/upgrader` |
| Deployer 程序 | `/Zongsoft/framework/upgrading/deployer` |
| 升级工具 | `/Zongsoft/framework/upgrading/tool` |
| Upgrader 部署目录 | terminal 部署目录下的 `plugins/zongsoft/upgrader` |
| Deployer 部署目录 | terminal 部署目录下的 `.deployer` |
| 可选命令插件 | `/Zongsoft/framework/Zongsoft.Commands`，仅用于额外命令验证 |

## Linux/WSL 注意点

- 只在 Linux shell 中执行构建、部署、打包、发布和验证；`.cmd` 脚本只能作为参数参考，不能直接执行。
- 检查 `dotnet --info`、`podman` / `docker`、`curl`、`tar`、`unzip`、`rsync`，以及部署目录、日志目录、临时目录写权限。
- 如果使用自包含 deployer，确认文件有执行权限、文件名大小写正确、RID/架构匹配。
- 脚本若因 CRLF 出现 `bad interpreter`，记录原因并只修正必要脚本。
- Linux x64 deployer AOT 发布可能依赖 `framework.linux-x64.yaml` 对应容器；未修改 deployer 时优先复用已验证产物。

## 执行清单

1. 检查 Redis 和 RustFS/S3：优先使用 `/Zongsoft/hosting` 中已有 Linux 可用脚本；验证 WSL 中可访问 endpoint。
2. 构建并部署 terminal：进入 `/Zongsoft/hosting/terminal`，构建、部署，选择 Linux 产物，记录输出目录。
3. 部署 upgrader：复制到 `plugins/zongsoft/upgrader`，确认依赖完整且大小写匹配。
4. 部署 deployer：按 upgrader 配置或源码确认 `.deployer` 位置；确认可执行入口可在 Linux/WSL 中启动。
5. 启动 terminal：保持运行，记录启动命令、工作目录、环境变量和日志位置。
6. 第一轮升级：加入 marker `Terminal upgrade marker: 1.0.0.1`；构建、pack、publish，验证 S3 对象存在，观察发现/下载/解压/部署/重启或热更新。
7. 第二轮升级：把 marker 和包版本递增到 `1.0.0.2`，重复构建、打包、发布和观察。

## 验收重点

- terminal 在 Linux/WSL 中正常启动，upgrader 正常加载。
- pack/publish 成功，包元数据中的名称、版本、模式、平台、架构正确。
- terminal 日志显示发现新版本、下载成功、解压成功、调用 deployer 且 deployer 成功。
- 两轮升级后都能观察到对应 marker。

## 交付报告补充

除 common 报告项外，必须包含：

- WSL 发行版、内核、架构和 .NET SDK 版本。
- WSL 中使用的仓库实际路径。
- terminal 部署目录。
- upgrader 插件部署位置。
- deployer 部署位置。
- 两次升级包版本号和包路径。
- terminal 两次升级后的日志、命令输出或 marker 证据。
