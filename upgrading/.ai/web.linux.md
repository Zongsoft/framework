# Linux/WSL Web 升级验证工作流

先阅读 `.ai/common.md`，再执行本场景。本文只记录 Linux/WSL Web 的差异项，通用约束、发布节奏、异常处理和报告格式以 common 为准。

## 场景目标

- 平台：Linux/WSL shell，不使用 Windows PowerShell、CMD 或 Windows 原生命令执行构建、部署和验证。
- 宿主程序：`/Zongsoft/hosting/web/default`。
- 升级身份：`Zongsoft.Web`。
- 托管方式：必须是 systemd service，并通过 Nginx 反向代理访问，不能只访问 Kestrel 后端端口代替。
- 安装包调试：必须用 `/Zongsoft/tools/packager` 分别制作并验证 `.deb` 和 `.tar.gz`。
- 验证轮次：至少两轮，示例版本为基线 `1.0.0` -> 全量 `1.1.0` -> 增量 `1.1.1`。

升级必须证明 Web service 能发现、下载、解压、调用 `Zongsoft.Upgrading.Deployer`、完成部署，并通过 systemd 重启、Nginx 入口、HTTP 响应、日志或可观测标记确认新版本生效。

## 必读和假设

执行前先声明：

- WSL 发行版、内核、架构、实际仓库路径、服务名、Nginx 入口、Kestrel 后端端口、发布通道、部署目录、安装目录。
- 包名是否仍为 `Zongsoft.Web`。
- 版本号必须高于当前运行应用版本；第一轮全量包提升 minor 版本号，第二轮增量包只提升 patch（第三部分）版本号。
- 如果仓库位于 Windows 盘符下，先确认实际挂载路径，例如 `D:\Zongsoft\framework\upgrading` -> `/mnt/d/Zongsoft/framework/upgrading`。

先读：

- `upgrading/tool/SKILL.md`
- `upgrading/upgrader/SKILL.md`
- `upgrading/deployer/SKILL.md`
- 如改用 Web 包管理器发布/发现，再读 `upgrading/web/SKILL.md`
- `/Zongsoft/tools/packager` 的 README、项目文件或现有脚本

## 关键路径

| 用途 | 路径 |
| --- | --- |
| Web 宿主 | `/Zongsoft/hosting/web/default` |
| Web 部署配置 | `/Zongsoft/hosting/web/default/.deploy` |
| Web 打包脚本 | `/Zongsoft/hosting/web/default/pack.cmd` 或等价 Linux 命令 |
| 升级打包脚本 | `/Zongsoft/hosting/web/default/upgrade.pack.cmd` 或等价 Linux 命令 |
| 升级发布脚本 | `/Zongsoft/hosting/web/default/upgrade.publish.cmd` 或等价 Linux 命令 |
| Packager 工具 | `/Zongsoft/tools/packager` |
| Upgrader 插件 | `/Zongsoft/framework/upgrading/upgrader` |
| Deployer 程序 | `/Zongsoft/framework/upgrading/deployer` |
| 升级工具 | `/Zongsoft/framework/upgrading/tool` |
| Upgrader 部署目录 | Web 安装目录下的 `plugins/zongsoft/upgrader` |
| Deployer 部署目录 | Web 安装目录下的 `.deployer` |
| Nginx 配置 | `/etc/nginx/conf.d/zongsoft.web.conf` |
| Nginx 配置模板 | `/Zongsoft/hosting/.deploy/default/nginx/zongsoft.web.conf` |
| Nginx reload 脚本 | `/Zongsoft/hosting/.deploy/default/nginx/reload-nginx.sh` |
| systemd 服务名 | 以 unit 文件或日志为准，默认先检查 `zongsoft.web` |
| Kestrel 后端端口 | 以配置或打包参数为准，现有脚本默认 `8069` |
| Nginx 代理入口 | 以配置为准，现有模板包含 `http://127.0.0.1:8080/` |

## Linux/systemd/Nginx 注意点

