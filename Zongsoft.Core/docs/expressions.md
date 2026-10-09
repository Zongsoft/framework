# Text templates

[English](expressions.md) | [简体中文](expressions.zh-Hans.md)

Zongsoft.Text.Templating.TemplateEvaluator evaluates text templates with extensible variable providers, namespaces, member navigation, dynamic indices, formatting, and optional recursive expansion.

Zongsoft.Text.Templating provides standalone template evaluation without depending on Profile or specific tools. This document covers usage, public contracts and implementation boundaries.

Template contracts (ITemplate and ITemplateRenderer), the evaluator, options and diagnostics belong to Zongsoft.Text.Templating. IVariableProvider, the in-memory Variables dictionary and the lexical infrastructure belong to Zongsoft.Expressions. ResolutionContext and FormattingContext are public nested classes of TemplateEvaluator; they describe reference resolution and interpolation formatting respectively.

| Component | Responsibility |
| --- | --- |
| TemplateEvaluator | Template regions, variable references, member navigation, events, recursion, formatting and diagnostics. |
| IVariableProvider | Raw variable lookup, data sources and their synchronization policies. |
| Variables | Variable storage in a case-insensitive in-memory dictionary. |
| Lexer / TokenScanner / Tokenizer | Shared lexical rules and scanning. |
| Reflector | Member and default-indexer access; templates follow its existing rules. |

## Getting started

An application can use the built-in case-insensitive Variables provider:

```csharp
using System;
using System.Globalization;
using Zongsoft.Expressions;
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

var text = evaluator.Evaluate("Hello ${name}, total ${app:price#0.00}");
// Hello Zongsoft, total 12.50

variables["APP:PRICE"] = 20m;
var updated = evaluator.Evaluate("Total ${app:price#0.00}"); // Total 20.00
```

## Public API

TemplateEvaluator exposes the following main members, with separate contexts for resolution and formatting events:

| Member signature | Purpose |
| --- | --- |
| `TemplateEvaluator(TemplateEvaluatorOptions options = null)` | Create an evaluator; option ownership is described under Options and lifetime. |
| `IList<IVariableProvider> Providers { get; }` | Variable providers ordered by priority. |
| `TemplateEvaluatorOptions Options { get; }` | Options used by this evaluator. |
| `event EventHandler<ResolutionContext> Resolving / Resolved` | Notifications before and after resolving a complete reference. |
| `event EventHandler<FormattingContext> Formatting / Formatted` | Notifications before and after formatting an interpolation. |
| `string Evaluate(ReadOnlySpan<char> text)` | Evaluate and return text, throwing on evaluation failure. |
| `bool TryEvaluate(ReadOnlySpan<char> text, out string result, out TemplateEvaluationException error)` | Use the same evaluation pipeline and return its success status and error. |

The variable provider contract belongs to Zongsoft.Expressions:

```csharp
public interface IVariableProvider
{
	bool TryGetValue(string name, out object value);
	bool TryGetValue(string @namespace, string name, out object value);
}
```

TryGetValue(name, out value) queries only the default namespace and is equivalent to both TryGetValue(null, name, out value) and TryGetValue(string.Empty, name, out value). The explicit overload takes the namespace first, followed by the variable name; all variable providers must treat null and an empty string as an unspecified namespace. The template evaluator uses this overload, passing null for unqualified references.

Providers are queried in registration order. The first true result wins, including a null value. False continues to the next provider; exceptions terminate evaluation. Providers must compare names and namespaces using OrdinalIgnoreCase. Null and an empty string both select the default namespace. A non-empty namespace never falls back to the default namespace.

Variable values, getter return values and index results are not cached. Reflection may reuse getter delegates; repeated references still perform repeated reads and may observe changing provider data.

## In-memory variables

Zongsoft.Expressions.Variables inherits Dictionary<string, object> and implements IVariableProvider. Its comparer is always StringComparer.OrdinalIgnoreCase. Indexers, collection initializers, Add, TryAdd, Remove, Clear and enumeration retain the dictionary behavior.

