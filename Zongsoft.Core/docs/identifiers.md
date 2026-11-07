# Identifiers

[English](identifiers.md) | [简体中文](identifiers.zh-Hans.md)

`Zongsoft.Components.Identifier` identifies a value within either a logical `Name` or a CLR `Type`. `Identifier<T>` and the versions with two through seven type parameters expose typed keys. Composite keys use `ValueTuple`; conversion to the non-generic identifier preserves the key type and metadata.

The non-generic identifier and `Identifier<T>` with a tuple key support two through seven components in complete text, JSON, type conversion, and ordinal key access or replacement. Dedicated generic variants and shorthand registration also support up to seven components. Tuple components must be scalar keys; nested tuples are unsupported.

```csharp
var machine = new Identifier("Machine", 17u, "Vending machine");
var user = new Identifier(typeof(Zongsoft.Security.Privileges.IUser), 17u);
var branch = new Identifier<uint, uint>("Branch", 1, 1001, (null, "HZ"), "Hangzhou");
Identifier untyped = branch;
Identifier<(uint, uint)> tuple = branch;
```

Identifiers with two to seven generic parameters support deconstruction into typed key components. Local variable names express their business meaning. A default identifier yields the default value of each component.

```csharp
var (tenantId, branchId) = branch;
```

Generic identifiers provide `Validate(name, predicate)` and `Validate(type, predicate)` overloads. The predicate receives each typed key component as a separate argument and runs once only after the existing category validation succeeds. A false predicate result rejects the identifier; predicate exceptions propagate to the caller. A null predicate throws `ArgumentNullException`. Equality operators continue to compare the complete identity.

```csharp
var valid = branch.Validate("Branch", static (tenantId, _) => tenantId != 0);
var specific = branch.Validate("Branch", static (tenantId, branchId) => tenantId != 0 && branchId != 0);
```

## Reading and replacing keys

TryGetValue retrieves the complete key, optionally checking a logical Name or CLR Type. Its ordinal overloads retrieve a zero-based component: a scalar has only component 0. Missing components, empty identifiers, category mismatches and failed conversions return false with a default output. Conversion follows existing identifier conversion rules; zero itself is a valid retrieved value. Specify the output type for component access because a runtime ordinal cannot infer a generic type from out var.

```csharp
branch.TryGetValue("Branch", out var key);
branch.TryGetValue("Branch", 0, out uint tenantId);
branch = branch.With(1, 1002);
```

With(ordinal, value) returns the same identifier variant with one component replaced, converting the value to the original component type. Other components and all metadata remain intact; the result shares the existing Properties dictionary. Empty identifiers throw InvalidOperationException, invalid ordinals throw ArgumentOutOfRangeException, and values that cannot be converted throw InvalidCastException. Initialize a model's category and key before using With in property setters. Validate remains the category/predicate check.

Typed identifiers also support object initializers and native `with` expressions for Value, Code, Label, Alias, Description and Properties. The constructor fixes the Name or Type category. Set Value as a complete typed key; use With(ordinal, value) when a runtime component index is needed. A native with expression preserves other fields and shares Properties; assigning a new Properties dictionary makes one shallow copy. These init accessors do not change the readonly fields of the non-generic Identifier.

```csharp
var branch = new Identifier<uint, uint>("Branch", 1, 1001)
{
	Code = "HZ001",
	Label = "Hangzhou",
	Alias = "HZ",
};
branch = branch with { Value = (1, 1002), Label = "Ningbo" };
```

## Identity and metadata

Use the concrete, non-null primary-key type for generic parameters; use the default identifier to represent an absent identity. Name and CLR Type validation both reject empty identifiers. A zero key is a present value; business predicates determine whether it is allowed.

Names contain only ASCII letters, digits, underscores and dots; the first character cannot be a dot. Names compare without case sensitivity. CLR types and key types are part of identity: `Machine:<uint>17`, `Machine:<int>17` and `Machine:17` are different identifiers. String keys compare with case sensitivity.

`Code`, `Label`, `Alias`, `Description` and `Properties` do not affect equality or hash codes. Code is an optional business number, independent of the identity key in Value. It preserves original text, does not fall back to Value or Alias, and does not make an identifier with a null Value nonempty. Alias is an optional display field. Non-generic metadata fields are readonly; typed variants expose init accessors. Constructors provide three metadata parameter groups, shared by all Name, Type and generic variants:

- `label, description, properties`: display label, description and extension properties.
- `(code, alias), label, description, properties`: business code and alias form the extra tuple; the label is a separate argument.
- `(code, alias, label), description, properties`: business code, alias and label form the extra tuple.

The extra tuple immediately follows the key. Use null for omitted tuple fields; all remaining trailing arguments are optional. Generic composite keys accept individual components or a complete key tuple; parameter position distinguishes key tuples from metadata tuples. Constructor grouping does not change the text protocol field order.

```csharp
var order = new Identifier("Order", 1001UL, ("SO001", null), "Purchase order");
var employee = new Identifier<uint>("Employee", 17u, ("E001", "J", "Jane"), "Purchasing");
```

