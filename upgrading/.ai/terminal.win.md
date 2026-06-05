# Windows Terminal 升级全流程验证工作流

## 目标

你需要在 Windows 本机验证 `/Zongsoft/hosting/terminal` 宿主程序的完整升级流程。

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

- 当前平台是 Windows，必须在本机 PowerShell / Windows 环境中执行。
- 不要使用 WSL。
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
| Terminal 宿主程序 | `/Zongsoft/hosting/terminal` |
| Upgrader 插件源码 | `/Zongsoft/framework/upgrading/upgrader` |
| Deployer 程序源码 | `/Zongsoft/framework/upgrading/deployer` |
| 待升级插件项目 | `/Zongsoft/framework/Zongsoft.Commands` |
| Upgrader 插件部署目录 | terminal 部署目录下的 `plugins/zongsoft/upgrader` |
| Deployer 部署目录 | terminal 部署目录下的 `.deployer` 目录 |
| 升级工具 / tool | 在仓库中查找已有 tool 项目或命令入口 |

## Windows 实操补充

以下补充来自实际执行 Windows 升级工作流时遇到的问题。terminal 工作流不涉及 Windows Service 安装，但构建、部署、打包、发布和 S3 通道验证会遇到相同的 Windows 自动化细节。

### 1. 非交互终端运行 Zongsoft CLI 的注意事项

在 Codex/自动化 PowerShell 这类非交互终端中，`dotnet deploy`、`dotnet-upgrade` 可能在初始化 `Zongsoft.Terminals.Terminal.Console` 时失败：

```text
System.TypeInitializationException: The type initializer for 'ConsoleTerminal' threw an exception.
System.IO.IOException: 句柄无效。
at System.ConsolePal.set_TreatControlCAsInput(Boolean value)
```

如果出现该错误，不要误判为部署、打包或发布逻辑失败。可改用独立 `cmd.exe` 进程运行命令，并将输出重定向到日志文件，例如：

```powershell
$log = 'D:\Zongsoft\framework\upgrading\.artifacts\terminal-pack.log'
$cmd = 'dotnet-upgrade pack --name:Zongsoft.Commands --kind:delta --version:1.0.0.1 --checksum:sha1 --compilation:Debug --framework:net10.0 --platform:windows --architecture:x64 --source:"D:\Zongsoft\framework\Zongsoft.Commands\src\bin\Debug\net10.0" --output:"D:\Zongsoft\\" > "' + $log + '" 2>&1'
$p = Start-Process -FilePath cmd.exe -ArgumentList '/c', $cmd -Wait -PassThru -WindowStyle Hidden
$p.ExitCode
Get-Content -LiteralPath $log
```

注意：`conhost.exe --headless` 曾被验证不能可靠执行子命令，不建议用它绕过该问题。

### 2. PowerShell 中要保护 `$(...)` 占位符

脚本中的 `$(edition)`、`$(framework)`、`$(scheme)` 等是 Zongsoft 部署工具占位符。直接在 PowerShell 中输入时会被 PowerShell 当作子表达式执行。

如果手工运行 `dotnet deploy`，需要用单引号保护这些参数：

```powershell
dotnet deploy `
	--verbosity:normal `
	--overwrite:newest `
	--host:terminal `
	--site:terminal `
	--scheme:default `
	--environment:development `
	--debug:on `
	--edition:Debug `
	--framework:net10.0 `
	--platform:windows `
	--architecture:x64 `
	'--destination:bin/$(edition)/$(framework)' `
	.deploy `
	'../.deploy/$(scheme)/$(host).deploy' `
	'../.deploy/$(scheme)/$(site).deploy'
