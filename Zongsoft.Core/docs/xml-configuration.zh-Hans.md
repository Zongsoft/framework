# XML 配置与处理指令

[English](xml-configuration.md) | [简体中文](xml-configuration.zh-Hans.md)

## 读取配置和指令

`Zongsoft.Configuration.Xml` 提供 XML 配置读取能力。配置根元素为 `configuration` 或 `options`，根下使用 `option path="..."` 声明配置路径。普通属性映射为配置键，`元素名.name` / `元素名.key` 指定集合项名称，文本和 CDATA 沿用单值集合的映射规则。

XML 处理指令（PI）以原始定义单独收集，不自动映射为配置键。文件和流提供程序在读取时执行已注册的指令，默认包含 `import`；未知指令默认保留定义，由使用者决定如何解释。静态 `XmlStreamConfigurationProvider.Read` 只解析当前流，不执行指令。

```xml
<?xml version="1.0" encoding="utf-8" ?>
<?zongsoft.tools.binding path="Kestrel/Endpoints/*" template="${this.Url}" ?>
<configuration>
	<option path="/Kestrel">
		<endpoints>
			<endpoint endpoint.name="http" url="http://*:8069">
				<?example note="application endpoint" ?>
			</endpoint>
		</endpoints>
	</option>
</configuration>
```

这里的 `zongsoft.tools.binding` 只是被收集的声明。读取器不会解释 `path` 或 `template` 参数，不生成 `.bindings`。

下例需要应用提供 `app.option`，展示单个文件提供程序的使用方式：

```csharp
using System.Linq;
using Microsoft.Extensions.Configuration;
using Zongsoft.Configuration;
using Zongsoft.Configuration.Xml;

using var configuration = (ConfigurationRoot)new ConfigurationBuilder()
	.AddOptionFile("app.option")
	.Build();

var provider = configuration.Providers.OfType<XmlConfigurationProvider>().Single();
var instructions = provider.Instructions;

foreach(var instruction in instructions)
{
	if(instruction.TryGetAttributes(out var attributes) &&
		attributes.TryGetValue("path", out var path))
	{
		//path 是参数文本，由调用方决定如何使用。
	}
}
```

`XmlStreamConfigurationProvider` 同样提供 `Instructions`。集合对应各提供程序当前的数据，按导入位置深度优先展开：先保留 `import` 自身，再放入子文件中的有效指令，随后继续当前文件。重复声明和原始来源信息保留；多个提供程序之间的指令不自动合并或去重。需要最终配置值的使用者应先完成配置构建，再用这些声明从最终配置取值。

## 导入配置

```xml
<?import path="defaults.option"?>
<configuration>
	<option path="/Service">
		<?import path="shared.option" required="false"?>
		<client timeout="30" />
	</option>
</configuration>
<?import path="overrides.option"?>
```

`import` 只允许出现在根元素之前、根元素之后或 `option` 的直接子级。根元素外的导入读取目标文件的全部选项；`option` 内的导入只读取目标文件中与当前 `option path` 完全相同的选项，并在当前指令位置合并。`configuration` 的直接子级及 `option` 的孙级、更深层级均不允许放置导入指令。

路径匹配先去除两端的 `/`、空格和 Tab，再将 `/` 转换为配置键分隔符 `:`，使用 `OrdinalIgnoreCase` 比较。缺省、空字符串及 `/` 均表示配置根路径。例如当前 `/Service` 匹配目标 `service/`，不匹配单独声明的 `/Service/Client`。目标中所有同路径的 `option` 块都按顺序处理；没有匹配项时成功返回空结果，与 `required` 无关。

按路径导入时，目标文件根元素外的导入继续传递相同筛选条件；只有选中的 `option` 及文件级指令参与处理和收集。其它选项内的指令不会执行，其引用文件也不会打开或监听；整个目标 XML 仍须格式良好。

每个文件独立积累配置，成功后才合并到引用者。后出现的导入或本地配置值覆盖先前的跨文件结果。因此可以先导入默认配置，再通过本地配置覆盖个别值。同一文件内普通 XML 声明产生重复键仍报错，即使两次声明之间存在导入；处理器和回调对配置字典的写入不计为普通声明。

