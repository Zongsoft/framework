# Linux/WSL Web 升级全流程验证工作流

## 目标

你需要在 Linux/WSL 环境中验证 `/Zongsoft/hosting/web/default` Web 宿主程序的完整升级流程。

本次验证的核心目标是确认 Web 宿主程序在 systemd 托管运行，并通过 Nginx 反向代理对外提供 HTTP 服务时，能够通过升级系统完成以下流程：

1. 发现 `Zongsoft.Web` Web 程序的新升级包；
2. 下载升级包；
3. 解压升级包；
4. 调用 `Zongsoft.Upgrading.Deployer` 执行部署；
5. 完成 Web 宿主程序或插件的升级；
6. 通过 systemd 服务重启、Nginx 反向代理访问、日志或 HTTP 可观测标记确认升级后的 Web 程序确实生效。

验证流程需要完成两轮：

1. 首次升级验证；
2. 在首次升级成功后，再次修改版本号或可观测标记并重复升级，验证连续升级流程。

另外，Linux 安装调试必须覆盖两种安装包：

1. 使用 `/Zongsoft/tools/packager` 制作 `.deb` 安装包并安装调试；
2. 使用 `/Zongsoft/tools/packager` 制作 `.tar.gz` 安装包并安装调试。

## 执行约束

- 当前平台是 Linux/WSL，必须在 WSL shell / Linux 环境中执行。
- 不要使用 Windows PowerShell、CMD 或 Windows 原生命令执行构建、部署和验证步骤；仅在需要说明 Windows 宿主路径映射时记录对应路径。
- Web 程序必须以 systemd service 的方式运行，不能只以前台控制台进程代替验证。
- Web 程序必须通过 Nginx 做反向代理，不能只访问 Kestrel 后端端口代替完整验证。
- 必须通过 `/Zongsoft/tools/packager` 工具制作 `.deb` 和 `.tar.gz` 安装包，并分别完成安装调试。
- 如果仓库位于 Windows 盘符下，先确认 WSL 中的实际挂载路径，例如 `/mnt/d/Zongsoft/...`；如果仓库在 WSL 文件系统中，则以实际 Linux 路径为准。
- 保持现有文件的换行符。
- 新文件使用 CRLF 换行格式。
- 代码文件使用 Tab 缩进。
- 不要随意重置、清理或覆盖用户已有改动。
- 如果需要 Redis 和 RustFS / S3 兼容存储服务，优先使用 `/Zongsoft/hosting` 目录中已有的 Podman 容器文件和脚本启动；如果 WSL 中 Podman 不可用，再查找仓库是否提供 Docker Compose 或其他 Linux 启动方式。
- 每个关键步骤都需要记录执行命令、执行结果、日志位置和判断结论。

## 相关路径

以下路径为预期路径。如果仓库中的实际命令名、项目名或部署路径与本文描述不一致，以源码、项目文件、脚本和日志为准，并在最终报告中说明差异。

| 用途 | 路径 |
| --- | --- |
| Web 宿主程序 | `/Zongsoft/hosting/web/default` |
| Web 部署配置 | `/Zongsoft/hosting/web/default/.deploy` |
| Web 打包脚本 | `/Zongsoft/hosting/web/default/pack.cmd` 或等价 Linux 命令 |
| Web 升级打包脚本 | `/Zongsoft/hosting/web/default/upgrade.pack.cmd` 或等价 Linux 命令 |
| Web 升级发布脚本 | `/Zongsoft/hosting/web/default/upgrade.publish.cmd` 或等价 Linux 命令 |
| Packager 工具源码 | `/Zongsoft/tools/packager` |
| Upgrader 插件源码 | `/Zongsoft/framework/upgrading/upgrader` |
| Deployer 程序源码 | `/Zongsoft/framework/upgrading/deployer` |
| Upgrader 插件部署目录 | Web 安装目录下的 `plugins/zongsoft/upgrader` |
| Deployer 部署目录 | Web 安装目录下的 `.deployer` 目录 |
| 升级工具 / tool | 在仓库中查找已有 tool 项目或命令入口 |
| Nginx 配置 | `/etc/nginx/conf.d/zongsoft.web.conf` |
| Nginx 配置模板 | `/Zongsoft/hosting/.deploy/default/nginx/zongsoft.web.conf` |
| Nginx reload 脚本 | `/Zongsoft/hosting/.deploy/default/nginx/reload-nginx.sh` |
| systemd 服务名称 | 以安装包、unit 文件或日志为准，默认可先检查 `zongsoft.web` |
| Kestrel 后端端口 | 以配置或打包参数为准，现有脚本默认 `8069` |
| Nginx 代理入口 | 以配置为准，现有模板包含 `http://127.0.0.1:8080/` |

