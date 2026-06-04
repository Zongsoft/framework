---
name: upgrader
description: Work on the Zongsoft.Upgrading.Upgrader client component. Use when modifying or reviewing automatic upgrade discovery, File or Web fetchers, package downloading and checksum verification, release selection, extraction, .deployment hand-off generation, worker startup scheduling, deployer launch behavior, upgrader plugin or option files, or upgrader documentation in the upgrading/upgrader project.
---

# Zongsoft Upgrading Upgrader

## Start Here

Read these local files before changing behavior:

- `README.md` for the public upgrade workflow and configuration.
- `Upgrader.cs` for `UpgradeAsync`, `Deploy`, shutdown, and worker scheduling.
- `Fetcher.cs`, `Fetcher.File.cs`, and `Fetcher.Web.cs` for release discovery and selection.
- `Downloader.cs`, `Downloader.File.cs`, and `Downloader.Web.cs` for package retrieval and checksum behavior.
- `Extractor.cs` for package extraction into the temporary `.app` directory.
- `Upgrader.Launcher.cs` for deployer process launch details.
- `Zongsoft.Upgrading.Upgrader.option` and `.plugin` for default configuration and startup registration.

## Core Contract

Preserve this client-side flow:

1. If a deployment descriptor already exists, report upgrade preparation as successful.
2. Fetch candidate releases from the configured `File` or `Web` channel.
3. Filter releases by application name, edition, platform, architecture, current version, optional target version, and deprecation state.
4. Select the newest full release as trunk when present, then append newer deltas in ascending version order.
5. Download each selected package into the application-specific temp directory.
6. Verify package size and checksum when release metadata provides them.
7. Extract packages into the temp `.app` directory.
8. Save a `.deployment` descriptor in the application directory.
9. Launch `Zongsoft.Upgrading.Deployer` from `{ApplicationPath}/.deployer` and shut down the host.

## Channel Notes

For `File` channel changes, keep manifest discovery compatible with the documented scan order:

```text
{root}/{application-name}/*.manifest
{root}/{application-type}/{application-name}/*.manifest
{root}/{application-type}/{application-name}*.manifest
{root}/{application-name}*.manifest
```

For `Web` channel changes, keep requests compatible with the Web package manager endpoint:

```text
{base-url}/{application-name}/{edition}?Name=...&Edition=...&Platform=...&Architecture=...&CurrentlyVersion=...
```

Include hardware fingerprint parameters when preserving or extending request construction; server-side evaluators may depend on them.

## Implementation Rules

- Keep the `File` and `Web` channel names case-insensitive and compatible with connection setting names under `/Upgrading`.
- Keep release matching stricter than package availability: application identity and runtime must match before download.
- Keep checksum verification before extraction and deployment hand-off.
- Keep `.deployment` creation as the only signal for the deployer to proceed.
- Keep Linux deployer launch detached enough to survive the current host shutdown.
- Avoid blocking concurrent worker ticks by preserving the interlocked upgrade flag.

## Common Tasks

When changing release selection, test combinations with only deltas, with one full release, and with a target `UpgradingVersion`.

When changing download behavior, verify existing local files are reused only when size and checksum match.

When changing deployment hand-off, update both upgrader documentation and deployer argument expectations.

## Validation

Prefer focused validation first:

```shell
dotnet build Zongsoft.Upgrading.Upgrader.slnx
```

For behavior changes, add or run tests around release filtering, downloader checksum paths, and deployment descriptor generation if the surrounding solution has test coverage available.