`path` 是必需的非空属性，一个指令导入一个文件，不展开通配符或变量。`required` 默认为 `true`，只接受布尔值。为 `false` 时仅忽略打开目标时的文件或目录不存在；权限、其它 IO 错误、XML 错误、循环及深度超限仍导致加载失败。

物理相对路径以声明文件的实际目录为基准，绝对路径保持原义。`FileStream` 始终提供实际路径基准，即使来源标识被覆盖。没有物理文件路径的 `IFileProvider` 使用同一提供器内的相对逻辑路径，规范化 `.` 和 `..`，禁止越过提供器根目录或退回磁盘查找。普通内存流可导入绝对物理路径；相对导入因缺少基准而失败，即使 `required="false"` 或 `FilePath` 看起来像文件名。

递归只检测活动导入链上的循环，物理文件身份包含文件与目录链接的解析。同一文件可以在不同位置重复导入，菱形依赖也会重新读取。默认最多同时加载 64 层，根文件计第一层；`MaximumDepth` 必须为正整数。

## 指令选项与回调

文件和流来源均具有只读的 `Options` 属性：

```csharp
using var configuration = (ConfigurationRoot)new ConfigurationBuilder()
	.AddOptionFile(source =>
	{
		source.Path = "app.option";
		source.ReloadOnChange = true;
		source.Options.Instructions.Add(XmlConfigurationInstructionOptions.Import(
			XmlConfigurationInstructionBehavior.Strict, maximumDepth: 16));
		source.Options.Instructions.Processing = context =>
		{
			//可通过 context.Data 改写本次执行参数，或设置 Handled 接管处理。
		};
	})
	.Build();
```

`XmlConfigurationInstructionOptions` 按名称配置行为；名称不区分大小写，不允许空项或重名项。`XmlConfigurationInstructionOptions.Import()` 创建带 `MaximumDepth` 的 `ImportOptions`。选项集合只配置行为，不注册处理器。

| 行为 | 执行规则 |
| --- | --- |
| `None` | 使用指令默认规则；Import 按 `required` 决定缺失处理。未知指令仍触发前后回调，未接管时保持 `Handled=false`。 |
| `Strict` | Import 强制目标存在，忽略 `required` 的值；未知指令可由前置回调接管，否则报错。 |
| `Ignore` | 只保留原始定义；不校验指令位置或参数，不执行处理器、指令回调或目标读取。 |
| `Suppress` | 遇到即报错，在解析参数和指令回调之前拒绝。 |

`Loading` 与 `Loaded` 是 `Action<XmlConfigurationContext>`，根文件和实际读取的导入文件都会触发。文件打开并通过循环与深度检查后触发 `Loading`，此时配置字典为空；解析和递归导入成功后触发 `Loaded`，其对当前字典的修改随后参与合并到引用者。缺失或被拒绝的文件不触发文件回调。

`Instructions.Processing` 和 `Instructions.Processed` 是 `Action<XmlConfigurationInstructionContext>`。顺序为前置回调、处理器、后置回调；`Handled=true` 跳过处理器，正常完成的处理器由调度逻辑标记为已处理。Import 的后置回调发生在递归读取和合并之后。处理器或回调异常终止加载；失败指令不触发后置回调。后置回调改写数据不会重新执行指令。

| 上下文成员 | 含义 |
| --- | --- |
| `FilePath` / `Depth` / `Referer` | 当前来源、加载层数及触发本文件读取的原始导入指令；根文件的 `Referer` 为 `null`。 |
| `Configuration` | 当前文件已读配置的可写字典，包含此前完成的导入，不包含引用者的配置。 |
| `Instruction` | 不可变的原始 PI 定义，包括名称、数据及来源位置。 |
| `Data` | 本次执行的可写数据，不裁剪空白；修改不改变原始定义。 |
| `GetAttributes()` / `TryGetAttributes(...)` | 按需解析当前执行数据；修改 `Data` 后重新解析。 |
| `OptionPath` | 所在 `option` 的规范化 `:` 路径，配置根路径为空字符串，文件级指令为 `null`。 |
| `Handled` | 是否由回调接管或已由处理器完成。 |
| `Behavior` | 前置回调之前已固定的行为。 |
| `Options` | 本条指令的独立选项副本；可调整专用设置，不能通过它改变已固定的 `Behavior`。 |

