---
name: deployer
description: 处理 upgrading/deployer 下的 Zongsoft.Upgrading.Deployer Native AOT 可执行程序。用于修改或审查 .deployment 描述文件处理、完整/增量文件部署、清理/复制规则、部署执行器事件、服务/Web/终端重启启动器、平台相关发布脚本、Visual Studio 发布配置，或 Zongsoft 自动升级 deployer 文档。
---

# Zongsoft Upgrading Deployer

## 入手位置

修改行为前，优先读取最小且有用的文件集合：

- `README.md`：了解公开行为、参数、重启策略和发布说明。
- `Program.cs`：进程入口行为和 `key=value` 参数解析。
- `Deployer.Deploy.cs`：部署顺序和描述文件生命周期。
- `Deployer.Helper.cs`：清理、保留和复制行为。
- `Launcher.cs`、`Launcher.*.cs`：重启选择和平台相关启动行为。
- `ILauncher.cs`：修改启动器契约时读取。
- `Executor.Initializer.cs`：内置执行器命令注册。
- `Zongsoft.Upgrading.Deployer.csproj`：Native AOT、目标框架、链接的 `.shared` 文件和打包设置。
- `publish.linux-x64.*`、`build.cake`、`Properties/PublishProfiles/*.pubxml`：修改发布自动化或 AOT 单文件制作时读取。
- `../../framework.linux-x64.yaml` 和 `../../framework-start.cmd`：制作 Linux x64 AOT 单文件或调整容器发布流程时读取；从 `upgrading` 目录视角对应 `../framework.linux-x64.yaml` 和 `../framework-start.cmd`。
- `../.shared/*.cs`：触碰部署描述文件、manifest/release 模型、执行器契约或共享命令时读取。

## 核心契约

除非任务明确要改变部署语义，否则保留以下顺序：

1. 将 `key=value` 命令行参数解析为 `Deployer.Argument`。
2. 等待 `app.id` 对应进程退出；如果缺失或为零则跳过。
3. 以独占方式打开 `.deployment` 描述文件。
4. 加载描述文件引用的 manifest。
5. 对 `Fully` 发布，清理应用根目录，同时保留 deployer 目录和部署描述文件。
6. 执行 `Deploying` 执行器。
7. 将 `Packages` 中已解压的包文件复制到应用根目录。
8. 执行 `Deployed` 执行器。
9. 释放描述文件锁，并通过 `app.type` 选择的启动器重启宿主。

将 `.deployment` 视为 upgrader 向 deployer 交接的边界。它包含 `Manifest` 和 `Packages`，部署期间会被独占打开，并由部署描述文件生命周期删除。

## 跨项目契约

保持 deployer 兼容 `Zongsoft.Upgrading.Upgrader` 写入的 `.deployment` 文件，以及 `Zongsoft.Tools.Upgrader` 创建或 `Zongsoft.Upgrading.Web` 导入的 manifest。

修改 `../.shared/Deployer*.cs`、`Manifest`、`Release`、`Executor`、命令实现、`ReleaseKind`、`Platform` 或 `Architecture` 可能影响 upgrader、tool 和 web 包管理器。共享契约变化时检查这些项目。

## 实现规则

保持 deployer 适合作为进程外升级部署程序：

- 避免引入不利于 Native AOT、trimming 或独立发布的依赖。
- 新增 API 时考虑 `PublishAot`、`IsAotCompatible`、`StaticExecutable` 和 `InvariantGlobalization` 假设。
- 完整发布时不要覆盖或清理 `.deployer` 目录。
- 保留严格的描述文件锁；它用于避免宿主和 deployer 竞争。
- 保持执行器名称与共享命令名一致：`Copy`、`Move`、`Link`、`Delete`。
- 只在文档化的 `Deploying` 和 `Deployed` 阶段执行 manifest 执行器。
- 保持启动器行为感知平台：Windows Web 回收 IIS 应用池，Linux 或 FreeBSD 服务使用 `systemctl start`，Windows daemon 服务使用 `sc start`，terminal 或 universal 宿主使用直接进程启动。
- 将服务重启命令视为运维敏感操作；除非用户明确要求，否则不要在测试中运行它们。
- Windows terminal 验证时，部署后确认 `.deployer\Zongsoft.Upgrading.Deployer.exe` 仍存在；宿主 clean/deploy 可能清掉手工部署的 `.deployer`，可复用 `bin/Release/net10.0/win-x64/publish` 下已验证的 Native AOT 单文件产物。
- 验证 deployer 成功时，同时检查 `.deployment` 最终被清理、`.version` 更新为目标应用版本，以及 deployer 日志中出现 Terminal launcher 启动新宿主进程的记录。

