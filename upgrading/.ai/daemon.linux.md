# Linux/WSL Daemon 升级验证工作流

先阅读 `.ai/common.md`，再执行本场景。本文只记录 Linux/WSL Daemon 的差异项，通用约束、发布节奏、异常处理和报告格式以 common 为准。

## 场景目标

- 平台：Linux/WSL shell，不使用 Windows PowerShell、CMD 或 Windows 原生命令执行构建、部署和验证。
- 宿主程序：`/Zongsoft/hosting/daemon`。
- 升级身份：`Zongsoft.Daemon`。
- 托管方式：必须是 systemd service，不能只用前台进程代替。
- 安装包调试：必须用 `/Zongsoft/tools/packager` 分别制作并验证 `.deb` 和 `.tar.gz`。
- 验证轮次：至少两轮，示例版本 `1.0.0.1` -> `1.0.0.2`。

升级必须证明 daemon service 能发现、下载、解压、调用 `Zongsoft.Upgrading.Deployer`、完成部署，并通过 systemd 重启、日志或可观测标记确认新版本生效。

## 必读和假设

执行前先声明：

- WSL 发行版、内核、架构、实际仓库路径、服务名、发布通道、部署目录、安装目录。
- 包名是否仍为 `Zongsoft.Daemon`；安装包名可沿用仓库约定，例如 `zongsoft.daemon`，它不等同于升级身份。
- 如果仓库位于 Windows 盘符下，先确认实际挂载路径，例如 `D:\Zongsoft\framework\upgrading` -> `/mnt/d/Zongsoft/framework/upgrading`。

先读：

- `upgrading/tool/SKILL.md`
- `upgrading/upgrader/SKILL.md`
- `upgrading/deployer/SKILL.md`
- `/Zongsoft/tools/packager` 的 README、项目文件或现有脚本

## 关键路径

| 用途 | 路径 |
| --- | --- |
| Daemon 宿主 | `/Zongsoft/hosting/daemon` |
| Packager 工具 | `/Zongsoft/tools/packager` |
| 升级打包脚本 | `/Zongsoft/hosting/daemon/upgrade.pack.cmd` 或等价 Linux 命令 |
| 升级发布脚本 | `/Zongsoft/hosting/daemon/upgrade.publish.cmd` 或等价 Linux 命令 |
| Upgrader 插件 | `/Zongsoft/framework/upgrading/upgrader` |
| Deployer 程序 | `/Zongsoft/framework/upgrading/deployer` |
| 升级工具 | `/Zongsoft/framework/upgrading/tool` |
| Upgrader 部署目录 | daemon 安装目录下的 `plugins/zongsoft/upgrader` |
| Deployer 部署目录 | daemon 安装目录下的 `.deployer` |
| systemd 服务名 | 以 unit 文件或日志为准，默认先检查 `zongsoft.daemon` |

## Linux/systemd 注意点

- 检查 `systemctl`、`journalctl`、`dpkg`、`apt`、`tar`、`gzip`、`curl`、`dotnet --info`、`podman` / `docker`。
- 确认当前用户可使用 `sudo` 安装包、写 systemd unit 目录、启动/停止服务。
- `.cmd` 脚本只能作为参数参考；Linux 验证必须使用等价 shell 命令。
- 如果使用自包含 deployer，确认执行权限、文件名大小写、RID/架构和服务账号权限。
- systemd 相关失败优先检查 `systemctl status <service>`、`journalctl -u <service>`、unit 的 `WorkingDirectory` / `ExecStart` / 环境变量。

## 执行清单

1. 检查 Redis 和 RustFS/S3：优先使用 `/Zongsoft/hosting` 中已有 Linux 可用脚本；验证 daemon/upgrader 使用的通道与发布目标一致。
2. 构建 packager：进入 `/Zongsoft/tools/packager`，确认 `.deb` 和 `.tar.gz` 打包入口、工具版本和输出目录。
3. 构建并部署 daemon：进入 `/Zongsoft/hosting/daemon`，构建、部署，选择 Linux 产物，记录输出目录。
4. 制作并调试 `.deb`：生成包，安装，`systemctl daemon-reload`，启动服务，查看 `systemctl status` 和 `journalctl`；调试后卸载，避免影响 `.tar.gz`。
5. 制作并调试 `.tar.gz`：解压到测试安装目录，配置或生成 systemd unit，启动服务并确认 upgrader 可加载。
6. 部署 upgrader：复制到当前用于升级验证的 daemon 安装目录下 `plugins/zongsoft/upgrader`。
7. 部署 deployer：按 upgrader 配置或源码确认 `.deployer` 位置；确认可执行入口可由 systemd 服务账号启动。
8. 第一轮升级：加入 marker `Daemon upgrade marker: 1.0.0.1`；构建、pack、publish，验证 S3 对象存在，观察发现/下载/解压/部署/停止/重启。
9. 第二轮升级：把 marker 和包版本递增到 `1.0.0.2`，重复构建、打包、发布和观察。
10. 清理：停止、禁用并卸载测试服务；删除 `.tar.gz` 测试目录和手工 unit；执行 `systemctl daemon-reload`；确认无遗留 daemon 进程。

## 验收重点

- `.deb` 和 `.tar.gz` 都由 packager 生成并完成安装调试。
- daemon systemd service 能启动并保持 `active (running)`。
- upgrader 正常加载；pack/publish 成功；S3 中存在对应版本对象。
- daemon 日志显示发现新版本、下载成功、解压成功、调用 deployer 且 deployer 成功。
- 升级后 systemd service 最终回到 `active (running)`。
- 两轮升级后都能观察到对应 marker。

## 交付报告补充

除 common 报告项外，必须包含：

- WSL 发行版、内核、架构和 .NET SDK 版本。
- packager 工具构建结果和命令入口。
- `.deb` 包路径、安装结果和卸载结果。
- `.tar.gz` 包路径、安装结果和清理结果。
- daemon 安装目录。
- systemd service 名称、unit 文件、状态和 `ExecStart`。
- upgrader 插件部署位置。
- deployer 部署位置。
- 两次升级包版本号和 marker 证据。
- systemd 服务和安装目录清理结果。