每次根加载复制全局注册表和来源选项，递归导入共享该快照，重新加载则取得新快照。选项的虚拟 `Clone()` 保留派生类型；包含可变引用成员的派生类型应重写它并复制这些成员。委托捕获的外部状态由调用方管理。

## 注册处理器

`XmlConfiguration.Instructions` 是线程安全的 `XmlConfigurationInstructionProcessorCollection`，默认注册 `Instructions.ImportInstructionProcessor.Instance`。`Add` 拒绝空实例和重名；查询按 `OrdinalIgnoreCase` 比较，枚举使用快照，不提供移除或替换。注册表保存共享处理器实例，执行时不持有注册表锁。

自定义处理器继承 `XmlConfigurationInstructionProcessor`，通过构造函数指定有效 XML 指令名，并实现 `Process(XmlConfigurationInstructionContext)`。名称不得为 `xml`，不含 `<?` 和 `?>`。实例可能被多个加载同时调用，每次执行的可变状态应放在上下文或局部变量中。

```csharp
XmlConfiguration.Instructions.Add(new FeatureInstructionProcessor());

sealed class FeatureInstructionProcessor() : XmlConfigurationInstructionProcessor("feature")
{
	public override void Process(XmlConfigurationInstructionContext context)
	{
		context.Configuration["Features:Example"] = context.Data;
	}
}
```

此示例直接使用自由文本数据；属性式参数解析是可选能力。Core 内置指令只有 Import，不提供 `set` 或 `zongsoft.tools.binding` 业务规则。

## 指令定义

`XmlConfigurationInstruction` 是只读对象，由读取器创建。

| 属性 | 含义 |
| --- | --- |
| `Name` | PI 名称，保留大小写，不含 `<?`、`?>`。 |
| `Data` | `XmlReader.Value` 提供的数据文本，不额外裁剪或解码。 |
| `IsEmpty` | 判断 `Data` 是否为 `null` 或空字符串，不裁剪空白，也不解析属性。 |
| `Path` | 父元素的实际 XML 结构路径；以 `/` 开始，保留限定名称及大小写，不含索引或谓词。 |
| `FilePath` | 来源文件路径或调用方提供的来源标识；匿名来源为空字符串。 |
| `LineNumber` / `LinePosition` | PI 名称起点的行列号，从 1 开始；例如行首 `<?example?>` 的列号为 3。 |

示例中 `zongsoft.tools.binding` 的 `Path` 为空字符串；`example` 的 `Path` 是 `/configuration/option/endpoints/endpoint`，并非配置路径 `/Kestrel/endpoints/http`。指令数据里的 `path="..."` 参数与对象的 `Path` 属性彼此独立。

PI 可以出现在根元素之前、所有元素内部以及根元素之后；具体处理器可以限制位置。纯解析收集当前流的全部 PI，按路径导入只收集有效范围内的 PI。根元素之外的 `Path` 为空字符串，同层级的重复元素可以具有相同路径，具体位置由行列号区分。XML 声明、注释和 CDATA 内形似 PI 的文字不会成为指令。

`Data` 不是原文件的字节切片：XML 读取器会移除名称与数据之间的分隔空白，并执行 XML 换行规范化；保留返回数据中的尾部空白和实体拼写。例如 `<?example value="a&amp;b" ?>` 的数据为 `value="a&amp;b" `。

`<?example?>` 和 `<?example   ?>` 的 `IsEmpty` 均为 `true`；`<?example hello   ?>` 为 `false`，其 `Data` 保留 `hello` 后面的三个空格。

## 按需解析属性式参数

`GetAttributes()` 返回 `IReadOnlyDictionary<string, string>`；格式无效时抛出 `FormatException`，错误包含指令来源位置。`TryGetAttributes(out ...)` 在相同格式错误下返回 `false`，输出为 `null`，不返回部分结果。其它异常不被转换为解析失败。