```

### 3. 手工部署 upgrader 与 S3 文件系统插件

如果 `dotnet deploy` 因非交互终端失败，可以手工部署最小验证集。

terminal 输出目录通常为：

```text
D:\Zongsoft\hosting\terminal\bin\Debug\net10.0
```

upgrader 插件应包含：

```text
plugins\zongsoft\upgrader\Zongsoft.Upgrading.Upgrader.plugin
plugins\zongsoft\upgrader\Zongsoft.Upgrading.Upgrader.option
plugins\zongsoft\upgrader\Zongsoft.Upgrading.Upgrader.dll
```

upgrader 默认 `File` 通道使用：

```text
zfs.s3:/upgrading/releases/
```

因此 terminal 还必须能加载 `zfs.s3` 文件系统提供程序。至少部署 `Zongsoft.Externals.Amazon` 插件：

```text
plugins\zongsoft\externals\amazon\Zongsoft.Externals.Amazon.plugin
plugins\zongsoft\externals\amazon\Zongsoft.Externals.Amazon.option
plugins\zongsoft\externals\amazon\Zongsoft.Externals.Amazon.dll
plugins\zongsoft\externals\amazon\AWSSDK.Core.dll
plugins\zongsoft\externals\amazon\AWSSDK.S3.dll
```

如果漏掉 `AWSSDK.S3.dll`，Windows 事件日志或 terminal 日志中可能出现：

```text
System.IO.FileNotFoundException: Could not load file or assembly 'AWSSDK.S3'
```

RustFS 默认连接配置可使用 `/Zongsoft/framework/externals/amazon/src/Zongsoft.Externals.Amazon.option` 中的配置：

```text
server=http://127.0.0.1:9000;region=cn-north-1;accessKey=rustfsadmin;secretKey=rustfsadmin
```

### 4. deployer 的固定位置

upgrader 源码通过 `{ApplicationPath}/.deployer/Zongsoft.Upgrading.Deployer.exe` 查找部署器。Windows 下发布命令为：

```cmd
dotnet publish Zongsoft.Upgrading.Deployer.csproj ^
	--self-contained ^
	--runtime win-x64 ^
	--framework net10.0 ^
	--configuration Release ^
	-p:PublishAot=true
```

发布后将 `bin\Release\net10.0\win-x64\publish\*` 复制到 terminal 输出目录的 `.deployer` 目录。确认存在：

```text
.deployer\Zongsoft.Upgrading.Deployer.exe
```

### 5. `dotnet-upgrade pack --output` 的尾斜杠

如果 `--output:"D:\Zongsoft"` 没有尾斜杠，工具可能把它当作文件基名，生成：

```text
D:\Zongsoft.zip
D:\Zongsoft.manifest
```

应使用带尾斜杠的目录：

```text
--output:"D:\Zongsoft\\"
```

预期输出类似：

```text
D:\Zongsoft\Zongsoft.Commands@1.0.0.1_win-x64.zip
D:\Zongsoft\Zongsoft.Commands@1.0.0.1_win-x64.manifest
```

### 6. 发布后验证 RustFS/S3 对象

如果没有 `aws`、`mc` 等工具，可用本机 NuGet 缓存中的 AWSSDK 程序集在 PowerShell 中列对象：

```powershell
Add-Type -Path "$env:USERPROFILE\.nuget\packages\awssdk.core\4.0.3.27\lib\net8.0\AWSSDK.Core.dll"
Add-Type -Path "$env:USERPROFILE\.nuget\packages\awssdk.s3\4.0.20.3\lib\net8.0\AWSSDK.S3.dll"

$config = [Amazon.S3.AmazonS3Config]::new()
$config.ServiceURL = 'http://127.0.0.1:9000'
$config.ForcePathStyle = $true
$config.UseHttp = $true

