# 文本模板

[English](expressions.md) | [简体中文](expressions.zh-Hans.md)

Zongsoft.Text.Templating.TemplateEvaluator 把带有变量引用的模板转换成字符串。它支持可扩展变量来源、命名空间、成员导航、动态索引、格式化和可选的字符串递归展开。

Zongsoft.Text.Templating 提供独立的模板求值能力，不依赖 Profile 或具体工具。本文包含使用方式、公开契约及实现边界。

模板契约 ITemplate、ITemplateRenderer，以及求值器、选项和诊断类型均位于 Zongsoft.Text.Templating；词法基础设施位于 Zongsoft.Expressions。变量来源的契约与实现见[变量文档](variables.zh-Hans.md)。ResolutionContext 和 FormattingContext 是 TemplateEvaluator 的公开嵌套类，分别描述引用取值和插值格式化。

| 设施 | 职责 |
| --- | --- |
| TemplateEvaluator | 模板分区、变量引用、成员导航、事件、递归、格式化和诊断。 |
| Lexer / TokenScanner / Tokenizer | 提供共享的词法规则与扫描机制。 |
| Reflector | 读取成员与默认索引器；模板遵守其现有规则。 |

## 开始使用

以下示例将 [Variables](variables.zh-Hans.md#内存变量) 注册为模板提供器，并演示命名空间和格式化：

```csharp
using System;
using System.Globalization;
using Zongsoft.Common;
using Zongsoft.Text.Templating;

var variables = new Variables
{
	["name"] = "Zongsoft",
	["app:price"] = 12.5m,
};

var evaluator = new TemplateEvaluator(new()
{
	Culture = CultureInfo.InvariantCulture,
});
evaluator.Providers.Add(variables);

var text = evaluator.Evaluate("你好，${name}，金额 ${app:price#0.00}");
// 你好，Zongsoft，金额 12.50

variables["APP:PRICE"] = 20m;
var updated = evaluator.Evaluate("金额 ${app:price#0.00}"); // 金额 20.00
```

## 公开接口

TemplateEvaluator 的主要公开成员如下，取值与格式化分别使用独立的事件上下文：

| 成员签名 | 作用 |
| --- | --- |
| `TemplateEvaluator(TemplateEvaluatorOptions options = null)` | 创建评估器，选项的持有规则见“选项与生命周期”。 |
| `IList<IVariables> Providers { get; }` | 按优先级排列的变量提供器集合。 |
| `TemplateEvaluatorOptions Options { get; }` | 当前评估器使用的选项。 |
| `event EventHandler<ResolutionContext> Resolving / Resolved` | 完整变量引用取值前后的通知。 |
| `event EventHandler<FormattingContext> Formatting / Formatted` | 插值格式化前后的通知。 |
| `string Evaluate(ReadOnlySpan<char> text)` | 求值并返回文本，求值失败抛出异常。 |
| `bool TryEvaluate(ReadOnlySpan<char> text, out string result, out TemplateEvaluationException error)` | 使用同一求值流程并返回成功状态与错误。 |

变量提供器遵循 [IVariables 契约](variables.zh-Hans.md#变量契约)。模板评估器调用带命名空间的 TryGetValue 重载；未限定命名空间的引用传入 null。

Providers 按注册顺序查询，第一个返回 true 的来源获胜，包括值为 null 的情况；返回 false 才继续查询，来源异常立即终止求值。

评估器不缓存变量值、getter 的返回值或索引结果。Reflection 可以复用 Getter 委托；相同引用出现两次仍执行两次取值，因此动态提供器可以返回不同结果。

## 文本输入

Evaluate(ReadOnlySpan<char> text) 和 TryEvaluate(ReadOnlySpan<char> text, out string result, out TemplateEvaluationException error) 接受字符串、字符缓冲区和切片。字符串可直接传入，无须手动转换；返回值仍为 string。

```csharp
var input = "前缀${name}后缀";
var result = evaluator.Evaluate(input.AsSpan(2, 7)); // Zongsoft
```

空 Span、default 以及 null 字符串转换而来的 Span 都视为空模板，返回空字符串且不触发变量事件。普通空白文本原样保留。提供器集合中的 null 仍属于调用错误。

内部 Parser 与 TokenScanner 均为 ref struct，直接保存并扫描 Span；引用片段以切片交给 Lexer，不创建片段字符串。每层在首次引用求值前复制一份源码供事件上下文和异常在调用结束后保存；解析失败时也保存对应输入。诊断中的 Template 只包含传入的切片，Position 相对于切片，递归错误相对于子模板。首个回调之后不再读取该层输入 Span，回调修改原字符数组不会改变已解析的节点或上下文原文。纯文本成功路径不为事件额外复制整份源码；词素值、数值装箱、语法节点和输出仍需要分配。

私有 Part、Accessor、Argument 和 Lexeme 节点统一使用 readonly struct，通过构造函数初始化 public readonly 字段，直接存储在列表数组中，不逐个分配节点对象，也不生成记录类型的相等运算符或解构成员。Reference 是 sealed class，在解析完成后通过构造函数设置只读字段，仅在存在成员或索引导航时分配访问器列表，否则列表为 null。集合在解析阶段构建，求值阶段只读，不额外复制或包装。只有一个文本或占位符片段的模板仅分配一个列表槽位，空模板不分配元素数组，不额外扫描输入。Parser 保持可变以维护解析游标。

## 词法扫描

Lexer.GetScanner(ReadOnlySpan<char> text) 返回 TokenScanner ref struct，接受字符串、切片及栈缓冲区，不复制输入。扫描期间须保持输入缓冲区有效且内容不变。空输入不产生词素。

```csharp
using var scanner = Lexer.Instance.GetScanner("prefix name == 12L suffix".AsSpan(7, 11));

while(scanner.Scan(out var position, out var length) is { } token)
	Console.WriteLine($"{position}, {length}: {token}");
```

Scan 跳过空白，返回相对于传入切片的 UTF-16 起始位置和词素原文长度。到达末尾时返回 null，位置为输入长度，词素长度为零。TokenScanner 只能在栈上使用，不能装箱或使用 LINQ；支持 foreach 模式，枚举从当前游标的副本开始，不推进原扫描器，也不关闭其输入流。

Lexer.GetScanner(Stream stream) 使用 UTF-8 并检测 BOM，一次性解码后走同一套扫描逻辑，支持不可寻址流。Dispose 关闭输入流并使该扫描器失效，初始读取失败也会关闭流。Span 入口不分配输入副本。流的所有权应保留在 GetScanner 返回的扫描器中，不要复制出另一个负责释放的所有者。

自定义分词器实现 `TokenResult Tokenize(ReadOnlySpan<char> text)`，只检查剩余文本的开头。成功返回 Token 和正数 Length，长度包含已消耗的全部源码字符，包括引号、转义及数字后缀；不匹配时返回 TokenResult.Fail()，不消耗文本。扫描器按 Tokenizers 注册顺序尝试，仅成功后推进游标。成功结果的 Length 为零、负数或超出剩余输入时抛 InvalidOperationException。扫描期间不要修改分词器集合。

```csharp
public interface ITokenizer
{
	TokenResult Tokenize(ReadOnlySpan<char> text);
}
```

TokenResult 是 readonly struct，包含 public readonly 字段 `int Length` 和 `Token Token`。通过 `new TokenResult(length, token)` 创建结果；`TokenResult.Fail()` 返回默认结构值，即 Token=null、Length=0。

LiteralTokenizerBase 选择最长的已配置字面量，单词同时检查标识符边界。CreateToken 收到的是配置中的字符串及其大小写，忽略大小写匹配不复制输入中的拼写。布尔、null 和标准符号分词器复用已有 Token。数字直接从 Span 解析；标识符只创建一次最终值。无转义的引号字符串只复制内容；含转义的字符串先解码到有界栈缓冲区或池化数组，再创建最终值。模板成员名称复用标识符词素的值。

Keyword 分词器创建 Token，但复用配置中的关键字字符串作为 Value。String 分词器先校验转义并计算解码长度，使用的池化数组归还时清空内容。

## 语法

模板统一使用 `${...}`；普通文本中的 `%name%` 和 `$(name)` 不具有变量展开含义。

| 模板 | 含义 |
| --- | --- |
| `${name}` | 默认命名空间变量。 |
| `${app.runtime:name}` | 多级命名空间中的变量。 |
| `${person.Home.Address}` | 按 Reflector 规则读取属性或字段。 |
| `${items[0].Name}` | 列表常量索引后继续导航。 |
| `${items[indices[0]].Name}` | 动态参数与嵌套列表索引。 |
| `${grid[row,column]}` | 多参数索引器。 |
| `${map['key']}、${map["key"]}` | 单引号或双引号字符串键。 |
| `${price#0.00}` | .NET 数值格式。 |
| `${date#yyyy-MM-dd HH:mm:ss}` | .NET 日期格式，内部空白保留。 |
| `\${name}` | 输出字面文本 ${name}。 |
| `${=expression}` | 为未来轻量表达式保留；当前报 UnsupportedExpression。 |

变量、成员、命名空间各段使用 ASCII 标识符规则：[A-Za-z_][A-Za-z0-9_]*。命名空间可以包含点，冒号仅分隔命名空间与根变量。成员忽略大小写；字典键按目标字典自己的比较规则处理。引用内不允许语法空白，${ name }、${arr[ index ]} 都是错误。引号中的键空白属于数据。

动态索引参数从提供器环境取值，不从被索引对象取值，也不继承外层命名空间。例如 ${app:arr[index]} 中 index 来自默认命名空间。动态参数也支持自己的命名空间和成员导航。

成员导航直接调用 Reflector.GetValue(ref object, name, parameters)，遵守其现有规则：字段和属性按 Public、Instance、Static、IgnoreCase 查找，空名称读取默认成员；多个结果使用第一项，不另做最佳重载或歧义判定。Type 目标表示被查找的类型，不按普通 Type 对象重新筛选。模板不增加公共 getter、实例成员或显式接口的独立筛选；方法调用和算术运算仍不属于模板语法。

普通成员传入成员名称，索引访问传入空名称以调用 GetDefaultMembers；反射枚举顺序不能作为稳定的重载优先级。GetValue 保留原始 Getter 异常，模板将实际读取失败包装为 NavigationFailed；Reflector.TryGetValue 的属性路径会捕获读取异常并返回 false，因此模板不使用该路径。

## 索引常量和参数绑定

无后缀整数优先 int，超出 int 范围时使用 long；超出 long 范围失败，大小写 L 后缀指定 long。小数默认 double，f/F、d/D、m/M 后缀分别指定 float、double、decimal。支持负数，但负索引不会改为从末尾计数。数字使用固定小数点，不随 Culture 改变；当前不支持科学计数法、十六进制或前置加号。数值后缀、范围和非法边界由 Lexer 统一处理。

true、false、null 忽略大小写，仅在完整裸索引参数中作为 bool 或 null 常量；${true} 是变量引用，${arr[true.Name]} 中 true 也是变量，关键字前缀不会截断标识符。常量直接作为对象值，不查询提供器或触发变量事件。单引号和双引号键均为 string，不插值，例如 `${map['${name}']}` 使用字面键 `${name}`。

索引参数完成取值、Resolved 回调及可选递归后，将原始对象传给 Reflector，不调用 Common.Convert 自动转换类型；Getter 按声明的参数类型拆箱或转换引用。例如 Dictionary<long, string> 使用 [42L]，不能用字符串 ['42'] 或 int 类型的 [42] 代替。null 也不会自动转成 int 的 0，object 参数保留原始类型，因此 int 1 与 long 1 可以是不同的键。Culture 只控制格式化。

索引器按 Reflector 的默认成员规则读取，不按实参类型挑选重载。其 Getter 拒绝不足的参数，但可能忽略多余参数，不承诺严格数量匹配。当前入口没有数组下标的专用支持，可使用具有默认索引器的 List<T> 等对象。数组的 Length 等普通属性仍按成员规则访问。

缺键和越界遵循目标索引器：Dictionary 的缺键通常抛异常，Hashtable 的缺键可能成功返回 null。模板不额外调用 Contains，也不补充显式接口成员查找。成功返回 null 始终属于成功取值；需要沿 null 继续导航时才失败。

## 转义与格式边界

普通模板文本和字符串索引键支持同一组转义：

| 输入 | 输出 |
| --- | --- |
| `\\` | 反斜杠 |
| `\$` | 美元符 |
| `\s` | 空格 |
| `\n、\r、\t` | 换行、回车、Tab |
| `\'、\"` | 单引号、双引号 |

未知转义和末尾未完成的反斜杠报错，不支持额外的 Unicode 或十六进制转义。转义输出的字符不会重新作为语法扫描。`\${` 只转义美元符，不开启新的字面区域；例如 `\${name` 输出字面 `${name`，而 `\${${name}}` 仍会求值内层 name。Windows 路径中的反斜杠也需要按模板规则转义。

`#` 后格式字符串由 .NET 单值格式化设施解释，不作为复合格式模板。模板层只识别引号和反斜杠保护的右大括号，并去掉未受保护的首尾空白；内部空白、引号及反斜杠原样传递。格式为空或只剩未保护空白时报 EmptyFormat。格式化事件主动设置的 Format 直接交给 .NET，不再次按模板格式语法裁剪。

已取得的末端 null 不做 Core 预处理，由 .NET 格式化；成员链中途 null 导致 NullTarget。没有格式时同样沿用基础库默认行为。格式化后的 Text 为 null 时直接交给 .NET 字符串拼接。

## 选项与生命周期

TemplateEvaluator 构造函数可接收 TemplateEvaluatorOptions：

| 属性 | 默认值 | 作用 |
| --- | --- | --- |
| `Culture` | null | 可显式设置 CultureInfo；null 保持 .NET 默认文化区域。 |
| `Recursive` | false | 对取值及 Resolved 后得到的字符串执行子模板求值。 |
| `MaximumDepth` | 64 | 最大模板层数，必须为正整数，根模板为 1。 |

构造直接持有传入选项；省略或传 null 时每个评估器创建独立选项。Providers 和 Options 属性的引用固定，内容可在求值之外配置。求值及递归期间，调用方保持选项、集合和事件订阅稳定；动态来源负责自己的数据同步。没有配置快照或全局共享默认实例。

Recursive=false 时，取得的字符串按字面数据使用。启用 Recursive 后，每个字符串值都会进入下一模板层，包括没有占位符的普通字符串。动态索引变量的字符串也会先递归，再作为字符串索引参数使用；引号常量不递归。相邻引用不累积深度，同名变量允许再次查询，不使用全局已访问集合拒绝重复值；循环由深度上限终止。格式化回调的 Text 输出不递归。

每层先完整解析再执行。根模板 ${name}-${bad 在读取 name 前就失败；子模板只能在取得字符串后解析，之前的回调及来源副作用不回滚。索引语法嵌套不增加模板递归深度；内部另设 256 层索引语法保护。

## 四个事件

```text
Resolving → 提供器与导航 → Resolved → 可选递归
          → Formatting → .NET 格式化 → Formatted → 拼接
```

事件属于评估器实例，使用 EventHandler<T>，sender 为评估器。动态索引引用也触发 Resolving/Resolved，但没有自己的格式化事件；递归子模板中的插值有完整事件序列。

通知单位是完整引用：`${person.Home.Name}` 只有一组 Resolving/Resolved，不逐段或按提供器尝试次数通知。动态索引中的变量引用独立嵌套通知，常量不通知；重复出现的引用分别通知。两种上下文均继承 EventArgs，通过 internal 构造函数由评估器创建。

Resolving/Resolved 使用 TemplateEvaluator.ResolutionContext：只读 Template、Expression、Namespace、Name、Position、Length、Depth、IsIndex；Value 和 Handled 可写。

Formatting/Formatted 使用 TemplateEvaluator.FormattingContext：只读元数据相同（没有 IsIndex）；Value、Format、Culture、Text、Handled 可写。格式化上下文独立于取值上下文。

```csharp
evaluator.Resolving += (_, context) =>
{
	if(string.Equals(context.Expression, "person.Name", StringComparison.OrdinalIgnoreCase))
	{
		context.Value = "匿名";
		context.Handled = true;
	}
};

evaluator.Formatted += (_, context) =>
{
	// 仅按应用需求处理最终文本；不会重新触发模板求值。
	context.Text = context.Text?.Trim();
};
```

Resolving 设置 Handled=true 接管完整引用，也会跳过其动态索引参数的默认求值；Value 可以为 null，之后继续执行 Resolved、可选递归和格式化，模板仍须先通过语法校验。Formatting 的 Handled=true 使用 Text。前置事件按订阅顺序通知全部订阅者，再检查最终 Handled；任意回调抛异常立即终止。后置回调可改值或文本，但不会重执行已经结束的阶段。

只有对应阶段成功或被显式接管后才执行后置事件。失败不触发对应后置通知，也不返回部分文本。

## 错误处理

Evaluate 始终返回字符串，求值失败抛 TemplateEvaluationException。TryEvaluate 使用相同流程，成功返回 true、求值结果和 error=null；失败返回 false、result=null 和 error，不重执行模板：

```csharp
if(!evaluator.TryEvaluate("金额：${app:price#0.00}", out var result, out var error))
	Console.WriteLine($"{error.Code} @ {error.Position}: {error.Stage}");
```

异常提供 Code、Stage、Template、Expression、Position、Length、Depth 和 InnerException。位置从 0 开始，以 UTF-16 字符计数；递归保留真正失败的子模板信息，外层不覆盖失败位置。未闭合占位符指向 ${ 起点。底层来源、导航、转换、格式化及回调异常存入 InnerException。

| Code | 说明 |
| --- | --- |
| `InvalidSyntax、InvalidWhitespace、InvalidEscape` | 引用语法、空白或转义无效。 |
| `UnclosedPlaceholder、EmptyFormat` | 占位符未闭合或格式为空。 |
| `UnsupportedExpression、SyntaxDepthExceeded` | 预留计算入口或索引语法过深。 |
| `MissingVariable、NullTarget` | 变量未找到或空对象继续导航。 |
| `ProviderFailed、NavigationFailed` | 来源、成员/索引读取或参数绑定失败。 |
| `DepthExceeded` | 字符串模板递归超限。 |
| `FormattingFailed、CallbackFailed` | .NET 格式化或事件回调失败。 |

Stage 为 Parsing、Resolving、Resolution、Resolved、Recursion、Formatting、Format 或 Formatted。程序判断应依赖错误码、阶段和异常类型，避免依赖本地化 Message。

非法选项和提供器集合含 null 属于调用错误，TryEvaluate 仍抛参数异常。空 Span（包括 null 字符串转换所得）返回空字符串，无变量事件。

## 实现入口

- [TemplateEvaluator](../src/Text/Templating/TemplateEvaluator.cs)：公开入口、事件及执行顺序。
- [取值上下文](../src/Text/Templating/TemplateEvaluator.ResolutionContext.cs)、[格式化上下文](../src/Text/Templating/TemplateEvaluator.FormattingContext.cs)：归属于 TemplateEvaluator 的公开嵌套事件上下文。
- [模板解析](../src/Text/Templating/TemplateEvaluator.Parser.cs)：模板分区与基于现有 Lexer 的引用语法，内部节点均私有。
- [TokenScanner](../src/Expressions/TokenScanner.cs)：Span 扫描并保留源码位置及消耗长度；流按 UTF-8/BOM 解码，释放扫描器时关闭输入流。
- [Reflector](../src/Reflection/Reflector.cs)：模板直接使用既有 GetValue 读取成员与默认索引器，保留原始读取异常。
- [模板测试](../test/Text/Templating/TemplateEvaluatorTest.cs)、[词法测试](../test/Expressions/LexerBoundaryTest.cs)：通过公开入口验证语义。

[IExpressionEvaluator](../src/Expressions/IExpressionEvaluator.cs) 承担脚本求值契约。[MemberExpressionParser](../src/Reflection/Expressions/MemberExpressionParser.cs) 接受 Span，但其语法允许空白和方法，数值与转义规则也不同，没有模板命名空间和逐引用诊断；[MemberExpressionEvaluator](../src/Reflection/Expressions/MemberExpressionEvaluator.cs) 的默认索引流程没有填充动态参数，也不承担模板事件、提供器和字符串递归语义。模板直接复用 [Lexer](../src/Expressions/Lexer.cs) 与 Reflector，不增加语法树转换层，内部节点、Parser 及语法保护保持私有，不增加仅供测试使用的入口。数组、参数转换和接口访问等通用能力应由 Reflector 的既有入口扩展。

## 集成边界

通过 evaluator.Providers.Add(profile.ToVariables()) 显式注册 [Profile 变量视图](variables.zh-Hans.md#profile-变量视图)。来源异常在模板查询时包装为 ProviderFailed，并保留原始异常。

Profile 加载与保存保留原文，不自动展开变量。Profile、ProfileEntry 和 ProfileDirectiveContext 不提供 Evaluate() 扩展；调用方显式注册变量视图并使用 TemplateEvaluator 求值。ProfileEntry.Value 保留原始值，导入参数不执行模板评估，内部读取会话不公开。

deployer、packager、migrator、containerizer 等工具负责组织命令参数、环境变量、配置和业务来源，调用 TemplateEvaluator，并决定条件分支和产物的求值时机；评估器不内置这些工具规则。

计算表达式预留 `${=expression#format}` 位置，用于共享词法、变量来源和格式化能力；当前只识别入口并报告 UnsupportedExpression，不执行运算符、函数或条件表达式。
