# 网络地址与协议值

[English](networking.md) | [简体中文](networking.zh-Hans.md)

`Zongsoft.Communication.NetworkAddress` 和 `NetworkProtocol` 为服务宿主及制作工具提供共同的值解析。二者均实现 `ISpanParsable<T>`，同时满足其基接口 `IParsable<T>`。它们不发现服务、不执行 XML 处理指令、不选择代理路由、不解析 DNS、不探测后端，也不改变现有监听。

## 地址

```csharp
var address = NetworkAddress.Parse("http://*:4317/opentelemetry");
// Scheme: http; Host: 0.0.0.0; Port: 4317; Path: /opentelemetry
// HostType: IPv4; AddressFamily: InterNetwork; IsAny: true

var custom = NetworkAddress.Parse("custom+v2://localhost:7000/a/../b%2Fc");
// Scheme: custom+v2; Path: /a/../b%2Fc
```

`NetworkAddress` 是表示 `scheme://host[:port][/path]` 的不可变引用类型。方案遵循 `[A-Za-z][A-Za-z0-9+.-]*`，不设置应用协议白名单。`mqtt`、`opc.tcp` 等均为普通方案名称，不从名称推断传输。

HTTP、HTTPS 的默认端口分别为 80、443；其它方案必须显式指定端口。所有端口必须在 1～65535 范围。必须提供主机；拒绝用户信息、查询串、片段、反斜杠及内部空白或控制字符，裁剪整个输入两端的空白。此类型表示网络地址，不负责解析所有 URI 形式，例如无主机的 Unix socket 路径。

| 属性 | 含义 |
| --- | --- |
| `Scheme` | 小写方案，不从其名称或端口号推断传输或版本。 |
| `Host` | 规范化的 IP 文本，或转为 IDNA ASCII、小写并去尾部根点的 DNS 名称；不含 IPv6 方括号和区域标识。IP 数值解析沿用 `IPAddress.TryParse`。 |
| `HostType` | `UriHostNameType.Dns`、`IPv4` 或 `IPv6`。 |
| `AddressFamily` | `InterNetwork`、`InterNetworkV6`；DNS 主机为 `Unspecified`，不执行名称解析。 |
| `Zone` | 区分大小写的 IPv6 区域标识，未声明时为 null。URL 使用 `%25` 分隔及 ASCII 非保留字符标识，例如 `[fe80::1%25Ethernet]`。 |
| `Port` | 实际端口，包含 HTTP/HTTPS 的默认值。 |
| `Path` | 原始的区分大小写路径，省略时为 `/`；尾部斜杠有意义。 |
| `IsAny` | `*` 与 `0.0.0.0` 为 IPv4 通配；`[::]` 为 IPv6 通配，两种地址族不合并。 |
| `IsLoopback` | `localhost` 或 IP 回环地址。`localhost` 保留为 DNS 主机，不展开成 IP。 |

所有方案使用相同的路径语法。路径保留有效 Unicode、合法 `%HH` 转义、点段、重复斜杠及 URI 路径标点；拒绝非法百分号转义、未配对的 UTF-16 代理项及非法 ASCII 路径字符。解析器不解码转义、不消解点段，也不执行请求路径匹配。例如，`/a/../b` 与 `/b`、`/%61` 与 `/a`、`/metrics` 与 `/metrics/` 均保持为不同值。

绑定场景要求的 ASCII HTTP 基路径、TCP/UDP 仅允许根路径等限制由绑定消费者执行，不由此通用类型施加。

`ToString()` 输出规范主机、显式端口及原始路径。IPv6 保留方括号，并以 `%25` 分隔区域标识。相等和哈希包括方案、规范主机、主机种类、区域、端口及路径。mapped IPv6 不合并为 IPv4。两个空地址引用相等，空引用与任何解析出的值不相等。

## 协议组合

```csharp
var protocol = NetworkProtocol.Parse("HTTP2 + GRPC");
// Name: grpc; Carrier: http2; HttpVersion: 2; Transport: Tcp
// ToString(): grpc+http2

var protocols = NetworkProtocol.Parse("http,http1,http3,grpc", ',');
// grpc, http1, http3（按规范字符串序数排序）
```

`NetworkProtocol` 是表示单项组合的只读值类型，不是枚举标志集合。标识符遵循 `[A-Za-z][A-Za-z0-9._-]*`，上层名称在此语法内开放。保留的承载名称为 `tcp`、`udp`、`http`、`http1`、`http2`、`http3`。