- 参数名使用 `OrdinalIgnoreCase` 比较，保留原有拼写；`Path="a" path="b"` 属于重复参数，解析失败。
- 属性值必须用单引号或双引号包围；属性之间可使用 XML 空白，等号两侧也允许 XML 空白。
- 空数据或仅含 XML 空白的数据成功解析为空集合；`value=""` 是一个空字符串值。
- 按 XML 属性规则解码实体和字符引用一次，并执行属性值空白规范化。例如 `a&amp;b` 得到 `a&b`，`&amp;amp;` 得到 `&amp;`。
- 反斜杠保持原义，不采用 C# 字符串转义规则；不自动转换数字、布尔值、设置文本或路径。
- 必须完整解析数据，不接受未加引号的值、重复属性、额外元素、注释或尾部杂文。
- 参数按独立 XML 元素的属性解析，不继承父元素的命名空间或 DTD 上下文。需要的命名空间声明须包含在 `Data` 中；不加载 DTD 或外部实体。

成功的参数集合只读并按指令实例缓存，可以并发读取。解析失败不会改变 `Data`，也不影响已经成功读取的配置。任意指令都可以采用其它数据语法：

```xml
<?custom some free-form text ?>
```

该 PI 可以正常收集；调用方可以直接解释 `Data`，属性式解析只是可选方法。

## 流、来源与快照

直接读取现有流时，可以同时取得配置字典和指令：

```csharp
using System.IO;
using Zongsoft.Configuration.Xml;

using var stream = File.OpenRead("app.option");
var values = XmlStreamConfigurationProvider.Read(stream, out var instructions);
```

`Read(Stream)` 及 `Read(Stream, out instructions, filePath)` 只解析当前流，不使用注册表、加载选项或回调，也不跟随 Import。`filePath` 显式提供来源标识；为 `null` 时从 `FileStream.Name` 推导，其它流采用空字符串。显式空字符串表示匿名来源。此参数只记录来源，不打开该路径，也不提供相对导入的路径基准。

配置构建器支持 `AddOptionStream(stream, filePath)`，也支持 `AddOptionStream(source => { ... })` 设置 `Stream`、`FilePath` 和 `Options`。这些提供程序会执行指令。文件提供程序记录实际文件流路径，虚拟文件来源则记录来源的逻辑路径。直接读取和流提供程序均不关闭调用方的流，文件提供程序沿用文件基类的流生命周期；导入打开的流由加载器关闭。

每次完整读取成功后，配置数据及其指令一起替换；之前取得的只读指令集合和对象保持不变。文件缺失或错误处理遵循 `FileConfigurationProvider`：基类清空数据时，指令也为空；基类保留旧数据时，旧指令随数据保留。失败读取不会发布部分指令。流提供程序仍遵循基类只能通过无参 `Load()` 加载一次的约束。

## 文件变化与验证

文件来源启用 `ReloadOnChange` 后，根文件及有效导入依赖的变化都会重新加载整个根配置。可选但缺失的目标和已读取但没有匹配选项的目标同样被监听；被跳过选项内的目标、忽略的导入不被监听。每轮加载刷新依赖集合，解除不再使用的订阅，释放配置时解除全部订阅，不释放调用方提供的 `IFileProvider`。通知能力取决于文件提供器；流来源不自动监听文件。

重载失败遵循 `FileConfigurationProvider` 的清空与异常处理规则，包括 `OnLoadException`；不恢复上一次成功结果。已经发现的依赖继续接收通知，以便后续变更重新触发加载。多文件读取不提供文件系统事务快照。

配置映射回归位于 [OptionConfigurationTest.cs](../test/unit/Configuration/Xml/OptionConfigurationTest.cs)。同目录的 PI、[处理器](../test/unit/Configuration/Xml/XmlConfigurationProcessorTest.cs)、[导入](../test/unit/Configuration/Xml/XmlConfigurationImportTest.cs)和[重载](../test/unit/Configuration/Xml/XmlConfigurationImportReloadTest.cs)测试覆盖词法、原始定义、回调快照、路径筛选、覆盖顺序、错误、流所有权、循环与通知释放。
