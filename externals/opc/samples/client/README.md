# OPC.UA Client Sample

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Purpose and Startup

This .NET 10 interactive client exercises OPC UA connection, subscription, and monitoring. Start the [local sample server](../README.md) first and verify its endpoint and security settings, then run the following command from the repository root:

```shell
dotnet run --project externals/opc/samples/client/Zongsoft.Externals.Opc.Samples.Client.csproj
```

[Program.cs](Program.cs) owns the client and registers the commands below. These are sample-terminal commands, not Bash scripts. Starting the program does not itself connect a session. Explicitly run `connect`; without arguments it uses the source's default connection string, including user/certificate settings, rather than guaranteeing anonymous access. An explicit endpoint-only argument demonstrates anonymous access if the server permits it.

🚨 Use only a local test server, not an industrial device. Replace the credential placeholders with a dedicated test account when required; do not paste real connection strings or `info` output into shared logs. Node IDs below illustrate syntax and may not exist on your server: discover actual identifiers with `browse` before subscribing. Authentication and certificate trust must be configured independently of node discovery.

## Commands

### Connect Command `connect`

Connects to the specified OPC.UA server. If no argument is specified, it connects to the local machine by default.

- Connect to a server anonymously:

```bash
connect 'opc.tcp://127.0.0.1:4840'
```

- Connect to a server with a specified user:
```bash
connect 'server=opc.tcp://127.0.0.1:4840;username=test-user;password=REPLACE_WITH_TEST_PASSWORD'
```

- Connect to a server with a specified certificate file:
```bash
connect 'server=opc.tcp://127.0.0.1:4840;Certificate=zfs.local:./certificates/certificate.pfx;CertificateSecret=REPLACE_WITH_TEST_CERTIFICATE_PASSWORD'
```

### Disconnect Command `disconnect`

Disconnects the current connection. This command has no arguments.

```bash
disconnect
```

### Info Command `info`

Shows current client information, including:

- Client name
- Current connection state
- Last heartbeat time
- Connection settings
- Subscriber information
	- Subscriber ID
	- Subscriber state
	- Subscriber statistics
	- Subscribed metric set
	- Internal object information, when the `detailed` option is enabled

- Show information for all subscribers:

```bash
info
```

- Show information for specified subscribers by passing subscriber IDs as arguments:

```bash
info 11 12 13
```

### Reset Command `reset`

Resets subscriber statistics.

- Reset statistics for all subscriptions:

```bash
reset
```

- Reset statistics for specified subscribers by passing one or more subscriber IDs separated by spaces:

```bash
reset 11 12 13
```

> Note: subscriber IDs can be found with the `info` command.

### Subscribe Command `subscribe`

Subscribes to the specified metric data. This command has the alias `sub`.

- Subscribe to one or more specified metrics separated by spaces:

```bash
subscribe ns=2;s=variable1 ns=2;i=1001 ns=2;g=E9DBF5F2-0AAB-49C0-AEE4-E1251A2CDCEA
```

- Add new subscription items to the specified subscriber:

```bash
subscribe -subscriber:11 ns=2;s=variable1 ns=2;i=1001 ns=2;g=E9DBF5F2-0AAB-49C0-AEE4-E1251A2CDCEA
```

- Load subscription metrics from files for bulk subscription:

```bash
subscribe -directory
subscribe -directory        filename1.txt filename2.txt filenameN.txt
subscribe -directory:subdir filename1.txt filename2.txt filenameN.txt
```

> - If the `directory` option is present without a value, its value defaults to `subscription` under the application's output directory. Omitting the option selects direct node arguments instead.
> - If no file names are specified, all `.txt` files in that subdirectory are loaded by default.

### Unsubscribe Command `unsubscribe`

Unsubscribes from subscriptions. This command has the alias `unsub`.

- Unsubscribe specified subscribers by passing one or more subscriber IDs separated by spaces:

```bash
unsubscribe 11 12 13
```

> Note: subscriber IDs can be found with the `info` command.

- Unsubscribe all subscriptions:

```bash
unsubscribe
```

### Listen Command `listen`

Listens to subscribed metric data. After entering listening mode, press `Ctrl` + `C` to exit. You can pass subscriber IDs as arguments; if no arguments are specified, all subscriptions are listened to.

Use the `spooling` option to enable buffered listening mode. Buffered mode also supports the following options:

> - `limit`: the buffer item limit. The default value is `1000`.
> - `period`: the buffer period in milliseconds. The default value is `1000`.
> - `distinct`: enables de-duplication. De-duplication is disabled by default.

- Listen:

> Note: normal listening can fall behind the data refresh rate when there are too many subscriptions, causing backlog.

```bash
listen
```

- Buffered listening:

```bash
listen -spooling -limit:10000
listen -spooling -limit:10000 -distinct
```

## Expected Result and Cleanup

After a successful connection and subscription to an existing variable, changing it in the local server should produce a value notification in `listen`. A successful connection alone does not prove the node exists or that the account can access it.

Press `Ctrl+C` to leave listening mode, then run `unsubscribe`, `disconnect`, and `exit` (confirm the exit prompt). Stop the sample server separately. Keep certificate trust files unless you deliberately created a disposable test store; never delete a shared certificate directory as routine cleanup.
