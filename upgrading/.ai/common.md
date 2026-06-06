# 升级验证通用工作流

本文是 `.ai` 场景文档的共享说明。执行时只读本文件和一个具体场景文件，避免把重复流程塞满上下文。

## AI 执行原则

按 `andrej-karpathy-skill` 的四个检查执行：

- 先声明假设：平台、架构、包名、版本号、发布通道、部署目录、服务名或入口。
- 保持简单：只做验证升级所需的 marker、配置和临时产物，不夹带重构。
- 改动要小：不要重置、清理或覆盖用户已有改动；只触碰当前验证必需文件。
- 每步可验证：记录命令、结果、日志位置和判断；失败不要跳过。

仓库约束：

- 保持现有文件换行符；新文件使用 CRLF。
- 代码文件使用 Tab 缩进。
- 如果修改共享 manifest、release、executor、runtime、checksum 或 `.deployment` 契约，同时检查 tool、upgrader、deployer；涉及 Web 包管理器时再检查 web 子项目。

## 通用目标

每个场景至少完成两轮升级：

1. 发布 `1.0.0.1` 或等价递增版本，确认目标程序升级成功。
2. 再发布 `1.0.0.2` 或等价递增版本，确认连续升级成功。

每轮都要证明：

- 发现新版本。
- 下载升级包。
- 解压升级包。
- 调用 `Zongsoft.Upgrading.Deployer`。
- 部署成功。
- 目标程序重启、热更新或重新加载成功。
- 通过日志、HTTP 响应、命令输出或 marker 观察到新版本。

## 必读资料

执行具体场景前先读：

- `upgrading/tool/SKILL.md`
- `upgrading/upgrader/SKILL.md`
- `upgrading/deployer/SKILL.md`

按场景再读：

- 需要 `.deb` / `.tar.gz` 安装包时，读 `/Zongsoft/tools/packager` 的 README、项目文件或现有脚本。
- 涉及 Web 包管理器或 Web 发布/发现流程时，读 `upgrading/web/SKILL.md`。

如果文档路径和仓库实际路径不同，以源码、项目文件、脚本和日志为准，并在报告中说明差异。

## 通用阶段

1. 环境确认：解析实际仓库路径，记录 OS、架构、`.NET SDK`、shell、权限、目标平台和 runtime。
2. 依赖服务：确认 Redis 和 RustFS/S3 兼容存储可用；优先使用 `/Zongsoft/hosting` 已有 Podman、Docker Compose、脚本或说明。
3. 宿主构建和部署：构建目标宿主，执行仓库约定的 deploy 命令，记录部署目录和入口文件。
4. Upgrader 部署：把 upgrader 插件部署到目标宿主的 `plugins/zongsoft/upgrader`，确认插件文件和依赖完整。
5. Deployer 部署：根据 upgrader 配置或源码确认 `.deployer` 位置，确认 deployer 可启动。
6. 启动目标程序：按场景要求以前台、Windows Service、systemd 或 systemd + Nginx 启动。
7. 第一轮升级：添加 `1.0.0.1` marker，构建、pack、publish、验证 S3 对象、观察升级。
8. 第二轮升级：添加 `1.0.0.2` marker，重复构建、pack、publish、验证 S3 对象、观察升级。
9. 清理：仅清理本次测试创建的服务、安装目录、Nginx 配置、临时包或进程；保留用户原有环境。
10. 报告：给出证据、问题、修复和最终结论。

## 打包和发布检查

pack 时确认：

- `--name` 是场景指定升级身份，例如 `Zongsoft.Terminal`、`Zongsoft.Daemon`、`Zongsoft.Web`。
- `--version` 递增且高于当前运行版本。
- `--kind`、平台、架构、框架、源目录和输出目录正确。
- 输出包和 manifest 实际存在。

publish 时确认：

- 使用的 S3 endpoint、bucket、prefix、账号和 upgrader 配置一致。
- 发布后能在 RustFS/S3 中看到包和元数据。
- 旧版本和新版本不会互相覆盖导致元数据异常。

## Deployer 规则

- 不要猜测 deployer 路径；优先从 upgrader 配置、upgrader 源码、运行日志和现有脚本确认。
- Windows 通常需要 `.deployer\Zongsoft.Upgrading.Deployer.exe`。
- Linux/WSL 需要确认入口是自包含可执行文件还是 `dotnet Zongsoft.Upgrading.Deployer.dll`。
- Native AOT 发布耗时较久；未修改 deployer 代码、发布属性、发布脚本或目标 runtime 时，可复用已验证产物。
- Linux x64 AOT 发布可能需要启动 `framework.linux-x64.yaml` 对应容器，例如在容器内执行 `./publish.linux-x64.sh`。

## Windows 易错点

- Windows Service 相关命令需要管理员权限；如果安装脚本弹出 UAC，等待授权后用 `sc query`、`sc qc`、`Get-Service` 复核最终状态。
- Codex/自动化 PowerShell 中 `dotnet deploy`、`dotnet-upgrade` 可能因 `ConsoleTerminal` 初始化失败并报 `句柄无效`；可改用独立 `cmd.exe` 进程执行，并把输出重定向到日志。
- PowerShell 中要用单引号保护 `$(edition)`、`$(framework)`、`$(scheme)` 等部署占位符。
- 手工部署 S3 通道时，除 upgrader 插件外，还要部署 `Zongsoft.Externals.Amazon`、`AWSSDK.Core.dll`、`AWSSDK.S3.dll`。
- `dotnet-upgrade pack --output` 指向目录时带尾斜杠，例如 `D:\Zongsoft\\`。
- 没有 `aws` / `mc` 时，可用 NuGet 缓存中的 AWSSDK 在 PowerShell 中列对象。

## Linux/WSL 易错点

- Linux/WSL 场景不要直接执行 Windows `.cmd`；只把它作为参数参考。
- 如果仓库在 Windows 盘符，先解析 `/mnt/<drive>/...` 路径，并注意权限、执行位和路径大小写。
- systemd 场景必须用 `systemctl` / `journalctl` 验证，不能用前台进程替代。
- 自包含可执行文件要检查 `chmod +x`、RID/架构、文件名大小写和服务账号权限。
- shell 脚本若因 CRLF 出现 `bad interpreter`，只修正相关脚本并记录原因。
- Nginx 场景必须执行 `nginx -t`，并验证后端 Kestrel 端口和 Nginx 代理入口。

## 失败处理

失败时先收集证据，再做最小修复：

- 记录失败命令、退出码和输出。
- 查找宿主日志、upgrader 日志、deployer 日志、Windows 事件日志或 systemd journal。
- 判断属于环境、配置、权限、路径、服务账号、运行时、打包参数、发布通道还是代码问题。
- 优先从失败步骤继续，不要重跑无关阶段。
- 如果三次小修复仍无法推进，整理日志、已尝试方案和下一步建议，再交给用户决策。

## 报告模板

最终报告至少包含：

- 执行环境和实际仓库路径。
- 关键假设：平台、架构、包名、版本号、发布通道、部署目录、服务名或入口。
- 依赖服务状态和连接配置摘要。
- 宿主部署目录、upgrader 部署目录、deployer 部署目录。
- 两轮升级包版本、包路径、manifest 路径和 publish 目标。
- S3/RustFS 发布后对象验证结果。
- 发现、下载、解压、部署、重启或热更新的日志结论。
- 两轮升级后的 marker 证据。
- 遇到的问题、修复方式和剩余风险。
- 清理结果。
- 最终结论：通过、未通过或部分通过，并说明原因。
