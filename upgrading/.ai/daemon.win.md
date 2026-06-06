# Windows Daemon 升级验证工作流

先阅读 `.ai/common.md`，再执行本场景。本文只记录 Windows Daemon 的差异项，通用约束、发布节奏、异常处理和报告格式以 common 为准。

## 场景目标

- 平台：Windows 本机 PowerShell / Windows 环境，不使用 WSL。
- 宿主程序：`/Zongsoft/hosting/daemon`。
- 升级身份：`Zongsoft.Daemon`。
- 托管方式：必须是 Windows Service，不能用前台控制台进程代替。
- 默认服务名：优先检查 `zongsoft.daemon`，以安装脚本、服务配置和日志为准。
- 验证轮次：至少两轮，示例版本为基线 `1.0.0` -> 全量 `1.1.0` -> 增量 `1.1.1`。

升级必须证明 daemon service 能发现、下载、解压、调用 `Zongsoft.Upgrading.Deployer`、完成部署，并通过 Windows Service 重启、日志或可观测标记确认新版本生效。

## 必读和假设

执行前先声明：

- 当前仓库路径、目标框架、架构、服务名、服务账号、发布通道、部署目录、是否具备管理员权限。
- 包名是否仍为 `Zongsoft.Daemon`。
- 版本号必须高于当前运行应用版本；第一轮全量包提升 minor 版本号，第二轮增量包只提升 patch（第三部分）版本号。

先读：

- `upgrading/tool/SKILL.md`
- `upgrading/upgrader/SKILL.md`
- `upgrading/deployer/SKILL.md`
- 若涉及 Web 包管理器或共享发布契约，再读 `upgrading/web/SKILL.md`

## 关键路径

| 用途 | 路径 |
| --- | --- |
| Daemon 宿主 | `/Zongsoft/hosting/daemon` |
| 安装脚本 | `/Zongsoft/hosting/daemon/install.cmd` |
| 卸载脚本 | `/Zongsoft/hosting/daemon/uninstall.cmd` |
| 升级打包脚本 | `/Zongsoft/hosting/daemon/upgrade.pack.cmd` |
| 升级发布脚本 | `/Zongsoft/hosting/daemon/upgrade.publish.cmd` |
| Upgrader 插件 | `/Zongsoft/framework/upgrading/upgrader` |
| Deployer 程序 | `/Zongsoft/framework/upgrading/deployer` |
| 升级工具 | `/Zongsoft/framework/upgrading/tool` |
| Upgrader 部署目录 | daemon 部署目录下的 `plugins/zongsoft/upgrader` |
| Deployer 部署目录 | daemon 部署目录下的 `.deployer` |

## Windows Service 注意点

- 执行 `install.cmd`、`uninstall.cmd`、`sc start`、`sc stop`、`sc delete` 前先检查管理员权限；若脚本弹出 UAC，等待用户授权后再复核服务状态。
- 不要把前台启动 daemon 的结果当作 Service 验证通过；必须用 `sc query`、`sc qc`、`Get-Service`、事件日志或 daemon 日志验证。
- 临时 marker 建议使用 Zongsoft 日志，避免 `Microsoft.Extensions.Logging.ILogger` 未进入约定文件日志：

```csharp
Zongsoft.Diagnostics.Logging.GetLogging<UpgradeMarkerService>().Info("Daemon upgrade marker: 1.1.0");
```

- 日志通常在 daemon 部署目录下的 `logs\yyyyMM\Zongsoft.Hosting.Daemon-*.log`。
- 非交互终端、PowerShell 占位符、手工部署 S3 插件、deployer AOT、`--output` 尾斜杠和 S3 对象验证规则见 `.ai/common.md` 的 Windows 小节。

## 执行清单

1. 检查 Redis 和 RustFS/S3：优先使用 `/Zongsoft/hosting` 中已有 Podman/compose/脚本；验证 endpoint、bucket、账号和发布通道一致。
2. 构建并部署 daemon：进入 `/Zongsoft/hosting/daemon`，构建、`dotnet deploy` / `deploy.cmd`，选择 Windows 平台产物，记录部署目录和 `.exe` 入口。
3. 部署 upgrader：复制到 `plugins/zongsoft/upgrader`，确认插件和依赖完整。
4. 部署 deployer：确认 `.deployer\Zongsoft.Upgrading.Deployer.exe` 存在，路径以 upgrader 配置或源码为准。
5. 安装并启动 Windows Service：执行 `install.cmd` 或等价命令，记录服务名、`binPath`、账号、状态、日志位置。
6. 第一轮升级：加入 marker `Daemon upgrade marker: 1.1.0`；构建 `Fully` 全量包，版本提升 minor，例如 `1.0.x` -> `1.1.0`；publish 后验证 S3 对象存在。观察升级前先停止 Windows Service，并删除宿主程序的 `bin` 目录，再重新部署干净基线 daemon、upgrader 和 deployer，确保全量升级测试干净完整；随后观察服务发现/下载/解压/部署/停止/重启。
7. 第二轮升级：把 marker 和包版本递增到 patch 版本，例如 `1.1.0` -> `1.1.1`；构建 `Delta` 增量包，重复发布和观察。
8. 清理：执行 `uninstall.cmd` 或 `sc stop` + `sc delete`，确认服务不存在且无遗留 daemon 进程。

## 验收重点

- Windows Service 创建成功、能启动并保持 `Running`。
- 服务 `binPath` 指向本次部署目录中的 daemon `.exe`。
- upgrader 正常加载；pack/publish 成功；S3 中存在对应版本对象。
- 第一轮必须是提升 minor 版本号的 `Fully` 全量包，并在删除宿主 `bin` 目录后完成恢复验证；第二轮必须是只提升 patch 版本号的 `Delta` 增量包。
- daemon 日志显示发现新版本、下载成功、解压成功、调用 deployer 且 deployer 成功。
- 升级后 Windows Service 最终回到 `Running`。
- 两轮升级后都能观察到对应 marker。

## 交付报告补充

除 common 报告项外，必须包含：

- Windows Service 名称、账号、状态和 `binPath`。
- daemon 部署目录。
- upgrader 插件部署位置。
- deployer 部署位置。
- 两次升级包版本号和包路径。
- 两次升级后的 daemon marker 证据。
- Windows Service 卸载结果。