$client = [Amazon.S3.AmazonS3Client]::new('rustfsadmin', 'rustfsadmin', $config)
$request = [Amazon.S3.Model.ListObjectsV2Request]::new()
$request.BucketName = 'upgrading'
$request.Prefix = 'releases/terminal'
$response = $client.ListObjectsV2Async($request).GetAwaiter().GetResult()
$response.S3Objects | Select-Object Key,Size,LastModified
$client.Dispose()
```

### 7. TestCommand 版本号和包版本号

`TestCommand` 输出的模拟版本号用于人工确认升级后的插件确实生效；升级包 `--version` 则用于 upgrader 判断是否存在新版本。两者建议保持同一轮次语义，但 `--version` 应使用升级系统可比较的版本号，例如：

```text
TestCommand version: 1.0.0.1
--version:1.0.0.1
```

如果包版本不高于当前已加载插件或应用版本，upgrader 可能会认为没有可用新版本。

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
- 升级发布命令能够连接到目标 S3 通道。

## 2. 构建并部署 terminal 宿主程序

执行以下工作：

1. 进入 `/Zongsoft/hosting/terminal`；
2. 编译构建 terminal 宿主程序；
3. 执行 `dotnet deploy` 或项目约定的部署命令；
4. 记录部署输出目录。

验收标准：

- terminal 构建成功；
- 部署命令执行成功；
- 能找到部署后的宿主程序目录。

## 3. 部署 upgrader 插件

将 `/Zongsoft/framework/upgrading/upgrader` 插件构建产物手动部署到 terminal 宿主程序的插件目录：

```text
plugins/zongsoft/upgrader
```

验收标准：

- upgrader 插件文件存在于宿主程序插件目录；
- 插件依赖文件完整；
- terminal 启动日志中可以看到 upgrader 插件被加载，或者没有插件加载错误。

## 4. 部署 Zongsoft.Upgrading.Deployer

构建 `/Zongsoft/framework/upgrading/deployer`，并将生成的 `Zongsoft.Upgrading.Deployer` 程序放到 terminal/upgrader 期望的位置。

不要猜测目标位置。优先通过以下信息确认：

1. upgrader 配置文件；
2. upgrader 源码中启动 deployer 的路径逻辑；
3. terminal 运行日志；
4. 仓库中已有部署脚本。

验收标准：

- deployer 可执行文件存在于正确目录；
- terminal/upgrader 能找到 deployer；
- 没有 `file not found` 或路径解析错误。

## 5. 启动 terminal 宿主程序

运行部署后的 terminal 宿主程序，并保持其运行，用于观察升级发现和执行过程。

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
TestCommand version: 1.0.0.1
```

验收标准：

- 项目编译成功；
- 新命令符合现有命令注册 / 发现机制；
- 可以通过代码或运行方式确认该命令会被插件加载。

## 2. 构建 Zongsoft.Commands

编译 `/Zongsoft/framework/Zongsoft.Commands` 项目。

验收标准：

- 构建成功；
- 生成用于打包的插件产物。

## 3. 制作升级包

使用升级 tool 中的 `pack` 命令，为 `Zongsoft.Commands` 制作升级包。

要求：

- 包版本号使用 `TestCommand` 中模拟的版本号；
- 打包模式使用 `Delta`；
- 明确记录 `pack` 命令参数和输出包位置。

验收标准：

- `pack` 命令执行成功；
- 生成升级包；
- 包元数据中的应用 / 插件名、版本号、模式、目标通道正确。

## 4. 发布升级包到 S3 通道

使用升级 tool 中的 `publish` 命令，将升级包发布到 S3 通道。

要求：

- 使用现有配置或仓库约定的 S3 / RustFS 配置；
- 发布后验证 S3 中确实存在该升级包和相关元数据。

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
- 升级后运行 `TestCommand`，输出版本号为首次发布版本，例如 `1.0.0.1`。

# 阶段三：再次升级验证

仅当首次升级完整成功后，继续执行本阶段。

## 1. 修改 TestCommand 版本号

修改 `TestCommand` 输出的模拟版本号，例如：

```text
TestCommand version: 1.0.0.2
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
- 升级后运行 `TestCommand`，输出版本号为第二次发布版本，例如 `1.0.0.2`。

# 异常处理要求

## Deployer 启动失败

如果 terminal 因为没有管理员权限而无法启动 deployer 进程，需要先定位失败原因。

需要检查：

1. terminal 日志；
2. upgrader 日志；
3. deployer 启动代码；
4. 失败原因是权限问题、路径问题、工作目录问题、UAC 问题，还是参数问题。

如果确认是管理员权限导致：

1. 尝试修改启动 deployer 进程的方式；
2. 优先考虑不依赖管理员权限的启动方式；
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

1. 执行环境；
2. 启动的依赖服务；
3. terminal 部署目录；
4. upgrader 插件部署位置；
5. deployer 部署位置；
6. 两次升级包的版本号；
7. `pack` / `publish` 命令摘要；
8. terminal 发现、下载、解压、部署、重启或加载的日志结论；
9. `TestCommand` 两次升级后的输出结果；
10. 遇到的问题和解决方案；
11. 最终结论：升级全流程是否验证通过。

