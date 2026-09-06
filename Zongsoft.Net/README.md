# Zongsoft.Net Networking Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Net)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Net)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**N**et](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Net) is a high-performance networking library in the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides functionality for _**S**ocket_ network development.

It provides asynchronous TCP clients, servers, channels, connection accounting, broadcast, and packet framing on top of `System.IO.Pipelines` and `Pipelines.Sockets.Unofficial`. Use it for application-specific binary protocols where HTTP, gRPC, or a broker is not the right transport.

## Packets and Streams

TCP is a byte stream: one send is not guaranteed to equal one receive. An `IPacketizer<T>` defines message boundaries. `Headed` uses a 4-byte big-endian length prefix and produces `ReadOnlySequence<byte>`; `Headless` treats currently available bytes as an `IMemoryOwner<byte>` package. Custom `TcpClient<T>`/`TcpServer<T>` instances accept an application packetizer.

Use `Headed` for most message protocols. Use `Headless` only when another protocol layer or connection lifetime provides unambiguous boundaries.

## Installation and Client Example

```shell
dotnet add package Zongsoft.Net
```

```csharp
using System.Buffers;
using System.Net;
using System.Text;
using Zongsoft.Net;

var client = TcpClient.Headed;
client.Address = new IPEndPoint(IPAddress.Loopback, 7969);
await client.ConnectAsync();
await client.SendAsync(
	new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("hello")),
	cancellationToken);
await client.DisconnectAsync(cancellationToken);
```

Assign an `IHandler<T>` to receive packages. The client reconnects lazily on send and reports total bytes; a server manages a channel per connection and can broadcast.

> 💡 The runnable [samples](samples/README.md) demonstrate handlers, start/stop, sends, and broadcasts on `127.0.0.1:7969`.

## Lifetime and Safety

Dispose or disconnect clients and stop servers during graceful shutdown. Propagate cancellation; handlers for different connections may run concurrently.

🚨 Framing is not security. Add authentication, integrity, encryption, package-size limits, timeouts, and admission limits. Never expose the sample plaintext listener to an untrusted network.

Custom packetizers must consume bytes only after a complete frame is available. Retaining a sequence or pooled memory beyond its callback lifetime can corrupt data or leak buffers.

## Related Resources

- [Runnable samples](samples/README.md)
- [System.IO.Pipelines](https://learn.microsoft.com/dotnet/standard/io/pipelines)
- [TCP specification](https://www.rfc-editor.org/rfc/rfc9293)
- [Implementation guidance](SKILL.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../Zongsoft.Plugins/README.md).

The manifest contributes the FTP file-system provider; the TCP API examples remain explicitly constructed network objects. Loading this plugin does not automatically start a TCP listener.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Net` | [Zongsoft.Net.plugin](src/Zongsoft.Net.plugin) |
| File copying and dependencies | [Zongsoft.Net.deploy](src/Zongsoft.Net.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft net]
nuget:Zongsoft.Net
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Net.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
