# Settings and connection settings

[English](settings.md) | [简体中文](settings.zh-Hans.md)

## Dictionary contract

`ISettings` inherits `IDictionary<string, string>` and `IEquatable<ISettings>`. Its own members are `Name`, `Value`, `IsEmpty` and `HasValue`; dictionary members are inherited without redeclaration. `Settings` also implements `IReadOnlyDictionary<string, string>`.

`Settings` represents semicolon-separated key/value text. Keys and names are case-insensitive; values are case-sensitive. `Add`, `Remove`, `Clear`, collection operations, and dictionary key/value views are implemented explicitly:

```csharp
ISettings settings = new Settings("mail", "Host=mail.example.test;Port=2525");
settings.Add("Sender", "sender@example.test");
settings["Port"] = "2526";
settings.Remove("Sender");
var text = settings.Value;
```

The raw indexer returns `null` for missing keys. Assigning `null` or an empty string removes a main entry. `Add` rejects duplicate keys. `ICollection<KeyValuePair<string, string>>.Remove` requires both the key and value to match.

`Value` includes all explicit settings. Mutations invalidate its cached text; the next read serializes the current state, escaping quoted text and separators. Assigning `Value` replaces the entire state; assigning `null` or empty text clears it. Duplicate text keys are applied in source order, with the last value winning. Driver aliases participate in the same sequence: `Server=a;Host=b;Server=c` resolves to `Server=c` when `Host` aliases `Server`. Output ordering, key casing and a trailing semicolon are not formatting contracts.

## Connection settings

`IConnectionSettings` inherits `ISettings` and adds the driver, `Properties`, `HasProperties`, `GetValue<T>` and `TryGetValue<T>`. `ConnectionSettingsBase` inherits `Settings` and shares its parser and main dictionary. Only connection settings expose `Properties`.

| View | Contents |
| --- | --- |
| Indexer, non-generic `TryGetValue`, `ContainsKey`, `Count`, `IsEmpty`, `HasValue`, enumeration | Explicit main entries, without synthesized defaults |
| Typed properties, `GetValue<T>`, `GetOptions()` | Converted main entries and descriptor defaults |
| `Properties` | Explicit extension entries |
| `Value` | Explicit main and extension entries |

Typed `TryGetValue<T>` succeeds for a convertible explicit value or a declared descriptor default. For a missing `bool` or `int` without a declared default, typed `TryGetValue<T>` returns `false`, while `GetValue<T>` returns the CLR default. Explicit `false` and `0` succeed in typed Try reads. An invalid explicit value remains available through the raw dictionary: typed getters and options conversion throw `InvalidOperationException`, and typed `TryGetValue<T>` returns `false`.

Without `TOptions`, arbitrary main keys are allowed. With `TOptions`, `Driver.Descriptors` determines valid names, aliases and composite paths, including descriptors marked `Ignored` for business parameters. Unknown text entries go to `Properties`; writing unknown main keys throws `ArgumentException`.

For constrained settings, writing a declared name, alias or valid composite path through `Properties` also throws `ArgumentException`, through either its indexer or `Add`. Failed writes preserve the existing state. Reads and removals operate only on the dictionary being accessed; clearing the main dictionary leaves extensions intact.

For a MySQL setting created from `server=example.test;port=3306;CircuitBreaker.Duration=60s`:

```csharp
settings.Properties["ABC"] = "Value";
settings["database"] = "zongsoft";
settings["port"] = "3307";
// settings.Properties["Port"] = "3308"; // Throws ArgumentException.
```

`Value` now preserves `CircuitBreaker.Duration=60s` and `ABC=Value` alongside the updated main parameters. `GetOptions()` projects driver parameters to native options. The complete framework `Value` is not necessarily a valid third-party connection string; use the driver's native options when passing it to a provider.

Ordinary settings compare name and main contents. Connection settings additionally compare driver name and extension contents. Entry order does not matter; all interface and base-class equality views agree. Ordinary and connection settings are unequal. Mutable settings should not be changed while used as hash keys.

## Configuration and driver options

Use one `Settings` instance for a group of scalar values. A dictionary key is the setting name and its value is the string itself. For example, mail settings hold `Sender`, `Host`, `Port` and `AuthorityCode` in one dictionary.

When converting ordinary settings or a protocol wrapper to a specific driver, rebuild explicitly:

```csharp
var options = driver.GetSettings(settings.Name, settings.Value).GetOptions();
```

`ConnectionSettingsUtility.GetOptions<TOptions>()` does not automatically rebuild another settings type through its driver. Read connection metadata through the driver instance's `Descriptors` property.

The configuration binder applies `Value` before independent properties, and treats `Driver` as connection metadata. An `ISettings` configuration target creates a `Settings` instance.

## Extending settings

`GetKey(string key, bool validate = false)` is the overridable entry point for normalizing main parameter names. Queries and removals use `validate: false`; indexer assignments and additions use `validate: true`. Connection settings resolve aliases and composite paths in the same call, rejecting unknown names only for validated writes when constrained by an options type. The base implementation preserves the supplied key without trimming whitespace.

`OnEntryChanged(string key, string value)` processes entries parsed from settings text. `OnValueChanged` invokes it in text order when initializing or replacing `Value`, allowing a derived implementation to route entries to the main dictionary or extension properties. It is not called by indexer assignments, additions, removals or clears.
