# Network address and protocol values

[English](networking.md) | [简体中文](networking.zh-Hans.md)

`Zongsoft.Communication.NetworkAddress` and `NetworkProtocol` provide shared value parsing for service hosts and deployment tools. Both implement `ISpanParsable<T>`, including its base interface `IParsable<T>`. They do not discover services, execute XML processing instructions, choose proxy routes, resolve DNS, probe a backend, or change an existing listener.

## Addresses

```csharp
var address = NetworkAddress.Parse("http://*:4317/opentelemetry");
// Scheme: http; Host: 0.0.0.0; Port: 4317; Path: /opentelemetry
// HostType: IPv4; AddressFamily: InterNetwork; IsAny: true

var custom = NetworkAddress.Parse("custom+v2://localhost:7000/a/../b%2Fc");
// Scheme: custom+v2; Path: /a/../b%2Fc
```

`NetworkAddress` is an immutable reference type for `scheme://host[:port][/path]`. A scheme follows `[A-Za-z][A-Za-z0-9+.-]*`; there is no application-protocol whitelist. Names such as `mqtt` or `opc.tcp` are ordinary schemes and do not imply a transport.

HTTP and HTTPS default to ports 80 and 443; other schemes require an explicit port. All ports must be between 1 and 65535. The host is required. User information, query strings, fragments, backslashes and embedded whitespace/control characters are rejected. Outer whitespace is trimmed. This is a network-address value, not a parser for every URI form, such as hostless Unix socket paths.

| Property | Meaning |
| --- | --- |
| `Scheme` | Lowercase scheme; no transport or version is inferred from its name or a port number. |
| `Host` | Canonical IP text or lowercase IDNA ASCII DNS name without its final root dot; excludes IPv6 brackets and zone. Numeric IP parsing follows `IPAddress.TryParse`. |
| `HostType` | `UriHostNameType.Dns`, `IPv4`, or `IPv6`. |
| `AddressFamily` | `InterNetwork`, `InterNetworkV6`, or `Unspecified` for a DNS host. No name resolution is performed. |
| `Zone` | Case-sensitive IPv6 zone identifier, or null. URL input uses `%25` before an unreserved ASCII identifier, for example `[fe80::1%25Ethernet]`. |
| `Port` | Effective port, including an HTTP/HTTPS default. |
| `Path` | Original case-sensitive path, or `/` if omitted. The trailing slash is significant. |
| `IsAny` | `*` and `0.0.0.0` are IPv4 wildcard addresses; `[::]` is an IPv6 wildcard. These families remain distinct. |
| `IsLoopback` | `localhost` or an IP loopback address. `localhost` remains a DNS host and is not expanded to IP addresses. |

Every scheme uses the same path syntax. Paths preserve valid Unicode, valid `%HH` escapes, dot segments, repeated slashes, and URI path punctuation. Invalid percent escapes, unpaired UTF-16 surrogates and invalid ASCII path characters are rejected. The parser does not decode escapes, remove dot segments, or perform request-path matching. For example, `/a/../b` differs from `/b`, `/%61` differs from `/a`, and `/metrics` differs from `/metrics/`.

Binding-specific restrictions, such as an ASCII HTTP base path or a root-only TCP/UDP path, belong to the binding consumer. They are not imposed by this general-purpose type.

`ToString()` writes the canonical host, an explicit port, and the preserved path. IPv6 retains brackets and a `%25` zone delimiter. Equality and hashing include scheme, canonical host, host kind, zone, port, and path. Mapped IPv6 is not collapsed into IPv4. Two null address references are equal; null differs from every parsed value.

## Protocol combinations

```csharp
var protocol = NetworkProtocol.Parse("HTTP2 + GRPC");
// Name: grpc; Carrier: http2; HttpVersion: 2; Transport: Tcp
// ToString(): grpc+http2

var protocols = NetworkProtocol.Parse("http,http1,http3,grpc", ',');
// grpc, http1, http3 (canonical ordinal order)
```

`NetworkProtocol` is a readonly value type representing one combination, not an enum flag set. An identifier follows `[A-Za-z][A-Za-z0-9._-]*`. Upper-level names are unrestricted within this syntax. The reserved carriers are `tcp`, `udp`, `http`, `http1`, `http2`, and `http3`.

