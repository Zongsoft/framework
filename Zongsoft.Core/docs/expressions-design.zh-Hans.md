# 文本模板求值设计

本文说明 Zongsoft.Text.Templating 模板评估器的现行契约、实现边界和后续功能范围。使用示例见[文本模板](expressions.zh-Hans.md)。

## 职责与阶段

Zongsoft.Text.Templating 提供独立的模板解析和求值能力，不依赖 Profile 或具体工具。ITemplate、ITemplateFormatter、TemplateEvaluator 及其选项和诊断类型集中在此命名空间。IVariableProvider 与词法基础设施位于 Zongsoft.Expressions，成员访问设施位于 Zongsoft.Reflection。

| 层次 | 职责 |
| --- | --- |
| TemplateEvaluator | 模板分区、变量引用、路径遍历、事件、递归、格式化和诊断。 |
| IVariableProvider | 按名称与命名空间查询原始对象值，决定数据来源及自身同步策略。 |
| Lexer / TokenScanner / Tokenizer | 共享标识符、字符串、数字、布尔、null 和符号的词法规则。 |
| Reflector | 成员及默认索引器读取；模板直接遵守其现有规则。 |
| Profile 集成 | 后续以扩展方法提供显式求值，不改变现有加载和保存原文的行为。 |
| tools 集成 | 后续提供命令行、环境变量、配置等来源适配。 |

不提供兼容语法或独立的模板成员访问框架。

## 公开入口与输入生命周期

```csharp
using Zongsoft.Expressions;

namespace Zongsoft.Text.Templating;

public partial class TemplateEvaluator
{
	public TemplateEvaluator(TemplateEvaluatorOptions options = null);

	public IList<IVariableProvider> Providers { get; }
	public TemplateEvaluatorOptions Options { get; }

	public event EventHandler<ResolutionContext> Resolving;
	public event EventHandler<ResolutionContext> Resolved;
	public event EventHandler<FormattingContext> Formatting;
	public event EventHandler<FormattingContext> Formatted;

	public string Evaluate(ReadOnlySpan<char> text);
	public bool TryEvaluate(
		ReadOnlySpan<char> text,
		out string result,
		out TemplateEvaluationException error);
}
```

变量来源契约位于 Zongsoft.Expressions：

```csharp
namespace Zongsoft.Expressions;

public interface IVariableProvider
{
	bool TryGetValue(string name, out object value);
	bool TryGetValue(string @namespace, string name, out object value);
}
```

不带命名空间的重载只查询默认命名空间，语义等价于 TryGetValue(null, name, out value)。显式重载按命名空间、变量名称的顺序传参；指定命名空间查询失败时不回退到默认命名空间。模板评估器使用显式重载，未限定命名空间的引用传入 null。

字符串可以隐式转换后直接调用，也可以传入字符串切片、字符数组或 stackalloc 缓冲区。返回结果为 string。

空 Span、default 和 null 字符串转换所得均为空模板，返回空字符串，不触发变量事件。普通空白文本原样保留。非法选项和 Providers 中的 null 属于调用错误，不由 TryEvaluate 吞掉。

私有 Parser 与 TokenScanner 均为 ref struct，直接保存并扫描输入 Span；引用片段通过切片交给 Lexer，不创建片段字符串。节点复用标识符 Token 的字符串值；引用原文和格式等需要长期保存的文本各自物化。

Part、Accessor、Argument 和 Lexeme 统一使用 readonly struct，通过构造函数初始化 public readonly 字段，直接存入对应 List 的元素数组，不逐个分配节点对象，也不生成记录类型的相等运算符、解构等成员。Reference 保留 sealed class，在引用完整解析后通过构造函数一次性设置只读字段；仅在出现成员或索引访问时创建 Accessors 列表，无导航时为 null。集合由解析器构建后交给节点，求值阶段不再修改，不为只读语义额外复制数组或包装集合。