如果上述路径在 WSL 中不存在，需要先定位实际路径。常见映射示例：

```text
D:\Zongsoft\framework\upgrading -> /mnt/d/Zongsoft/framework/upgrading
```

# 阶段一：环境准备

## 1. 检查 Linux/WSL 运行环境

确认 WSL 发行版和 Linux 工具链满足验证需求。

需要检查：

1. WSL 发行版、内核和架构；
2. systemd 是否可用；
3. `dotnet` SDK 版本；
4. `nginx` 是否安装并可由 `systemctl` 管理；
5. `podman` 或 `docker` 是否可用；
6. `curl`、`tar`、`gzip`、`dpkg`、`apt`、`systemctl`、`journalctl` 等基础工具是否可用；
7. 当前 shell 用户是否可以使用 `sudo` 安装软件包、写入 systemd unit 目录、写入 `/etc/nginx/conf.d`、启动和停止服务；
8. 如果仓库位于 `/mnt/<drive>`，确认文件权限、可执行位和换行符不会影响 Linux 运行。

验收标准：

- 能在 WSL/Linux 中执行 `dotnet --info`；
- `systemctl` 可用，且 systemd 正常运行；
- Nginx 可启动、可 reload，且 `nginx -t` 通过；
- 能构建 .NET 项目；
- 能启动或连接依赖服务；
- 部署目录、日志目录、临时目录和 Nginx 配置目录可写；
- 需要执行的 Linux 可执行文件和 shell 脚本具有执行权限。

## 2. 检查依赖服务

确认升级流程所需的 Redis 和 S3 兼容存储服务是否已运行。

如果服务未运行：

1. 进入 `/Zongsoft/hosting`；
2. 查找 Podman、Docker Compose 相关脚本、compose 文件或说明文档；
3. 使用已有 Linux 脚本启动 Redis 和 RustFS；
4. 验证服务端口、账号、bucket、endpoint 等配置是否与升级系统配置一致；
5. 如果服务运行在 Windows 宿主或其他网络命名空间中，确认 WSL 中可以访问对应 endpoint。

验收标准：

- Redis 可连接；
- RustFS / S3 endpoint 可访问；
- 升级发布命令能够连接到目标 S3 通道；
- Web/upgrader 使用的通道配置与发布目标一致。

## 3. 构建 `/Zongsoft/tools/packager` 工具

进入 `/Zongsoft/tools/packager`，确认用于制作安装包的 packager 工具可用。

要求：

1. 阅读 README、项目文件和现有脚本，确认本仓库中实际的 `.deb` / `.tar.gz` 打包命令；
2. 构建 packager 工具；
3. 确认 `.deb` 和 `.tar.gz` 打包命令可在 Linux shell 中执行；
4. 记录工具版本、命令入口和构建输出目录。

验收标准：

- packager 工具构建成功；
- 能执行用于打包 Web 的命令入口；
- 后续 `.deb` 和 `.tar.gz` 包均由该工具生成。

## 4. 检查 Nginx 配置模板

检查 `/Zongsoft/hosting/.deploy/default/nginx/zongsoft.web.conf` 和 `reload-nginx.sh`。

需要确认：

1. Nginx 监听端口；
2. `server_name`；
3. `proxy_pass` 指向的后端地址；
4. 后端端口是否与 Web 打包参数中的 `--daemon-bind` 一致；
5. 安装包是否会把配置安装到 `/etc/nginx/conf.d/zongsoft.web.conf`；
6. 安装后是否会执行 Nginx reload。

验收标准：

- Nginx 配置语法可通过 `nginx -t`；
- `proxy_pass` 目标和 Web 后端监听端口一致；
- Nginx reload 脚本可执行，且不会因为缺少 Nginx 导致安装流程中断。

# 阶段二：安装包制作和 systemd + Nginx 安装调试

## 1. 构建并部署 Web 宿主程序

执行以下工作：

1. 进入 `/Zongsoft/hosting/web/default`；
2. 编译构建 Web 宿主程序；
3. 执行 `dotnet deploy` 或项目约定的 Linux 部署命令；
4. 选择 Linux 平台产物；
5. 记录部署输出目录。

