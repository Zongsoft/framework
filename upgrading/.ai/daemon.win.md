# Windows Daemon 升级全流程验证工作流

## 目标

你需要在 Windows 本机验证 `/Zongsoft/hosting/daemon` 宿主程序的完整升级流程。

本次验证的核心目标是确认 daemon 宿主程序在 Windows Service 托管运行时，能够通过升级系统完成以下流程：

1. 发现 `Zongsoft.Daemon` 宿主程序的新升级包；
2. 下载升级包；
3. 解压升级包；
4. 调用 `Zongsoft.Upgrading.Deployer` 执行部署；
5. 完成 daemon 宿主程序或插件的升级；
6. 通过 Windows Service 重启、日志或可观测标记确认升级后的 daemon 确实生效。

验证流程需要完成两轮：

1. 首次升级验证；
2. 在首次升级成功后，再次修改版本号或可观测标记并重复升级，验证连续升级流程。

## 执行约束

- 当前平台是 Windows，必须在本机 PowerShell / Windows 环境中执行。
- 不要使用 WSL。
- daemon 程序必须以 Windows Service 的方式运行，不能只以前台控制台进程代替验证。
- 优先调用 `/Zongsoft/hosting/daemon/install.cmd` 将 daemon 宿主程序安装到 Windows Service 托管运行。
- 测试完成后必须调用 `/Zongsoft/hosting/daemon/uninstall.cmd` 或等价命令卸载测试服务。
- 保持现有文件的换行符。
- 新文件使用 CRLF 换行格式。
- 代码文件使用 Tab 缩进。
- 不要随意重置、清理或覆盖用户已有改动。
- 如果需要 Redis 和 RustFS / S3 兼容存储服务，优先使用 `/Zongsoft/hosting` 目录中已有的 Podman 容器文件和脚本启动。
- 每个关键步骤都需要记录执行命令、执行结果、日志位置和判断结论。

## 相关路径

以下路径为预期路径。如果仓库中的实际命令名、项目名或部署路径与本文描述不一致，以源码、项目文件、脚本和日志为准，并在最终报告中说明差异。

| 用途 | 路径 |
| --- | --- |
| Daemon 宿主程序 | `/Zongsoft/hosting/daemon` |
| Daemon 安装脚本 | `/Zongsoft/hosting/daemon/install.cmd` |
| Daemon 卸载脚本 | `/Zongsoft/hosting/daemon/uninstall.cmd` |
| Upgrader 插件源码 | `/Zongsoft/framework/upgrading/upgrader` |
| Deployer 程序源码 | `/Zongsoft/framework/upgrading/deployer` |
| Upgrader 插件部署目录 | daemon 部署目录下的 `plugins/zongsoft/upgrader` |
| Deployer 部署目录 | 根据 upgrader 配置、源码或日志确认 |
| 升级工具 / tool | 在仓库中查找已有 tool 项目或命令入口 |
| Daemon 升级打包脚本 | `/Zongsoft/hosting/daemon/upgrade.pack.cmd` |
| Daemon 升级发布脚本 | `/Zongsoft/hosting/daemon/upgrade.publish.cmd` |
| Windows Service 默认名称 | `zongsoft.daemon` |

# 阶段一：环境准备

## 1. 检查依赖服务

确认升级流程所需的 Redis 和 S3 兼容存储服务是否已运行。

如果服务未运行：

1. 进入 `/Zongsoft/hosting`；
2. 查找 Podman 相关脚本、compose 文件或说明文档；
3. 使用已有脚本启动 Redis 和 RustFS；
4. 验证服务端口、账号、bucket、endpoint 等配置是否与升级系统配置一致。

验收标准：

- Redis 可连接；
- RustFS / S3 endpoint 可访问；
- 升级发布命令能够连接到目标 S3 通道；
- daemon/upgrader 使用的通道配置与发布目标一致。

## 2. 构建并部署 daemon 宿主程序

执行以下工作：

1. 进入 `/Zongsoft/hosting/daemon`；
2. 编译构建 daemon 宿主程序；
3. 执行 `dotnet deploy`、`deploy.cmd` 或项目约定的部署命令；
4. 选择 Windows 平台产物；
5. 记录部署输出目录。

验收标准：

