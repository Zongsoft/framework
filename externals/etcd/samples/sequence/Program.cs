using System;

using Zongsoft.Externals.Etcd;

var connectionString = args.Length > 0 ? args[0] : "server=127.0.0.1;port=2379";
using var sequence = new EtcdService("sample", connectionString) { Namespace = "samples:sequence" };

var number = await sequence.IncreaseAsync("orders", seed: 1000);
var fraction = await sequence.IncreaseAsync("score", 0.25, 1.5);

Console.WriteLine($"Order number: {number}");
Console.WriteLine($"Score: {fraction}");
Console.WriteLine("Run the sample again to observe persistent increments.");
