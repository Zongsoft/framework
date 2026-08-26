# Zongsoft.Messaging.ZeroMQ Protocol

This document specifies the current, backward-incompatible `1.0` wire protocol used by the ZeroMQ provider. All text frames use UTF-8. Field names and commands are case-sensitive. Mixed protocol versions are not supported.

## 1. Topology

| Subsystem | Client socket | Broker socket | Purpose |
| --- | --- | --- | --- |
| Discovery | REQ | REP | Discover the Broker epoch and runtime ports |
| Broadcast | XPUB / SUB | XSUB / XPUB | `MostOnce` broadcast |
| Control | DEALER | ROUTER | `LeastOnce` registration, acceptance, delivery, and acknowledgement |

The Control endpoint is enabled only when the Broker has an `IMessageStorage`. Discovery, Broadcast, and Control remain independent; Control does not implement the request/response adapter's application semantics.

## 2. Discovery

The request is one text frame:

```text
Zongsoft.Messaging.ZeroMQ
Protocol-Version:1.0
Command:Discover
Instance:<client-instance>
```

With Control enabled, a successful response orders `Ports` as `Control,Incoming,Outgoing`:

```text
Zongsoft.Messaging.ZeroMQ
Protocol-Version:1.0
Epoch:<broker-epoch>
Ports:<control>,<incoming>,<outgoing>
```

Without Control, the Control port is omitted:

```text
Zongsoft.Messaging.ZeroMQ
Protocol-Version:1.0
Epoch:<broker-epoch>
Ports:<incoming>,<outgoing>
```

Clients accept exactly two or three port values. A Broker restart creates a new `Epoch`; clients invalidate prior Broadcast subscription state and reconnect.

## 3. Broadcast

### 3.1 Welcome

The Broker XPUB uses a Welcome frame to synchronize the Broker epoch:

```text
\0Zongsoft.Messaging.ZeroMQ
Protocol-Version:1.0
Epoch:<broker-epoch>\0
```

### 3.2 Business messages

Every Broadcast business message has exactly two frames:

```text
Frame 0:
<physical-topic>
Protocol-Version:1.0
Identifier:<message-id>
Identity:<producer-instance>
Tags:<tags>                    # optional
Compression:<algorithm>       # optional

Frame 1:
<payload>
```

`physical-topic` includes the optional `Group:` prefix; the subscriber removes it before constructing `Message`. Header values cannot contain CR or LF. `Tags` may contain commas, semicolons, and colons. `Compression` is omitted for uncompressed messages. Compression applies only to the second-frame business payload.

A heartbeat is an anonymous empty-payload message without Identifier, Identity, Tags, or Compression. An empty payload with an Identifier is a normal business message.

If the application XPUB has no matching subscription at publication time, the client returns `null`, does not wait for a future subscriber, and does not send the message.

## 4. Control

ROUTER identity frames are excluded from the fixed frame counts below. DEALER frames are:

### 4.1 Registration and liveness

```text
REGISTER   Session, Subscription, Topic
REGISTERED Subscription
UNREGISTER Session, Subscription
PING       Session, Subscription
```

### 4.2 Publication acceptance

```text
PUBLISH Identifier, Topic, Identity, Tags, Timestamp, Expiration, Compression, Data
ACCEPTED Identifier
UNROUTABLE Identifier
ERROR Code, Identifier
```

`Timestamp` and a non-zero `Expiration` are UTC ticks. The Compression frame is empty when uncompressed. The Broker validates only the algorithm name and persists Data unchanged; it does not decompress ordinary business payloads.

With no online matching subscription, the Broker returns `UNROUTABLE` and writes nothing. Otherwise, Pending persistence must complete before the Broker returns `ACCEPTED`.

### 4.3 Delivery and acknowledgement

```text
DELIVER Subscription, Identifier, Topic, Identity, Tags, Timestamp, Attempt, Compression, Data
ACK     Session, Subscription, Identifier
```

The subscriber decompresses Data before constructing `Message`. The Broker selects competing online subscribers. An unacknowledged message is redelivered with the same Identifier; any valid ACK stops delivery and begins asynchronous removal.

## 5. Persistent payload

The outer Storage record uses `Message.Identifier`, `Topic`, `Identity`, `Tags`, and `Timestamp`. Its private Data serializes:

- `Version`: always `1.0`;
- `Compression`: the algorithm name, or empty when uncompressed;
- `Data`: the compressed business payload;
- `Expiration`: the absolute expiration time.

The Broker does not read old persistent formats. Clear or replace an existing Pending Storage before deploying protocol `1.0`.

## 6. Limits and errors

- Header: 16 KiB; Topic: 1024 bytes; Identifier: 256 characters; Payload: 64 MiB.
- Broadcast business messages have exactly two frames; Control commands have fixed frame counts.
- Supported payload algorithms are Brotli, GZip, ZLib, and Deflate, matched case-insensitively.
- A malformed frame, unknown algorithm, or damaged compressed payload drops only the current message and records a diagnostic; it must not terminate the Poller.
- A non-null `MostOnce` result means local send only. A non-null `LeastOnce` result means durable Broker acceptance only. Neither means that a Handler ran.
