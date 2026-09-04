# 📦 发布 NuGet 包

[English](PUBLISHING.md) |
[简体中文](PUBLISHING.zh-Hans.md)

本仓库通过手动触发的 [Publish NuGet Packages](.github/workflows/publish-nuget.yml) 工作流发布 NuGet 包。普通代码推送和拉取请求只会运行持续集成，不会发布任何包。

> 💡 提示：当维护者在 GitHub Actions 中选择一个项目并点击 **Run workflow** 时，就明确表达了“现在发布这个包”的意图。GitHub 不会根据代码变更自动猜测是否需要发布。

如果你只需要发布包，可以直接阅读[发布前检查](#发布前检查)和[在-github-上发布](#在-github-上发布)。仓库管理员首次配置或排查身份验证问题时，再阅读[一次性配置](#一次性配置)和[概念说明](#概念说明)。

## 🧭 谁决定何时发布

发布时机始终由维护者决定，整个过程分为三步：

1. 维护者手动启动发布工作流，并选择本次要发布的项目。
2. GitHub 根据工作流中的项目选项执行对应的 Cake 命令。
3. [Trusted Publishing](#trusted-publishing) 只验证这个 GitHub 工作流是否有权向 [nuget.org](https://nuget.org) 发布；它不会主动触发发布。

> 💡 提示：`git push`、合并拉取请求和持续集成本身都不会发布 NuGet 包。只有手动启动 `publish-nuget.yml` 才会进入发布流程。

## ⚙️ 一次性配置

> 💡 提示：下面的配置通常只需由仓库管理员完成一次。日常发布人员不需要每次重复配置。

### 配置 nuget.org 策略

[nuget.org](https://nuget.org) 上的 [Trusted Publishing](#trusted-publishing) 策略必须与发布工作流保持一致：

| 配置项 | 值 |
| --- | --- |
| Repository Owner | `Zongsoft` |
| Repository | `framework` |
| Workflow File | `publish-nuget.yml` |
| Environment | `release` |

策略的 **Scope** 设置为 **Push new packages and package versions**，**Glob Pattern** 设置为 `Zongsoft.*`。这表示工作流可以首次发布名称匹配 `Zongsoft.*` 的新包，也可以发布已有包的新版本。

> 💡 提示：**Glob Pattern** 是 Package ID 的匹配规则，不是仓库文件路径。`Zongsoft.*` 表示允许发布名称以 `Zongsoft.` 开头的包。

> 🚨 注意：除非以后明确需要通过工作流下架包，否则不要授予 **Unlist package** 权限。

### 配置 GitHub release 环境

打开 GitHub 仓库，进入 **Settings** → **Environments** → **release**。如果 `release` 尚不存在，请先创建它，然后完成以下设置：

1. 在 **Environment secrets** 中添加名为 `NUGET_USER` 的 [Environment Secret](#environment-secret)，值填写 [nuget.org](https://nuget.org) 的用户名，不要填写电子邮箱或 API Key。
2. 在 **Deployment protection rules** 中按需设置 [Required reviewers](#required-reviewer)。这是一道可选的人工确认关卡。
3. 在 **Deployment branches and tags** 中选择 **Selected branches and tags**，添加类型为 **Branch**、名称为 `main` 的[部署分支规则](#deployment-branch-rule)。

> 🚨 注意：如果仓库只有一名发布人员，可以把自己设为 Reviewer，但不要启用 **Prevent self-review**，否则自己启动的工作流无法由自己批准。如果不需要发布前的二次确认，也可以不设置 Required reviewers。

> 💡 提示：工作流文件已经检查发布分支必须是 `main`；环境中的分支规则是 GitHub 服务端的第二层保护，建议仍然设置。

## ✅ 发布前检查

1. 更新包版本和发布说明，将改动提交到 `main`。
2. 正式占用一个不可重复使用的版本号前，先进入准备发布的项目目录，在本地生成并检查包：

   ```powershell
   dotnet pack -c Release -o ./artifacts
   ```

3. 确认包 ID、版本、依赖项、README、许可证和符号包均正确。
4. 推送发布提交，并等待持续集成通过。

选中的 Cake 脚本会构建对应解决方案，并发布该项目目录下生成的所有适用 `.nupkg`，因此某些选项可能同时发布多个关联包。

> 💡 提示：`Zongsoft.Data` 采用特殊处理。选择 `Zongsoft.Data` 只发布核心数据包，不会连带发布任何驱动；每个数据驱动都是独立的发布选项，需要分别选择和发布。

### Zongsoft.Data 本地 Cake 命令

在 `Zongsoft.Data` 目录中执行对应的本地 Cake 命令：

> 🚨 注意：下面是兼容现有方式的本地发布命令，不是 GitHub Trusted Publishing 流程。直接从本机向 [nuget.org](https://nuget.org) 推送包时，仍需在本机提供自己创建的长期 API Key。

```powershell
# 仅发布 Zongsoft.Data（以下两条命令效果相同）
dotnet cake --edition Release --target pack
dotnet cake --edition Release --target pack --drivers none

# 发布所有数据驱动
dotnet cake --edition Release --target pack --drivers *

# 发布指定的单个数据驱动
dotnet cake --edition Release --target pack --drivers=mssql
dotnet cake --edition Release --target pack --drivers=mysql
dotnet cake --edition Release --target pack --drivers=sqlite
dotnet cake --edition Release --target pack --drivers=duckdb
dotnet cake --edition Release --target pack --drivers=influx
dotnet cake --edition Release --target pack --drivers=tdengine
dotnet cake --edition Release --target pack --drivers=postgres
dotnet cake --edition Release --target pack --drivers=clickhouse
```

> 💡 提示：省略 `--drivers` 时，Cake 脚本将其按空值处理，与传入 `--drivers none` 的效果相同，都只处理 `Zongsoft.Data` 核心项目；该规则同时适用于构建、测试和发布。若要处理全部驱动，必须明确传入 `--drivers *`。

## 🚀 在 GitHub 上发布

1. 打开仓库的 **Actions** 页面。
2. 选择 **Publish NuGet Packages**。
3. 点击 **Run workflow**，并保持分支为 `main`。
4. 选择要发布的项目或数据驱动。
5. 启动工作流；如果配置了 [Required reviewers](#required-reviewer)，再批准 `release` 环境的部署。
6. 工作流完成后，到 [nuget.org](https://nuget.org) 确认新版本已经发布。

也可以通过 GitHub CLI 在终端中启动同一个工作流。每次只复制执行准备发布的包所对应的一条命令。

```powershell
# 核心项目
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Core
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Net
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Web
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Plugins
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Plugins.Web
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Commands
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Security
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Reporting
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Diagnostics
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Intelligences
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Hardwares

# 数据驱动
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data.MsSql
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data.MySql
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data.SQLite
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data.DuckDB
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data.Influx
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data.TDengine
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data.PostgreSql
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data.ClickHouse

# 消息队列
gh workflow run publish-nuget.yml --ref main -f project=messaging/zero
gh workflow run publish-nuget.yml --ref main -f project=messaging/mqtt
gh workflow run publish-nuget.yml --ref main -f project=messaging/kafka
gh workflow run publish-nuget.yml --ref main -f project=messaging/rabbit

# 自动升级
gh workflow run publish-nuget.yml --ref main -f project=upgrading/upgrader
gh workflow run publish-nuget.yml --ref main -f project=upgrading/tool
gh workflow run publish-nuget.yml --ref main -f project=upgrading/web

# 外部扩展
gh workflow run publish-nuget.yml --ref main -f project=externals/aliyun
gh workflow run publish-nuget.yml --ref main -f project=externals/amazon
gh workflow run publish-nuget.yml --ref main -f project=externals/redis
gh workflow run publish-nuget.yml --ref main -f project=externals/polly
gh workflow run publish-nuget.yml --ref main -f project=externals/wechat
gh workflow run publish-nuget.yml --ref main -f project=externals/closedxml
gh workflow run publish-nuget.yml --ref main -f project=externals/etcd
gh workflow run publish-nuget.yml --ref main -f project=externals/garnet
gh workflow run publish-nuget.yml --ref main -f project=externals/hangfire
gh workflow run publish-nuget.yml --ref main -f project=externals/scriban
gh workflow run publish-nuget.yml --ref main -f project=externals/python
gh workflow run publish-nuget.yml --ref main -f project=externals/lua
gh workflow run publish-nuget.yml --ref main -f project=externals/opc
```

## 🧩 概念说明

<a id="trusted-publishing"></a>
### Trusted Publishing（受信任发布）

Trusted Publishing 是 [nuget.org](https://nuget.org) 与 GitHub Actions 之间建立的信任关系。它根据仓库所有者、仓库名、工作流文件和 Environment 等信息识别发布者，从而避免在 GitHub 中长期保存 NuGet API Key。

> 💡 提示：Trusted Publishing 解决的是“谁有权发布”，不是“什么时候发布”。发布时机仍由维护者通过 **Run workflow** 决定。

<a id="github-environment"></a>
### GitHub Environment

GitHub Environment 是 GitHub Actions 中专门用于保护部署或发布任务的一组配置。本文使用的环境名是 `release`，工作流通过 `environment: release` 引用它。

> 💡 提示：`release` 只是一个环境名称，不是 Git 分支，也不是 GitHub Release。它可以集中保存发布所需的 Secret，并设置人工审批和允许发布的分支。

<a id="environment-secret"></a>
### Environment Secret 与 NUGET_USER

Environment Secret 是只提供给指定 GitHub Environment 的隐藏配置值。`NUGET_USER` 保存的是 [nuget.org](https://nuget.org) 用户名，`NuGet/login@v1` 用它确定要以哪个 NuGet 账号申请临时发布凭据。

> 💡 提示：`NUGET_USER` 不是密码，也不是 API Key。本文把它保存为 Secret，是为了让发布身份配置统一放在 `release` 环境中。

<a id="required-reviewer"></a>
### Required reviewer（必需审查者）

Required reviewer 是 `release` 环境上的可选人工审批规则。配置后，手动启动工作流仍不足以立即发布：发布任务会暂停，等待指定人员在 Actions 运行页面点击 **Review deployments** → **Approve and deploy**。

它的设置入口是 **Settings** → **Environments** → **release** → **Deployment protection rules** → **Required reviewers**。

> 🚨 注意：如果启用 **Prevent self-review**，启动工作流的人不能审批自己的发布。单人维护仓库通常不应启用该选项；需要双人复核时，则可以启用并指定另一名维护者。

<a id="deployment-branch-rule"></a>
### Deployment branch rule（部署分支规则）

部署分支规则用于限制哪些分支可以使用 `release` 环境。本文只允许 `main`，因此即使有人从其他分支手动启动工作流，也不能取得该环境的发布权限。

它的设置入口是 **Settings** → **Environments** → **release** → **Deployment branches and tags**。选择 **Selected branches and tags**，再添加类型为 **Branch**、名称为 `main` 的规则。

> 💡 提示：这不是普通的 Git 分支保护规则。它不会限制提交或合并代码，只限制工作流能否使用 `release` 环境执行发布。

<a id="oidc-temporary-api-key"></a>
### OIDC 与临时 API Key

OIDC 是 OpenID Connect 的缩写。在这里，它让 GitHub 用一份短期、可验证的身份证明告诉 [nuget.org](https://nuget.org)：“这次请求确实来自指定仓库、工作流和环境”。因此无需在 GitHub 中长期保存 NuGet API Key。

该工作流只授予 `contents: read` 和 `id-token: write` 权限。发布时的身份验证过程如下：

> 💡 身份验证过程：GitHub 为本次工作流签发 OIDC 身份证明 → [nuget.org](https://nuget.org) 检查 Trusted Publishing 策略 → `NuGet/login@v1` 获得短期 API Key → Cake 使用该临时值推送包。

之所以仍能看到 `NUGET_API_KEY` 这个名称，是因为 `dotnet nuget push` 的发布接口仍接收 API Key。这个值只为当前工作流临时生成，不是保存在 GitHub Secrets 中的长期密钥。只有直接从本机发布时，才需要维护者自行创建和保管长期 API Key。

> 🚨 注意：如果重命名 `publish-nuget.yml` 或修改其 `release` Environment，必须同步更新 [nuget.org](https://nuget.org) 上的 Trusted Publishing 策略，否则 [nuget.org](https://nuget.org) 将无法识别这个工作流。

## 🩺 故障排查

- **工作流显示 Waiting，迟迟没有开始发布**：这通常不是故障，而是 `release` 环境正在等待 [Required reviewer](#required-reviewer) 审批。打开本次 Actions 运行，点击 **Review deployments** → **Approve and deploy**。
- **无法审批自己启动的工作流**：检查 `release` 环境是否启用了 **Prevent self-review**。单人维护仓库应关闭该选项。
- **读取不到 NUGET_USER**：确认它添加在 `release` 的 **Environment secrets** 中，并且值为 [nuget.org](https://nuget.org) 用户名而不是电子邮箱。
- **No matching trust policy**：检查 [nuget.org](https://nuget.org) 策略中的仓库所有者、仓库名、工作流文件名和 [Environment](#github-environment) 是否完全匹配。
- **NuGet login 返回 403**：确认发布作业仍然具有 `id-token: write` 权限。
- **包不在允许范围内**：确认 Package ID 匹配 `Zongsoft.*`，并且属于策略选定的 [nuget.org](https://nuget.org) 所有者。
- **全新 Package ID 被拒绝**：确认策略允许 **Push new packages and package versions**，并确认 Package ID 匹配其 Glob Pattern。
- **重复版本被跳过**：Cake 脚本启用了 `SkipDuplicate`；再次发布前必须递增项目版本。

## 📚 参考资料

- [nuget.org 上的受信任发布](https://learn.microsoft.com/zh-cn/nuget/nuget-org/trusted-publishing)
- [管理 Trusted Publishing 策略](https://www.nuget.org/account/trustedpublishing?fromApiKeys=true)
- [NuGet/login GitHub Action](https://github.com/NuGet/login)
- [手动运行 GitHub Actions 工作流](https://docs.github.com/zh/actions/how-tos/manage-workflow-runs/manually-run-a-workflow)
- [管理 GitHub 部署环境](https://docs.github.com/zh/actions/how-tos/deploy/configure-and-manage-deployments/manage-environments)
- [审查 GitHub 部署](https://docs.github.com/zh/actions/how-tos/deploy/configure-and-manage-deployments/review-deployments)
