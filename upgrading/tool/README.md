# Automatic Upgrade Packager

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Tools.Upgrader)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Tools.Upgrader)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README-zh_CN.md)

-----

## Overview

[**Z**ongsoft.**T**ools.**U**pgrader](https://github.com/Zongsoft/framework/tree/main/upgrading/tool/Zongsoft.Tools.Upgrader) is the automatic upgrade plugin library packager of the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework, providing three subcommands: **pack**, **checksum**, and **publish**.

### Basic Usage

```shell
# Pack
dotnet-upgrade pack [options...] [arguments...]

# Checksum
dotnet-upgrade checksum [options] <package-file...>

# Publish
dotnet-upgrade publish [options] <package-file...>
```

-----

## Pack

### Command Options

Option | Type | Required | Default | Description
----|:----:|:----:|:----:|------
`--name` | string | ✓ | - | Application name, must match the published application name; used by the upgrader for application matching
`--version` | string | ✓ | - | Version number, consisting of up to four integer segments, e.g. `1.2.3` or `1.2.3.4`
`--platform` | string | ✓ | - | Operating system platform, options: `windows`, `linux`
`--framework` | string | ✓ | - | .NET framework identifier, e.g. `net10.0`, `net9.0`, `net8.0`
`--source` | string | - | Current directory | Source directory of the publish output, supports variable substitution
`--kind` | string | - | `Fully` | Publish type: `Fully` (full), `Delta` (incremental)
`--edition` | string | - | Empty | Version distribution name, common values: `stable`, `community`, `standard`, `professional`, `enterprise`
`--architecture` | string | - | `x64` | CPU architecture, options: `x64`, `x32`, `arm64`, `arm32`
`--checksum` | string | - | `sha1` | Checksum algorithm, options: `sha1`, `sha256`, `sha384`, `sha512`
`--output` | string | - | Auto-generated | Output package file path, supports variable substitution. When not specified, the file name is auto-generated based on `name`, `edition`, `version`, `platform`, and `architecture`
`--exclude` | string | - | Empty | Files or directories to exclude, separated by semicolons; supports variable substitution and `*` / `?` wildcards
`--overwrite` | bool | - | `false` | Whether to overwrite an existing target file
`--tags` | string | - | Empty | Tag collection, separated by commas or semicolons
`--title` | string | - | Empty | Publish title
`--summary` | string | - | Empty | Publish summary, can be text content or a text file path
`--description` | string | - | Empty | Publish description, can be text content or a text file path

#### Output File Name Rules

When `--output` is not specified or only an output directory is specified, the tool auto-generates the file name according to the following rules:

- **Without `--edition`**: `{name}@{version}_{runtime}`, e.g. `Zongsoft.Daemon@1.1.0_win-x64.zip`
- **With `--edition`**: `{name}({edition})@{version}_{runtime}`, e.g. `Zongsoft.Daemon-stable@1.1.0_linux-x64.zip`

Where `{runtime}` is composed of `platform` and `architecture`, e.g. `win-x64`, `linux-arm64`.

#### Custom Options and Variables

In addition to the built-in options above, you can also pass **arbitrary custom options** on the command line (e.g. `--compilation:Debug`). The values of these custom options also enter the variable substitution system and can be referenced in `--source`, `--output`, and command arguments using variable syntax.

### Variable Substitution

The tool supports variable substitution in `--source`, `--output`, and command arguments. Variables are automatically replaced with their corresponding values. Two syntaxes are supported:

- `$(VariableName)` — Visual Studio style
- `%VariableName%` — Windows style

**Available variable sources (priority from high to low):**

1. Command-line options (including all built-in and custom options) — option values are injected with the option name as the variable name
2. System environment variables — all environment variables are automatically loaded at tool startup

**Built-in convenience variables:**

Variable | Description | Example Value
--------|------|--------
`Runtime` or `RuntimeIdentifier` | Runtime identifier composed of `platform` and `architecture` | `win-x64`

> Example: `--source:"D:\build\$(compilation)\$(framework)"`
> Where `$(compilation)` and `$(framework)` will be replaced with the values of the corresponding options or environment variables.

### Release Manifest

After packing completes, the tool automatically generates a `.manifest` release manifest file alongside the package file, which includes:

- Basic information such as application name, version, and platform
- Publish type (full/incremental)
- Package file size and path
- Package file checksum
- Tags, title, summary, and description (if specified)
- Executor definitions (if specified via `--executor.*`)

### Executor

You can define executors for a release using the `--executor.<name>@<event>` option format, which are called by the upgrader after the upgrade deployment completes. For example:

```shell
--executor.link@deployed:"$(name).service /path/to/systemd/$(name).service"
```

The above command defines an executor named `link` that runs the `$(name).service /path/to/systemd/$(name).service` command when the `deployed` event is triggered.

> If no event name is specified after `@`, the tool will output a warning and ignore the executor definition.

### Command Arguments

Command arguments are used to precisely control which files and directories are included in the package.

- **No arguments specified**: all files and subdirectories in the `--source` directory are packed.
- **Arguments specified**: only the directories or files specified by the arguments are packed, supporting the following features:
- **`--exclude` specified**: matching files and directories are skipped in both full-directory and argument-based packing.

Feature | Description
-----|-----
**Relative path** | Defaults to a path relative to the `--source` directory
**Absolute path** | Absolute paths are supported, allowing references to files outside the source directory
**Wildcards** | The file name part of the path supports `*` and `?` wildcards
**Rename** | Use `:` colon to separate the source path from the target name inside the package

#### Exclude Rules

Use `--exclude` to specify one or more files or directories to skip. Multiple rules are separated by semicolons. Relative rules are resolved from `--source`; absolute rules are matched by absolute path. The `*` and `?` wildcards are supported and do not cross directory separators.

```shell
# Exclude one directory and one file
--exclude:"logs;appsettings.Development.json"

# Exclude all .pdb files at any depth, plus temp directories under wwwroot
--exclude:"*.pdb;wwwroot/temp*"

# Exclude one-level debug DLLs under plugins
--exclude:"plugins/*/debug?.dll"
```

#### Rename Rules

Append `:` after the argument path to specify the target path or name of the entry inside the package:

Target Name | Effect
------------|-----
_Empty_ or `~` or `/` | Place in the package root directory
`path/to/dir` | Place in the specified subdirectory
For wildcard paths | The part after the colon indicates the target directory for files inside the package

**Example breakdown:**

```shell
# Pack README.md into the docs/ directory within the package and rename it
"D:/Zongsoft/framework/README.md:docs/README.framework.md"

# Place the build output directory contents into the package root (~ means root)
bin/$(compilation)/$(framework):~

# Pack .config files starting with "web" into the package root
web*.config
```

### Packing Examples

- Daemon program _(full publish)_

```shell
dotnet-upgrade pack
	--name:Zongsoft.Daemon
	--version:1.1.0
	--edition:stable
	--checksum:sha1
	--compilation:Debug
	--framework:net10.0
	--platform:windows
	--architecture:x64
	--kind:Fully
	--source:"D:\\Zongsoft\\hosting\\daemon\\bin\\$(compilation)\\$(framework)"
	--output:../../../
	--tags:tag1,tag2,tagX
	--executor.link@deployed:"$(name).service /Zongsoft/hosting/.deploy/$(scheme)/systemd/$(name).service"
```

- Daemon program _(incremental publish)_

```shell
dotnet-upgrade pack
	--name:Zongsoft.Daemon
	--version:1.1.0
	--edition:stable
	--checksum:sha1
	--compilation:Debug
	--framework:net10.0
	--platform:windows
	--architecture:x64
	--kind:Delta
	--source:"D:/Zongsoft/hosting/daemon/bin/$(compilation)/$(framework)"
	--output:../../../
	plugins/zongsoft/upgrader
	plugins/zongsoft/externals/redis
	plugins/zongsoft/externals/hangfire
```

- Web program _(full publish)_

```shell
dotnet-upgrade pack
	--name:Zongsoft.Hosting.Web
	--version:1.1.0
	--edition:stable
	--checksum:sha1
	--compilation:Debug
	--framework:net10.0
	--platform:windows
	--architecture:x64
	--kind:Fully
	--source:./
	--output:./$(name)($(edition))@$(version)_$(runtime).zip
	--tags:tag1,tag2,tagX
	--executor.link@deployed:"zongsoft.web.service /Zongsoft/hosting/.deploy/default/systemd/zongsoft.web.service"
	mime
	appsettings.json
	web*.config
	web*.option
	"D:/Zongsoft/framework/README.md:docs/README.framework.md"
	"../README.md"
	wwwroot
	plugins
	bin/$(compilation)/$(framework):~
```

- Web program _(incremental publish)_

```shell
dotnet-upgrade pack
	--name:Zongsoft.Hosting.Web
	--version:1.1.0
	--edition:stable
	--checksum:sha1
	--compilation:Debug
	--framework:net10.0
	--platform:windows
	--architecture:x64
	--kind:Delta
	--source:.
	--output:.
	web.option
	bin/$(compilation)/$(framework)/*:~
	plugins/zongsoft/upgrader
	plugins/zongsoft/externals/redis
	plugins/zongsoft/externals/hangfire
```

### Packing Process

1. Parse and normalize all option values (apply variable substitution)
2. Verify the existence of the source directory and auto-complete the output file name
3. If `--overwrite` is enabled, delete existing package and manifest files first
4. Iterate over command arguments to generate ZIP package entries (conflict detection: duplicate entries trigger a warning and are skipped)
5. Calculate the package file checksum and generate the `.manifest` release manifest

-----

## Checksum

When you have manually modified the contents of a package file (`.zip`), you need to recalculate the checksum and update the corresponding `.manifest` manifest file.

### Command Options

Option | Short | Type | Default | Description
-----|------|------|--------|-----
`--algorithm` | `-a` | string | `Sha1` | Checksum algorithm, options: `Sha1`, `Sha256`, `Sha384`, `Sha512`

### Arguments

Accepts one or more package file paths. The tool automatically locates the corresponding `.manifest` manifest file, recalculates the checksum, and writes it back.

You can pass any of the following forms:
- `.zip` package file path
- `.manifest` manifest file path
- A file name without an extension (auto-completes `.zip`)

### Examples

```shell
# Specify algorithm and package file
dotnet-upgrade checksum --algorithm:sha1 Zongsoft.Daemon-stable@1.1.0_win-x64.zip

# Use short option form to batch-checksum multiple packages
dotnet-upgrade checksum -a:sha256 package1.zip package2.zip
```

-----

## Publish

Supports two publish channels: `amazon.s3` and `web`.

### Examples

- _**A**mazon.**S3**_ File System

```shell
dotnet-upgrade publish
	--channel:amazon.s3
	--server:127.0.0.1:9000
	--access:rustfsadmin
	--secret:rustfsadmin
	--destination:/upgrading/releases/daemon
	Zongsoft.Daemon-stable@1.1.0_win-x64
```

- _**W**eb_ publish site

```shell
dotnet-upgrade publish
	--channel:web
	--url:localhost:8069/upgrading
	Zongsoft.Daemon-stable@1.1.0_win-x64
```

-----

## License

This project is licensed under the [MIT](https://github.com/Zongsoft/framework/blob/main/LICENSE) license.
