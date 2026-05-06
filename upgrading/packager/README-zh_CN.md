# 自动升级打包器

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Upgrading.Packager)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Upgrading.Packager)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README-zh_CN.md)

-----

## 概述

[**Z**ongsoft.**U**pgrading.**P**ackager](https://github.com/Zongsoft/framework/tree/main/upgrading/packager/Zongsoft.Upgrading.Packager) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的自动升级插件库打包器，提供了打包、校验与发布三个子命令。

### 基本用法

```shell
# 打包（pack 为默认命令，可直接传入选项省略命令名）
dotnet-pack [pack] [选项...] [参数...]

# 校验
dotnet-pack checksum [选项] <包文件...>

# 发布
dotnet-pack publish [选项] <包文件...>
```

-----

## 打包

### 命令选项

选项 | 类型 | 必填 | 默认值 | 说明
----|:----:|:----:|:----:|------
`--name` | string | ✓ | - | 应用名称，必须与发布的应用名一致，升级器以此作为应用匹配依据
`--version` | string | ✓ | - | 版本号，由最多四段整数组成，如 `1.2.3` 或 `1.2.3.4`
`--platform` | string | ✓ | - | 操作系统平台，可选值：`windows`、`linux`
`--framework` | string | ✓ | - | .NET 框架标识，如 `net10.0`、`net9.0`、`net8.0`
`--source` | string | - | 当前目录 | 发布包源目录，支持变量替换
`--kind` | string | - | `Fully` | 发布类型：`Fully`（全量）、`Delta`（增量）
`--edition` | string | - | 空 | 版本分发名，常见取值：`stable`、`community`、`standard`、`professional`、`enterprise`
`--architecture` | string | - | `x64` | CPU 架构，可选值：`x64`、`x32`、`arm64`、`arm32`
`--checksum` | string | - | `sha1` | 校验码算法，可选值：`sha1`、`sha256`、`sha384`、`sha512`
`--output` | string | - | 自动生成 | 输出包文件路径，支持变量替换。默认根据 `name`、`edition`、`version`、`platform`、`architecture` 自动生成名称
`--overwrite` | bool | - | `false` | 是否覆盖已存在的目标文件
`--tags` | string | - | 空 | 标签集合，以逗号或分号分隔
`--title` | string | - | 空 | 发布标题
`--summary` | string | - | 空 | 发布概述，可为文本内容或文本文件路径
`--description` | string | - | 空 | 发布说明，可为文本内容或文本文件路径

#### 输出文件名规则

当未指定 `--output` 或仅指定了输出目录时，工具按照以下规则自动生成文件名：

- **未指定 `--edition`**：`{name}@{version}_{runtime}`，如 `Zongsoft.Daemon@1.1.0_win-x64.zip`
- **指定了 `--edition`**：`{name}({edition})@{version}_{runtime}`，如 `Zongsoft.Daemon(stable)@1.1.0_linux-x64.zip`

其中 `{runtime}` 由 `platform` 和 `architecture` 组合而成，如 `win-x64`、`linux-arm64`。

#### 自定义选项与变量

除了上述内建选项外，你还可以在命令行中传入**任意自定义选项**（如 `--compilation:Debug`），这些自定义选项的值同样会进入变量替换系统，可在 `--source`、`--output` 及命令参数中通过变量语法引用。

### 变量替换

工具支持在 `--source`、`--output` 以及命令参数中使用变量替换，变量会自动替换为对应的值。支持两种语法：

- `$(变量名)` —— Visual Studio 风格
- `%变量名%` —— Windows 风格

**可用变量来源（优先级从高到低）：**

1. 命令行选项（包括所有内建选项和自定义选项）—— 选项值会以选项名作为变量名注入
2. 系统环境变量 —— 工具启动时自动加载全部环境变量

**内建的便捷变量：**

变量名 | 说明 | 示例值
--------|------|--------
`Runtime` 或 `RuntimeIdentifier` | 由 `platform` 和 `architecture` 组合的运行时标识 | `win-x64`

> 使用示例：`--source:"D:\build\$(compilation)\$(framework)"`
> 其中 `$(compilation)` 和 `$(framework)` 将被替换为对应选项或环境变量的值。

### 发布清单

打包完成后，工具会在包文件同级目录自动生成一份 `.manifest` 发布清单文件，其中包含：

- 应用名称、版本、平台等基本信息
- 发布类型（全量/增量）
- 包文件大小与路径
- 包文件的校验码
- 标签、标题、概述、发布说明（如已指定）
- 执行器定义（如已通过 `--executor.*` 指定）

### 执行器

支持通过 `--executor.<名称>@<事件>` 格式的选项为发布定义执行器，在升级部署完成后由升级器调用。例如：

```shell
--executor.link@deployed:"$(name).service /path/to/systemd/$(name).service"
```

上述命令定义了一个名为 `link` 的执行器，在 `deployed`（部署完成）事件触发时执行 `$(name).service /path/to/systemd/$(name).service` 命令。

> 如果 `@` 后面未指定事件名，工具会输出警告并忽略该执行器定义。

### 命令参数

命令参数用于精确控制打包哪些文件和目录。

- **未指定参数**：将 `--source` 源目录中的所有文件和子目录全部打包。
- **指定了参数**：只打包参数所指定的目录或文件，支持以下特性：

特性 | 说明
-----|-----
**相对路径** | 默认为相对于 `--source` 源目录的路径
**绝对路径** | 支持绝对路径，可以引用源目录之外的文件
**通配符** | 路径的文件名部分支持 `*` 和 `?` 通配符
**重命名** | 使用 `:` 冒号分隔源路径与包内目标名称

#### 重命名规则

在参数路径后追加 `:` 可指定该条目在包内的目标路径或名称：

目标名称写法 | 效果
------------|-----
_空_ 或 `~` 或 `/` | 放入包根目录
`path/to/dir` | 放入指定的子目录
对于通配符路径 | 冒号后的部分表示文件在包内的目标目录

**示例解读：**

```shell
# 将 README.md 打包到包内的 docs/ 目录并重命名
"D:/Zongsoft/framework/README.md:docs/README.framework.md"

# 将编译输出目录的内容放入包根目录（~ 表示根）
bin/$(compilation)/$(framework):~

# 将以 web 打头的 .config 文件打包到包根目录
web*.config
```

### 打包范例

- 后台程序 _（全量发布）_

```shell
dotnet-pack
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

- 后台程序 _（增量发布）_

```shell
dotnet-pack
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

- _Web_ 程序 _（全量发布）_

```shell
dotnet-pack
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

- _Web_ 程序 _（增量发布）_

```shell
dotnet-pack
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

### 打包流程说明

1. 解析并规范化各选项值（应用变量替换）
2. 验证源目录存在性，自动补全输出文件名
3. 若启用 `--overwrite`，先删除已有的包文件和清单文件
4. 遍历命令参数生成 ZIP 包条目（冲突检测：同名条目会警告并跳过）
5. 计算包文件校验码，生成 `.manifest` 发布清单

-----

## 校验

当手动修改过打包文件（`.zip`）的内容后，需要重新计算校验码并更新到对应的 `.manifest` 清单文件中。

### 命令选项

选项 | 简写 | 类型 | 默认值 | 说明
-----|------|------|--------|-----
`--algorithm` | `-a` | string | `Sha1` | 校验算法，可选值：`Sha1`、`Sha256`、`Sha384`、`Sha512`

### 参数

支持传入一个或多个包文件路径。工具会自动定位对应的 `.manifest` 清单文件，重新计算校验码并写入。

可以传入以下任意形式：
- `.zip` 包文件路径
- `.manifest` 清单文件路径
- 不带扩展名的文件名（自动补全 `.zip`）

### 范例

```shell
# 指定算法和包文件
dotnet-pack checksum --algorithm:sha1 Zongsoft.Daemon(stable)@1.1.0_win-x64.zip

# 使用短选项形式，批量校验多个包
dotnet-pack checksum -a:sha256 package1.zip package2.zip
```

-----

## 发布

支持 `amazon.s3` 和 `web` 两种发布通道。

### 范例

- _Amazon.S3_ 文件系统

```shell
dotnet-pack publish
	--channel:amazon.s3
	--server:127.0.0.1:9000
	--access:rustfsadmin
	--secret:rustfsadmin
	--destination:/upgrading/releases/daemon
	Zongsoft.Daemon(stable)@1.1.0_win-x64
```

- _Web_ 发布站点

```shell
dotnet-pack publish
	--channel:web
	--url:localhost:8069/upgrading
	Zongsoft.Daemon(stable)@1.1.0_win-x64
```

-----

## 许可

本项目采用 [MIT](https://github.com/Zongsoft/framework/blob/main/LICENSE) 许可协议。