- daemon 构建成功；
- 部署命令执行成功；
- 能找到部署后的宿主程序目录；
- 部署目录中存在单一、明确的 daemon `.exe` 入口。

## 3. 部署 upgrader 插件

将 `/Zongsoft/framework/upgrading/upgrader` 插件构建产物手动部署到 daemon 宿主程序的插件目录：

```text
plugins/zongsoft/upgrader
```

验收标准：

- upgrader 插件文件存在于宿主程序插件目录；
- 插件依赖文件完整；
- daemon 服务启动日志中可以看到 upgrader 插件被加载，或者没有插件加载错误。

## 4. 部署 Zongsoft.Upgrading.Deployer

构建 `/Zongsoft/framework/upgrading/deployer`，并将生成的 `Zongsoft.Upgrading.Deployer` 程序放到 daemon/upgrader 期望的位置。

不要猜测目标位置。优先通过以下信息确认：

1. upgrader 配置文件；
2. upgrader 源码中启动 deployer 的路径逻辑；
3. daemon 运行日志；
4. 仓库中已有部署脚本。

验收标准：

- deployer 可执行文件存在于正确目录；
- daemon/upgrader 能找到 deployer；
- 没有 `file not found` 或路径解析错误。

## 5. 安装并启动 Windows Service

使用 `/Zongsoft/hosting/daemon/install.cmd` 安装 daemon 服务。

要求：

- 在管理员权限 PowerShell 或提升后的脚本窗口中执行；
- 记录服务名称，默认值通常为 `zongsoft.daemon`；
- 如果脚本提示选择服务名、版本、框架或是否立即启动，记录输入值；
- 使用 `sc query`、`Get-Service` 或事件日志确认服务状态；
- 确认服务的 `binPath` 指向本次部署目录中的 daemon `.exe`。

验收标准：

- Windows Service 创建成功；
- 服务能启动并保持运行；
- 服务启动日志中没有阻断升级流程的错误；
- upgrader 正常加载。

# 阶段二：首次打包、发布和升级验证

## 1. 修改 daemon 可观测版本标记

在 `/Zongsoft/hosting/daemon` 中添加或修改一个可观测的测试标记，用于证明升级后的 daemon 代码或配置确实生效。

可选方式：

- 在 daemon 启动时输出一条明确日志；
- 添加一个临时 hosted service，在启动后输出版本标记；
- 修改已有配置中的测试版本值，并确认 daemon 启动日志会输出该值。

首次版本号示例：

```text
Daemon upgrade marker: 1.0.0-test.1
```

验收标准：

- 项目编译成功；
- 标记能被 Windows Service 运行日志、应用日志或约定日志文件观察到；
- 标记不会破坏 daemon 正常启动、停止和升级流程。

## 2. 构建 daemon

编译 `/Zongsoft/hosting/daemon` 项目。

验收标准：

- 构建成功；
- 生成用于打包的 daemon 产物；
- 产物平台为 Windows；
- 产物中包含首次版本标记。

## 3. 制作 daemon 升级包

使用升级 tool 中的 `pack` 命令，或 `/Zongsoft/hosting/daemon/upgrade.pack.cmd`，为 `Zongsoft.Daemon` 制作升级包。

要求：

- 包版本号使用首次模拟版本号；
- 包名称应为 `Zongsoft.Daemon`，除非源码或脚本显示实际名称不同；
- 打包模式按 daemon 脚本或升级系统约定执行，现有脚本通常使用 `fully`；
- 平台参数使用 Windows 对应值；
- 明确记录 `pack` 命令参数和输出包位置。

验收标准：

- `pack` 命令执行成功；
- 生成升级包；
- 包元数据中的应用名、版本号、模式、平台、架构、目标通道正确。

## 4. 发布升级包到 S3 通道

使用升级 tool 中的 `publish` 命令，或 `/Zongsoft/hosting/daemon/upgrade.publish.cmd`，将升级包发布到 S3 通道。

要求：

- 使用现有配置或仓库约定的 S3 / RustFS 配置；
- 发布目标通常为 daemon 升级发布路径，例如 `upgrading/releases/daemon`；
- 发布后验证 S3 中确实存在该升级包和相关元数据。

验收标准：

- `publish` 命令执行成功；
- S3 / RustFS 中可以看到升级包；
- daemon/upgrader 使用的通道配置与发布目标一致。

