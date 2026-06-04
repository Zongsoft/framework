---
name: tool
description: Work on the Zongsoft.Tools.Upgrader command-line package tool. Use when modifying or reviewing dotnet-upgrade pack, checksum, or publish commands, release manifest generation, package ZIP entry rules, variable substitution, executor options, Amazon S3 or Web publishing, localized command resources, or tool documentation in the upgrading/tool project.
---

# Zongsoft Tools Upgrader

## Start Here

Read these local files before changing behavior:

- `README.md` for public command syntax and examples.
- `Program.cs` for command registration and command-line expression handling.
- `Packager.Pack.cs` for `pack` options, variable substitution, entry selection, exclusions, and manifest generation.
- `Packager.Checksum.cs` for checksum recalculation rules.
- `Packager.Publish.cs` for `amazon.s3` and `web` publishing flows.
- `Packager.cs` for ZIP writing and duplicate entry handling.
- `Normalizer.cs` for `$(Variable)` and `%Variable%` replacement.
- `AmazonS3.cs` for S3-compatible upload behavior.
- `Properties/Resources*.resx` when changing user-visible messages.

## Core Contract

Keep the tool centered on three subcommands:

```shell
dotnet-upgrade pack [options...] [arguments...]
dotnet-upgrade checksum [options] <package-file...>
dotnet-upgrade publish [options] <package-file...>
```

For `pack`, preserve this sequence:

1. Parse required identity options: `name`, `version`, `platform`, and `framework`.
2. Normalize variables from command-line options first, then environment variables.
3. Resolve `Runtime` and `RuntimeIdentifier` from platform plus architecture.
4. Validate source and output paths.
5. Apply `--exclude` rules to full-directory and argument-based packing.
6. Write package ZIP entries, warning and skipping duplicates.
7. Compute checksum and write the `.manifest` beside the package.

For `checksum`, accept `.zip`, `.manifest`, or extension-less package names, locate the paired manifest, recompute checksum, and update the manifest.

For `publish`, keep `amazon.s3`, `s3`, and `web` channel aliases compatible with existing command lines.

## Option And Entry Rules

Preserve command option parsing style used by Zongsoft command-line tooling, including colon forms such as:

```shell
--name:Zongsoft.Daemon
--version:1.1.0
--source:"D:\build\$(compilation)\$(framework)"
```

For package arguments:

- Treat relative paths as relative to `--source`.
- Allow absolute paths.
- Allow `*` and `?` wildcards in the file-name portion.
- Use `source:target` to rename or relocate entries inside the package.
- Treat empty target, `~`, and `/` as the package root.

For executor options, preserve the `--executor.<name>@<event>:"command"` pattern. Warn and ignore executor definitions that omit the event name.

## Publish Notes

For Web publishing, keep URL normalization compatible with inputs pointing to the site root, `/Upgrading`, `/Upgrading/Upgrader`, or `/Upgrading/Releases`. The flow imports the manifest, uploads the package for each imported release, then marks releases as published.

For Amazon S3 publishing, keep package and manifest upload paths compatible with the configured `--destination` and release file names.

Never log secret values from `--secret`, `--access`, `--authorization`, or `--credential`.

## Validation

Prefer focused validation first:

```shell
dotnet build Zongsoft.Tools.Upgrader.slnx
```

For command changes, run representative CLI commands against a temporary output directory, including one `pack`, one `checksum`, and the affected publish path when credentials or a local test endpoint are available.
