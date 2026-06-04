# Linux/WSL Terminal 升级全流程验证工作流

## 目标

你需要在 Linux/WSL 环境中验证 `/Zongsoft/hosting/terminal` 宿主程序的完整升级流程。

本次验证的核心目标是确认 terminal 宿主程序能够通过升级系统完成以下流程：

1. 发现 `Zongsoft.Commands` 插件的新升级包；
2. 下载升级包；
3. 解压升级包；
4. 调用 `Zongsoft.Upgrading.Deployer` 执行部署；
5. 完成 terminal 宿主程序或插件的升级；
6. 验证升级后的 `Zongsoft.Commands` 插件确实生效。

验证流程需要完成两轮：

1. 首次升级验证；
2. 在首次升级成功后，再次修改版本号并重复升级，验证连续升级流程。

## 执行约束

- 当前平台是 Linux/WSL，必须在 WSL shell / Linux 环境中执行。
- 不要使用 Windows PowerShell、CMD 或 Windows 原生命令执行构建、部署和验证步骤；仅在需要说明 Windows 宿主路径映射时记录对应路径。
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
| Terminal 宿主程序 | `/Zongsoft/hosting/terminal` |
| Upgrader 插件源码 | `/Zongsoft/framework/upgrading/upgrader` |
| Deployer 程序源码 | `/Zongsoft/framework/upgrading/deployer` |
| 待升级插件项目 | `/Zongsoft/framework/Zongsoft.Commands` |
| Upgrader 插件部署目录 | terminal 部署目录下的 `plugins/zongsoft/upgrader` |
| Deployer 部署目录 | 根据 upgrader 配置、源码或日志确认 |
| 升级工具 / tool | 在仓库中查找已有 tool 项目或命令入口 |

如果上述路径在 WSL 中不存在，需要先定位实际路径。常见映射示例：

```text
D:\Zongsoft\framework\upgrading -> /mnt/d/Zongsoft/framework/upgrading
```

# 阶段一：环境准备

## 1. 检查 Linux/WSL 运行环境

确认 WSL 发行版和 Linux 工具链满足验证需求。

需要检查：

1. WSL 发行版、内核和架构；
2. `dotnet` SDK 版本；
3. `podman` 或 `docker` 是否可用；
4. `curl`、`tar`、`unzip`、`rsync` 等基础工具是否可用；
5. 当前 shell 用户对部署目录、日志目录和临时目录是否有写入权限；
6. 如果仓库位于 `/mnt/<drive>`，确认文件权限、可执行位和换行符不会影响 Linux 运行。

验收标准：

- 能在 WSL/Linux 中执行 `dotnet --info`；
- 能构建 .NET 项目；
- 能启动或连接依赖服务；
- 部署目录可写；
- 需要执行的 Linux 可执行文件具有执行权限。

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
- 升级发布命令能够连接到目标 S3 通道。

## 3. 构建并部署 terminal 宿主程序

执行以下工作：

1. 进入 `/Zongsoft/hosting/terminal`；
2. 编译构建 terminal 宿主程序；
3. 执行 `dotnet deploy` 或项目约定的 Linux 部署命令；
4. 记录部署输出目录；
5. 确认生成产物适用于 Linux/WSL 运行时，不要误用 Windows 专用产物。

验收标准：

- terminal 构建成功；
- 部署命令执行成功；
- 能找到部署后的宿主程序目录；
- terminal 可执行入口在 Linux 中可以运行。

## 4. 部署 upgrader 插件

将 `/Zongsoft/framework/upgrading/upgrader` 插件构建产物手动部署到 terminal 宿主程序的插件目录：

```text
plugins/zongsoft/upgrader
```

验收标准：

- upgrader 插件文件存在于宿主程序插件目录；
- 插件依赖文件完整；
- 文件名大小写与 Linux 文件系统中的实际引用一致；
- terminal 启动日志中可以看到 upgrader 插件被加载，或者没有插件加载错误。

## 5. 部署 Zongsoft.Upgrading.Deployer

构建 `/Zongsoft/framework/upgrading/deployer`，并将生成的 `Zongsoft.Upgrading.Deployer` 程序放到 terminal/upgrader 期望的位置。

不要猜测目标位置。优先通过以下信息确认：

1. upgrader 配置文件；
2. upgrader 源码中启动 deployer 的路径逻辑；
3. terminal 运行日志；
4. 仓库中已有部署脚本。

Linux/WSL 下还需要确认：

1. deployer 入口是 `dotnet Zongsoft.Upgrading.Deployer.dll` 还是自包含可执行文件；
2. 如果是自包含可执行文件，是否具有执行权限；
3. shebang、文件名大小写、相对路径和工作目录是否符合 Linux 行为；
4. deployer 所需运行时标识符是否与当前 WSL 环境匹配。

验收标准：

- deployer 可执行入口存在于正确目录；
- terminal/upgrader 能找到 deployer；
- deployer 在 Linux/WSL 中可以启动；
- 没有 `file not found`、`permission denied`、`bad interpreter` 或路径解析错误。

## 6. 启动 terminal 宿主程序

运行部署后的 terminal 宿主程序，并保持其运行，用于观察升级发现和执行过程。

要求：

- 使用 Linux shell 启动；
- 记录启动命令、工作目录、环境变量和日志位置；
- 如果需要长时间观察，使用 `tmux`、`screen`、后台进程或项目约定的服务启动方式；
- 不要通过 Windows 终端直接启动 Windows 版可执行文件。

验收标准：

