# Expression templates

[English](expressions.md) | [简体中文](expressions.zh-Hans.md)

Zongsoft.Expressions.TemplateEvaluator evaluates text templates with extensible variable providers, namespaces, member navigation, dynamic indices, formatting, and optional recursive expansion.

This is phase 1, Expressions. Profile extensions and tools adapters belong to later phases. Profile still loads and saves original text. The [design record](expressions-design.zh-Hans.md) tracks all decisions and implementation progress.

## Getting started

An application can supply a case-insensitive provider:

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

var text = evaluator.Evaluate("Hello ${name}, total ${app:price#0.00}");
// Hello Zongsoft, total 12.50

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

Providers are queried in registration order. The first true result wins, including a null value. False continues to the next provider; exceptions terminate evaluation. Providers must compare names and namespaces using OrdinalIgnoreCase. The default namespace is null. An explicit namespace never falls back to the default namespace.

Variable values, getters and index results are not cached. Repeated references perform repeated reads and may observe changing provider data.

## Syntax

| Template | Meaning |
| --- | --- |
| `${name}` | A variable in the default namespace. |
| `${app.runtime:name}` | A variable in a dotted namespace. |
| `${person.Home.Address}` | Public instance property or field navigation. |
| `${arr[0].Name}` | Navigation after a constant index. |
| `${arr[indices[0]].Name}` | A dynamic argument with nested indexing. |
| `${grid[row,column]}` | A multidimensional array or multi-parameter indexer. |
| `${map['key']}, ${map["key"]}` | Single- or double-quoted string keys. |
| `${price#0.00}` | A .NET number format. |
| `${date#yyyy-MM-dd HH:mm:ss}` | A .NET date format with internal whitespace. |
| `\${name}` | Literal text ${name}. |
| `${=expression}` | Reserved for future computation; currently fails with UnsupportedExpression. |

Variable, member and namespace segments follow the ASCII rule [A-Za-z_][A-Za-z0-9_]*. Member names are case-insensitive; dictionary keys follow the target dictionary's comparer. References cannot contain syntactic whitespace: ${ name } and ${arr[ index ]} are invalid. Spaces inside quoted keys are data.

Dynamic arguments resolve against providers, independently of the indexed object and its namespace. In ${app:arr[index]}, index is in the default namespace.

Navigation reads public instance fields, readable properties, indexers, and public interface contracts. Static or non-public members, write-only properties, method calls and arithmetic are unsupported. A Type value is an ordinary object, not an instruction to access static members of the represented type. Equally valid case-insensitive member matches fail as ambiguous.

## Index constants and binding

Unsuffixed integers use int when possible, otherwise long. Negative values and L/l suffixes are supported. Fractional numbers default to double; f/F, d/D and m/M select float, double and decimal. Numeric literals use a fixed decimal point independently of Culture. Scientific notation, hexadecimal and leading plus signs are currently unsupported.

Case-insensitive true, false and null are constants only as complete bare index arguments. ${true} is a variable; true in ${arr[true.Name]} is also a variable. Quoted string arguments never interpolate.

After resolving an argument, running Resolved and optionally expanding a string, binding calls Zongsoft.Common.Convert.ConvertValue(value, targetType). Arrays, lists, dictionaries and custom indexers share this conversion behavior, including target defaults for null: null converts to int zero. Culture controls formatting and does not override conversion culture.

Indexer selection first requires the correct argument count, then prefers exact types or the unique most-specific assignable signature before conversion candidates. Ambiguity fails instead of trying multiple getters. Known missing keys, invalid bounds or unreadable targets fail; a custom getter returning null succeeds.

## Escaping and format boundaries

Template text and quoted index keys share these escapes:

| Input | Character |
| --- | --- |
| `\\` | Backslash |
| `\$` | Dollar |
| `\n, \r, \t` | Newline, carriage return, tab |
| `\', \"` | Single quote, double quote |