`Properties` is an optional mutable `IDictionary<string, string>`. Constructors and typed Properties initializers make one shallow copy of the supplied dictionary; struct copies and lossless identifier conversions share the internal dictionary. Keys are case-insensitive and may contain Unicode text, spaces, core member names or an empty string. Keys and values use the same escaping rules; null values are written as empty strings. Formatting reads the current dictionary in sorted key order without copying it. Concurrent mutation requires application synchronization.

## Text and JSON

```text
Name:<keyTypes>keyValues#Code?Label|Alias(Description){key=value;...}
$TypeAlias:<keyTypes>keyValues#Code?Label|Alias(Description){key=value;...}
```

A scalar string key may omit `<string>`. Other key types are explicit unless a matching shorthand is registered. Composite components are separated by commas in complete text. Metadata follows the fixed order Code, Label/Alias, Description, Properties. Empty optional metadata is omitted; an alias without a label uses `?|Alias`. Description can appear alone as `(Description)` and requires a closing parenthesis. Key types follow the scope colon, so they remain distinct from generic arguments in the CLR Type.

```text
Machine:17
Machine:<uint>17
$Zongsoft.Security.User:<uint>17
Branch:<uint,uint>1,1001?Hangzhou|HZ(Regional office){zone=east}
Store:<uint,ushort>1,2
Order:<ulong>1001#SO001?Purchase order(Internal)
$List<int>:<uint>17
```

Backslash escapes structural delimiters, including angle brackets and parentheses, and itself. For example, `Order:<ulong>1001(Internal\(purchase\))` contains literal parentheses in its description. Control characters use `\n`, `\r`, `\t` or `\uXXXX`. Contents are not trimmed. Numeric values use invariant culture; dates and times use round-trip formats. Enum and custom scalar keys require reversible text conversion; arbitrary `ToString()` output is insufficient. CLR types use `TypeAlias`; parsing resolves only known aliases and already loaded types. An unknown Type never becomes a logical Name.

All identifier versions implement `ISpanParsable`, with matching string and span `Parse`/`TryParse` overloads. The format provider does not change the protocol. Empty text yields the default identifier; `Parse(null)` throws `ArgumentNullException` and `TryParse(null)` returns false. Invalid structures, escapes, duplicate properties, component counts and values fail parsing.

`ToString()`, `TypeConverter` and JSON use the same text. JSON writes a complete identifier string including its scope, or `null` for the default value. Bare JSON numbers are rejected. JSON property names use identifier text. Explicit primitive casts and `IConvertible` extract the key value; they are not identifier serialization.

## Shorthand registration

Register application formats before creating the application or accepting input:

```csharp
Identifier.Formatting.Register<uint, uint>("Branch");
Identifier.Formatting.Register<uint, ushort>("Store");

var branch = new Identifier<uint, uint>("Branch", 1, 1001);
var text = branch.ToString();           // Branch:1-1001
var full = branch.ToString("F");        // Branch:<uint,uint>1,1001
var parsed = Identifier.Parse(text);
```

Registration also accepts a CLR Type and supports one through seven components. The default separator is `-`; explicit separators can also be `,`, `/` or `.`; component content escapes the selected separator. Identical registrations are idempotent, conflicting registrations throw, and rules cannot be replaced. Concurrent reads and registrations are supported.

Explicit key types override registered rules. A string key in a registered scope includes `<string>` to avoid misinterpretation. Invalid shorthand counts, types or ranges fail; unregistered, untyped non-generic input is a scalar string. Scalar generic parsing accepts a declared or registered key type assignable to `T`, preserving the actual key type without numeric conversion. For example, `Identifier<object>` can carry `Scalar:<uint>17`, while `Identifier<int>` rejects it. With no declaration or registration, a scalar carrier that accepts strings parses a string key; other generic parameters provide the expected key types. Composite keys require matching component types. Shorthand exchanged between applications requires matching registrations; `ToString("F")` is independent of application shorthand rules.

## Identifiable objects and Security

`IIdentifiable.Identifier` remains readable and writable. `IIdentifiable<T>` derives from it and provides a default bridge that preserves metadata and performs key conversion. Its key type does not need to implement `IEquatable<T>`; enum keys can use the same interface. Implementations still validate their own category and business constraints. Read-only objects throw `NotSupportedException` on assignment.

Security identifies users through `IUser` and roles through `IRole`. The Security module registers the `Zongsoft.Security.User` and `Zongsoft.Security.Role` type aliases when `Zongsoft.Security.Module.Current` is created. Initialize the module before exchanging these short names. Without that registration, Core uses the regular full type representation, such as `$Zongsoft.Security.Privileges.IUser@Zongsoft.Core:<uint>17`. Their key types are supplied by the actual model and are not globally registered. Members expose their actual user or role identity; a `Member` object is not itself the key. Claims retain the claim value type and are assigned through the model's identifier setter. An identifier is not an authorization grant: services must continue validating tenant scope and permissions.
