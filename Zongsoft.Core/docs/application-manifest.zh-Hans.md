# 应用清单

[English](application-manifest.md) | [简体中文](application-manifest.zh-Hans.md)

[`Zongsoft.Services.ApplicationManifest`](../src/Services/ApplicationManifest.cs) 读写应用目录下的 `.edition` 文件，包含应用名称、版本号、具名发行版及当前选择。

## 模型与文件格式

`Name` 表示应用名称。清单包含顶层 `Version` 或非空且有序的 `Editions` 集合，两种表示互斥；包含发行版时，`Version` 为 `null`。编辑过程中允许暂不设置版本信息，但保存前必须具有版本号或至少一个发行版。

根条目决定版本表示方式，并可指定当前发行版：

| 根条目 | 含义 |
| --- | --- |
| `MyApplicationName@1.0.1` | 单版本模式，`Editions` 为空。 |
| `MyApplicationName` | 多发行版，未选择当前项。 |
| `MyApplicationName=` | 同样表示未选择，保存时省略等号。 |
| `MyApplicationName=Professional` | 多发行版，选择 `Professional`。 |

多发行版清单示例：

```ini
MyApplicationName=Professional

[Community]
1.0.1

[Professional]
1.1.0

[Enterprise]
1.1.2
```

版本号采用 `System.Version` 支持的两段、三段或四段数字格式。每个发行版段落只包含一个裸版本号，不支持 `Version=1.0.1` 键值项、嵌套段落或行尾注释。发行版名称忽略大小写且必须唯一；指定的当前项必须存在，无效引用会抛出 `FormatException`，错误位置指向根条目。

加载复用 [Profile 解析](profiles.zh-Hans.md#读取与导入)，包括空白、空行、整行注释及 BOM 处理。发行版名称遵循 [Profile 章节名称规则](profiles.zh-Hans.md#章节名称)，空格和 Tab 表示层级，清单拒绝嵌套段落。应用和发行版构造函数仅检查名称非空并移除两端空白，不额外验证保留字符；调用方选择可表示为 Profile 格式的名称，首行中的 `@` 和 `=` 是格式分隔符。

清单设置 `ImportBehavior = ProfileDirectiveBehavior.Suppress`，遇到 `#@import` 或 `;@import` 指令，包括空参数指令，均在打开导入文件前抛出 `FormatException`。

## 发行版集合与当前选择

`ApplicationManifest.EditionCollection` 继承 `KeyedCollection<string, ApplicationManifest.Edition>`，名称键采用 `OrdinalIgnoreCase` 比较。集合保持条目顺序，通过基类 API 支持插入、替换、查找和删除。`IsEmpty` 表示集合是否为空，`Add(string name, Version version)` 返回已添加的发行版。

`Editions.Current` 的类型为 `ApplicationManifest.Edition`：

- `default(ApplicationManifest.Edition)` 表示未选择，不会自动选择首项。
- 可直接赋字符串，按名称选择已有发行版；名称移除两端空白并忽略大小写，读取时返回集合中的完整条目及版本号。
- 赋值完整 `Edition` 时，名称和版本号均须匹配。无效选择抛出参数名为 `value` 的 `ArgumentException`，原选择保持不变。
- `Current = default`、`Current = null` 和 `Current = ""` 均取消选择。纯空白名称无效；字符串转换产生仅含名称的引用，不能作为条目加入集合。
- 删除当前项或清空集合会取消选择；同名替换跟随新版本号，改名替换则取消选择。失败的修改不改变集合及当前项。

`Edition` 的相等性、哈希码和相等运算符统一按忽略大小写的名称及版本号值比较。清单及其集合不保证线程安全。

```csharp
using System;
using Zongsoft.Services;

var manifest = new ApplicationManifest("MyApplicationName");
manifest.Editions.Add("Community", new Version(1, 0, 1));
manifest.Editions.Add("Professional", new Version(1, 1, 0));
manifest.Editions.Current = "Professional";
manifest.Save(AppContext.BaseDirectory);
```

## 加载与保存

`Load`、`Save` 支持目录、显式文件路径、流及文本读写器。路径为 null 或空串时使用 `AppContext.BaseDirectory`，现有目录读写其中的 `.edition` 文件。

`Load(string path)` 在文件不存在时返回 `null`。`Save(string path)` 可在现有目录中创建 `.edition`；显式路径既不是现有目录也不是现有文件时，不执行写入，不自动创建父目录。

保存到已有文件时，先读取该目标文件的空行和注释；目标为空或新建，以及流、文本输出时，采用已加载的布局。保存默认保留空行和注释内容，按集合顺序输出发行版，未选择时省略根条目的等号，选中时使用段落的实际名称。注释标记按 Profile 规则统一为 `#`。增删段落或调整顺序后，原有备注及空行仍保留，位置可能随段落调整；新增段落之间保留一个空行。

文件和流输出 UTF-8 无 BOM 文本及平台默认换行；文本写入器采用其 `NewLine` 和编码设置。写入前检查版本信息完整性及 Profile 输出语法，文件末尾保留换行。

无论成功或失败，传入的流及文本读写器均保持打开，由调用方负责释放。加载从当前位置读取至结尾；保存从当前位置写入，不重置位置或截断流，也不要求流支持定位。格式、集合及布局保留行为见 [回归测试](../test/Services/ApplicationManifestTest.cs)。

此 API 提供清单模型及文件读写。宿主应用上下文和升级组件仍使用 `ApplicationIdentifier`，尚未自动读取 `.edition`。
