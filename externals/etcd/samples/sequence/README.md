# Etcd Sequence Sample

[English](README.md) | [简体中文](README.zh-Hans.md)

## Purpose and Concepts

This executable demonstrates integer and floating-point distributed increments through [EtcdService](../../README.md). The adapter uses compare-and-swap transactions to resolve concurrent changes; a sequence is stored data, not a process-local counter.

The seed applies only when the key is absent. The first result is `seed + interval`, not the seed itself.

## Prerequisites and Run

Use .NET 10 and a disposable etcd v3 endpoint. Debug builds require the local Core DLL. From the framework root:

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -f net10.0
dotnet run --project externals/etcd/samples/sequence -- "server=127.0.0.1;port=2379"
```

The first positional argument overrides the default connection settings. The program always selects namespace `samples:sequence`.

## Key Code

```csharp
using Zongsoft.Externals.Etcd;

using var service = new EtcdService("sample", "server=127.0.0.1;port=2379")
{
	Namespace = "samples:sequence",
};

var number = await service.IncreaseAsync("orders", seed: 1000);
var fraction = await service.IncreaseAsync("score", 0.25, 1.5);
Console.WriteLine(number);
Console.WriteLine(fraction);
```

## Expected Result and Cleanup

With absent keys the results are 1001 and 1.75. A second run produces 1002 and 2.0; another process or preexisting data changes these values. Floating-point operations follow double precision and are not appropriate for exact monetary amounts.

Disposal closes the client, not the stored values. After all sample processes stop, remove only `samples:sequence:orders` and `samples:sequence:score` to reset the experiment.

🚨 Do not reset a sequence used by business identifiers: reusing values can create duplicate identities.
