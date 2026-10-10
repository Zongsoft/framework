# Profile 配置：读取、指令与保存

[English](profiles.md) | [简体中文](profiles.zh-Hans.md)

Profile 读取 INI 声明，处理指令并合并导入来源，将修改保存回各自的声明文件。

- [读取与导入](#读取与导入)
- [声明与保存](#声明与保存)
- [变量视图](#变量视图)

## 读取与导入

`Profile.Load` 支持文件路径、Stream 和 TextReader。每次根加载创建 ProfileReader 的内部嵌套会话 ProfileReader.Session，固定选项及全局指令注册表快照，负责指令调度、文件通知及活动加载链检查。ProfileReader 解析单个来源；ImportDirective 解释导入路径，通过同一会话完成递归读取。Profile 保留声明、有效引用和来源关系。ProfileWriter 输出声明并协调来源提交，不执行指令或回调。

### 选项与指令设置

```csharp
using Zongsoft.Configuration.Profiles;

var options = new ProfileOptions
{
	Directives =
	{
		ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Strict, maximumDepth: 16),
		new ProfileDirectiveOptions("custom", ProfileDirectiveBehavior.Suppress),
	},
	Loading = context => Console.WriteLine($"正在读取 {context.FilePath}，层数 {context.Depth}"),
	Loaded = context => Console.WriteLine($"已读取 {context.Profile.FilePath}"),
};

var profile = Profile.Load("settings.ini", options);
```

`ProfileOptions(bool preserveBlanks = true)` 提供 PreserveBlanks、只读 Directives 集合、Loading/Loaded（`Action<ProfileContext>`）。集合提供 Processing/Processed（`Action<ProfileDirectiveContext>`）和指回所属 ProfileOptions 的只读 Options 属性。回调默认为 null。不传加载选项时不记录空行；显式 new ProfileOptions() 记录空行。Profile 不执行变量展开。

`ProfileDirectiveOptions` 提供只读 Name 和可写 Behavior。名称以字母或下划线开头，只能包含字母、数字、下划线、连字符或点号，不含注释标记和 `@` 前缀。`ProfileDirectiveOptionsCollection` 按名称忽略大小写索引，拒绝 null 和重名项，支持按名称查找。集合枚举顺序不控制执行顺序。未配置的指令采用内置默认设置，删除选项恢复默认设置；添加选项不注册执行程序。

`ProfileDirectiveOptions.Import(behavior = None, maximumDepth = 0)` 创建公开嵌套类型 ImportOptions，名称固定为 `import`。MaximumDepth 非负：零采用内置上限 64，正数指定上限，负数赋值抛出 ArgumentOutOfRangeException 并保留原值。根文件计一层，设为 1 时只允许读取根文件。每条指令从会话快照复制选项，导入执行时读取上下文 Options 中的上限；Processing 可调整本条指令的 MaximumDepth，原选项及其它指令不受影响。循环检测独立生效。

`ProfileDirectiveBehavior` 是通用策略枚举。None 采用指令内置行为；Strict 要求按具体指令的规则严格处理；Ignore 保留注释声明，不进入指令处理；Suppress 在指令回调和执行之前抛出 ProfileException。

| 行为 | 导入指令 | 没有实现的其它指令 |
| --- | --- | --- |
| None | 读取文件，允许导入文件或目录缺失。 | 交给回调处理；未接管的声明作为注释保留。 |
| Strict | 要求全部直接和递归导入文件存在。 | 前置回调未接管时抛出异常。 |
| Ignore | 不触发指令回调，不访问导入文件。 | 不触发指令回调，不执行。 |
| Suppress | 拒绝指令，包括空参数指令。 | 拒绝指令，包括空参数指令。 |

这些策略在包含指令的文档触发 Loading 后生效，不阻止根文件通知。指令名称匹配忽略大小写；赋值时拒绝未定义的枚举值。

读取会话在根加载入口复制选项集合，并调用各指令选项的虚拟 Clone 方法。复制后的集合指向复制后的 ProfileOptions，并复制两项指令回调。递归读取共享复制后的设置。默认 Clone 保留实际派生类型并浅复制字段；含可变引用成员的派生选项须重写以复制这些成员。修改原集合、原选项属性或回调属性只影响后续根加载；委托捕获的可变状态由调用方管理。

### 全局指令注册表

`Profile.Directives` 是进程共用的 ProfileDirectiveCollection，默认包含 `ImportDirective.Instance`，其类型位于 `Zongsoft.Configuration.Profiles.Directives` 命名空间。它接受公开 ProfileDirectiveBase 的派生实现，基类提供只读 Name 和 Process(ProfileDirectiveContext) 方法。集合支持 Add、Count、名称索引、Contains、TryGetValue 及快照枚举。名称忽略大小写，拒绝空实例及重名注册；注册只增加实现，不提供移除或替换操作。

```csharp
// 在应用初始化时注册一次。
Profile.Directives.Add(new NoteDirective());

var options = new ProfileOptions();
options.Directives.Processing = context => Console.WriteLine(context.Name);
options.Directives.Add(new ProfileDirectiveOptions("note", ProfileDirectiveBehavior.Strict));
using var input = new StringReader("#@note Hello");
var profile = Profile.Load(input, options);

public sealed class NoteDirective() : ProfileDirectiveBase("note")
{
	public override void Process(ProfileDirectiveContext context)
	{
		context.Profile.Entries.Add("note", context.Argument);
	}
}
```

每次根加载在克隆选项和执行回调之前复制注册表。递归导入共享这份名称到实例的快照；加载过程中新增的注册只影响后续根加载。注册表使用 SynchronizedDictionary 存储，修改及获取快照时由字典同步，执行指令时不持锁。实例在并发加载间共享，每次调用的可变状态放在局部变量或上下文，所用依赖也须支持并发；快照复制实例引用，不复制实例。

执行顺序为 Ignore/Suppress 检查、Processing 回调、未 Handled 时调用注册实现、成功后 Processed。已注册指令自行定义 None/Strict 的具体规则。未知 Strict 指令必须由 Processing 接管；未知 None 指令可以保持未处理。回调设置 Handled=true 可接管已注册实现。

ImportDirective 将完整参数作为单个文件路径、按声明来源解析相对路径、处理可选或严格文件缺失，并取得深度上限（默认 64）。它通过上下文的内部读取操作将已打开的流交给当前 ProfileReader.Session；会话拥有并释放流，共享活动链检查，完成解析和合并后通知 Loaded。ImportDirective 不引用 ProfileReader 或 ProfileWriter。自定义实现中调用公共 Profile.Load 会开始独立的根加载，不属于内置导入的递归通道。

### 文件读取回调

Loading 和 Loaded 覆盖根配置及导入配置。Loading 在文件打开、循环和深度检查通过后、解析之前触发。Loaded 在解析及递归导入成功后触发；导入配置已经合并到直接引用者。前后通知分别创建公开 ProfileContext，属性全部只读：

| 属性 | 含义 |
| --- | --- |
| FilePath | 绝对加载路径；匿名输入为空字符串。 |
| Depth | 活动加载深度，根配置为 1，直接导入为 2。 |
| Referer | 直接引用者 Profile；根配置为 null。 |
| Profile | Loading 时为 null；Loaded 时为解析完成的配置。 |

可选缺失和被拒绝的文件不触发文件通知。实际重复读取分别通知。Loading、解析或合并失败时不触发该文件的 Loaded。回调异常向外传播，流和活动状态会清理，已有通知及合并不回滚。通知期间文件仍在活动链中。保留的 Loading 上下文不会在以后填入 Profile；引用的 Profile 模型仍可编辑。

### 指令处理回调

指令是紧接注释标记的 `@name`，例如 `#@import a.ini` 或 `;@custom value`。名称与参数以空格或 Tab 分隔。注释标记和 `@` 之间有空白时作为普通注释。原始指令文本保存在 ProfileComment 声明中。

Directives.Processing 在识别名称及原始参数后、执行之前触发。Directives.Processed 在整条指令成功处理后触发，包括其递归文件读取和合并。每条导入指令对应一个文件和一组指令通知，每个成功读取的文件收到一组文件通知。按顺序声明两条导入指令时：

```text
Loading(root)
  Directives.Processing(import)
    Loading(a.ini)
    Loaded(a.ini)
  Directives.Processed(import)
  Directives.Processing(import)
    Loading(b.ini)
    Loaded(b.ini)
  Directives.Processed(import)
Loaded(root)
```

递归指令嵌套在所属文件的通知之间。空参数导入和可选缺失导入仍会完成指令处理。前置回调、执行或嵌套回调失败时不触发 Directives.Processed。Ignore 和 Suppress 在指令回调之前生效。

前后回调共享同一个公开 sealed 的 ProfileDirectiveContext，它继承 ProfileContext。继承的 Profile 为正在解析的配置，Referer 为该配置的直接引用者。例如 A 导入 B、B 导入 C，B 中的指令上下文为 Profile=B、Referer=A、Depth=2：

| 属性 | 含义 |
| --- | --- |
| Name | 保留声明大小写的指令名称。 |
| Argument | 可写的指令参数，去除两端空白；赋值 null 时保留 null，具体语义由指令解释。 |
| Handled | 前置回调设为 true 可接管执行；已注册实现正常返回后，调度逻辑也会设为 true。 |
| Behavior | 本次指令固定的处理策略。 |
| Options | 本条指令独立的选项副本，包含派生属性；指令实现直接读取该副本，前置回调的修改可影响本条指令的专用设置（例如导入的 MaximumDepth），不改变会话设置、其它指令或已固定的 Behavior。未显式配置时为普通 ProfileDirectiveOptions，显式派生选项保留其类型。 |
| Profile / Section | 声明指令的配置和当前章节；根章节对应 null。 |
| FilePath / LineNumber / Depth | 声明来源路径、从 1 开始的行号和活动文件深度。 |

前置回调对 Argument 和 Handled 的修改参与执行；后置回调中的修改不重新执行指令。执行参数不回写原始声明。例如可重定向导入并处理自定义指令：

```csharp
options.Directives.Processing = context =>
{
	if(context.Name.Equals("import", StringComparison.OrdinalIgnoreCase))
		context.Argument = "shared.ini";
	else if(context.Name.Equals("note", StringComparison.OrdinalIgnoreCase))
	{
		Console.WriteLine(context.Argument);
		context.Handled = true;
	}
};
```

保存仍输出原始导入注释。None 下未知且未接管的指令以 Handled=false 完成；Strict 则拒绝。扩展上下文不提供 Reader、输入流或递归读取入口。

### 语法、路径与递归

每条导入指令将去除两端空白后的完整参数作为单个文件路径。参数内部的空格、Tab 和 `|` 均属于路径内容，具体字符是否合法由操作系统及文件系统决定。null 或空参数不读取文件。路径不解释或移除引号、不展开变量或通配符；包含空格的路径直接书写，无需引号。相对路径基于声明文件的加载目录，绝对路径可直接使用。导入合并到当前 Profile 根，包括写在章节内的导入。

导入多个文件时，按读取顺序分别声明：

```ini
#@import ../shared files/base.env
#@import ../.shared/product.env
#@import ../.shared/production.env
```

[ApplicationManifest](application-manifest.zh-Hans.md) 配置 `Directives = { ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Suppress) }`，在打开导入文件前以 FormatException 拒绝导入指令，包括两种注释标记及空参数。

只有活动链中的重复才构成循环。菱形及顺序重复导入每次重新读取。身份检查解析文件及祖先目录链接，同时保留原始相对路径基准。Windows 忽略路径大小写，其它平台使用 Ordinal。深度上限还限制硬链接等未识别别名。循环和深度错误包含导入链、声明文件及从 1 开始的行号。默认限制下，第 65 个活动文件在其 Loading 回调前被拒绝。

None 只忽略打开导入文件阶段的文件或目录不存在；权限、解析和回调失败正常传播。Strict 在缺失时报告目标路径、引用者、行号及原始 IO 异常。根文件始终必须存在。

FileStream 根输入提供路径并参与循环检查。匿名输入允许绝对导入，拒绝相对导入。Profile.Load(Stream) 关闭传入流。显式编码只作用于根文件，导入采用 UTF-8 并识别 BOM。Profile.Load(TextReader, ProfileOptions) 从当前位置读取，成功和失败均保持读取器打开。基于 FileStream 的 StreamReader 提供来源路径。读取首行开头的 BOM 被忽略，行号从当前位置开始计算。

### 章节名称

ProfileSection 移除名称两端空白，拒绝空名称或 `/`、`\`、`|`、`*`、`?`、`=`、`%`、`^`、`&`、`<`、`>`、`{`、`}`。章节查找忽略大小写。段落标题的空格和 Tab 分隔层级，`[network proxy]` 声明子章节。构建模型时使用父子章节，保存拒绝单个章节名称中的空白；名称不支持引号和转义。

### 合并、保存与下游

章节递归合并，后来的声明替换有效引用，同时保留各声明原值和 Profile 来源。同文件重复键报错，本地与导入按读取顺序覆盖。成功合并登记直接导入关系，被覆盖的子配置也参与保存。

Reader 记录声明基线，Loaded 中的修改保持待保存状态。Save() 写回接收者及其导入子树的修改来源，显式输出只处理本地声明。将注释改成指令文本不会执行或重建来源关系，需重新加载以建立新的导入图。

deployer 使用 Loading 记录导入文件哈希，根文件单独登记；packager 在 Loading 中收集合并前的引用者声明，在 Loaded 中收集各解析来源，包括根文件；containerizer 在 Loading 中验证来源声明。按路径另行打开并计算哈希不保证摘要严格对应解析字节，这由下游负责。

导入回归见 [ProfileImportTest](../test/Configuration/Profiles/ProfileImportTest.cs)。

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

每次保存创建一个 internal sealed ProfileWriter。Profile 保留公开便捷入口；Reader 负责解析，Writer 负责输出与提交。没有公共 Writer、单例、上下文工厂或读写公共基类。Writer 仅固定 PreserveBlanks，不复制指令选项或执行回调；ProfileContext 描述当前来源及配置，回调配置和指令选项不改变已加载模型的保存范围。

输出规则：null 值写作 `name`，空字符串写作 `name=`；注释规范化为 `#`，空注释保留为 `#`；保留声明顺序、空章节及必要的作用域切换。PreserveBlanks 为 true 时输出已记录空行，为 false 时不恢复原空行。加载时未保留的空行无法在保存时恢复。

名称和内容必须能由现有语法表示。不增加引号、转义和多行值语法；非法换行、章节名内的层级分隔空白、无法往返的条目值等在输出前拒绝。注释统一表示为 ProfileComment，包括导入文本；编辑注释可改变导入文本，但不会重新解析或更新已经登记的导入关系，需要重新加载。Comments.Add 接受 CRLF 或 LF 分隔的多行注释。

Writer 使用局部章节状态与传入的 TextWriter 输出声明，没有公开写入上下文或 OnWrite 通知。导入文本与其他注释同样输出；关联文件遍历只依据加载时登记的导入关系。

保存期间不得修改相关配置；检测到声明变化即失败，重复进入同一 Profile 的保存也被拒绝。调用方须避免并发修改，传入的 TextWriter 也须遵守该约束。

### 基线、重复来源与提交

Reader 在读取声明时登记原始基线，Loaded 回调中的修改保持待保存状态。保存比较当前声明内容、顺序及可变数组，不只依赖 setter 标记。成功写回来源后更新基线，恢复到原值会重新成为未修改；另存为其它路径或写入流不清除来源待保存状态。

保存按规范化文件身份去重，使用与 Reader 相同的文件及祖先目录链接解析。Windows 不区分路径大小写，其它平台区分。同一路径只有一个修改实例时保存该实例；多个修改实例快照相同只输出一次，冲突则在任何输出前失败。未修改的旧实例不会覆盖修改实例，保存不刷新其它重复读取实例。

关联保存先生成全部同目录临时文件，完成校验、序列化及关闭后，再按子文件先于父文件的顺序替换目标。准备阶段失败不替换任何原文件。未修改的只读文件无需写入；修改后的只读目标明确拒绝。不会自动创建目标目录，也不会以删除目标作为替换回退。

多个文件的替换不是事务。提交失败会停止后续提交，抛出 ProfileException，InnerException 保留 IO 原因，Data["FailedPath"] 为失败目标，Data["CompletedPaths"] 为已完成路径数组。成功文件更新基线，未成功文件保持待保存状态，可以重试。已完成的提交不回滚。

临时文件在退出时清理。若主操作已失败且清理也失败，保留主异常，并在其 Data 中以临时路径记录清理异常；调用方可据此清理残留文件。

文件写入跟随符号链接目标，不替换链接本身。不承诺硬链接联动、并发写入冲突检测、跨文件原子性或断电持久性。文件权限和替换行为仍受操作系统与文件系统约束。

### 编码、资源与验证

文件与 Stream 默认使用 Encoding.UTF8；显式编码仅用于当前显式输出，不记录并恢复原编码或 BOM。TextWriter 的编码和换行由调用方决定。已修改文件按所选格式重新输出，不承诺逐字节还原。

路径资源由 Writer 管理。Stream 保持既有关闭行为；TextWriter 不关闭。流与文本写入器可能在异常前收到部分输出，不能回滚。

[ProfileWriterTest](../test/Configuration/Profiles/ProfileWriterTest.cs) 覆盖来源写回、范围隔离、声明往返、重复实例、准备/提交失败与重试。Linux/macOS 的权限和链接验证必须原生执行，不能用 Windows 结果代替。哈希严格对应解析字节仍由下游输入快照方案处理，Core 不依赖部署、NuGet 或哈希类型。

## 变量视图

`ProfileExtension.ToVariables()` 返回 `Zongsoft.Expressions.IVariables`，将配置对象适配为实时只读变量视图：

| 转换入口 | 查询范围 |
| --- | --- |
| `profile.ToVariables()` | 根级条目及所有章节的当前有效条目，包含已合并的导入结果。 |
| `section.ToVariables()` | 所选章节及全部子章节，保留从根到该章节的完整命名空间。 |
| `entry.ToVariables()` | 仅所选条目，保留其所属章节的完整命名空间。 |

转换不复制字典；值修改及所选配置或章节内的增删、替换反映到后续查询。章节或条目视图始终持有所选对象，原集合移除或替换该对象不会使视图转向新对象。视图不自动查询其它配置、预读文件或展开模板，也不提供额外的并发同步。

### 名称映射

根级条目属于默认命名空间。章节层级用 `.` 连接，章节名称内的 `.` 原样保留；每个点分段须符合 ASCII 标识符规则 `[A-Za-z_][A-Za-z0-9_]*`。非法分段，包括空段、数字开头或含其它字符的段，使该章节及其子章节不参与变量映射。

条目名称中的每个 `.` 和 `-` 替换成 `_`，再验证完整标识符；例如 `db-name`、`db.name` 均映射为 `db_name`，`-name` 映射为 `_name`。其它非法条目不提供变量。转换不会修改原始名称、值或保存内容。

```ini
product=erp

[mysql]
db-name=zongsoft

[io rustfs]
database=attachments

[app.runtime]
worker-count=4
```

对应变量为 `product`、`mysql:db_name`、`io.rustfs:database`、`app.runtime:worker_count`。章节视图不会移到默认命名空间，例如 `profile.Sections["mysql"].ToVariables()` 仍需通过命名空间 `mysql` 查询。

### 查询与冲突

`TryGetValue(name, out value)`、`TryGetValue(null, name, out value)` 和 `TryGetValue("", name, out value)` 等价，仅查询默认命名空间。名称和命名空间按 OrdinalIgnoreCase 比较，非空命名空间不回退到父级或默认空间。

查询参数不裁剪、不归一化，应传入转换后的名称 `db_name`；直接查询 `db-name` 或 `db.name` 返回 false。查询名称为 null 时抛 ArgumentNullException，其它非法查询或不存在的变量返回 false。转换对象为 null 也抛 ArgumentNullException。

值保留为原始 string 或 null；存在且值为 null 的条目仍返回 true。提供程序不负责格式化、递归、成员访问或类型转换。

名称转换可能产生重名，例如同一章节的 `db-name` 与 `db.name`，或者 `[io rustfs]` 和 `[io.rustfs]` 中同名的条目。只在查询冲突变量时抛 ProfileException，不因冲突拒绝创建视图，其它变量仍可查询。具有相同命名空间但不同变量名称的章节可以共同提供变量。

冲突限于所选视图范围；单条目视图不检查其它条目。正常导入中同一章节同一原始名称的覆盖已经由 Profile 合并，不算映射冲突。删除冲突条目后，后续查询立即恢复正常。

### 用于模板评估

```csharp
using Zongsoft.Configuration.Profiles;
using Zongsoft.Expressions;
using Zongsoft.Text.Templating;

var profile = Profile.Load("settings.ini");
IVariables variables = profile.ToVariables();

variables.TryGetValue("mysql", "db_name", out var database);

var evaluator = new TemplateEvaluator();
evaluator.Providers.Add(variables);
var text = evaluator.Evaluate("Database=${mysql:db_name};Storage=${io.rustfs:database}");
```

通过模板查询冲突变量时，TemplateEvaluator 抛出 Code 为 ProviderFailed 的 TemplateEvaluationException，InnerException 保留 ProfileException。是否递归评估字符串由 TemplateEvaluatorOptions.Recursive 控制。

这些扩展只提供变量来源。Profile 加载、导入参数与 Save 不自动求值，ProfileEntry.Value 保留原文。运行期间变量来源的组合和模板求值由调用方显式组织。

实现见 [ProfileExtension](../src/Configuration/Profiles/ProfileExtension.cs)，契约见 [IVariables](../src/Expressions/IVariables.cs)，行为测试见 [ProfileVariablesTest](../test/Configuration/Profiles/ProfileVariablesTest.cs)。