- 检查 `systemctl`、`journalctl`、`nginx -t`、`curl`、`dpkg`、`apt`、`tar`、`gzip`、`dotnet --info`、`podman` / `docker`。
- 确认当前用户可使用 `sudo` 安装包、写 systemd unit 目录、写 `/etc/nginx/conf.d`、启动/停止服务并 reload Nginx。
- `.cmd` 脚本只能作为参数参考；Linux 验证必须使用等价 shell 命令。
- Web 包必须包含运行所需的 `appsettings.json`、`web*.config`、`web*.option`、`wwwroot`、`plugins` 和程序集。
- Nginx 配置的 `proxy_pass` 必须指向实际 Kestrel 后端端口；升级后也要重新验证 Nginx 入口。
- Nginx 失败优先检查 `nginx -t`、`/etc/nginx/conf.d/zongsoft.web.conf`、error/access log、reload 状态、端口监听和 WSL 网络限制。

## 执行清单

1. 检查 Redis 和 RustFS/S3：优先使用 `/Zongsoft/hosting` 中已有 Linux 可用脚本；验证 Web/upgrader 使用的通道与发布目标一致。
2. 构建 packager：进入 `/Zongsoft/tools/packager`，确认 `.deb` 和 `.tar.gz` 打包入口、工具版本和输出目录。
3. 检查 Nginx 模板：确认监听端口、`server_name`、`proxy_pass`、后端端口、安装位置和 reload 脚本。
4. 构建并部署 Web：进入 `/Zongsoft/hosting/web/default`，构建、部署，选择 Linux 产物，记录输出目录。
5. 制作并调试 `.deb`：生成包，安装，`systemctl daemon-reload`，`nginx -t`，启动 Web service，reload Nginx，分别访问 Kestrel 和 Nginx；调试后卸载。
6. 制作并调试 `.tar.gz`：解压或执行安装脚本，配置 systemd 和 Nginx，启动服务，reload Nginx，分别访问 Kestrel 和 Nginx。
7. 部署 upgrader：复制到当前用于升级验证的 Web 安装目录下 `plugins/zongsoft/upgrader`。
8. 部署 deployer：按 upgrader 配置或源码确认 `.deployer` 位置；确认可执行入口可由 systemd 服务账号启动。
9. 第一轮升级：加入 HTTP 或日志 marker `Web upgrade marker: 1.1.0`；构建 `Fully` 全量包，版本提升 minor，例如 `1.0.x` -> `1.1.0`；publish 后验证 S3 对象存在。观察升级前先停止 Web service，并删除宿主程序的 `bin` 目录，再重新部署干净基线 Web、upgrader 和 deployer，确保全量升级测试干净完整；随后观察发现/下载/解压/部署/停止/重启，并通过 Nginx 入口确认 marker。
10. 第二轮升级：把 marker 和包版本递增到 patch 版本，例如 `1.1.0` -> `1.1.1`；构建 `Delta` 增量包，重复发布和观察。
11. 清理：停止、禁用并卸载测试服务；删除 `.tar.gz` 测试目录、手工 unit 和测试 Nginx 配置；执行 `systemctl daemon-reload`、`nginx -t`、reload Nginx；确认无遗留 Web 进程。

## 验收重点

- `.deb` 和 `.tar.gz` 都由 packager 生成并完成 systemd + Nginx 安装调试。
- Web systemd service 能启动并保持 `active (running)`。
- Kestrel 后端端口和 Nginx 代理入口都访问成功。
- upgrader 正常加载；pack/publish 成功；S3 中存在对应版本对象。
- 第一轮必须是提升 minor 版本号的 `Fully` 全量包，并在删除宿主 `bin` 目录后完成恢复验证；第二轮必须是只提升 patch 版本号的 `Delta` 增量包。
- Web 日志显示发现新版本、下载成功、解压成功、调用 deployer 且 deployer 成功。
- 升级后 systemd service 回到 `active (running)`，`nginx -t` 通过，Nginx 入口可访问。
- 两轮升级后都能通过 HTTP 响应、日志或 marker 观察到对应版本。

## 交付报告补充

除 common 报告项外，必须包含：

- WSL 发行版、内核、架构和 .NET SDK 版本。
- packager 工具构建结果和命令入口。
- `.deb` 包路径、安装结果和卸载结果。
- `.tar.gz` 包路径、安装结果和清理结果。
- Web 安装目录。
- systemd service 名称、unit 文件、状态和 `ExecStart`。
- Kestrel 后端端口和访问结果。
- Nginx 配置路径、代理入口、`nginx -t` 结果和访问结果。
- upgrader 插件部署位置。
- deployer 部署位置。
- 两次升级包版本号和 HTTP/log marker 证据。
- systemd 服务、Nginx 配置和安装目录清理结果。