| Operation | Example |
| --- | --- |
| Store a default variable | `variables["name"] = "Zongsoft";` |
| Store a namespaced variable | `variables["app.runtime:name"] = "worker";` |
| Query the default namespace | `variables.TryGetValue("NAME", out var value);` |
| Query an explicit namespace | `variables.TryGetValue("APP.RUNTIME", "NAME", out var value);` |
| Remove a namespaced variable | `variables.Remove("app.runtime:name");` |

Dictionary operations use the full key. The provider overload takes namespace and name separately and composes a key; it does not trim or split either argument. The colon is reserved as the namespace separator and must not occur within either component. Null and an empty string both omit the namespace qualifier and query `name` directly; a non-empty namespace queries `namespace:name`. A namespace containing only whitespace is non-empty and is not ignored. A null name throws ArgumentNullException.

Use `new Variables()`, `new Variables(capacity)` or `new Variables(entries)` to create a dictionary. The entries constructor copies an IEnumerable<KeyValuePair<string, object>>; later additions, replacements and removals are independent of the source collection, while values retain their original object references. All constructors use the same comparer. Duplicate keys under case-insensitive comparison are rejected when copying entries.

Values are raw objects and may be null. Variables does not evaluate templates, navigate members or convert types. Mutations are visible to subsequent provider queries. Like Dictionary, it does not synchronize concurrent reads and writes; callers provide synchronization when needed.

## Text input

Evaluate(ReadOnlySpan<char> text) and TryEvaluate(ReadOnlySpan<char> text, out string result, out TemplateEvaluationException error) accept strings, character buffers and slices. Strings convert implicitly; the result remains a string.

```csharp
var input = "prefix${name}suffix";
var result = evaluator.Evaluate(input.AsSpan(6, 7)); // Zongsoft
```

Empty spans, default and spans converted from null strings are empty templates: they return an empty string without variable events. Plain whitespace is preserved. Null provider entries remain API errors.

The internal Parser and TokenScanner are ref structs that hold and scan spans directly. Reference regions pass to Lexer as slices without allocating fragment strings. Each layer copies its source once before evaluating the first reference so events and errors can retain it after the call; parsing failures also retain their input. Template contains only the supplied slice, Position is relative to that slice, and recursive diagnostics refer to the child template. That layer no longer reads the input span after the first callback, so callback mutations of the original character array cannot change parsed nodes or retained source text. Successful plain text does not need this extra source copy. Token values, boxed numbers, syntax nodes and output still require allocations.

Private Part, Accessor, Argument and Lexeme nodes consistently use readonly structs with constructor-initialized public readonly fields, stored directly in list arrays. They do not allocate individual node objects or generate record equality operators or deconstruction members. Reference is a sealed class initialized with readonly fields after parsing; its accessor list is allocated only when member or index navigation occurs and is otherwise null. Collections are built during parsing and only read during evaluation, without extra copies or wrappers. A template containing a single text or placeholder part uses one list slot; an empty template allocates no element array. Neither requires an additional input scan. Parser remains mutable to maintain its cursor.

## Lexical scanning

Lexer.GetScanner(ReadOnlySpan<char> text) returns a TokenScanner ref struct. It accepts strings, slices and stack buffers without copying the input. Keep the input buffer valid and unchanged while scanning. Empty input produces no tokens.

```csharp
using var scanner = Lexer.Instance.GetScanner("prefix name == 12L suffix".AsSpan(7, 11));

while(scanner.Scan(out var position, out var length) is { } token)
	Console.WriteLine($"{position}, {length}: {token}");
```

Scan skips whitespace and reports the token's UTF-16 position and source length relative to the supplied slice. At the end it returns null, the input length as position and zero as length. TokenScanner is stack-bound and cannot be boxed or used with LINQ. Pattern-based foreach enumerates from a copy of its current cursor; it neither advances the original scanner nor closes its stream.

Lexer.GetScanner(Stream stream) decodes the stream once using UTF-8 with BOM detection, then scans the decoded text through the same path. It supports non-seekable streams. Dispose closes the stream and invalidates that scanner; a failed initial read also closes the stream. Span-backed scanners require no input allocation. Keep stream ownership with the scanner returned by GetScanner rather than copying it to another owner.