验收标准：

- Web 构建成功；
- 部署命令执行成功；
- 能找到部署后的 Web 程序目录；
- Web 可执行入口在 Linux 中可以运行；
- 部署产物中包含 `appsettings.json`、`web*.config`、`web*.option`、`wwwroot`、`plugins` 和运行所需程序集。

## 2. 制作 `.deb` 安装包

使用 `/Zongsoft/tools/packager` 工具，为 Web 制作 `.deb` 安装包。

要求：

- 包名称应为 `Zongsoft.Web`，除非源码或脚本显示实际名称不同；
- 平台参数使用 Linux；
- 架构参数与当前 WSL 环境一致，例如 `x64` 或 `arm64`；
- 设置环境变量参数，例如 `Environment` 和 `ASPNETCORE_ENVIRONMENT`；
- 设置 Web 后端绑定端口，现有脚本默认 `--daemon-bind:8069`；
- 包中包含 Nginx 配置，并安装到 `/etc/nginx/conf.d/zongsoft.web.conf`；
- 包中包含安装后和卸载后的 Nginx reload 脚本；
- 明确记录打包命令、版本号、输出目录和生成文件名。

验收标准：

- `.deb` 包生成成功；
- 包元数据、版本号、平台和架构正确；
- 包内容包含 Web 可执行入口、配置、插件目录、systemd unit 和 Nginx 配置；
- `.deb` 中的 systemd 服务名称、`ExecStart`、工作目录和环境变量正确；
- `.deb` 中的 Nginx 配置代理到正确的后端端口。

## 3. 安装并调试 `.deb` 包

使用 `dpkg`、`apt` 或安装包约定方式安装 `.deb` 包。

要求：

1. 安装前确认旧 Web 服务和旧 Nginx 配置状态；
2. 安装包后执行或确认已执行 `systemctl daemon-reload`；
3. 执行 `nginx -t`；
4. 启动 Web systemd service；
5. 确认 Nginx 已 reload 或手动 reload；
6. 使用 `systemctl status` 和 `journalctl` 观察 Web 服务；
7. 分别通过后端 Kestrel 端口和 Nginx 反向代理入口访问 Web；
8. 记录安装位置、unit 文件位置、服务名称、Nginx 配置位置和日志位置；
9. 调试完成后停止并卸载该 `.deb` 安装，避免影响 `.tar.gz` 安装调试。

验收标准：

- `.deb` 安装成功；
- Web systemd service 能启动并保持运行；
- `journalctl` 或应用日志中没有阻断升级流程的错误；
- Nginx 配置文件安装成功，`nginx -t` 通过；
- 访问后端端口成功；
- 访问 Nginx 代理入口成功；
- 卸载 `.deb` 后服务、unit、Nginx 配置和安装目录状态明确。

## 4. 制作 `.tar.gz` 安装包

使用 `/Zongsoft/tools/packager` 工具，为 Web 制作 `.tar.gz` 安装包。

要求：

- 使用与 `.deb` 包同一套源码和配置；
- 明确记录打包命令、版本号、输出目录和生成文件名；
- 检查压缩包内目录结构、可执行权限、配置文件、systemd unit、Nginx 配置和安装脚本；
- 确认 `.tar.gz` 同名安装脚本或包内 `install.sh` 会安装 systemd service 和 Nginx 配置。

验收标准：

- `.tar.gz` 包生成成功；
- 包内容可解压到目标安装目录；
- 可执行文件权限、文件名大小写和配置路径符合 Linux 行为；
- 包内包含安装或生成 systemd service 与 Nginx 配置所需内容。

## 5. 安装并调试 `.tar.gz` 包

将 `.tar.gz` 包解压到测试安装目录，或执行同名安装脚本，并按包内说明或仓库约定配置 systemd service 和 Nginx。

要求：

1. 选择明确的测试安装目录；
2. 解压并确认文件权限；
3. 安装或生成 systemd unit；
4. 安装或复制 Nginx 配置到 `/etc/nginx/conf.d/zongsoft.web.conf`；
5. 执行 `systemctl daemon-reload`；
6. 执行 `nginx -t`；
7. 启动 Web systemd service；
8. reload Nginx；
9. 使用 `systemctl status` 和 `journalctl` 观察服务；
10. 分别通过后端 Kestrel 端口和 Nginx 反向代理入口访问 Web；
11. 记录安装位置、unit 文件位置、服务名称、Nginx 配置位置和日志位置。

