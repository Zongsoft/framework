# Windows Terminal 升级验证工作流

先阅读 `.ai/common.md`，再执行本场景。本文只记录 Windows Terminal 的差异项，通用约束、发布节奏、异常处理和报告格式以 common 为准。

## 场景目标

- 平台：Windows 本机 PowerShell / Windows 环境，不使用 WSL。
- 宿主程序：`/Zongsoft/hosting/terminal`。
- 升级身份：`Zongsoft.Terminal`。
- 托管方式：普通 terminal 进程；不涉及 Windows Service。
- 验证轮次：至少两轮，示例版本 `1.0.0.1` -> `1.0.0.2`。

升级必须证明 terminal 能发现、下载、解压、调用 `Zongsoft.Upgrading.Deployer`、完成部署，并通过日志、命令输出或可观测标记确认新版本生效。

## 必读和假设

执行前先声明：

- 当前仓库路径、目标框架、架构、发布通道、部署目录。
- 包名是否仍为 `Zongsoft.Terminal`；除非源码或配置明确支持插件级升级，不要用 `Zongsoft.Commands` 作为默认发布身份。
- 版本号必须高于当前已加载应用版本，否则 upgrader 可能判定没有新版本。

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

## Windows 注意点

- 非交互终端中 `dotnet deploy` 或 `dotnet-upgrade` 若因 `ConsoleTerminal` / `句柄无效` 失败，不要误判业务失败；改用 `Start-Process cmd.exe /c ... > log 2>&1`，记录日志和退出码。
- 手工执行含 `$(edition)`、`$(framework)`、`$(scheme)` 的部署命令时，用单引号保护占位符，避免 PowerShell 当作子表达式。
- 如果 `dotnet deploy` 不可用，可手工部署最小插件集：`Zongsoft.Upgrading.Upgrader.*` 和 `Zongsoft.Externals.Amazon.*`，并确认 `AWSSDK.Core.dll`、`AWSSDK.S3.dll` 存在。
- Deployer 在 Windows 下按 Native AOT 单文件发布，最终确认 `.deployer\Zongsoft.Upgrading.Deployer.exe` 存在；未修改 deployer 相关代码或发布属性时可复用已验证产物。
- `dotnet-upgrade pack --output` 使用目录时带尾斜杠，例如 `--output:"D:\Zongsoft\\"`，避免生成到 `D:\Zongsoft.zip` 这类错误位置。
- 没有 `aws` / `mc` 时，可用本机 NuGet 缓存中的 AWSSDK 在 PowerShell 中列 RustFS/S3 对象。

## 执行清单

1. 检查 Redis 和 RustFS/S3：优先使用 `/Zongsoft/hosting` 中已有 Podman/compose/脚本；验证 endpoint、bucket、账号和发布通道一致。
2. 构建并部署 terminal：进入 `/Zongsoft/hosting/terminal`，构建、部署，记录输出目录。
3. 部署 upgrader：复制到 `plugins/zongsoft/upgrader`，确认插件和依赖完整，启动日志无加载错误。
4. 部署 deployer：确认 upgrader 配置或源码中的查找路径，再放入 terminal 部署目录下 `.deployer`。
5. 启动 terminal：保持运行并记录启动命令、工作目录、环境变量和日志位置。
6. 第一轮升级：加入可观测标记，例如 `Terminal upgrade marker: 1.0.0.1`；构建、pack、publish，验证 S3 对象存在，观察发现/下载/解压/部署/重启或热更新。
7. 第二轮升级：把标记和包版本递增到 `1.0.0.2`，重复构建、打包、发布和观察。

## 验收重点

- terminal 正常启动，upgrader 正常加载。
- pack/publish 成功，包元数据中的名称、版本、模式、平台、架构正确。
- terminal 日志显示发现新版本、下载成功、解压成功、调用 deployer 且 deployer 成功。
- 两轮升级后都能观察到对应 marker。
- 所有命令、输出摘要、日志路径、判断结论写入最终报告。

## 交付报告补充

除 common 报告项外，必须包含：

- terminal 部署目录。
- upgrader 插件部署位置。
- deployer 部署位置。
- 两次升级包版本号和包路径。
- terminal 两次升级后的日志、命令输出或 marker 证据。