## 5. 观察 Windows Service 升级行为

保持 daemon Windows Service 运行，观察它是否发现刚发布的升级包。

需要确认：

1. 是否检测到新版本；
2. 是否开始下载；
3. 是否下载成功；
4. 是否解压成功；
5. 是否进入部署阶段；
6. 是否调用 `Zongsoft.Upgrading.Deployer`；
7. deployer 是否执行成功；
8. Windows Service 是否被正确停止、更新和重新启动；
9. daemon 是否升级到新版本。

验收标准：

- daemon 日志显示发现升级包；
- 下载、解压、部署过程无错误；
- deployer 执行成功；
- Windows Service 最终处于 `Running` 状态；
- 升级后日志或可观测标记显示首次发布版本，例如 `1.0.0-test.1`。

# 阶段三：再次升级验证

仅当首次升级完整成功后，继续执行本阶段。

## 1. 修改 daemon 可观测版本标记

修改 daemon 输出的模拟版本号，例如：

```text
Daemon upgrade marker: 1.0.0-test.2
```

## 2. 重新构建、打包、发布

重复以下步骤：

1. 构建 `Zongsoft.Daemon`；
2. 使用 `tool pack` 或 `upgrade.pack.cmd` 制作新的升级包；
3. 使用 `tool publish` 或 `upgrade.publish.cmd` 发布到同一个 S3 通道；
4. 验证 S3 中新版本发布成功。

验收标准：

- 新升级包版本号为递增后的版本；
- 发布成功；
- 旧版本和新版本不会互相覆盖导致元数据异常。

## 3. 等待 Windows Service 再次升级

观察 daemon Windows Service 是否再次发现新版本，并完成下载、解压、部署和服务重启。

验收标准：

- daemon 发现第二个版本；
- deployer 再次执行成功；
- Windows Service 最终处于 `Running` 状态；
- 升级后日志或可观测标记显示第二次发布版本，例如 `1.0.0-test.2`。

# 清理要求

测试完成后必须卸载 Windows Service。

执行要求：

1. 调用 `/Zongsoft/hosting/daemon/uninstall.cmd`；
2. 或使用等价的 `sc stop` 和 `sc delete` 命令；
3. 确认服务已不存在；
4. 记录是否保留部署目录、日志和升级包用于排查。

验收标准：

- `Get-Service zongsoft.daemon` 或对应服务名不再返回有效服务；
- 没有遗留运行中的 daemon 进程；
- 清理结果写入最终报告。

# 异常处理要求

## Deployer 启动失败

如果 daemon 因为没有管理员权限而无法启动 deployer 进程，需要先定位失败原因。

需要检查：

1. daemon 日志；
2. upgrader 日志；
3. deployer 启动代码；
4. Windows 事件日志；
5. 失败原因是权限问题、路径问题、工作目录问题、UAC 问题、服务账号问题，还是参数问题。

如果确认是权限导致：

1. 尝试修改启动 deployer 进程的方式；
2. 优先考虑与 Windows Service 运行账号兼容的启动方式；
3. 记录修改内容和验证结果；
4. 如果仍无法解决，不要继续盲目修改，整理失败原因、日志、已尝试方案，然后与用户讨论下一步方案。

## Windows Service 启动或重启失败

如果升级后服务无法启动，需要检查：

1. `sc query` 和 `sc qc` 输出；
2. Windows 事件查看器中的应用和系统日志；
3. daemon 应用日志；
4. 服务 `binPath` 是否仍指向正确文件；
5. 服务账号是否有部署目录和日志目录权限；
6. 升级过程中是否覆盖、删除或锁定了正在运行的文件。

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

1. 执行环境；
2. 启动的依赖服务；
3. daemon 部署目录；
4. Windows Service 名称、状态和 `binPath`；
5. upgrader 插件部署位置；
6. deployer 部署位置；
7. 两次升级包的版本号；
8. `pack` / `publish` 命令摘要；
9. daemon 发现、下载、解压、部署、停止、重启或加载的日志结论；
10. 两次升级后的 daemon 可观测标记结果；
11. 遇到的问题和解决方案；
12. Windows Service 卸载结果；
13. 最终结论：Windows Service 下 daemon 升级全流程是否验证通过。
