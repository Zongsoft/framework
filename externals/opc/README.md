# Zongsoft.Externals.Opc Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Opc)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Opc)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Connection Settings (OpcConnectionSettings)

[`OpcConnectionSettings`](src/Configuration/OpcConnectionSettings.cs) has no `TOptions` constraint and accepts arbitrary main parameter names. `Server` and its alias `Url` address the same parameter. `OpcClient` reads typed server, timeout, identity and security properties to establish a session, rather than generating SDK options through a generic `GetOptions()` step.

The raw dictionary exposes explicit parameters; typed reads supply defaults. `Value` retains all explicit main parameters and extension properties. Retaining an unknown entry does not mean the OPC client consumes it. See the [Core settings contract](../../Zongsoft.Core/docs/settings.md) for the complete rules.

## Notification Buffer Tests

[SubscriberSpoolingTest](test/SubscriberSpoolingTest.cs) produces notifications through a real, disconnected `Subscriber` and the OPC UA SDK's `MonitoredItem.SaveValueInCache`. The path includes entry lookup, value extraction, the consumer callback, and `Spooler` batching. It creates no session, opens no port, and generates no certificate.

A single awaitable producer controls the test notification source. The synchronous consumer only captures the current `PutAsync`; after the notification callback returns, the producer awaits admission before sending another reading, retaining at most one pending operation. Coverage includes successive and repeated values, identifier/value pairing, slow consumption at capacity, flush completion, cancellation and error propagation, an explicit final flush after detaching consumers, non-draining disposal, and periodic callback cancellation, cleanup and failure notification.

From the repository root, build the current Core before running OPC tests. Use Debug so the adapter references the workspace Core assembly; Release references the published package.

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -c Debug -p:GeneratePackageOnBuild=false
dotnet build externals/opc/Zongsoft.Externals.Opc.slnx -c Debug -p:GeneratePackageOnBuild=false
dotnet test externals/opc/test/Zongsoft.Externals.Opc.Tests.csproj -c Debug -f net8.0 --no-build --filter FullyQualifiedName~SubscriberSpoolingTest
```

Replace `net8.0` with `net9.0` and `net10.0` to check the other targets. The owner decides whether to flush remaining items explicitly before closing; `DisposeAsync` does not drain the buffer itself.

The awaitable test producer is not an asynchronous backpressure solution for production subscription callbacks: `Consume(Action)` does not await asynchronous admission. These tests do not cover server sampling, network Publish dispatch, reconnection, or overflow in the SDK's own queues. Applications must choose an explicit capacity policy; discarding `PutAsync` or using an `async void` consumer does not observe completion or failures. For protocol end-to-end checks, use the [local client/server samples](samples/README.md).

## Certificate Generation

If X509 certificates are used for authentication, use [OpenSSL](https://docs.openssl.org) to generate the X509 certificate files.

> 💡 **Note:** adjust the command parameter values as needed.

### Generate a Private Key

- Use the [`openssl genpkey`](https://docs.openssl.org/master/man1/openssl-genpkey) command to generate an unencrypted private key file.

```bash
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out certificate.private.pem -outform PEM
```

- Use the [`openssl genpkey`](https://docs.openssl.org/master/man1/openssl-genpkey) command to generate an encrypted private key file. Specify the password with the `-pass` parameter.

```bash
openssl genpkey -algorithm RSA -aes256 -pkeyopt rsa_keygen_bits:2048 -out certificate.private.pem -outform PEM -pass pass:"password"
```

### Create a Self-Signed Certificate

The `-subj` parameter contains these fields:
> - CN = Common Name
> - C  = Country or region
> - ST = State or province
> - L  = Locality or city
> - O  = Organization
> - OU = Organizational unit or department

- Use the [`openssl req`](https://docs.openssl.org/master/man1/openssl-req) command to create a self-signed certificate with an unencrypted private key.

```bash
openssl req -new -x509 -key certificate.private.pem -days 3650 -out certificate.der -outform DER -subj "/C=CN/ST=Province|State/L=City/O=Organization/OU=Branch|Department/CN=Common Name/emailAddress=certificate@zongsoft.com"
```

- Use the [`openssl req`](https://docs.openssl.org/master/man1/openssl-req) command to create a self-signed certificate with an encrypted private key. Specify the private-key password with the `-passin` parameter.

```bash
openssl req -new -x509 -key certificate.private.pem -passin pass:"password" -days 3650 -out certificate.der -outform DER -subj "/C=CN/ST=Province|State/L=City/O=Organization/OU=Branch|Department/CN=Common Name/emailAddress=certificate@zongsoft.com"
```

### Merge into a PKCS#12 File

- Use the [`openssl pkcs12`](https://docs.openssl.org/master/man1/openssl-pkcs12) command to merge a certificate file without a password.
	> Note: if the private key is encrypted, specify its password with the `-passin` parameter.

```bash
openssl pkcs12 -inkey certificate.private.pem -in certificate.der -export -out certificate.pfx -passout pass:"" -name "FriendlyName"
```

```bash
openssl pkcs12 -inkey certificate.private.pem -passin pass:"password" -in certificate.der -export -out certificate.pfx -passout pass:"" -name "FriendlyName"
```

- Use the [`openssl pkcs12`](https://docs.openssl.org/master/man1/openssl-pkcs12) command to merge a certificate file with a password. Specify the certificate-file password with the `-passout` parameter.
	> Note: if the private key is encrypted, specify its password with the `-passin` parameter.

```bash
openssl pkcs12 -inkey certificate.private.pem -in certificate.der -export -out certificate.pfx -passout pass:"password" -name "FriendlyName"
```

```bash
openssl pkcs12 -inkey certificate.private.pem -passin pass:"password" -in certificate.der -export -out certificate.pfx -passout pass:"password" -name "FriendlyName"
```

## OPC UA Server

To start an OPC UA server through plugins, deploy the optional daemon plugin and option selected by `site=daemon`. The main plugin only supplies the OPC client and server types; it does not start a listener by itself.

`Server.Urls` in the daemon option determines the actual service listener addresses and accepts multiple explicit `opc.tcp` URLs separated by commas. `Server.Discovery` is the registration target and controls server announcement separately; it is not a listener. The binding is generated from the same `Urls` value and preserves URL paths. `Server.Enabled` controls both startup and the declaration.

The binding declares OPC UA over TCP only. Certificates, trust stores, security policies and externally announced Endpoints remain part of OPC UA runtime configuration; the declaration does not imply HTTP health checks, proxying or NAT mapping.

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

The plugin makes the OPC adapter and SDK available. Application code configures endpoints, certificate trust and session/subscription lifetimes; no OPC connection is implied by copying the manifest.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Opc` | [Zongsoft.Externals.Opc.plugin](src/Zongsoft.Externals.Opc.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Opc.deploy](src/Zongsoft.Externals.Opc.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals opc]
nuget:Zongsoft.Externals.Opc
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Opc.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