Custom tokenizers implement `TokenResult Tokenize(ReadOnlySpan<char> text)` and inspect the beginning of the remaining input. Success returns a Token and a positive Length that includes every consumed source character, including quotes, escapes and numeric suffixes. No match returns TokenResult.Fail() without consuming text. The scanner tries Tokenizers in registration order and advances only on success. A successful result whose Length is zero, negative or exceeds the remaining input throws InvalidOperationException. Keep the tokenizer collection unchanged during scanning.

```csharp
public interface ITokenizer
{
	TokenResult Tokenize(ReadOnlySpan<char> text);
}
```

TokenResult is a readonly struct with public readonly fields `int Length` and `Token Token`. Construct a result with `new TokenResult(length, token)`; `TokenResult.Fail()` returns the default struct value, with Token=null and Length=0.

LiteralTokenizerBase chooses the longest matching configured literal, checking identifier boundaries for words. CreateToken receives that configured string, including its configured casing; case-insensitive matching does not allocate a copy of the input spelling. Boolean, null and standard symbol tokenizers reuse cached tokens. Numbers parse directly from spans; identifiers allocate their final value once. Quoted strings without escapes copy only their content; escaped strings decode into a bounded stack buffer or a pooled array before creating their final value. Template member names reuse identifier token values.

The Keyword tokenizer creates a Token but reuses the configured keyword string as its Value. The String tokenizer validates escapes and computes the decoded length before decoding; pooled arrays are cleared when returned.

## Syntax

Templates use `${...}` consistently; `%name%` and `$(name)` in ordinary text do not expand variables.

| Template | Meaning |
| --- | --- |
| `${name}` | A variable in the default namespace. |
| `${app.runtime:name}` | A variable in a dotted namespace. |
| `${person.Home.Address}` | Property or field navigation using Reflector rules. |
| `${items[0].Name}` | Navigation after a constant list index. |
| `${items[indices[0]].Name}` | A dynamic argument with nested list indexing. |
| `${grid[row,column]}` | A multi-parameter indexer. |
| `${map['key']}, ${map["key"]}` | Single- or double-quoted string keys. |
| `${price#0.00}` | A .NET number format. |
| `${date#yyyy-MM-dd HH:mm:ss}` | A .NET date format with internal whitespace. |
| `\${name}` | Literal text ${name}. |
| `${=expression}` | Reserved for future computation; currently fails with UnsupportedExpression. |

Variable, member and namespace segments follow the ASCII rule [A-Za-z_][A-Za-z0-9_]*. Namespaces can contain dots; a colon separates the namespace from the root variable. Member names are case-insensitive; dictionary keys follow the target dictionary's comparer. References cannot contain syntactic whitespace: ${ name } and ${arr[ index ]} are invalid. Spaces inside quoted keys are data.

Dynamic arguments resolve against providers, independently of the indexed object and its namespace. In ${app:arr[index]}, index is in the default namespace. Dynamic arguments can also specify their own namespace and member navigation.

Navigation calls Reflector.GetValue(ref object, name, parameters) directly. It searches fields and properties using Public, Instance, Static and IgnoreCase; an empty name selects default members. Multiple results use the first member, without separate overload selection or ambiguity checks. A Type target denotes the type to search. Templates add no separate public-getter, instance-only or explicit-interface filtering. Method calls and arithmetic remain outside the template grammar.

Ordinary members pass their name; index access passes an empty name to select GetDefaultMembers. Reflection enumeration order is not a stable overload priority. GetValue preserves original getter exceptions, which templates wrap as NavigationFailed. The property path in Reflector.TryGetValue catches read exceptions and returns false, so templates do not use that path.

## Index constants and binding

Unsuffixed integers use int when possible, otherwise long; values outside the long range fail, and L/l suffixes select long. Fractional numbers default to double; f/F, d/D and m/M select float, double and decimal. Negative values are supported, but negative indices do not count from the end. Numeric literals use a fixed decimal point independently of Culture. Scientific notation, hexadecimal and leading plus signs are currently unsupported. Lexer handles suffixes, ranges and invalid numeric boundaries.

