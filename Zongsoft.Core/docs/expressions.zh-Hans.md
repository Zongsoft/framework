# 表达式模板

[English](expressions.md) | [简体中文](expressions.zh-Hans.md)

Zongsoft.Expressions.TemplateEvaluator 把带有变量引用的模板转换成字符串。它支持可扩展变量来源、命名空间、成员导航、动态索引、格式化和可选的字符串递归展开。

本次实现第一阶段 Expressions。Profile 的扩展方法和工具变量来源适配属于后续阶段；Profile 加载及保存仍保留原文。完整决策与实施记录见[设计文档](expressions-design.zh-Hans.md)。

## 开始使用

以下示例为应用提供一个忽略名称大小写的变量来源：

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using Zongsoft.Expressions;

var evaluator = new TemplateEvaluator(new()
{
	Culture = CultureInfo.InvariantCulture,
});
evaluator.Providers.Add(new Variables());

var text = evaluator.Evaluate("你好，${name}，金额 ${app:price#0.00}");
// 你好，Zongsoft，金额 12.50

sealed class Variables : IVariableProvider
{
	private readonly Dictionary<string, object> _values = new(StringComparer.OrdinalIgnoreCase)
	{
		["name"] = "Zongsoft",
		["app:price"] = 12.5m,
	};