验收标准：

- `.tar.gz` 安装成功；
- Web systemd service 能启动并保持运行；
- Nginx 配置文件安装成功，`nginx -t` 通过；
- 访问后端端口成功；
- 访问 Nginx 代理入口成功；
- Web 日志中没有阻断升级流程的错误；
- upgrader 正常加载。

# 阶段三：升级运行环境准备

## 1. 部署 upgrader 插件

将 `/Zongsoft/framework/upgrading/upgrader` 插件构建产物部署到当前用于升级验证的 Web 安装目录：

```text
plugins/zongsoft/upgrader
```

验收标准：

- upgrader 插件文件存在于 Web 安装目录；
- 插件依赖文件完整；
- 文件名大小写与 Linux 文件系统中的实际引用一致；
- Web 启动日志中可以看到 upgrader 插件被加载，或者没有插件加载错误。

## 2. 部署 Zongsoft.Upgrading.Deployer

构建 `/Zongsoft/framework/upgrading/deployer`，并将生成的 `Zongsoft.Upgrading.Deployer` 程序放到 Web/upgrader 期望的位置。

不要猜测目标位置。优先通过以下信息确认：

1. upgrader 配置文件；
2. upgrader 源码中启动 deployer 的路径逻辑；
3. Web 运行日志；
4. 仓库中已有部署脚本。

Linux/WSL 下还需要确认：

1. deployer 入口是 `dotnet Zongsoft.Upgrading.Deployer.dll` 还是自包含可执行文件；
2. 如果是自包含可执行文件，是否具有执行权限；
3. shebang、文件名大小写、相对路径和工作目录是否符合 Linux 行为；
4. deployer 所需运行时标识符是否与当前 WSL 环境匹配。

验收标准：

- deployer 可执行入口存在于正确目录；
- Web/upgrader 能找到 deployer；
- deployer 在 Linux/WSL 中可以启动；
- 没有 `file not found`、`permission denied`、`bad interpreter` 或路径解析错误。

## 3. 启动 systemd Web 服务和 Nginx

运行当前用于升级验证的 Web systemd service，并保持其运行，用于观察升级发现和执行过程。

要求：

- 使用 `systemctl start` 启动 Web 服务；
- 使用 `systemctl start nginx` 或环境约定方式启动 Nginx；
- 记录服务名称、unit 文件、工作目录、环境变量、后端端口、Nginx 配置和日志位置；
- 使用 `journalctl -u <service>` 或项目日志文件持续观察；
- 使用 Nginx 入口访问 Web；
- 不要以前台进程代替 systemd 服务验证。

验收标准：

- Web systemd service 正常启动；
- Nginx 正常启动或 reload；
- upgrader 正常加载；
- 后端端口可访问；
- Nginx 代理入口可访问；
- 日志中没有阻断升级流程的错误。

# 阶段四：首次打包、发布和升级验证

## 1. 修改 Web 可观测版本标记

在 `/Zongsoft/hosting/web/default` 中添加或修改一个可观测的测试标记，用于证明升级后的 Web 代码或配置确实生效。

可选方式：

- 添加一个临时 HTTP endpoint，返回明确版本标记；
- 在 Web 启动时输出一条明确日志；
- 修改已有配置中的测试版本值，并确认 HTTP 响应或启动日志会输出该值。

首次版本号示例：

```text
Web upgrade marker: 1.0.0.1
```

验收标准：

- 项目编译成功；
- 标记能被 `curl`、`journalctl`、应用日志或约定日志文件观察到；
- 标记通过 Nginx 反向代理入口也能验证；
- 标记不会破坏 Web 正常启动、停止和升级流程。

## 2. 构建 Web

编译 `/Zongsoft/hosting/web/default` 项目。

验收标准：

- 构建成功；
- 生成用于打包的 Web 产物；
- 产物平台为 Linux；
- 产物中包含首次版本标记。

## 3. 制作 Web 升级包

使用升级 tool 中的 `pack` 命令，或将 `/Zongsoft/hosting/web/default/upgrade.pack.cmd` 转换为等价 Linux shell 命令，为 `Zongsoft.Web` 制作升级包。

要求：