Unknown or incomplete escapes fail. Decoded characters are never rescanned as syntax. Escaping \${ only escapes the dollar, not an entire literal region: \${${name}} still evaluates the inner name.

.NET interprets the format after #. The template parser recognizes quotes and backslashes protecting a closing brace and trims only unprotected leading/trailing whitespace. Internal whitespace, quotes and backslashes pass through unchanged. Empty formats fail with EmptyFormat. A Format value assigned by an event passes directly to .NET without source-syntax trimming.

A successfully resolved terminal null goes directly to .NET formatting. Navigating through an intermediate null fails with NullTarget. Values without explicit formats also use .NET defaults.

## Options and lifetime

The constructor accepts optional TemplateEvaluatorOptions:

| Property | Default | Meaning |
| --- | --- | --- |
| `Culture` | null | Optional CultureInfo; null uses .NET defaults. |
| `Recursive` | false | Evaluate strings returned by resolution and Resolved as child templates. |
| `MaximumDepth` | 64 | Positive maximum template depth; the root is depth 1. |

The evaluator retains the supplied options instance. Omitted/null options create a separate instance for each evaluator. Providers and Options are fixed references with configurable contents. Keep options, collection membership and event subscriptions stable throughout evaluation, including recursion. Providers manage synchronization of their own data. There are no configuration snapshots or shared default options.

With Recursive enabled, every resolved string enters the next template depth, including plain strings without placeholders. Dynamic string index arguments expand before binding; quoted constants do not. Siblings do not accumulate depth, and repeated variable names may be queried again. Cycles end at MaximumDepth. Formatting callback Text is never recursively evaluated.

Each template layer is fully parsed before execution. Thus ${name}-${bad fails before querying name. Child templates can only be parsed after obtaining their strings; earlier side effects are not rolled back. Nested index syntax does not consume template depth and has a separate internal limit of 256 levels.

## Events

```text
Resolving → providers/navigation → Resolved → optional recursion
          → Formatting → .NET formatting → Formatted → append
```

All four events belong to the evaluator and use EventHandler<T>; sender is the evaluator. Dynamic index references have Resolving/Resolved events, but no direct formatting events. References inside recursive child templates have their own complete event sequence.

VariableEvaluationContext exposes read-only Template, Expression, Namespace, Name, Position, Length, Depth and IsIndex, plus writable Value and Handled.

VariableFormattingContext has the same read-only metadata except IsIndex, plus writable Value, Format, Culture, Text and Handled. Resolution and formatting use separate contexts.

```csharp
evaluator.Resolving += (_, context) =>
{
	if(string.Equals(context.Expression, "person.Name", StringComparison.OrdinalIgnoreCase))
	{
		context.Value = "Anonymous";
		context.Handled = true;
	}
};

evaluator.Formatted += (_, context) =>
{
	// Application-specific final text processing; no further template expansion.
	context.Text = context.Text?.Trim();
};
```

Resolving takes over the complete reference, skipping providers, navigation and dynamic arguments; source syntax must still be valid. Formatting with Handled=true uses Text. Every before-event subscriber runs in subscription order before the final Handled value is checked. Any callback exception stops evaluation immediately.

After-events may replace the resolved value or formatted text but never repeat completed stages. They run only after successful or explicitly handled corresponding stages. Failures do not return partial output.

## Errors

Evaluate always returns a string or throws TemplateEvaluationException. TryEvaluate runs the same pipeline once; failure returns false with result=null and an error:

```csharp
if(!evaluator.TryEvaluate("Total: ${app:price#0.00}", out var result, out var error))
	Console.WriteLine($"{error.Code} @ {error.Position}: {error.Stage}");
```

Errors expose Code, Stage, Template, Expression, Position, Length, Depth and InnerException. Positions use zero-based UTF-16 indices in the failing layer. Recursive failures retain the actual child template and depth. Unclosed placeholders point to their opening ${. Provider, navigation, conversion, formatting and callback exceptions are retained as InnerException.

| Code | Meaning |
| --- | --- |
| `InvalidSyntax, InvalidWhitespace, InvalidEscape` | Invalid reference syntax, whitespace or escape. |
| `UnclosedPlaceholder, EmptyFormat` | Missing closing brace or empty format. |
| `UnsupportedExpression, SyntaxDepthExceeded` | Reserved computation or excessively nested index syntax. |
| `MissingVariable, NullTarget` | Undefined variable or navigation through null. |
| `ProviderFailed, NavigationFailed` | Provider, member/index access or argument binding failure. |
| `DepthExceeded` | Recursive template depth exceeded. |
| `FormattingFailed, CallbackFailed` | .NET formatting or event failure. |

Stage is Parsing, Resolving, Resolution, Resolved, Recursion, Formatting, Format or Formatted. Use codes, stages and exception types for programmatic handling, rather than localized Message text.

Null input, invalid options and null provider entries are API errors; TryEvaluate still throws argument exceptions for them. An empty template returns an empty string without variable events.

## Implementation

- [TemplateEvaluator](../src/Expressions/TemplateEvaluator.cs): public entry points, events and execution.
- [Template parser](../src/Expressions/TemplateEvaluator.Parser.cs): template regions and reference grammar consuming the existing Lexer; all syntax nodes remain private.
- [TokenScanner](../src/Expressions/TokenScanner.cs): shared string/stream tokenization; Scan(out position, out length) reports source spans. Streams are decoded into a character buffer using UTF-8/BOM detection and closed when the scanner is disposed.
- [MemberAccess](../src/Reflection/MemberAccess.cs): internal public-instance contract selection and ConvertValue binding, with actual reads delegated to Reflector.
- [Template tests](../test/Expressions/TemplateEvaluatorTest.cs) and [lexer tests](../test/Expressions/LexerBoundaryTest.cs): verification through public behavior.

IExpressionEvaluator remains the script evaluation contract; existing MemberExpression APIs continue serving their consumers. Templates reuse the lexer and Reflection access layer without exposing parser nodes, sessions or test-only entry points.
