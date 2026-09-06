# Zongsoft.Net Samples

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Projects

| Project | Purpose |
| --- | --- |
| [server](server) | Runs a headed TCP server, displays received UTF-8 messages, and broadcasts acknowledgements. |
| [client](client) | Connects to the TCP server and sends or receives headed UTF-8 messages. |

Both projects target .NET 10 and use the length-prefixed `TcpServer.Headed` and `TcpClient.Headed` APIs. The default endpoint is `127.0.0.1:7969`.

## Run

Start the server from the repository root:

```shell
dotnet run --project Zongsoft.Net/samples/server/Zongsoft.Net.Samples.Server.csproj
```

Then start the client in another terminal:

```shell
dotnet run --project Zongsoft.Net/samples/client/Zongsoft.Net.Samples.Client.csproj
```

The server accepts an optional IP address and port. The client accepts the same two positional arguments:

```shell
dotnet run --project Zongsoft.Net/samples/server/Zongsoft.Net.Samples.Server.csproj -- 0.0.0.0 9000
dotnet run --project Zongsoft.Net/samples/client/Zongsoft.Net.Samples.Client.csproj -- 127.0.0.1 9000
```

## Commands

On the client, use `connect`, `disconnect`, `info`, and `send <message>`. Sending automatically establishes a connection when needed.

On the server, use `start [address] [port]`, `stop`, `info`, and `broadcast <message>`. Every client message is printed and acknowledged to all connected clients with an `ACK:` prefix.

## Suggested Scenario

1. Start the server and client.
2. Run `connect` and `send Hello Zongsoft.Net` in the client.
3. Confirm that the server prints the message and the client receives `ACK: Hello Zongsoft.Net`.
4. Run `broadcast Maintenance starts soon` in the server and confirm that the client receives it.

## Framing and Key Code

TCP is a byte stream, not a message queue. The headed packetizer adds a four-byte length prefix so a receiver can join split reads and separate adjacent messages. Both peers must use the same packetizer. See [the library guide](../README.md) before replacing either side with another client.

Read [client/Program.cs](client/Program.cs) and [server/Program.cs](server/Program.cs) for command registration and handlers. The ACK text is sample application data, not a durable protocol acknowledgment.

## Prerequisites and Cleanup

Use .NET 10 and an interactive terminal. Debug builds may require the local Core output; build `Zongsoft.Core/src/Zongsoft.Core.csproj -f net10.0` first if that referenced assembly is missing.

Run `disconnect` on clients and `stop` on the server before exiting the terminals. No database or message files are created. A port-in-use failure usually means a previous server process remains.

🚨 Binding to `0.0.0.0` exposes all interfaces. Use loopback for this unauthenticated, unencrypted demonstration; it is not a production secure transport.
