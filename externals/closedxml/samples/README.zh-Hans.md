# Zongsoft.Externals.ClosedXml 范例

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

本范例演示如何使用 `Zongsoft.Externals.ClosedXml` 导入和导出电子表格。它通过 Bogus 生成本地化的 `User` 数据，使用 `SpreadsheetGenerator` 导出、使用 `SpreadsheetExtractor` 导入，并显示工作簿元数据及导入结果。

## 运行

本范例面向 .NET 10，请在仓库根目录运行：

```shell
dotnet run --project externals/closedxml/samples/Zongsoft.Externals.ClosedXml.Samples.csproj
```

## 导出

`export` 生成本地化的虚拟用户并写入格式化工作簿，别名为 `out`。`--count` 或 `-c` 控制记录数，`--culture` 或 `-l` 选择数据及表格的区域文化：

```text
export --count:20 --culture:zh-CN users.xlsx
out -c:50 -l:en-US users.en-US.xlsx
```

未指定输出文件时，范例会在当前目录创建 `users.<区域文化>(<数量>).xlsx`。导出后会显示工作表、表格、区域、列数和声明的数据行数。

## 导入

`import` 通过 `SpreadsheetExtractor` 读取工作簿行，别名为 `in`：

```text
import users.xlsx
in users.en-US.xlsx
```

未指定文件时，命令读取当前目录中的 `users.xlsx`。程序会输出工作簿元数据和每条导入的 `User` 记录。

## 验证往返转换

`verify` 导出工作簿后立即导入同一文件，适合检查类型转换、可空值、本地化和长文本：

```text
verify --count:10 --culture:en-US
verify -c:25 -l:zh-CN roundtrip.xlsx
```

请确认报告的导入记录数与 `--count` 相同，并检查输出值是否符合所选区域文化。

## 源码、安全与清理

[Program.cs](Program.cs) 在这个独立诊断工具中自行持有具体的生成器、提取器实例。插件应用应参照[类库说明](../README.zh-Hans.md)，从服务容器定位公共归档接口。

🚨 导出以创建方式打开目标文件，可能覆盖已有文件。请在临时目录使用唯一文件名、小规模虚拟数据，不要使用生产工作簿。导入会打印每条记录，不要输入敏感数据。

完成往返检查后执行 `exit` 并确认退出提示。查看生成结果后，仅删除本次创建的 `.xlsx` 文件；`verify` 不会自动删除输出文件。不需要清理外部服务或数据库。