## AOT 单文件发布

deployer 的发布产物应保持为自包含、单文件、Native AOT 可执行程序。修改发布文档、脚本或配置时，保持 `README.md`、`README-zh_CN.md`、`publish.linux-x64.*`、Visual Studio 发布配置和 `Zongsoft.Upgrading.Deployer.csproj` 的发布属性一致。

Native AOT 编译发布耗时较久，Linux x64 AOT 发布通常更慢。执行发布前先确认确实需要重新发布，预留足够时间，记录开始/结束时间和输出目录；在 `.ai` 工作流验证中，如果没有修改 deployer 相关代码、项目发布属性、发布脚本或目标 runtime，不需要重新编译和发布 AOT 程序，直接复用已验证的发布产物。

Windows x64 发布命令应保持包含这些关键属性：

```cmd
dotnet publish "Zongsoft.Upgrading.Deployer.csproj" ^
  -c Release ^
  -f net10.0 ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:PublishReadyToRun=true ^
  -p:PublishAot=true
```

Linux x64 AOT 单文件制作需要先启动 Podman 容器。优先参考 `README.md` 的 Publishing/Linux 章节，并保留以下流程：

1. 从仓库根目录或 `upgrading` 工作目录启动 `../../framework-start.cmd` 或 `../framework-start.cmd`，它使用 `framework.linux-x64.yaml` 启动 `zongsoft-framework` 容器。
2. 确认容器已完成加载：

```cmd
podman ps -a --pod
podman logs zongsoft-framework
```

3. 进入容器内发布：

```cmd
podman exec --workdir /Zongsoft/framework/upgrading/deployer -it zongsoft-framework sh
```

```shell
./publish.linux-x64.sh
```

4. 或者在容器运行期间，从宿主执行 `publish.linux-x64.ps1` 或 `publish.linux-x64.cmd`。
5. 发布完成后按 README 指引停止容器。

不要把 Linux AOT 单文件发布简化为普通宿主机 `dotnet publish`，除非任务明确要求改变构建环境；该流程依赖 Linux Alpine/.NET SDK 容器环境。

## 常见任务

修改清理或复制行为时，验证 `Fully` 和 `Delta` 两种模式，并保留包目录遍历语义。

修改重启行为时，更新匹配的 launcher 文件，并确认 `Launcher.Launch` 仍按 `app.type` 的 `Web`、`Daemon`、`Terminal` 和 fallback 选择。

修改命令行参数时，更新 `README.md`，并保持兼容 upgrader 生成的参数：

```text
app.id=12345
app.name=Zongsoft.Hosting.Terminal
app.type=Terminal
app.path=/opt/zongsoft/terminal
host.path=/opt/zongsoft/terminal/Zongsoft.Hosting.Terminal
host.args#0=...
deployment=/opt/zongsoft/terminal/.deployment
```

修改发布命令或发布配置时，让 Visual Studio、PowerShell、CMD、shell 脚本、Podman 容器流程和 README 示例在相同 runtime/framework 下保持一致。

## 文档

行为变化时同步更新文档：

- 更新 `README.md` 和 `README-zh_CN.md`，说明公开参数、部署、重启或发布变化。
- 修改项目发布属性时，同步更新发布脚本/配置。
- 服务权限等运维要求只写入文档，不作为测试副作用。

## 验证

优先执行聚焦验证：

```shell
dotnet build Zongsoft.Upgrading.Deployer.slnx
```

发布相关变化时，在目标环境中检查或运行相关发布命令/脚本。Native AOT 敏感变化时，在可行情况下对目标 runtime 执行一次代表性的 `dotnet publish`；该步骤可能耗时较久，尤其 Linux AOT 发布，验证计划中要预留等待时间并记录耗时。

除非用户明确要求集成测试，否则不要真实重启服务或修改线上应用目录。验证部署行为时使用临时应用目录和描述文件。
