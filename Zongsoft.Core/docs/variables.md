# Variables

[English](variables.md) | [简体中文](variables.zh-Hans.md)

`Zongsoft.Common` provides variable contracts and implementations independent of expression and template syntax. `IVariables` queries raw values by name and optional namespace; `Variables` provides in-memory storage, dictionary wrappers and environment variable access. Command options and Profile configuration can also supply variables.

## Getting started

```csharp
using Zongsoft.Common;

var variables = new Variables
{
	["name"] = "Zongsoft",
	["app.runtime:workers"] = 4,
	["optional"] = null,
};

IVariables source = variables;
source.TryGetValue("NAME", out var name); // Zongsoft
source.TryGetValue("APP.RUNTIME", "WORKERS", out var workers); // 4
var found = source.TryGetValue("optional", out var value); // found is true, value is null
```

## Variable contract

Variable sources implement the following interface:

```csharp
public interface IVariables
{
	bool TryGetValue(string name, out object value);
	bool TryGetValue(string @namespace, string name, out object value);
}
```

`TryGetValue(name, out value)` queries only the default namespace and is equivalent to both `TryGetValue(null, name, out value)` and `TryGetValue(string.Empty, name, out value)`. The explicit overload takes the namespace first, followed by the variable name. They are separate arguments; the name is not split again into namespace and name.

