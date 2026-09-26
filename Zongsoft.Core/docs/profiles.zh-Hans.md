# Profile 配置：读取、声明与保存

[English](profiles.md) | [简体中文](profiles.zh-Hans.md)

本文介绍 Profile 的读取与导入、声明及来源模型，以及按来源保存和文件提交的规则。

- [读取与导入](#读取与导入)
- [声明与保存](#声明与保存)

## 读取与导入

`Profile.Load` 读取 INI，`#@import` 是读取器内置语法。Core 不提供通用指令接口、注册集合、执行处理器或公开读写操作上下文，不依赖部署、NuGet、哈希或锁文件类型。

### 职责与生命周期

- `Profile` 是声明与有效配置模型，提供公开 Load/Save 入口；内部 Import 方法合并有效条目并登记直接导入关系。
- 每次根加载创建一个内部 sealed `ProfileReader`，整个导入链共享该 Reader。它负责文件打开、解析、导入识别、回调及循环/深度保护；私有嵌套 Context 仅保存当前 Profile、行号和章节。
- `ReadCore` 管理流和活动路径，`Parse` 通过 ProfileUtility 识别行及导入语法。Reader 不使用静态状态或加载缓存，所有退出路径清理状态，失败后内部同实例可以重试。
- `ProfileWriter` 只输出声明并协调来源文件提交，不执行导入文本，也不提供写入回调。详见 [声明与保存](#声明与保存)。

```text
Profile.Load -> ProfileReader.Read -> ReadFile/ReadCore -> Parse
                                    ^                      |
                                    +--- 内置导入 ----------+
子文件解析完成 -> 父 Profile.Import(子文件) -> Imported 回调 -> 清理活动状态
```

公开 Load/Save 签名不变；内部读取入口不提供通用回调或外部继承点。导入完成后，模型保留来源关系，不保留 Reader。

### 导入通知配置

```csharp
using Zongsoft.Configuration.Profiles;

var options = new ProfileOptions
{
	Importing = context => Console.WriteLine($"正在读取 {context.FilePath}，层数 {context.Depth}"),
	Imported = context => Console.WriteLine($"已将 {context.Profile.FilePath} 合并到 {context.Referer.FilePath}"),
};

var profile = Profile.Load("settings.ini", options);
```

`ProfileOptions(bool preserveBlanks = true)` 保留空行构造参数，公开可写属性为 PreserveBlanks、RequireImports、MaximumDepth、Importing、Imported。两个回调均为 `Action<ProfileContext>`，默认 null。不传加载选项时不记录空行；显式 new ProfileOptions() 记录空行。

导入由 Reader 自动执行，没有启用开关。MaximumDepth 默认 64，仅接受正整数；非法赋值抛出 ArgumentOutOfRangeException 并保留原值。根文件计为第一层，设为 1 时只允许根文件，设为 128 等更高值可以读取更深的导入链。例如 `new ProfileOptions { MaximumDepth = 128 }`。需要中止时从任一回调抛出异常，整个加载失败，不提供静默跳过单个文件的返回值。

Reader 在根加载开始时浅复制 ProfileOptions，固定空行选项、RequireImports、MaximumDepth 及两个委托引用，全部子读取共享该快照。外部随后替换选项属性不会影响本次加载；回调捕获的可变状态仍由调用方保证并发安全。Writer 不执行导入回调，保存范围只取决于已加载的来源关系。

`ProfileContext` 是 public sealed 类型，由 Reader 内部构造，属性全部只读：

| 属性 | 含义 |
| --- | --- |
| FilePath | 本次导入文件规范化后的绝对加载路径。 |
| Depth | 当前文件的活动加载层数；根文件为 1，直接导入为 2。 |
| Referer | 包含本次导入声明的直接引用者 Profile。 |
| Profile | Importing 时为 null；Imported 时为已完成解析、递归导入和合并的子 Profile。 |

前后通知分别使用不同的上下文实例，保留的前置上下文不会在导入后被填入 Profile。只读属性固定的是引用，引用的 Profile 模型仍可编辑。上下文没有 Reader、输入流或递归入口，也不用于保存。

### 语法、路径与递归

`#@import defaults.ini` 或 `;@import defaults.ini` 执行导入，名称忽略大小写，@import 后必须是空格、Tab 或行尾。空参数不做任何读取；@imported 等其他名称都是普通注释。导入文本按普通 ProfileComment 声明记录，没有公开的指令模型。

多个路径以空格、Tab 或 `|` 分隔，不增加引号转义、变量展开或通配符。相对路径以包含导入语句的加载文件目录为基准，也允许绝对路径。循环检测单独解析文件及祖先目录链接，不改变相对路径基准。Windows 使用不区分大小写比较，其他平台使用 Ordinal；硬链接等未识别别名由深度限制兜底。

仅活动链重复构成循环，菱形和顺序重复导入允许且每次重新读取。循环与深度异常包含原因、导入链、来源文件及从 1 开始的行号。默认 MaximumDepth 允许同时活动 64 个文件，第 65 个在通知前拒绝。自定义限制改变此边界，不关闭循环检测。可选导入只忽略打开阶段的文件或目录不存在，权限和其他异常正常传播。

FileStream 根文件有路径并参与身份检查；匿名流允许绝对路径导入，相对导入明确报错。Profile.Load 关闭传入的流。显式根编码仅用于根文件，导入采用默认 UTF-8 并识别 BOM。

RequireImports 默认为 false，打开导入时仅忽略文件或目录不存在；设置 RequireImports = true 后，所有直接和递归导入都必须存在。缺失时抛出 ProfileException，消息包含目标路径、声明文件及从 1 开始的行号，并保留原始 IO 异常。根文件始终必须存在；打开失败不触发导入回调。

### 导入通知

| 回调 | 时机 |
| --- | --- |
| Importing | 文件已打开且循环、深度检查通过，解析内容之前；上下文的 Profile 为 null。 |
| Imported | 子文件及递归导入完成，且已经合并到父 Profile；上下文的 Profile 为该子文件，Referer 为直接引用者；通知时文件仍在活动栈中。 |

根文件不通知。A 导入 B、B 导入 C 的顺序为 Importing(B)、Importing(C)、Imported(C)、Imported(B)。每次实际重复导入分别通知；缺失文件或被拒绝文件不产生对应通知。

Importing、解析或合并失败时不触发该文件的 Imported。任一回调抛异常即终止加载，释放流并清理活动状态，不回滚已有通知和合并；调用方须丢弃失败加载收集的结果。

### 合并、保存与下游

导入合并到当前 Profile 根节点，包括章节中的导入。章节递归合并，后来的声明替换有效引用，保留各声明原值与 Profile 来源。同文件重复键仍报错，本地/导入之间按读取顺序覆盖。成功合并登记直接导入关系，被覆盖或无有效条目的子文件也能参与保存。

Reader 按读取内容登记声明基线，Imported 中的修改保持待保存状态。Save() 仅写回接收者及其导入子树的修改来源；显式路径/流/文本输出只处理本地声明。普通注释可编辑为导入文本，但编辑文本不自动加载或更新来源关系；需要重新加载以建立新导入图。

deployer 使用 Importing 记录导入文件哈希，根文件单独登记；packager 使用前后通知验证各来源。按路径另行计算哈希仍不保证摘要严格对应解析字节，此输入快照问题属于下游。

实施与平台验证见 [任务清单](../PROFILE-BUILTIN-IMPORT-TASKS.md)。

## 声明与保存

### 保存入口

| 入口 | 输出范围 |
| --- | --- |
| `Save()` / `Save(options)` | 自身及递归导入的文件，仅将修改写回各自来源。 |
| 子 Profile 的 `Save()` | 自身和导入子树，不包括父文件或兄弟文件。 |
| `Save(path, ...)` | 仅输出当前 Profile 的本地声明；null 路径回退到 FilePath。 |
| `Save(Stream, ...)` / `Save(TextWriter, ...)` | 仅输出当前 Profile 的本地声明，不写入导入文件。 |

未修改文件保持原始字节和最后写入时间。显式输出始终生成内容，因此可用于主动规范化格式。另存为不改变 FilePath，不改写相对导入参数；将文件另存到其它目录可能改变下次加载时的导入基准目录。

例如 app.ini 只有 `#@import defaults.ini`，defaults.ini 声明 `[network]` 下的 `timeout=30`：

```csharp
var profile = Profile.Load("app.ini");
var entry = profile.Sections["network"].Entries["timeout"];
entry.Value = "60";
profile.Save();
```

此操作仅修改 defaults.ini，app.ini 不重写。此例调用 `entry.Profile.Save()` 结果相同。`ProfileSection.SetEntryValue` 和 `Profile.SetOptionValue` 也修改当前有效条目所属的声明，不自动生成父文件覆盖。

需要本地覆盖时，显式调用 `profile.Sections["network"].Entries.Add("timeout", "60")`。已有导入项允许添加本地声明，已有本地同名声明仍报错。保存后 app.ini 保留导入指令，并在后面添加 `[network]` 和 `timeout=60`。

### 声明与有效视图

声明使用 Profile 内部的嵌套 Statement 类，定义在 Profile.Declarations.cs 中。Profile 的内部声明列表按输入顺序保留本文件的条目、注释（包括导入文本）、章节声明和 `[]` 根作用域切换。重复进入同一章节是不同声明；显式空章节不会丢失。空行沿用 Profile.Blanks 的位置数组，参与快照比较和输出，不复制另一套可修改的空行数据。

现有集合是合并后的有效查询视图，Writer 不使用其分组枚举顺序输出。声明列表和有效索引引用条目对象，不维护两份可修改的值。ProfileItem.Profile 指向声明来源；后来的本地声明或导入通过替换有效引用覆盖，不能改写被覆盖声明的值。

同一文件内相同完整键重复声明仍报错；本地与导入之间、多个导入之间按实际读取顺序决定有效值。章节内的导入仍合并到当前 Profile 根节点。成功合并后登记直接导入关系，因此即使子文件没有有效条目，或所有条目都被覆盖，也能参与关联保存。读取不会缓存重复导入。

集合编辑同步声明和名称索引。替换条目或章节移除旧键，重复检查失败不会部分更新。父集合不能删除仅属于来源文件的导入声明；混合集合的 Clear 在修改前拒绝这种操作。子文件结构、删除或覆盖关系变化后，需要重新加载父配置；没有自动增量更新的依赖图。

### Writer 生命周期与输出

每次保存创建一个 internal sealed ProfileWriter。Profile 保留公开便捷入口；Reader 负责解析，Writer 负责输出与提交。没有公共 Writer、单例、上下文工厂或读写公共基类。Writer 通过 ProfileOptions.Clone 固定空行选项，不执行或读取导入回调；ProfileContext 仅描述导入通知，回调配置和 MaximumDepth 不改变已加载模型的保存范围。

输出规则：null 值写作 `name`，空字符串写作 `name=`；注释规范化为 `#`，空注释保留为 `#`；保留声明顺序、空章节及必要的作用域切换。PreserveBlanks 为 true 时输出已记录空行，为 false 时不恢复原空行。加载时未保留的空行无法在保存时恢复。

名称和内容必须能由现有语法表示。不增加引号、转义和多行值语法；非法换行、章节名内的层级分隔空白、无法往返的条目值等在输出前拒绝。注释统一表示为 ProfileComment，包括导入文本；编辑注释可改变导入文本，但不会重新解析或更新已经登记的导入关系，需要重新加载。Comments.Add 接受 CRLF 或 LF 分隔的多行注释。

Writer 使用局部章节状态与传入的 TextWriter 输出声明，没有公开写入上下文或 OnWrite 通知。导入文本与其他注释同样输出；关联文件遍历只依据加载时登记的导入关系。

保存期间不得修改相关配置；检测到声明变化即失败，重复进入同一 Profile 的保存也被拒绝。调用方须避免并发修改，传入的 TextWriter 也须遵守该约束。

### 基线、重复来源与提交

Reader 在读取声明时登记原始基线，导入完成回调中的修改保持待保存状态。保存比较当前声明内容、顺序及可变数组，不只依赖 setter 标记。成功写回来源后更新基线，恢复到原值会重新成为未修改；另存为其它路径或写入流不清除来源待保存状态。

保存按规范化文件身份去重，使用与 Reader 相同的文件及祖先目录链接解析。Windows 不区分路径大小写，其它平台区分。同一路径只有一个修改实例时保存该实例；多个修改实例快照相同只输出一次，冲突则在任何输出前失败。未修改的旧实例不会覆盖修改实例，保存不刷新其它重复读取实例。

关联保存先生成全部同目录临时文件，完成校验、序列化及关闭后，再按子文件先于父文件的顺序替换目标。准备阶段失败不替换任何原文件。未修改的只读文件无需写入；修改后的只读目标明确拒绝。不会自动创建目标目录，也不会以删除目标作为替换回退。

多个文件的替换不是事务。提交失败会停止后续提交，抛出 ProfileException，InnerException 保留 IO 原因，Data["FailedPath"] 为失败目标，Data["CompletedPaths"] 为已完成路径数组。成功文件更新基线，未成功文件保持待保存状态，可以重试。已完成的提交不回滚。

临时文件在退出时清理。若主操作已失败且清理也失败，保留主异常，并在其 Data 中以临时路径记录清理异常；调用方可据此清理残留文件。

文件写入跟随符号链接目标，不替换链接本身。不承诺硬链接联动、并发写入冲突检测、跨文件原子性或断电持久性。文件权限和替换行为仍受操作系统与文件系统约束。

### 编码、资源与验证

文件与 Stream 默认使用 Encoding.UTF8；显式编码仅用于当前显式输出，不记录并恢复原编码或 BOM。TextWriter 的编码和换行由调用方决定。已修改文件按所选格式重新输出，不承诺逐字节还原。

路径资源由 Writer 管理。Stream 保持既有关闭行为；TextWriter 不关闭。流与文本写入器可能在异常前收到部分输出，不能回滚。

任务清单见 [PROFILE-WRITER-TASKS.md](../PROFILE-WRITER-TASKS.md)。测试覆盖来源写回、范围隔离、声明往返、重复实例、准备/提交失败与重试。Windows 多框架结果记录在任务清单；Linux/macOS 的权限和链接验证必须原生执行，不能用 Windows 结果代替。哈希严格对应解析字节仍由下游输入快照方案处理，Core 不依赖部署、NuGet 或哈希类型。