只有一个文本或占位符片段时，Part 列表容量设为 1，避免结构元素占用多余槽位；空模板不分配元素数组。该判断利用已经完成的解析，不预扫描模板。Parser 使用可变 ref struct 保存输入 Span 并维护解析游标。

每层在首次执行变量引用前保存一份源码字符串，供事件上下文和异常在调用结束后继续持有。解析失败时在创建异常时保存输入。成功的纯文本路径不为事件额外复制整份源码。词素对象、数值装箱、节点及最终字符串仍有分配。

Template 只包含传入的切片，Position 相对切片，以 UTF-16 代码单元计数。递归诊断指向实际失败的子模板。首个回调之后不再读取输入 Span，因此回调修改原字符数组不会改变已解析的节点或上下文原文。

## 词法契约与分配边界

```csharp
public interface ITokenizer
{
	TokenResult Tokenize(ReadOnlySpan<char> text);
}

public readonly struct TokenResult
{
	public readonly int Length;
	public readonly Token Token;
	public TokenResult(int length, Token token);
	public static TokenResult Fail();
}
```

Lexer.GetScanner(ReadOnlySpan<char>) 返回栈上的 TokenScanner，不复制输入。扫描器持有输入及当前位置，跳过空白后把同一剩余切片按注册顺序交给各分词器；匹配失败不推进，成功后按 Length 推进。Length 是消耗的 UTF-16 字符数，包含引号、转义及后缀。成功时必须大于零且不超过剩余长度，否则抛 InvalidOperationException。Fail() 返回默认结果，Token=null、Length=0。

Scan(out position, out length) 返回相对于输入切片的起始位置和源码长度。空输入有效；末尾返回 null，position 为输入长度、length 为零。扫描期间输入和分词器集合须保持稳定。TokenScanner 不实现 IEnumerable，不装箱；foreach 通过嵌套 Enumerator 的独立游标副本遍历，不推进原扫描器或释放输入流。

Stream 入口一次性以 UTF-8 和 BOM 检测解码，不要求流可寻址，随后复用 Span 扫描路径。扫描器 Dispose 关闭源流并使自身失效；初始读取失败同样关闭源流。调用方保持单一释放所有者，不复制持有流的扫描器来转移所有权。

| 分词器 | 分配策略 |
| --- | --- |
| LiteralTokenizerBase | Span 前缀比较，选择最长匹配；单词校验标识符边界。CreateToken 直接接收配置中的字面量及其大小写，不生成匹配用临时字符串。 |
| Boolean / Null / Symbol | 复用已有 Token。 |
| Keyword | 创建 Token，Value 复用配置中的字符串。 |
| Number | 数字解析直接接收 Span；只保留 Token 和数值装箱。 |
| Identifier | 扫描完成后仅创建最终字符串值。 |
| String | 无转义时直接复制内容；有转义时先校验并计算解码长度，短文本使用栈缓冲区，长文本租用数组，最终创建一个字符串；归还数组时清空内容。 |

## 模板语法

统一使用 `${...}`。普通文本中的 %name% 和 $(name) 没有变量展开含义。

| 语法 | 含义 |
| --- | --- |
| `${name}` | 默认命名空间中的根变量。 |
| `${app.runtime:name}` | 指定命名空间中的根变量。 |
| `${person.Home.Address}` | 逐段成员导航。 |
| `${items[0].Name}` | 索引之后继续导航。 |
| `${items[indices[0]].Name}` | 嵌套动态索引，示例中的 items、indices 可为列表。 |
| `${matrix[row,column]}` | 多参数索引器。 |
| `${map['key']}`、`${map["key"]}` | 字符串常量键。 |
| `${price#0.00}` | .NET 单值格式化。 |
| `${=expression}` | 预留计算入口，当前以 UnsupportedExpression 失败。 |

名称各段采用 ASCII 标识符：[A-Za-z_][A-Za-z0-9_]*。命名空间可包含点，冒号只用于命名空间与根变量的分隔。