- terminal 正常启动；
- upgrader 正常加载；
- 日志中没有阻断升级流程的错误。

# 阶段二：首次打包、发布和升级验证

## 1. 修改 Zongsoft.Commands

在 `/Zongsoft/framework/Zongsoft.Commands` 项目中添加一个 `TestCommand` 命令。

要求：

- 命令可以被 terminal 或命令系统发现；
- 命令输出一个模拟版本号；
- 版本号后续用于验证升级是否生效。

首次版本号示例：

```text
TestCommand version: 1.0.0-test.1
```

验收标准：

- 项目编译成功；
- 新命令符合现有命令注册 / 发现机制；
- 可以通过代码或运行方式确认该命令会被插件加载。

## 2. 构建 Zongsoft.Commands

编译 `/Zongsoft/framework/Zongsoft.Commands` 项目。

验收标准：

- 构建成功；
- 生成用于打包的插件产物；
- 产物适用于 Linux/WSL 运行，不包含 Windows 专用路径假设。

## 3. 制作升级包

使用升级 tool 中的 `pack` 命令，为 `Zongsoft.Commands` 制作升级包。

要求：

- 包版本号使用 `TestCommand` 中模拟的版本号；
- 打包模式使用 `Delta`；
- 明确记录 `pack` 命令参数和输出包位置；
- 在 Linux shell 中执行命令，并确认路径参数使用 Linux 路径格式。

验收标准：

- `pack` 命令执行成功；
- 生成升级包；
- 包元数据中的应用 / 插件名、版本号、模式、目标通道正确。

## 4. 发布升级包到 S3 通道

使用升级 tool 中的 `publish` 命令，将升级包发布到 S3 通道。

要求：

- 使用现有配置或仓库约定的 S3 / RustFS 配置；
- 发布后验证 S3 中确实存在该升级包和相关元数据；
- 如果 S3 endpoint 绑定在 Windows 宿主或容器网络中，确认 WSL 中访问的 endpoint 与 terminal/upgrader 使用的 endpoint 一致。

验收标准：

- `publish` 命令执行成功；
- S3 / RustFS 中可以看到升级包；
- terminal/upgrader 使用的通道配置与发布目标一致。

## 5. 观察 terminal 升级行为

保持 terminal 宿主程序运行，观察它是否发现刚发布的升级包。

需要确认：

1. 是否检测到新版本；
2. 是否开始下载；
3. 是否下载成功；
4. 是否解压成功；
5. 是否进入部署阶段；
6. 是否调用 `Zongsoft.Upgrading.Deployer`；
7. deployer 是否执行成功；
8. terminal 是否完成重启或热更新；
9. `Zongsoft.Commands` 插件是否升级到新版本。

验收标准：

- terminal 日志显示发现升级包；
- 下载、解压、部署过程无错误；
- deployer 执行成功；
- 升级后运行 `TestCommand`，输出版本号为首次发布版本，例如 `1.0.0-test.1`。

# 阶段三：再次升级验证

仅当首次升级完整成功后，继续执行本阶段。

## 1. 修改 TestCommand 版本号

修改 `TestCommand` 输出的模拟版本号，例如：

```text
TestCommand version: 1.0.0-test.2
```

## 2. 重新构建、打包、发布

重复以下步骤：

1. 构建 `Zongsoft.Commands`；
2. 使用 `tool pack` 制作新的 Delta 升级包；
3. 使用 `tool publish` 发布到同一个 S3 通道；
4. 验证 S3 中新版本发布成功。

验收标准：

- 新升级包版本号为递增后的版本；
- 发布成功；
- 旧版本和新版本不会互相覆盖导致元数据异常。

## 3. 等待 terminal 再次升级

观察 terminal 是否再次发现新版本，并完成下载、解压、部署。

验收标准：

- terminal 发现第二个版本；
- deployer 再次执行成功；
- 升级后运行 `TestCommand`，输出版本号为第二次发布版本，例如 `1.0.0-test.2`。

# 异常处理要求

## Deployer 启动失败

如果 terminal 无法启动 deployer 进程，需要先定位失败原因。

需要检查：

1. terminal 日志；
2. upgrader 日志；
3. deployer 启动代码；
4. 失败原因是权限问题、路径问题、工作目录问题、可执行权限问题、运行时问题，还是参数问题。

Linux/WSL 下重点检查：

1. deployer 文件是否缺少执行权限；
2. 是否误引用了 Windows 路径或 Windows 可执行文件；
3. 文件名大小写是否匹配；
4. 脚本是否因为 CRLF 出现 `bad interpreter`；
5. 运行时标识符、架构和 `dotnet` 运行时是否匹配；
6. WSL 中是否可以访问部署目录、临时目录和日志目录。

如果确认是权限导致：

1. 尝试修正文件权限或启动 deployer 进程的方式；
2. 优先考虑不依赖 `sudo` 的启动方式；
3. 记录修改内容和验证结果；
4. 如果仍无法解决，不要继续盲目修改，整理失败原因、日志、已尝试方案，然后与用户讨论下一步方案。

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
4. terminal 部署目录；
5. upgrader 插件部署位置；
6. deployer 部署位置；
7. 两次升级包的版本号；
8. `pack` / `publish` 命令摘要；
9. terminal 发现、下载、解压、部署、重启或加载的日志结论；
10. `TestCommand` 两次升级后的输出结果；
11. 遇到的问题和解决方案；
12. 最终结论：Linux/WSL 下升级全流程是否验证通过。
