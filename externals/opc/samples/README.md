# Zongsoft.Externals.Opc Samples

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Projects

| Project | Purpose |
| --- | --- |
| [server](server) | Starts an OPC UA server with an in-memory address space containing folders, variables, objects, and arrays. |
| [client](client) | Connects to an OPC UA server and exercises browsing, reading, writing, subscriptions, and monitoring. |

Both projects target .NET 10. The server uses the OPC UA endpoint settings supplied on its command line; the client's `connect` command defaults to `opc.tcp://localhost:4840` with the additional identity settings declared in its source.

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

## Safety, Expected Result, and Cleanup

🚨 These commands can write node values. Use only the in-memory sample address space, not a production industrial endpoint. Starting the client does not establish a session; `connect` without arguments uses the defaults in [client/Program.cs](client/Program.cs), including its user/certificate fields. To request anonymous local access explicitly, use `connect 'opc.tcp://localhost:4840'` if permitted by the server.

Browse an existing numeric variable, subscribe to it, and use the server's `set` command on that same identifier. The client should observe the changed value while listening. Leave `listen` with `Ctrl+C` before issuing cleanup commands.

Unsubscribe and disconnect the client, then run `exit` and confirm. Run `stop` and `exit` in the server terminal. Values initialized by [server/Program.cs](server/Program.cs) are in-memory; do not remove certificate files or trust stores shared by other applications. For application-side provider selection and certificate concepts, see the [library guide](../README.md).