提供器按 OrdinalIgnoreCase 比较变量名称和命名空间；成员比较采用 Reflector 的 IgnoreCase；字典键采用容器自己的比较器。

引用中不允许语法空白，`${ name }`、`${items[ index ]}` 都失败。引号内的空白是字符串数据。方法调用、算术和条件表达式不属于当前引用语法。

未闭合的占位符报错，指向其开始的 `${`。每层先完整解析，再进行该层的任何来源查询或回调；因此 `${name}-${bad` 不会先读取 name。递归子模板只有取得字符串后才能解析，已发生的外部副作用不回滚。

索引语法嵌套与字符串递归是两套深度：索引语法设有内部 256 层限制，不消耗 MaximumDepth。

## 索引常量和动态参数

常量直接作为对象值，不查询来源，不触发变量事件。

| 常量 | CLR 类型与规则 |
| --- | --- |
| 无后缀整数 | 能容纳时为 int，否则为 long；超出 long 范围失败。 |
| L / l 后缀整数 | long。 |
| 无后缀小数、d / D | double。 |
| f / F | float。 |
| m / M | decimal。 |
| true、false | bool，不区分大小写。 |
| null | null，不区分大小写。 |
| 单引号或双引号字符串 | string，使用同一转义规则。 |

支持负数，不把负索引改成从末尾计数。数值词法使用固定小数点，不随 Culture 改变；当前不支持科学计数法、十六进制或前置加号。数字后缀、范围和非法数值边界统一由既有 Lexer 处理。

true、false、null 只在完整的裸索引参数位置表示常量。`${true}` 是变量引用，`${items[true.Name]}` 中 true 也是变量。关键字前缀不能截断标识符。

动态参数使用与外层相同的提供器集合，但独立确定命名空间。例如 `${app:items[index]}` 的 index 从默认命名空间查询，不从 items 对象或 app 空间查询。参数也支持自己的命名空间及成员导航。

引号中的字符串不插值，`${map['${name}']}` 使用字面键 `${name}`。动态参数取得字符串时，按 Recursive 选项处理后再传给索引器；展开结果仍是字符串。

## 成员与索引执行

每个导航步骤调用：

```csharp
value = Reflection.Reflector.GetValue(ref value, accessor.Name, arguments);
```

普通成员传名称，索引传空名称。索引参数先完成自身的变量查询、Resolved 和可选递归，再以原始类型传入 Reflector。

| 方面 | 执行规则 |
| --- | --- |
| 成员范围 | Reflector 以 Public、Instance、Static、IgnoreCase 查找字段和属性；模板不增加独立的实例或 getter 可见性筛选。 |
| Type 目标 | 表示 Reflector 要查找的类型，不另按普通 Type 实例处理。 |
| 多个候选 | 使用 Reflector 返回的首项，不另做最佳重载和歧义判定。不能将反射枚举顺序当成稳定的重载优先级。 |
| 默认索引器 | 空名称调用 GetDefaultMembers，使用首项。 |
| 参数类型 | 不调用 Common.Convert；Getter 按其参数类型拆箱或转换引用。 |
| 参数数量 | GetValue 的 Getter 拒绝不足的参数，可能忽略多余参数；模板不增加严格数量检查。 |
| 显式接口 | 不补充接口契约查找。 |
| 数组 | 当前 Reflector 没有专门的数组下标处理；Length 等普通属性仍走成员读取。 |
| 缺键及越界 | 由实际索引器决定，模板不额外检查 Contains。 |

例如 Dictionary<long, string> 应使用 [1L]，不能依赖 [1] 或 ['1'] 自动转成 long。null 参数也不会自动变成 int 的 0。object 参数保留原值，因此 int 1 与 long 1 可以是不同的键。

Dictionary 缺键通常抛异常，Hashtable 缺键可以成功返回 null。成功取得的末端 null 是成功值；只有继续沿 null 导航时才报 NullTarget。

使用 GetValue 可以保留原始 Getter 异常。当前 TryGetValue 的属性路径会捕获读取异常并返回 false，不适合这里的错误原因保留需求。模板将实际读取失败包装为 NavigationFailed。