	public bool TryGetValue(string name, string @namespace, out object value) =>
		_values.TryGetValue(@namespace == null ? name : @namespace + ":" + name, out value);
}
```

Providers 按注册顺序查询，第一个返回 true 的来源获胜，包括值为 null 的情况；返回 false 才继续查询。提供器负责按 OrdinalIgnoreCase 比较名称和命名空间。默认命名空间为 null，指定命名空间不会自动回退到默认空间。来源异常立即终止求值。

评估器不缓存变量值、getter 或索引结果。相同引用出现两次就执行两次取值，因此动态提供器可以返回不同结果。

## 语法

| 模板 | 含义 |
| --- | --- |
| `${name}` | 默认命名空间变量。 |
| `${app.runtime:name}` | 多级命名空间中的变量。 |
| `${person.Home.Address}` | 公共实例属性或字段导航。 |
| `${arr[0].Name}` | 常量索引后继续导航。 |
| `${arr[indices[0]].Name}` | 动态参数与嵌套索引。 |
| `${grid[row,column]}` | 多维数组或多参数索引器。 |
| `${map['key']}、${map["key"]}` | 单引号或双引号字符串键。 |
| `${price#0.00}` | .NET 数值格式。 |
| `${date#yyyy-MM-dd HH:mm:ss}` | .NET 日期格式，内部空白保留。 |
| `\${name}` | 输出字面文本 ${name}。 |
| `${=expression}` | 为未来轻量表达式保留；当前报 UnsupportedExpression。 |

变量、成员、命名空间各段使用 ASCII 标识符规则：[A-Za-z_][A-Za-z0-9_]*。成员忽略大小写；字典键按目标字典自己的比较规则处理。引用内不允许语法空白，${ name }、${arr[ index ]} 都是错误。引号中的键空白属于数据。

动态索引参数从提供器环境取值，不从被索引对象取值，也不继承外层命名空间。例如 ${app:arr[index]} 中 index 来自默认命名空间。

只读公共实例字段、属性、索引器及公共接口契约。静态成员、不可读属性、非公共成员、方法调用和算术运算不在本阶段支持范围内。Type 值仍视为普通对象，不转为其所表示类型的静态访问。成员大小写匹配后有多个同等目标时报告歧义。

## 索引常量和参数绑定

无后缀整数优先 int，超出 int 范围时使用 long；支持负数以及大小写 L 后缀。小数默认 double，支持 f/F、d/D、m/M 后缀。数字使用固定小数点，不随 Culture 改变；当前不支持科学计数法、十六进制或前置加号。

true、false、null 忽略大小写，仅在完整裸索引参数中作为常量；${true} 是变量引用，${arr[true.Name]} 中 true 也是变量。字符串索引键不插值。

数组、列表、字典及自定义索引器均在参数完成取值、Resolved 回调及可选递归之后，使用 Zongsoft.Common.Convert.ConvertValue(value, targetType) 绑定。沿用该转换器的 null 默认值语义，例如 null 转成 int 为 0。Culture 只控制格式化，不额外影响参数转换。

索引器先匹配参数个数，再选精确类型或唯一最具体的可赋值签名，最后才使用需要转换的候选；歧义时报错，不调用多个 getter 试探。可确认的缺键、越界和不可读取均失败；自定义 getter 成功返回 null 则是成功取值。

## 转义与格式边界

普通模板文本和字符串索引键支持同一组转义：

| 输入 | 输出 |
| --- | --- |
| `\\` | 反斜杠 |
| `\$` | 美元符 |
| `\n、\r、\t` | 换行、回车、Tab |
| `\'、\"` | 单引号、双引号 |

未知转义和末尾未完成的反斜杠报错。转义输出的字符不会重新作为语法扫描。\${ 只转义美元符，不开启新的字面区域；例如 \${${name}} 仍会求值内层 name。

# 后格式字符串由 .NET 格式化设施解释。模板层只识别引号和反斜杠保护的右大括号，并去掉未受保护的首尾空白；内部空白、引号及反斜杠原样传递。格式为空或只剩未保护空白时报 EmptyFormat。格式化事件主动设置的 Format 直接交给 .NET，不再次按模板格式语法裁剪。

已取得的末端 null 不做 Core 预处理，由 .NET 格式化；成员链中途 null 导致 NullTarget。没有格式时同样沿用基础库默认行为。

## 选项与生命周期

TemplateEvaluator 构造函数可接收 TemplateEvaluatorOptions：

| 属性 | 默认值 | 作用 |
| --- | --- | --- |
| `Culture` | null | 可显式设置 CultureInfo；null 保持 .NET 默认文化区域。 |
| `Recursive` | false | 对取值及 Resolved 后得到的字符串执行子模板求值。 |
| `MaximumDepth` | 64 | 最大模板层数，必须为正整数，根模板为 1。 |

构造直接持有传入选项；省略或传 null 时每个评估器创建独立选项。Providers 和 Options 属性的引用固定，内容可在求值之外配置。求值及递归期间，调用方保持选项、集合和事件订阅稳定；动态来源负责自己的数据同步。没有配置快照或全局共享默认实例。

启用 Recursive 后，每个字符串值都会进入下一模板层，包括没有占位符的普通字符串。动态索引变量的字符串也会先递归再转换；引号常量不递归。相邻引用不累积深度，同名变量允许再次查询；循环由深度上限终止。格式化回调的 Text 输出不递归。

每层先完整解析再执行。根模板 ${name}-${bad 在读取 name 前就失败；子模板只能在取得字符串后解析，之前的回调及来源副作用不回滚。索引语法嵌套不增加模板递归深度；内部另设 256 层索引语法保护。

## 四个事件

```text
Resolving → 提供器与导航 → Resolved → 可选递归
          → Formatting → .NET 格式化 → Formatted → 拼接
```

事件属于评估器实例，使用 EventHandler<T>，sender 为评估器。动态索引引用也触发 Resolving/Resolved，但没有自己的格式化事件；递归子模板中的插值有完整事件序列。

Resolving/Resolved 使用 VariableEvaluationContext：只读 Template、Expression、Namespace、Name、Position、Length、Depth、IsIndex；Value 和 Handled 可写。

Formatting/Formatted 使用 VariableFormattingContext：只读元数据相同（没有 IsIndex）；Value、Format、Culture、Text、Handled 可写。格式化上下文独立于取值上下文。

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

Resolving 接管的是完整引用，也会跳过其动态索引参数的默认求值；模板仍须先通过语法校验。Formatting 的 Handled=true 使用 Text。前置事件按订阅顺序通知全部订阅者，再检查最终 Handled；任意回调抛异常立即终止。后置回调可改值或文本，但不会重执行已经结束的阶段。

只有对应阶段成功或被显式接管后才执行后置事件。失败不触发对应后置通知，也不返回部分文本。

## 错误处理

Evaluate 始终返回字符串，求值失败抛 TemplateEvaluationException。TryEvaluate 使用相同流程，失败返回 false、result=null 和 error，不重执行模板：

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

null 模板输入、非法选项和提供器集合含 null 属于调用错误，TryEvaluate 仍抛参数异常。空模板返回空字符串，无变量事件。

## 实现入口

- [TemplateEvaluator](../src/Expressions/TemplateEvaluator.cs)：公开入口、事件及执行顺序。
- [模板解析](../src/Expressions/TemplateEvaluator.Parser.cs)：模板分区与基于现有 Lexer 的引用语法，内部节点均私有。
- [TokenScanner](../src/Expressions/TokenScanner.cs)：字符串及流统一分词，Scan(out position, out length) 保留源码位置；流按 UTF-8/BOM 读入字符缓冲，释放扫描器时关闭输入流。
- [MemberAccess](../src/Reflection/MemberAccess.cs)：内部公共实例契约筛选和 ConvertValue 参数绑定；实际读取复用 Reflector。
- [模板测试](../test/Expressions/TemplateEvaluatorTest.cs)、[词法测试](../test/Expressions/LexerBoundaryTest.cs)：通过公开入口验证语义。

已有 IExpressionEvaluator 继续承担脚本求值契约，已有 MemberExpression 入口继续服务原有消费者。模板使用共享词法设施与 Reflection 读取能力，不公开内部模板解析节点、会话或仅供测试使用的入口。