| Input | Name | Carrier |
| --- | --- | --- |
| `http2`, `udp` | null | `http2`, `udp`, respectively |
| `grpc`, `protobuf`, `custom.v2` | The supplied name, lowercased | null |
| `grpc+tcp`, `http2+grpc` | `grpc` | `tcp`, `http2`, respectively |
| `opc+http2`, `custom+http3` | `opc`, `custom`, respectively | `http2`, `http3`, respectively |

Names are case-insensitive. Outer whitespace and whitespace around `+` are trimmed. A combination must contain exactly one upper-level name and one reserved carrier. Operand order is irrelevant: `http2+grpc` equals `grpc+http2`; normalization always puts the upper-level name first. Two unknown operands, two carriers, or three operands fail because they do not identify the two roles unambiguously.

Syntax does not prove implementation support. For example, `grpc+http1` is structurally valid, while an adapter may be unable to map it. A bare `http4` is an upper-level name with an unspecified carrier, not an HTTP version recognized by Core.

`IsHttp` identifies HTTP carriers; `HttpVersion` is 1, 2, or 3, and is zero when unspecified or not HTTP. `Transport` is `ProtocolType.Tcp` for HTTP/1, HTTP/2 and direct TCP, `Udp` for HTTP/3 and direct UDP, and `Unspecified` when the carrier or HTTP version is unspecified.

Core does not give application names default carriers: `grpc` is not expanded to `grpc+http2`, and `protobuf` is not expanded to `protobuf+http`. Such conventions belong to a binding or application policy. The default struct has both `Name` and `Carrier` null, so `IsEmpty` is true and `ToString()` returns an empty string. A bare upper-level name is not empty. Parsed values use canonical equality and hashing.

## Parsing and collections

Both types expose `Parse(string)`, `TryParse(string, out T)`, and corresponding `ReadOnlySpan<char>` overloads. String overloads delegate to span parsing. The standard interfaces are implemented explicitly; generic callers constrained to `IParsable<T>` or `ISpanParsable<T>` can use their methods with an `IFormatProvider`. Parsing is culture-independent and ignores that provider; the provider is not a protocol-policy registry.

`NetworkProtocol.Parse(text, separator)` and `TryParse(text, separator, out NetworkProtocol[])` parse alternatives from a string or span. The separator is a required `char`, for example `,`, `;` or `|`. Identifier characters, `+`, whitespace, control characters and surrogate code units cannot be separators. Requiring this argument keeps single-value and collection calls distinct, including `out var` calls.

Collection parsing removes duplicates and returns a caller-owned array sorted by canonical string using ordinal comparison. A generic HTTP item is removed only when the same upper-level name has an explicit HTTP version: `http,http2` becomes `http2`; `custom+http,custom+http2` becomes `custom+http2`. An unspecified carrier is preserved: `grpc,grpc+http2` keeps both items. Different upper-level names and all explicitly declared versions remain; the parser does not select the highest version.

`Parse` throws `FormatException` for invalid input, including null or empty text and an invalid collection separator. Single-value `TryParse` returns false with a default protocol or null address. Collection `TryParse` returns false with an empty array if any item is invalid; it never returns a partial declaration. Omitted optional attributes are handled by the caller.

## Templates and consumer responsibilities

Evaluate a `protocols` template through the existing [TemplateEvaluator](expressions.md), then parse the resulting text, for example with `NetworkProtocol.Parse(text, ',')`. Expressions such as `${this.Protocols}` and `${this.Settings['protocols']}` use the same evaluator; there is no second interpolation parser in `NetworkProtocol`.

The binding consumer applies application shorthand defaults, validates binding-specific path restrictions, checks address/protocol consistency, and chooses mappings. Core parsing alone does none of these. Parsing `grpc+http3` does not inspect the backend or require it to run.

The existing `EndpointConverter` and `HandlerAttribute` are unchanged. These APIs introduce no ASP.NET Core, gRPC, Nginx or tool dependency. Tests are [NetworkAddressTest](../test/unit/Communication/NetworkAddressTest.cs) and [NetworkProtocolTest](../test/unit/Communication/NetworkProtocolTest.cs).