数组、参数转换、接口访问等能力若需完善，应扩展 Reflector 的既有通用入口。

## 来源、选项与状态

Providers 按注册顺序查询，首个返回 true 的提供器获胜，包括 true + null。false 才继续下一个；所有提供器返回 false 则报 MissingVariable。异常立即终止，不伪装为 false。命名空间精确查询，不回退；默认命名空间为 null。

每次引用重新查询根变量并执行 getter、索引器，不缓存根变量、getter 返回值或完整路径结果。Reflection 可以缓存 Getter 委托，来源也可自行提供稳定数据或缓存。

| 选项 | 默认值 | 语义 |
| --- | --- | --- |
| Culture | null | .NET 格式化使用默认文化区域；可显式指定 CultureInfo，不预先快照。 |
| Recursive | false | 是否将解析后的字符串作为子模板求值。 |
| MaximumDepth | 64 | 正整数，根模板计一层。 |

构造直接持有传入的选项；省略或 null 时创建独立选项。Providers 和 Options 引用只读，其内容允许在求值之外修改。求值及自动递归期间，调用方保持集合、选项及订阅稳定，提供器管理自身数据同步。没有调用级配置快照或共享默认选项。

## 执行顺序与事件

```text
完整解析当前模板
  → Resolving
  → 提供器查询与成员导航
  → Resolved
  → 可选的字符串递归
  → Formatting
  → .NET 单值格式化
  → Formatted
  → 拼接结果
```

四个事件属于评估器实例，使用 EventHandler<T>，sender 为评估器。

通知单位是完整引用：person.Home.Name 只有一组 Resolving / Resolved，不逐段通知，也不对各个提供器尝试发通知。动态索引中的变量引用具有独立的嵌套通知；常量没有。重复出现的引用分别通知。

ResolutionContext 与 FormattingContext 是 TemplateEvaluator 的公开嵌套类，分别在 TemplateEvaluator.ResolutionContext.cs 和 TemplateEvaluator.FormattingContext.cs 中定义。它们继承 EventArgs，构造函数保持 internal，由求值器创建；两种阶段保持独立上下文，通过构造函数传递元数据。

TemplateEvaluator.ResolutionContext 的只读属性为 Template、Expression、Namespace、Name、Position、Length、Depth、IsIndex，可写属性为 Value、Handled。

TemplateEvaluator.FormattingContext 保留同类只读元数据（没有 IsIndex），可写属性为 Value、Format、Culture、Text、Handled。格式化上下文独立于取值上下文。

Resolving 设置 Handled=true 时接管整个引用，包括其动态参数的默认求值；Value 可以是 null。接管仍要求语法有效，之后执行 Resolved、递归及格式化。

Formatting 设置 Handled=true 时使用 Text。所有前置订阅者按顺序执行，再检查最终 Handled；任何订阅者抛异常立即终止。后置回调可以改值或文本，但不重执行已经完成的步骤。

后置通知只在对应阶段成功或显式接管后触发。失败不返回部分文本，不把后置通知当作 finally。

动态索引引用本身没有格式化事件。若动态字符串触发子模板求值，子模板中的引用具有自己的完整事件序列。

## 字符串递归

Recursive=false 时，已取得的字符串按字面数据使用。Recursive=true 时，在 Resolved 后将每个字符串作为子模板求值，包括没有占位符的普通字符串。

同名引用可以再次查询，不使用全局已访问集合拒绝重复值；过长递归及循环由 MaximumDepth 终止。相邻引用不累计深度。

动态字符串参数也遵守递归选项，所得字符串直接作为索引参数，不自动变成数字。格式化回调产生的 Text 不递归。

解析、来源或回调已经发生的外部副作用不回滚。递归错误保留实际失败的子模板，不被外层重新标记为根模板错误。

## 转义与格式

普通模板文本与字符串索引键支持：