Case-insensitive true, false and null are bool or null constants only as complete bare index arguments. ${true} is a variable; true in ${arr[true.Name]} is also a variable, and keyword prefixes do not truncate identifiers. Constants pass directly as object values without provider queries or variable events. Single- and double-quoted keys are strings and never interpolate: `${map['${name}']}` uses the literal key `${name}`.

After resolving an argument, running Resolved and optionally expanding a string, the original object is passed to Reflector without automatic conversion through Common.Convert. Getters unbox or cast references according to their declared parameter types. For example, Dictionary<long, string> requires [42L], not the string ['42'] or the int [42]. Null does not become int zero, and object parameters retain their original types, so int 1 and long 1 can be different keys. Culture controls formatting only.

Indexers follow Reflector's default-member selection, without choosing overloads by argument types. Its getter rejects too few arguments but may ignore extras; exact argument counts are not guaranteed. The current entry point has no special support for array indexing. Use objects with default indexers, such as List<T>. Ordinary array properties such as Length still follow normal member access.

Missing keys and invalid bounds follow the target indexer. Dictionary typically throws for missing keys, while Hashtable may successfully return null. Templates add neither Contains checks nor explicit-interface lookup. A successful null result stays successful; further navigation through null fails.

## Escaping and format boundaries

Template text and quoted index keys share these escapes:

| Input | Character |
| --- | --- |
| `\\` | Backslash |
| `\$` | Dollar |
| `\s` | Space |
| `\n, \r, \t` | Newline, carriage return, tab |
| `\', \"` | Single quote, double quote |

Unknown or incomplete escapes fail; additional Unicode and hexadecimal escape forms are unsupported. Decoded characters are never rescanned as syntax. Escaping `\${` only escapes the dollar, not an entire literal region: `\${name` outputs literal `${name`, while `\${${name}}` still evaluates the inner name. Backslashes in Windows paths also require template escaping.

.NET interprets the format after `#` as a single-value format, not a composite format template. The template parser recognizes quotes and backslashes protecting a closing brace and trims only unprotected leading/trailing whitespace. Internal whitespace, quotes and backslashes pass through unchanged. Empty formats fail with EmptyFormat. A Format value assigned by an event passes directly to .NET without source-syntax trimming.

A successfully resolved terminal null goes directly to .NET formatting. Navigating through an intermediate null fails with NullTarget. Values without explicit formats also use .NET defaults. A null formatted Text passes directly to .NET string concatenation.

## Options and lifetime

The constructor accepts optional TemplateEvaluatorOptions:

| Property | Default | Meaning |
| --- | --- | --- |
| `Culture` | null | Optional CultureInfo; null uses .NET defaults. |
| `Recursive` | false | Evaluate strings returned by resolution and Resolved as child templates. |
| `MaximumDepth` | 64 | Positive maximum template depth; the root is depth 1. |

The evaluator retains the supplied options instance. Omitted/null options create a separate instance for each evaluator. Providers and Options are fixed references with configurable contents. Keep options, collection membership and event subscriptions stable throughout evaluation, including recursion. Providers manage synchronization of their own data. There are no configuration snapshots or shared default options.

With Recursive=false, resolved strings remain literal data. With Recursive enabled, every resolved string enters the next template depth, including plain strings without placeholders. Dynamic string index arguments expand and remain strings when passed to the indexer; quoted constants do not expand. Siblings do not accumulate depth, and repeated variable names may be queried again without a global visited set rejecting them. Cycles end at MaximumDepth. Formatting callback Text is never recursively evaluated.

