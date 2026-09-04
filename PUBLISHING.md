# 📦 Publishing NuGet Packages

[English](PUBLISHING.md) |
[简体中文](PUBLISHING.zh-Hans.md)

This repository publishes NuGet packages through the manually triggered [Publish NuGet Packages](.github/workflows/publish-nuget.yml) workflow. Regular pushes and pull requests only run continuous integration and never publish packages.

> 💡 Tip: a maintainer explicitly expresses the intent to publish by selecting a project in GitHub Actions and clicking **Run workflow**. GitHub does not guess whether a package should be published from the code changes.

For routine releases, go directly to [Before publishing](#before-publishing) and [Publish from GitHub](#publish-from-github). Repository administrators only need [One-time configuration](#one-time-configuration) and [Concepts](#concepts) during initial setup or authentication troubleshooting.

## 🧭 Who decides when to publish

The maintainer always decides when to publish. The process has three parts:

1. A maintainer manually starts the publishing workflow and selects the project for this release.
2. GitHub runs the corresponding Cake command for that selection.
3. [Trusted Publishing](#trusted-publishing) only proves that this GitHub workflow is allowed to publish to [nuget.org](https://nuget.org); it never starts a release on its own.

> 💡 Tip: a `git push`, a merged pull request, or a continuous-integration run does not publish NuGet packages. Publishing starts only when someone manually runs `publish-nuget.yml`.

## ⚙️ One-time configuration

> 💡 Tip: these settings normally need to be completed only once by a repository administrator. Release maintainers do not repeat them for every package version.

### Configure the nuget.org policy

The [Trusted Publishing](#trusted-publishing) policy on [nuget.org](https://nuget.org) must match the publishing workflow:

| Setting | Value |
| --- | --- |
| Repository Owner | `Zongsoft` |
| Repository | `framework` |
| Workflow File | `publish-nuget.yml` |
| Environment | `release` |

Set **Scope** to **Push new packages and package versions** and **Glob Pattern** to `Zongsoft.*`. This allows the workflow to publish a matching package ID for the first time and to publish new versions of existing packages.

> 💡 Tip: a **Glob Pattern** matches package IDs, not repository file paths. `Zongsoft.*` allows package IDs that begin with `Zongsoft.`.

> 🚨 Caution: do not grant the **Unlist package** scope unless the workflow is intentionally extended to remove packages from listing.

### Configure the GitHub release environment

Open the GitHub repository and go to **Settings** → **Environments** → **release**. If `release` does not exist, create it first, then configure the following settings:

1. Under **Environment secrets**, add an [Environment Secret](#environment-secret) named `NUGET_USER`. Its value is the [nuget.org](https://nuget.org) profile name—not an email address or API key.
2. Under **Deployment protection rules**, optionally configure [Required reviewers](#required-reviewer) as a human confirmation gate.
3. Under **Deployment branches and tags**, select **Selected branches and tags**, then add a [deployment branch rule](#deployment-branch-rule) with type **Branch** and name `main`.

> 🚨 Caution: for a repository with one release maintainer, that maintainer may be the reviewer, but **Prevent self-review** must remain disabled. Otherwise, the person who starts the workflow cannot approve it. If a second confirmation is unnecessary, Required reviewers can be left unconfigured.

> 💡 Tip: the workflow already verifies that the publishing branch is `main`. The environment branch rule is a second, server-side safeguard and is still recommended.

## ✅ Before publishing

1. Update the package version and release notes, then commit the changes to `main`.
2. Before consuming a permanent version number, open the project directory you intend to publish, then build and inspect the package locally:

   ```powershell
   dotnet pack -c Release -o ./artifacts
   ```

3. Confirm that the package ID, version, dependencies, README, license, and symbols are correct.
4. Push the release commit and wait for continuous integration to pass.

The selected Cake script builds its solution and publishes every applicable `.nupkg` produced under that project directory. Some selections may therefore publish multiple related packages.

> 💡 Tip: `Zongsoft.Data` is handled specially. Selecting `Zongsoft.Data` publishes only the core data package and never includes a driver; each data driver is a separate workflow option and must be selected and published independently.

### Local Cake commands for Zongsoft.Data

Run the equivalent local Cake commands from the `Zongsoft.Data` directory:

> 🚨 Caution: these commands preserve the existing local publishing method; they are not GitHub Trusted Publishing. A direct push from a developer computer to [nuget.org](https://nuget.org) still requires a user-created, long-lived API key on that computer.

```powershell
# Publish only Zongsoft.Data (the following commands are equivalent)
dotnet cake --edition Release --target pack
dotnet cake --edition Release --target pack --drivers none

# Publish all data drivers
dotnet cake --edition Release --target pack --drivers *

# Publish one specified data driver
dotnet cake --edition Release --target pack --drivers=mssql
dotnet cake --edition Release --target pack --drivers=mysql
dotnet cake --edition Release --target pack --drivers=sqlite
dotnet cake --edition Release --target pack --drivers=duckdb
dotnet cake --edition Release --target pack --drivers=influx
dotnet cake --edition Release --target pack --drivers=tdengine
dotnet cake --edition Release --target pack --drivers=postgres
dotnet cake --edition Release --target pack --drivers=clickhouse
```

> 💡 Tip: when `--drivers` is omitted, the Cake script treats it as an empty value. This has the same effect as passing `--drivers none`: only the `Zongsoft.Data` core project is processed. This rule applies to builds, tests, and publishing. Pass `--drivers *` explicitly to process every driver.

## 🚀 Publish from GitHub

1. Open the repository's **Actions** page.
2. Select **Publish NuGet Packages**.
3. Select **Run workflow** and keep the branch set to `main`.
4. Choose the project or data driver to publish.
5. Start the workflow and, when [Required reviewers](#required-reviewer) are configured, approve the `release` environment deployment.
6. Verify the new package version on [nuget.org](https://nuget.org) after the workflow completes.

The same workflow can be started from a terminal with GitHub CLI. Run only the command for the package you intend to publish.

```powershell
# Core projects
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

# Data drivers
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data.MsSql
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data.MySql
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data.SQLite
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data.DuckDB
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data.Influx
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data.TDengine
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data.PostgreSql
gh workflow run publish-nuget.yml --ref main -f project=Zongsoft.Data.ClickHouse

# Messaging packages
gh workflow run publish-nuget.yml --ref main -f project=messaging/zero
gh workflow run publish-nuget.yml --ref main -f project=messaging/mqtt
gh workflow run publish-nuget.yml --ref main -f project=messaging/kafka
gh workflow run publish-nuget.yml --ref main -f project=messaging/rabbit

# Upgrading packages
gh workflow run publish-nuget.yml --ref main -f project=upgrading/upgrader
gh workflow run publish-nuget.yml --ref main -f project=upgrading/tool
gh workflow run publish-nuget.yml --ref main -f project=upgrading/web

# External packages
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

## 🧩 Concepts

<a id="trusted-publishing"></a>
### Trusted Publishing

Trusted Publishing is a trust relationship between [nuget.org](https://nuget.org) and GitHub Actions. It identifies the publisher by repository owner, repository name, workflow file, and environment, avoiding a long-lived NuGet API key stored in GitHub.

> 💡 Tip: Trusted Publishing answers “who may publish,” not “when should we publish.” A maintainer still decides when to publish by clicking **Run workflow**.

<a id="github-environment"></a>
### GitHub Environment

A GitHub Environment is a set of GitHub Actions settings used to protect a deployment or publishing job. This repository names that environment `release`, and the workflow refers to it with `environment: release`.

> 💡 Tip: `release` is only an environment name. It is neither a Git branch nor a GitHub Release. It groups publishing secrets, human approvals, and allowed deployment branches.

<a id="environment-secret"></a>
### Environment Secret and NUGET_USER

An Environment Secret is a hidden value available only to jobs that use a particular GitHub Environment. `NUGET_USER` contains the [nuget.org](https://nuget.org) profile name so that `NuGet/login@v1` knows which NuGet account is requesting the temporary publishing credential.

> 💡 Tip: `NUGET_USER` is not a password or an API key. It is stored as a secret here so that all publishing identity settings remain under the `release` environment.

<a id="required-reviewer"></a>
### Required reviewer

A required reviewer is an optional human approval rule on the `release` environment. When configured, manually starting the workflow is not enough to publish immediately. The publishing job pauses until an allowed reviewer opens the Actions run and selects **Review deployments** → **Approve and deploy**.

Configure it under **Settings** → **Environments** → **release** → **Deployment protection rules** → **Required reviewers**.

> 🚨 Caution: with **Prevent self-review** enabled, the person who started the workflow cannot approve it. A repository with one maintainer should normally leave this option disabled. Enable it when releases require a second maintainer's approval.

<a id="deployment-branch-rule"></a>
### Deployment branch rule

A deployment branch rule limits which branches may use the `release` environment. This repository allows only `main`, so a workflow manually started from any other branch cannot acquire the environment's publishing access.

Configure it under **Settings** → **Environments** → **release** → **Deployment branches and tags**. Select **Selected branches and tags**, then add a rule with type **Branch** and name `main`.

> 💡 Tip: this is not ordinary Git branch protection. It does not control commits or merges; it only controls whether a workflow can use the `release` environment to publish.

<a id="oidc-temporary-api-key"></a>
### OIDC and the temporary API key

OIDC stands for OpenID Connect. Here, it lets GitHub present a short-lived, verifiable identity that tells [nuget.org](https://nuget.org), “this request really comes from the configured repository, workflow, and environment.” No long-lived NuGet API key needs to be stored in GitHub.

The workflow grants only `contents: read` and `id-token: write`. Authentication then follows this sequence:

> 💡 Authentication flow: GitHub issues an OIDC identity for this workflow run → [nuget.org](https://nuget.org) checks the Trusted Publishing policy → `NuGet/login@v1` receives a short-lived API key → Cake uses the temporary value to push packages.

The name `NUGET_API_KEY` still appears because `dotnet nuget push` accepts an API key as its publishing credential. This value is created only for the current workflow run and is not a long-lived key stored in GitHub Secrets. Only a direct local publish requires a maintainer-created, long-lived API key.

> 🚨 Caution: if `publish-nuget.yml` is renamed or its `release` Environment changes, update the Trusted Publishing policy on [nuget.org](https://nuget.org) as well. Otherwise, [nuget.org](https://nuget.org) cannot recognize the workflow.

## 🩺 Troubleshooting

- **The workflow remains in Waiting instead of publishing**: this normally means the `release` environment is waiting for a [required reviewer](#required-reviewer). Open the Actions run and select **Review deployments** → **Approve and deploy**.
- **You cannot approve a workflow you started**: check whether **Prevent self-review** is enabled on the `release` environment. Disable it for a single-maintainer repository.
- **NUGET_USER is unavailable**: verify that it is defined under the `release` environment's **Environment secrets** and contains the [nuget.org](https://nuget.org) profile name rather than an email address.
- **No matching trust policy**: verify the repository owner, repository, exact workflow filename, and [Environment](#github-environment) in the [nuget.org](https://nuget.org) policy.
- **NuGet login returns 403**: verify that the publishing job still has `id-token: write`.
- **Package is outside the allowed scope**: verify that its ID matches `Zongsoft.*` and that it belongs to the selected [nuget.org](https://nuget.org) owner.
- **A new package ID is rejected**: verify that the policy allows **Push new packages and package versions** and that the package ID matches its glob pattern.
- **A repeated version is skipped**: the Cake scripts use `SkipDuplicate`; increment the project version before publishing again.

## 📚 References

- [Trusted Publishing on nuget.org](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
- [Manage Trusted Publishing policies](https://www.nuget.org/account/trustedpublishing?fromApiKeys=true)
- [NuGet/login GitHub Action](https://github.com/NuGet/login)
- [Manually running a GitHub Actions workflow](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow)
- [Managing GitHub deployment environments](https://docs.github.com/en/actions/how-tos/deploy/configure-and-manage-deployments/manage-environments)
- [Reviewing GitHub deployments](https://docs.github.com/en/actions/how-tos/deploy/configure-and-manage-deployments/review-deployments)