| 转义 | 内容 |
| --- | --- |
| `\\` | 反斜杠。 |
| `\$` | 美元符。 |
| `\s` | 空格。 |
| `\n`、`\r`、`\t` | 换行、回车、Tab。 |
| `\'`、`\"` | 单引号、双引号。 |

未知或不完整转义失败；不支持额外的 Unicode 或十六进制转义写法。转义后的字符作为字面数据追加，不再次解释为插值。

反斜杠只转义下一个字符，不建立新的字面区域。例如 `\${name` 可输出字面 `${name`；后面新出现的未转义占位符仍正常解析。Windows 路径中的反斜杠需要按模板规则转义。

`#` 开始格式字符串。格式中引号及反斜杠可保护右大括号，只有未保护的右大括号结束占位符。模板只裁剪首尾未保护的空白，保留内部空白、引号和反斜杠并交给 .NET；格式为空时报 EmptyFormat。

实际格式化复用 .NET 单值插值设施，不把格式字符串当成复合格式模板。最终 null 直接交给 .NET 处理，不提前变为空字符串。事件直接指定的 Format 不再按模板源码规则裁剪。Text 为 null 时直接参与 .NET 拼接。

## 诊断

Evaluate 求值失败抛 TemplateEvaluationException。TryEvaluate 使用相同流程，成功时 error=null，失败时返回 false、result=null 和该异常，不重新执行模板。

异常提供 Code、Stage、Template、Expression、Position、Length、Depth、InnerException。程序判断依赖错误码、阶段和异常类型，不依赖本地化 Message。

| Code | 含义 |
| --- | --- |
| InvalidSyntax、InvalidWhitespace、InvalidEscape | 语法、空白或转义失败。 |
| UnclosedPlaceholder、EmptyFormat | 未闭合占位符或空格式。 |
| UnsupportedExpression、SyntaxDepthExceeded | 预留计算入口或语法嵌套超限。 |
| MissingVariable、NullTarget | 根变量不存在或沿 null 继续导航。 |
| ProviderFailed、NavigationFailed | 来源或 Reflector 读取失败。 |
| DepthExceeded | 字符串递归深度超限。 |
| FormattingFailed、CallbackFailed | 格式化或回调异常。 |

Stage 区分 Parsing、Resolving、Resolution、Resolved、Recursion、Formatting、Format、Formatted。来源、getter 和回调等实际抛出的异常作为 InnerException 保留。

## 现有设施的复用边界

[MemberExpressionParser](../src/Reflection/Expressions/MemberExpressionParser.cs) 已接受 Span，但其语法允许空白和方法，没有模板命名空间，数值与转义规则也不同，不提供模板要求的逐引用源码信息。

[MemberExpressionEvaluator](../src/Reflection/Expressions/MemberExpressionEvaluator.cs) 的默认索引流程未填充动态参数，也没有提供器、逐引用事件和字符串递归语义。模板直接复用 [Lexer](../src/Expressions/Lexer.cs) 与 [Reflector](../src/Reflection/Reflector.cs)，不增加语法树转换层，不改变这些成员表达式入口。

内部模板节点、Parser 及语法保护均保持私有，不为测试扩大生产可见性。[IExpressionEvaluator](../src/Expressions/IExpressionEvaluator.cs) 继续承担脚本求值契约。

## 后续功能边界

Profile 的变量环境入口尚需设计。预期由扩展方法提供 Profile、ProfileEntry 和 ProfileDirectiveContext 的显式求值，保持 Value 与保存原文；导入参数的求值时机、分词及重新加载行为单独确定，不公开内部读取会话。

工具集成覆盖 deployer、packager、migrator、containerizer，负责命令参数、环境变量、配置和业务来源组织，迁移调用与模板语法，并明确条件分支和产物的求值时机。

未来计算表达式使用 `${=expression#format}` 的语法位置，共享词法、变量来源和格式化能力。当前只识别该入口并报告不支持，不实现运算符、函数或条件表达式。
