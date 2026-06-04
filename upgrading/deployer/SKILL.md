---
name: deployer
description: Work on the Zongsoft.Upgrading.Deployer component. Use when modifying or reviewing the Native AOT deployment executable, .deployment descriptor handling, full or delta release copy behavior, deployment executor events, service or terminal restart launchers, deployer publishing scripts, or deployer documentation in the upgrading/deployer project.
---

# Zongsoft Upgrading Deployer

## Start Here

Read these local files before changing behavior:

- `README.md` for public behavior and command-line contract.
- `Deployer.Deploy.cs` for deployment sequencing.
- `Deployer.Helper.cs` for cleanup and copy behavior.
- `Launcher.cs` plus `Launcher.*.cs` for restart behavior.
- `Executor.Initializer.cs` for built-in executor command registration.
- `Program.cs` for command-line parsing and process entry behavior.
- `publish.linux-x64.*` and the Windows `dotnet publish` command in `README.md` for publish instructions.

## Core Contract

Preserve this order unless the task explicitly changes deployment semantics:

1. Parse `key=value` command-line arguments into `Deployer.Argument`.
2. Wait for `app.id` to exit, unless it is missing or zero.
3. Open the `.deployment` descriptor with exclusive access.
4. Load the manifest referenced by the descriptor.
5. For `Fully` releases, clean the application root while preserving the deployer directory and deployment descriptor.
6. Run `Deploying` executors.
7. Copy extracted package files from `Packages` into the application root.
8. Run `Deployed` executors.
9. Release the descriptor lock and restart the host through the launcher selected by `app.type`.

Treat `.deployment` as the hand-off boundary from the upgrader. It contains `Manifest` and `Packages`, is opened exclusively during deployment, and is deleted by the deployment descriptor lifecycle.

## Implementation Rules

Keep the deployer suitable for out-of-process upgrade deployment:

- Avoid dependencies that are hostile to Native AOT or standalone publishing.
- Do not overwrite or clean the `.deployer` directory during full releases.
- Keep deployment file locking strict; it prevents the host and deployer from racing.
- Keep executor names aligned with shared command names: `Copy`, `Move`, `Link`, `Delete`.
- Keep launcher behavior platform-aware: IIS app pool recycle for Windows Web, `systemctl start` for Linux or FreeBSD services, `sc start` for Windows daemon services, and direct process start for terminal or universal hosts.
- Treat service restart commands as operationally sensitive; do not run them in tests unless the user explicitly asks.

## Common Tasks

When changing cleanup or copy behavior, verify both `Fully` and `Delta` modes and preserve package directory traversal semantics.

When changing restart behavior, update the matching launcher file and confirm `Launcher.Launch` still selects by `app.type` values `Web`, `Daemon`, `Terminal`, and fallback.

When changing command-line arguments, update `README.md` and keep compatibility with arguments produced by the upgrader:

```text
app.id=12345
app.name=Zongsoft.Hosting.Terminal
app.type=Terminal
app.path=/opt/zongsoft/terminal
host.path=/opt/zongsoft/terminal/Zongsoft.Hosting.Terminal
host.args#0=...
deployment=/opt/zongsoft/terminal/.deployment
```

## Validation

Prefer focused validation first:

```shell
dotnet build Zongsoft.Upgrading.Deployer.slnx
```

For publish-related changes, also inspect or run the relevant `publish.linux-x64.*` script in the intended environment. Avoid actually restarting real services or mutating a live application directory unless the user explicitly requests an integration test.

For Windows publish documentation or automation, keep the command-line path compatible with the Native AOT project settings:

```cmd
dotnet publish Zongsoft.Upgrading.Deployer.csproj ^
	--self-contained ^
	--runtime win-x64 ^
	--framework net10.0 ^
	--configuration Release ^
	-p:PublishAot=true
```