| 输入 | Name | Carrier |
| --- | --- | --- |
| `http2`、`udp` | null | 分别为 `http2`、`udp` |
| `grpc`、`protobuf`、`custom.v2` | 输入名称的小写形式 | null |
| `grpc+tcp`、`http2+grpc` | `grpc` | 分别为 `tcp`、`http2` |
| `opc+http2`、`custom+http3` | 分别为 `opc`、`custom` | 分别为 `http2`、`http3` |

名称不区分大小写，裁剪输入两端及 `+` 两侧空白。组合必须恰好包含一个上层名称和一个保留承载。顺序无关：`http2+grpc` 等于 `grpc+http2`，规范化始终将上层名称置前。两个未知操作数、两个承载或三个操作数均失败，因为它们不能明确表达这两个角色。

语法有效不证明实现支持。例如，`grpc+http1` 在结构上有效，但适配器可能无法映射。单独的 `http4` 表示未指定承载的上层名称，不是 Core 已识别的 HTTP 版本。

`IsHttp` 标识 HTTP 承载；`HttpVersion` 为 1、2 或 3，未声明或不是 HTTP 时为零。HTTP/1、HTTP/2 和直接 TCP 的 `Transport` 为 `ProtocolType.Tcp`，HTTP/3 和直接 UDP 为 `Udp`，未声明承载或 HTTP 版本时为 `Unspecified`。

Core 不为应用名称补全默认承载：不把 `grpc` 展开为 `grpc+http2`，也不把 `protobuf` 展开为 `protobuf+http`。这些约定属于绑定或应用策略。结构的默认值中 `Name`、`Carrier` 均为 null，因而 `IsEmpty` 为真，`ToString()` 返回空字符串；单独的上层名称不是空协议。解析出的值采用规范化后的相等和哈希语义。

## 解析与集合

二者均公开 `Parse(string)`、`TryParse(string, out T)` 及对应的 `ReadOnlySpan<char>` 重载，字符串重载转交 span 解析。标准接口采用显式实现；受 `IParsable<T>` 或 `ISpanParsable<T>` 约束的泛型调用方可使用带 `IFormatProvider` 的接口方法。解析与文化无关，并忽略该提供程序；它不承担协议策略注册的职责。

`NetworkProtocol.Parse(text, separator)` 和 `TryParse(text, separator, out NetworkProtocol[])` 从字符串或 span 解析并列项。分隔符是必填的 `char`，例如 `,`、`;` 或 `|`；不能使用标识字符、`+`、空白、控制字符或代理项。必填参数使单值和集合调用保持明确，包括使用 `out var` 的调用。

集合解析去重后按规范字符串进行序数排序，返回调用方拥有的数组。只有相同上层名称具有显式 HTTP 版本时才移除其泛称：`http,http2` 得到 `http2`，`custom+http,custom+http2` 得到 `custom+http2`。未指定承载的值仍然保留：`grpc,grpc+http2` 保留两项。不同上层名称及所有明确声明的版本保持独立，解析器不选择最高版本。

`Parse` 对无效输入（包括 null、空文本及非法集合分隔符）抛出 `FormatException`。单值 `TryParse` 失败返回 false，并输出默认协议或空地址引用。集合 `TryParse` 在任一项无效时返回 false 和空数组，不返回部分接受的声明。可选属性的省略由调用方处理。

## 模板与消费者职责

先使用既有 [TemplateEvaluator](expressions.zh-Hans.md) 对 `protocols` 模板求值，再解析结果，例如调用 `NetworkProtocol.Parse(text, ',')`。`${this.Protocols}` 与 `${this.Settings['protocols']}` 共用同一求值器，`NetworkProtocol` 不实现第二套插值语法。

绑定消费者负责应用协议简写的默认承载、绑定专用的路径限制、地址与协议一致性校验及映射选择；Core 的值解析不承担这些策略。解析 `grpc+http3` 不检查后端，也不要求后端运行。

现有 `EndpointConverter` 与 `HandlerAttribute` 保持原有行为。新增 API 不引入 ASP.NET Core、gRPC、Nginx 或工具依赖。测试入口为 [NetworkAddressTest](../test/Communication/NetworkAddressTest.cs) 和 [NetworkProtocolTest](../test/Communication/NetworkProtocolTest.cs)。