Names and namespaces use `OrdinalIgnoreCase`. Null and empty namespaces are equivalent, and a nonempty namespace never falls back to the default namespace. [Environment variable views](#environment-variable-views) are an explicit exception to the name comparison rule and follow platform behavior.

The Boolean result indicates whether lookup succeeded: a present null value still returns true. Missing variables return false without throwing; other source-access exceptions may propagate.

Providers return raw objects without parsing references, navigating members, converting types, formatting or expanding templates. Each implementation owns its caching, live-value and synchronization policies. The interface does not require repeated queries to return the same value.

## In-memory variables

Zongsoft.Common.Variables inherits Dictionary<string, object> and implements IVariables. Its comparer is always StringComparer.OrdinalIgnoreCase. Indexers, collection initializers, Add, TryAdd, Remove, Clear and enumeration retain the dictionary behavior.

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

## Dictionary variable views

Use `Variables.Wrap(dictionary)` or `dictionary.ToVariables()` (in `Zongsoft.Common`) to expose an `IDictionary`, `IDictionary<string, object>` or `IDictionary<object, object>` as a live `IVariables` view:

```csharp
using System.Collections.Generic;
using Zongsoft.Common;
using Zongsoft.Text.Templating;

IDictionary<string, object> dictionary = new Dictionary<string, object>
{
	["Name"] = "Zongsoft",
	["App:Version"] = "1.0",
};

IVariables variables = Variables.Wrap(dictionary);
var evaluator = new TemplateEvaluator { Providers = { variables } };
dictionary["Name"] = "Updated"; // Subsequent lookups see this change.

// Explicitly enable reuse when repeatedly adapting the same dictionary.
IVariables shared = dictionary.ToVariables(reuse: true);
IVariables same = Variables.Wrap(dictionary, reuse: true);
```

`Wrap` exposes three overloads restricted to dictionary types, with corresponding `ToVariables` extension methods:

```csharp
public static IVariables Wrap<TDictionary>(TDictionary dictionary, bool reuse = false)
	where TDictionary : IDictionary;
public static IVariables Wrap(IDictionary<string, object> dictionary, bool reuse = false);
public static IVariables Wrap(IDictionary<object, object> dictionary, bool reuse = false);
```

Concrete dictionaries implementing non-generic `IDictionary` (including `Dictionary<string, object>`, `Dictionary<object, object>` and `Hashtable`) prefer the constrained generic method, so direct calls avoid ambiguity between these interfaces. Types implementing only one of the generic dictionary interfaces use its interface overload. Arbitrary `object` values and non-dictionary objects implementing only `IVariables` cannot be passed directly. A custom type implementing both generic interfaces but not non-generic `IDictionary` requires an explicit cast to either interface. A bare `null` also requires an explicit dictionary type.

Internal adapter selection prefers the string-keyed generic interface, then the object-keyed generic interface, then the non-generic interface, keeping view behavior consistent across overloads for the same dictionary.

Both entry points accept an optional `bool reuse = false`. By default, each call creates a fresh adapter without reading, populating or removing cache entries; the view still reads the same source dictionary. With `reuse: true`, both entry points and different static interface types return the same view for the same dictionary instance. If the dictionary already implements `IVariables`, both entry points return that object regardless of `reuse`.

Reused adapters are cached in a `ConditionalWeakTable` by reference identity; overridden equality does not merge distinct dictionaries. The cached view remains available while the dictionary is alive, and holding any view keeps its source available. The cache does not prevent collection when neither object is otherwise reachable. Concurrent calls with reuse enabled share the associated view, although the factory may create extra adapters during the first concurrent access. Reuse avoids repeated adapter allocations; the default avoids cache lookup and registration when the caller creates and holds a view once.

The view reads current entries, including additions, replacements, removals and null values. It does not copy entries, cache query results, evaluate templates or convert values. For an independent copy of string-keyed object values, pass `IEnumerable<KeyValuePair<string, object>>` to `new Variables(entries)`. The cache is thread-safe; source access still follows the source dictionary's synchronization requirements.

Only string keys provide variables. Other key types are ignored without calling `ToString()`. Keys follow the same convention as `Variables`: `name` for the default namespace and `namespace:name` for a named namespace. Null and empty namespaces select the default namespace, with no fallback from a nonempty namespace. Query parameters are not trimmed, split or normalized. A null dictionary or query name throws `ArgumentNullException`.

Lookups always compare keys using `OrdinalIgnoreCase`. Standard `Dictionary<string, object>` and `ConcurrentDictionary<string, object>` instances using `StringComparer.OrdinalIgnoreCase` are queried directly. Other implementations, derived types and comparers scan current entries in enumeration order and return immediately on the first match, even when its value is null. Duplicate names under case-insensitive comparison are allowed; an exact-case match has no extra priority. This scan takes O(n) in the worst case. A present null value counts as success and blocks later providers.

## Environment variable views

Use `Variables.Environments(EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)` to obtain a live environment variable view and explicitly register it with the template evaluator:

```csharp
using Zongsoft.Common;
using Zongsoft.Text.Templating;

var evaluator = new TemplateEvaluator
{
	Providers = { Variables.Environments() },
};

var text = evaluator.Evaluate("${PATH}");
```

Each target has a shared view instance, but values are not cached. Every lookup calls `System.Environment.GetEnvironmentVariable(name, target)`, so later queries observe process environment variables added, changed or removed after the view was created. Values are raw strings without conversion or template expansion. A present empty string is a successful lookup; a missing variable returns false. Access exceptions propagate and are wrapped as `ProviderFailed` during template evaluation. Values need not remain consistent across multiple queries.

The view returned by `Variables.Environments()` is an explicit exception to the case-insensitive `IVariables` contract. The interface contract and dictionary/Profile behavior remain unchanged. Environment variable names follow platform rules: Windows ignores case, while Unix/Linux is case-sensitive. Unix/Linux callers must use the correct spelling; names differing only in case are queried separately. Names are preserved without converting `__`, underscores or other characters into namespaces. Template references still follow the template identifier grammar.

The view provides only the default namespace. `TryGetValue(name, out value)`, a null namespace and an empty namespace are equivalent. Other namespaces return false without fallback. Query arguments are not trimmed; a null name throws `ArgumentNullException`.

`Process` reads the current process. `User` and `Machine` follow .NET platform support and find no variables on Unix/Linux. Targets are not merged and do not fall back to one another. Invalid enum values throw `ArgumentOutOfRangeException` with parameter name `target` when requesting the view.

`Variables.Wrap(Environment.GetEnvironmentVariables())` wraps the dictionary snapshot obtained at that call and uses case-insensitive dictionary view rules. Use `Variables.Environments()` for live values and platform-specific name comparison.

## Command option variables

`CommandLine.CmdletOptionCollection` directly implements `Zongsoft.Common.IVariables`. Register `CommandContext.Options` as a template provider:

```csharp
using Zongsoft.Common;
using Zongsoft.Text.Templating;

IVariables variables = context.Options;
variables.TryGetValue("install_path", out var path);

var evaluator = new TemplateEvaluator { Providers = { context.Options } };
var text = evaluator.Evaluate("${install_path}");
```

Variable names replace `.` and `-` in option names with `_`: both `install.path` and `install-path` map to `install_path`. The resulting name must match `[A-Za-z_][A-Za-z0-9_]*`; otherwise the option and its short name are excluded from variable lookup. Valid short names are also available as variables. Lookup names must already be valid identifiers and are not trimmed, normalized or split into namespaces. Ordinary option access still uses the original name, for example `context.Options.GetValue("install-path")`.

Variable lookup ignores case and provides only the default namespace; null and empty namespaces are equivalent, and any nonempty namespace returns false. Explicitly supplied values take precedence over descriptor defaults, preserving the option's converted type. A present null still succeeds and blocks fallback. When names map to the same variable, the first match in the supplied options' enumeration order wins; if none matches, descriptor defaults are searched in descriptor order.

The normalized name index is built lazily on the first valid variable lookup and then reused for dictionary lookups. Names are fixed at that point: later additions, removals or replacements in the descriptor collection do not rebuild the index. Default values are read from the original descriptor objects on every lookup, so changes to those defaults remain visible. The index is safely published for concurrent reads; concurrent changes to descriptors require caller coordination.

## Profile variable views

[Profile](profiles.md#variable-views) exposes live IVariables views of a whole configuration, a selected section subtree or one entry through ProfileExtension.ToVariables(). Register a view explicitly with evaluator.Providers.Add(profile.ToVariables()). Section levels form a dot-separated namespace, retaining dots within section names; dots and hyphens in entry names become underscores before identifier validation. Lookup returns raw values. When several entries map to the same variable, only queries for that variable throw ProfileException; template evaluation wraps it as ProviderFailed and preserves the original exception.

## Template integration

All of these variable sources can be explicitly added to `TemplateEvaluator.Providers`. Templates query them in registration order and use the first successful result, including null. See the [text template documentation](expressions.md) for syntax, member navigation, formatting, events and error handling.

## Implementation

- [IVariables](../src/Common/IVariables.cs): variable lookup contract.
- [Variables](../src/Common/Variables.cs) and [VariablesExtension](../src/Common/VariablesExtension.cs): in-memory storage, dictionary wrappers and extension methods.
- [Environment variable views](../src/Common/Variables.Environments.cs): platform environment variable access.
- [Command option collection](../src/Components/CommandLine.Options.cs): variable indexing and lookup for command options.
- [ProfileExtension](../src/Configuration/Profiles/ProfileExtension.cs): configuration variable adapters.
- [In-memory variable tests](../test/Common/VariablesTest.cs), [dictionary wrapper tests](../test/Common/VariablesWrappingTest.cs), [object-keyed dictionary tests](../test/Common/VariablesObjectWrappingTest.cs) and [environment variable tests](../test/Common/VariablesEnvironmentsTest.cs).
- [Command option variable tests](../test/Components/CommandLineVariablesTest.cs) and [Profile variable tests](../test/Configuration/Profiles/ProfileVariablesTest.cs).