- 包版本号使用首次模拟版本号；
- 包名称应为 `Zongsoft.Web`，除非源码或脚本显示实际名称不同；
- 打包模式按 Web 脚本或升级系统约定执行，现有脚本通常使用 `fully`；
- 平台参数使用 Linux 对应值；
- 包内容包含 `mime`、`appsettings.json`、`web*.config`、`web*.option`、`wwwroot`、`plugins` 和构建输出；
- 明确记录 `pack` 命令参数和输出包位置；
- 不要在 Linux 验证中直接执行 `.cmd` 文件。

验收标准：

- `pack` 命令执行成功；
- 生成升级包；
- 包元数据中的应用名、版本号、模式、平台、架构、目标通道正确。

## 4. 发布升级包到 S3 通道

使用升级 tool 中的 `publish` 命令，或将 `/Zongsoft/hosting/web/default/upgrade.publish.cmd` 转换为等价 Linux shell 命令，将升级包发布到 S3 通道。

要求：

- 使用现有配置或仓库约定的 S3 / RustFS 配置；
- 发布目标通常为 Web 升级发布路径，例如 `upgrading/releases/web`；
- 发布后验证 S3 中确实存在该升级包和相关元数据；
- 如果 S3 endpoint 绑定在 Windows 宿主或容器网络中，确认 WSL 中访问的 endpoint 与 Web/upgrader 使用的 endpoint 一致。

验收标准：

- `publish` 命令执行成功；
- S3 / RustFS 中可以看到升级包；
- Web/upgrader 使用的通道配置与发布目标一致。

## 5. 观察 systemd Web 升级行为

保持 Web systemd service 和 Nginx 运行，观察 Web 是否发现刚发布的升级包。

需要确认：

1. 是否检测到新版本；
2. 是否开始下载；
3. 是否下载成功；
4. 是否解压成功；
5. 是否进入部署阶段；
6. 是否调用 `Zongsoft.Upgrading.Deployer`；
7. deployer 是否执行成功；
8. Web systemd service 是否被正确停止、更新和重新启动；
9. Nginx 配置是否仍有效，是否需要 reload；
10. Web 是否升级到新版本。

验收标准：

- Web 日志显示发现升级包；
- 下载、解压、部署过程无错误；
- deployer 执行成功；
- Web systemd service 最终处于 `active (running)` 状态；
- Nginx `nginx -t` 通过；
- 升级后通过 Nginx 代理入口访问成功；
- 升级后 HTTP 响应、日志或可观测标记显示首次发布版本，例如 `1.0.0.1`。

# 阶段五：再次升级验证

仅当首次升级完整成功后，继续执行本阶段。

## 1. 修改 Web 可观测版本标记

修改 Web 输出的模拟版本号，例如：

```text
Web upgrade marker: 1.0.0.2
```

## 2. 重新构建、打包、发布

重复以下步骤：

1. 构建 `Zongsoft.Web`；
2. 使用 `tool pack` 制作新的升级包；
3. 使用 `tool publish` 发布到同一个 S3 通道；
4. 验证 S3 中新版本发布成功。

验收标准：

- 新升级包版本号为递增后的版本；
- 发布成功；
- 旧版本和新版本不会互相覆盖导致元数据异常。

## 3. 等待 systemd Web 再次升级

观察 Web systemd service 是否再次发现新版本，并完成下载、解压、部署和服务重启。

验收标准：

- Web 发现第二个版本；
- deployer 再次执行成功；
- Web systemd service 最终处于 `active (running)` 状态；
- Nginx 代理入口访问成功；
- 升级后 HTTP 响应、日志或可观测标记显示第二次发布版本，例如 `1.0.0.2`。

# 清理要求

测试完成后必须清理 systemd 服务、Nginx 配置和安装目录。

执行要求：

1. 停止 Web systemd service；
2. 禁用 Web systemd service；
3. 如果使用 `.deb` 安装，执行 `apt remove`、`apt purge` 或等价卸载命令；
4. 如果使用 `.tar.gz` 安装，删除测试安装目录和手动安装的 unit 文件；
5. 删除测试安装写入的 `/etc/nginx/conf.d/zongsoft.web.conf`，除非它属于用户已有环境；
6. 执行 `systemctl daemon-reload`；
7. 执行 `nginx -t` 并 reload Nginx；
8. 确认没有遗留运行中的 Web 进程；
9. 记录是否保留日志和升级包用于排查。

验收标准：