Each template layer is fully parsed before execution. Thus ${name}-${bad fails before querying name. Child templates can only be parsed after obtaining their strings; earlier side effects are not rolled back. Nested index syntax does not consume template depth and has a separate internal limit of 256 levels.

## Events

```text
Resolving → providers/navigation → Resolved → optional recursion
          → Formatting → .NET formatting → Formatted → append
```

All four events belong to the evaluator and use EventHandler<T>; sender is the evaluator. Dynamic index references have Resolving/Resolved events, but no direct formatting events. References inside recursive child templates have their own complete event sequence.

Notifications cover a complete reference: `${person.Home.Name}` has one Resolving/Resolved pair, not one pair per segment or provider attempt. Dynamic index references have independent nested notifications, constants have none, and repeated references each produce notifications. Both context types derive from EventArgs and are created by the evaluator through internal constructors.

TemplateEvaluator.ResolutionContext exposes read-only Template, Expression, Namespace, Name, Position, Length, Depth and IsIndex, plus writable Value and Handled.

TemplateEvaluator.FormattingContext has the same read-only metadata except IsIndex, plus writable Value, Format, Culture, Text and Handled. Resolution and formatting use separate contexts.

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

Resolving with Handled=true takes over the complete reference, skipping providers, navigation and dynamic arguments. Value may be null; Resolved, optional recursion and formatting still follow, and source syntax must still be valid. Formatting with Handled=true uses Text. Every before-event subscriber runs in subscription order before the final Handled value is checked. Any callback exception stops evaluation immediately.

After-events may replace the resolved value or formatted text but never repeat completed stages. They run only after successful or explicitly handled corresponding stages. Failures do not return partial output.

## Errors

Evaluate always returns a string or throws TemplateEvaluationException. TryEvaluate runs the same pipeline once; success returns true with the result and error=null, while failure returns false with result=null and an error:

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

Invalid options and null provider entries are API errors; TryEvaluate still throws argument exceptions for them. Empty spans, including those converted from null strings, return an empty string without variable events.

## Implementation

- [TemplateEvaluator](../src/Text/Templating/TemplateEvaluator.cs): public entry points, events and execution.
- [IVariableProvider](../src/Expressions/IVariableProvider.cs) and [Variables](../src/Expressions/Variables.cs): variable provider contract and in-memory dictionary implementation.
- [Resolution context](../src/Text/Templating/TemplateEvaluator.ResolutionContext.cs) and [formatting context](../src/Text/Templating/TemplateEvaluator.FormattingContext.cs): public nested event contexts owned by TemplateEvaluator.
- [Template parser](../src/Text/Templating/TemplateEvaluator.Parser.cs): template regions and reference grammar consuming the existing Lexer; all syntax nodes remain private.
- [TokenScanner](../src/Expressions/TokenScanner.cs): Span scanning with source positions and consumed lengths; streams are decoded using UTF-8/BOM detection and closed when the scanner is disposed.
- [Reflector](../src/Reflection/Reflector.cs): templates call the existing GetValue entry point for members and default indexers, preserving original read failures.
- [Template tests](../test/Text/Templating/TemplateEvaluatorTest.cs), [variable tests](../test/Expressions/VariablesTest.cs) and [lexer tests](../test/Expressions/LexerBoundaryTest.cs): verification through public behavior.

[IExpressionEvaluator](../src/Expressions/IExpressionEvaluator.cs) provides the script evaluation contract. [MemberExpressionParser](../src/Reflection/Expressions/MemberExpressionParser.cs) accepts spans, but its grammar allows whitespace and methods, has different numeric and escape rules, and does not provide template namespaces or per-reference diagnostics. [MemberExpressionEvaluator](../src/Reflection/Expressions/MemberExpressionEvaluator.cs) does not populate dynamic arguments in its default index path or implement template events, providers and string recursion. Templates reuse [Lexer](../src/Expressions/Lexer.cs) and Reflector directly without an AST conversion layer. Internal nodes, Parser and syntax guards remain private, without test-only entry points. Common capabilities such as array indexing, argument conversion and interface access belong in extensions to Reflector's existing entry points.

## Integration boundaries

[Profile](profiles.md) loads and saves original text without expanding variables automatically. Explicit evaluation extensions for Profile, ProfileEntry and ProfileDirectiveContext are not yet provided. Integration should preserve ProfileEntry.Value and saved source text; the variable-environment entry point, import-argument evaluation timing, tokenization and reload behavior still require separate decisions, without exposing the internal read session.

Tools such as deployer, packager, migrator and containerizer organize command arguments, environment variables, configuration and business sources. When adopting TemplateEvaluator, the tools layer handles call-site and template-syntax migration and determines when conditions and generated artifacts are evaluated. The evaluator does not embed these tool-specific rules.

The `${=expression#format}` position is reserved for computation sharing lexical, variable-provider and formatting facilities. Currently it only recognizes the entry point and reports UnsupportedExpression; operators, functions and conditional expressions are not executed.
