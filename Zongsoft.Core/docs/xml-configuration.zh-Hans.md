# XML 配置与处理指令

[English](xml-configuration.md) | [简体中文](xml-configuration.zh-Hans.md)

## 读取配置和指令

`Zongsoft.Configuration.Xml` 提供 XML 配置读取能力。配置根元素为 `configuration` 或 `options`，根下使用 `option path="..."` 声明配置路径。普通属性映射为配置键，`元素名.name` / `元素名.key` 指定集合项名称，文本和 CDATA 沿用单值集合的映射规则。

XML 处理指令（PI）作为独立信息收集，不进入配置键字典，不执行导入、配置修改或绑定提取。文件和流提供程序默认收集全部 PI，无须开启选项。

```xml
<?xml version="1.0" encoding="utf-8" ?>
<?binding path="Kestrel/Endpoints" url="Url" ?>
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

这里的 `binding` 只是被收集的声明。读取器不会解释 `path` 或 `url` 参数，不生成 `.bindings`。

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

`XmlStreamConfigurationProvider` 同样提供 `Instructions`。集合对应各提供程序当前的数据，保留文档顺序及重复声明；多文件指令不会在本层自动合并、去重或确定优先级。

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

示例中 `binding` 的 `Path` 为空字符串；`example` 的 `Path` 是 `/configuration/option/endpoints/endpoint`，并非配置路径 `/Kestrel/endpoints/http`。指令数据里的 `path="..."` 参数与对象的 `Path` 属性彼此独立。

收集范围包括根元素之前、所有元素内部以及根元素之后。根元素之外的 `Path` 为空字符串，同层级的重复元素可以具有相同路径，具体位置由行列号区分。XML 声明、注释和 CDATA 内形似 PI 的文字不会成为指令。

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

现有 `Read(Stream)` 保持可用。新增重载的第三个参数 `filePath` 可以显式提供来源标识；为 `null` 时从 `FileStream.Name` 推导，其它流采用空字符串。显式空字符串表示匿名来源。此参数只记录来源，不打开该路径，也不作为配置参数的解析基准。

配置构建器支持 `AddOptionStream(stream, filePath)`；通过配置来源直接构建时，可设置 `XmlStreamConfigurationSource.FilePath`。文件提供程序记录实际文件流路径，虚拟文件来源则记录来源的 `Path`。直接读取和流提供程序均不关闭调用方的流，文件提供程序沿用文件基类的流生命周期。

每次完整读取成功后，配置数据及其指令一起替换；之前取得的只读指令集合和对象保持不变。文件缺失或错误处理遵循 `FileConfigurationProvider`：基类清空数据时，指令也为空；基类保留旧数据时，旧指令随数据保留。失败读取不会发布部分指令。流提供程序仍遵循基类只能通过无参 `Load()` 加载一次的约束。

## 验证

配置映射回归位于 [OptionConfigurationTest.cs](../test/Configuration/Xml/OptionConfigurationTest.cs)，PI 契约测试位于同目录，覆盖词法、位置、原始数据、可选属性解析、流所有权和文件重新加载。

本层不提供处理器注册、指令执行、递归导入或 `binding` 业务规则；后续实现可以消费指令定义并自行制定语义。