- `systemctl status <service>` 不再显示有效运行服务，或明确显示已卸载 / not-found；
- 没有遗留运行中的 Web 进程；
- Nginx 配置恢复到测试前状态；
- 清理结果写入最终报告。

# 异常处理要求

## Deployer 启动失败

如果 Web 无法启动 deployer 进程，需要先定位失败原因。

需要检查：

1. Web 日志；
2. upgrader 日志；
3. deployer 启动代码；
4. systemd journal；
5. 失败原因是权限问题、路径问题、工作目录问题、可执行权限问题、运行时问题，还是参数问题。

Linux/WSL 下重点检查：

1. deployer 文件是否缺少执行权限；
2. 是否误引用了 Windows 路径或 Windows 可执行文件；
3. 文件名大小写是否匹配；
4. 脚本是否因为 CRLF 出现 `bad interpreter`；
5. 运行时标识符、架构和 `dotnet` 运行时是否匹配；
6. systemd 服务账号是否可以访问部署目录、临时目录和日志目录；
7. systemd unit 中的 `WorkingDirectory`、`ExecStart` 和环境变量是否正确。

如果确认是权限导致：

1. 尝试修正文件权限、目录所有者或启动 deployer 进程的方式；
2. 优先考虑与 systemd 服务账号兼容的启动方式；
3. 记录修改内容和验证结果；
4. 如果仍无法解决，不要继续盲目修改，整理失败原因、日志、已尝试方案，然后与用户讨论下一步方案。

## systemd 服务启动或重启失败

如果升级后服务无法启动，需要检查：

1. `systemctl status <service>` 输出；
2. `journalctl -u <service>` 输出；
3. Web 应用日志；
4. unit 文件中的 `ExecStart` 是否仍指向正确文件；
5. 服务账号是否有部署目录和日志目录权限；
6. 升级过程中是否覆盖、删除或锁定了正在运行的文件；
7. 是否需要 `systemctl daemon-reload`；
8. 后端端口是否被占用。

## Nginx 反向代理失败

如果后端端口可访问但 Nginx 入口不可访问，需要检查：

1. `nginx -t` 输出；
2. `/etc/nginx/conf.d/zongsoft.web.conf` 内容；
3. `proxy_pass` 是否指向正确后端端口；
4. Nginx 是否已 reload；
5. Nginx error log 和 access log；
6. 端口监听和防火墙配置；
7. WSL 与宿主机网络转发限制。

## 安装包制作或安装失败

如果 `.deb` 或 `.tar.gz` 制作、安装失败，需要检查：

1. `/Zongsoft/tools/packager` 工具构建和运行日志；
2. 打包参数中的平台、架构、版本、源目录和 Web 后端端口；
3. 包内目录结构；
4. `.deb` 的 maintainer scripts、依赖项、systemd unit 和 Nginx 配置；
5. `.tar.gz` 的可执行权限、相对路径、安装脚本和 Nginx 配置安装逻辑。

## 其他失败

遇到失败时，不要直接跳过。

需要执行：

1. 记录失败命令；
2. 记录错误输出；
3. 查找相关日志；
4. 判断失败属于环境、配置、代码、权限还是流程问题；
5. 优先尝试可验证的小修复；
6. 修复后从失败步骤继续执行。

# 最终交付内容

完成验证后，输出一份结果报告，报告中至少包含：

1. 执行环境，包括 WSL 发行版、内核、架构和 .NET SDK 版本；
2. WSL 中使用的仓库实际路径；
3. 启动的依赖服务；
4. `/Zongsoft/tools/packager` 工具构建和命令入口；
5. `.deb` 安装包路径、安装结果和卸载结果；
6. `.tar.gz` 安装包路径、安装结果和清理结果；
7. Web 安装目录；
8. systemd service 名称、unit 文件、状态和 `ExecStart`；
9. Web 后端监听端口和访问结果；
10. Nginx 配置路径、代理入口、`nginx -t` 结果和访问结果；
11. upgrader 插件部署位置；
12. deployer 部署位置；
13. 两次升级包的版本号；
14. `pack` / `publish` 命令摘要；
15. Web 发现、下载、解压、部署、停止、重启或加载的日志结论；
16. 两次升级后的 Web HTTP 响应、日志或可观测标记结果；
17. 遇到的问题和解决方案；
18. systemd 服务、Nginx 配置和安装目录清理结果；
19. 最终结论：systemd + Nginx 下 Web 升级全流程是否验证通过。
