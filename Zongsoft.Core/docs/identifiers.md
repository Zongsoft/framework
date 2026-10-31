# Identifiers

[English](identifiers.md) | [简体中文](identifiers.zh-Hans.md)

`Zongsoft.Components.Identifier` identifies a value within either a logical `Name` or a CLR `Type`. `Identifier<T>` and the versions with two through five type parameters expose typed keys. Composite keys use `ValueTuple`; conversion to the non-generic identifier preserves the key type and metadata.

```csharp
var machine = new Identifier("Machine", 17u, "Vending machine");
var user = new Identifier(typeof(Zongsoft.Security.Privileges.IUser), 17u);
var branch = new Identifier<uint, uint>("Branch", 1, 1001, "Hangzhou", alias: "HZ");
Identifier untyped = branch;
Identifier<(uint, uint)> tuple = branch;
```

Identifiers with two to five generic parameters support deconstruction into typed key components. Local variable names express their business meaning. A default identifier yields the default value of each component.

```csharp
var (tenantId, branchId) = branch;
```

Generic identifiers provide `Validate(name, predicate)` and `Validate(type, predicate)` overloads. The predicate receives each typed key component as a separate argument and runs once only after the existing category validation succeeds. A false predicate result rejects the identifier; predicate exceptions propagate to the caller. A null predicate throws `ArgumentNullException`. Equality operators continue to compare the complete identity.

```csharp
var valid = branch.Validate("Branch", static (tenantId, _) => tenantId != 0);
var specific = branch.Validate("Branch", static (tenantId, branchId) => tenantId != 0 && branchId != 0);
```

## Identity and metadata

Use the concrete, non-null primary-key type for generic parameters; use the default identifier to represent an absent identity. Name and CLR Type validation both reject empty identifiers. A zero key is a present value; business predicates determine whether it is allowed.

Names contain only ASCII letters, digits, underscores and dots; the first character cannot be a dot. Names compare without case sensitivity. CLR types and key types are part of identity: `Machine:<uint>17`, `Machine:<int>17` and `Machine:17` are different identifiers. String keys compare with case sensitivity.

`Code`, `Label`, `Alias`, `Description` and `Properties` do not affect equality or hash codes. Code is an optional read-only business number, independent of the identity key in Value. It preserves original text, does not fall back to Value or Alias, and does not make an identifier with a null Value nonempty. Alias is a read-only display field. Constructors accept metadata in the order `label`, `description`, `alias`, `properties`, `code`; use named arguments for optional metadata.

`Properties` is an optional mutable `IDictionary<string, string>`. Constructors make one shallow copy of the supplied dictionary; struct copies and lossless identifier conversions share the internal dictionary. Keys are case-insensitive and may contain Unicode text, spaces, core member names or an empty string. Keys and values use the same escaping rules; null values are written as empty strings. Formatting reads the current dictionary in sorted key order without copying it. Concurrent mutation requires application synchronization.

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

Registration also accepts a CLR Type and supports one through five components. The default separator is `-`; explicit separators can also be `,`, `/` or `.`; component content escapes the selected separator. Identical registrations are idempotent, conflicting registrations throw, and rules cannot be replaced. Concurrent reads and registrations are supported.

Explicit key types override registered rules. A string key in a registered scope includes `<string>` to avoid misinterpretation. Invalid shorthand counts, types or ranges fail; unregistered, untyped non-generic input is a scalar string. Scalar generic parsing accepts a declared or registered key type assignable to `T`, preserving the actual key type without numeric conversion. For example, `Identifier<object>` can carry `Scalar:<uint>17`, while `Identifier<int>` rejects it. With no declaration or registration, a scalar carrier that accepts strings parses a string key; other generic parameters provide the expected key types. Composite keys require matching component types. Shorthand exchanged between applications requires matching registrations; `ToString("F")` is independent of application shorthand rules.

## Identifiable objects and Security

`IIdentifiable.Identifier` remains readable and writable. `IIdentifiable<T>` derives from it and provides a default bridge that preserves metadata and performs key conversion. Its key type does not need to implement `IEquatable<T>`; enum keys can use the same interface. Implementations still validate their own category and business constraints. Read-only objects throw `NotSupportedException` on assignment.

Security identifies users through `IUser` and roles through `IRole`. The Security module registers the `Zongsoft.Security.User` and `Zongsoft.Security.Role` type aliases when `Zongsoft.Security.Module.Current` is created. Initialize the module before exchanging these short names. Without that registration, Core uses the regular full type representation, such as `$Zongsoft.Security.Privileges.IUser@Zongsoft.Core:<uint>17`. Their key types are supplied by the actual model and are not globally registered. Members expose their actual user or role identity; a `Member` object is not itself the key. Claims retain the claim value type and are assigned through the model's identifier setter. An identifier is not an authorization grant: services must continue validating tenant scope and permissions.
