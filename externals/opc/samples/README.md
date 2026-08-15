# Zongsoft.Externals.Opc Samples

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Projects

| Project | Purpose |
| --- | --- |
| [server](server) | Starts an OPC UA server with an in-memory address space containing folders, variables, objects, and arrays. |
| [client](client) | Connects to an OPC UA server and exercises browsing, reading, writing, subscriptions, and monitoring. |

Both projects target .NET 10. The server uses the OPC UA endpoint settings supplied on its command line; with no additional arguments, the client connects to `opc.tcp://localhost:4840`.

## Run

Start the server from the repository root:

```shell
dotnet run --project externals/opc/samples/server/Zongsoft.Externals.Opc.Samples.Server.csproj
```

Then start the client in another terminal:

```shell
dotnet run --project externals/opc/samples/client/Zongsoft.Externals.Opc.Samples.Client.csproj
```

## Server Commands

The server creates sample folders, scalar variables, arrays, and a `Person` object. Use the client `browse` command to discover their actual OPC node identifiers.

Display server state, elapsed time, certificate information, and active channels:

```text
info
```

Read one or more nodes:

```text
get <node-id>
get <node-id-1> <node-id-2> <node-id-3>
```

Write a scalar or array value. Values are converted to the node's declared data type:

```text
set <scalar-node-id> 42
set <array-node-id> 10 20 30
```

For a numeric node, `--round:<count>` repeats the write at 100-millisecond intervals while incrementing the value on each round:

```text
set --round:10 <numeric-node-id> 100
```

Use `stop` and `start` to exercise the server lifecycle. `start` reuses the endpoint arguments supplied when the server process was launched.

## Client Commands

The client supports connection management, browsing, single and batch reads, writes, subscriptions, statistics, and live value listening. A typical sequence is:

```text
connect
browse
subscribe <node-id-1> <node-id-2>
info
listen
unsubscribe <subscriber-id>
disconnect
```

See the [complete client instructions](client/README.md) for all command arguments, aliases, file-based bulk subscriptions, and output options. Certificate and authentication settings must match when security is enabled.
