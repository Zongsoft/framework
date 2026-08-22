using System;
using System.Threading.Tasks;

using Zongsoft.Externals.Etcd;
using Zongsoft.Services.Distributing;

var connectionString = args.Length > 0 ? args[0] : "server=127.0.0.1;port=2379";
using var etcd = new EtcdService("slaver", connectionString) { Namespace = "samples:distributed-lock" };
var options = new DistributedLockOptions(TimeSpan.FromSeconds(5)) { RenewalInterval = TimeSpan.FromSeconds(2) };

for(var index = 0; index < 10; index++)
{
	await using var locker = await etcd.AcquireAsync("worker", options);
	await locker.EnterAsync();
	var value = await etcd.IncreaseAsync("counter");
	Console.WriteLine($"SLAVER fence={locker.FencingToken}, counter={value}");
	await Task.Delay(1000);
}
