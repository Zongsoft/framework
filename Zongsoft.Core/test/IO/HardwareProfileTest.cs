using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Zongsoft.IO.Hardwares.Tests;

public class HardwareProfileTest
{
	[Fact]
	public void TestIndex()
	{
		var first = new Hardware("mainboard", "mainboard", "mainboard", "mainboard");
		var second = new UniqueHardware("uid", "processor", "cpu#1", "processor", "processor/cpu");
		var third = new Hardware("device", "target", "other", "device");
		var profile = new HardwareProfile([first, second, third]);

		Assert.Same(first, profile["mainboard"]);
		Assert.Same(second, profile["uid"]);
		Assert.Same(second, profile["cpu#1"]);
		Assert.Null(profile["none"]);
	}

	[Fact]
	public void TestFind()
	{
		var disk = new Hardware("disk", "disk0", "disk", "storage/disk");
		var controller = new Hardware("controller", "controller0", "controller", "storage");
		var cpu = new Hardware("cpu", "cpu0", "processor", "processor/cpu");
		var profile = new HardwareProfile([disk, controller, cpu]);

		Assert.Equal([controller, disk], profile.Find("/storage/").OrderBy(hardware => hardware.Code));
		Assert.Equal([disk], profile.Find("storage/disk"));
		Assert.Equal([cpu], profile.Find("processor"));
		Assert.Equal(3, profile.Find(null).Count());
	}

	[Fact]
	public void TestComponent()
	{
		var child = new HardwareComponent("child", "child0");
		var component = new HardwareComponent("root", "root0", components: [child]);

		Assert.Equal("root", component.Name);
		Assert.Single(component.Components);
		Assert.Same(child, component.Components[0]);
	}

	[Fact]
	public async Task TestLoad()
	{
		var services = new ServiceCollection();
		services.AddSingleton<IHardwareCollector>(new Collector(
		[
			new Hardware("mainboard", "board0", "board", "mainboard"),
			new Hardware("memory", "memory0", "ram", "memory/dimm"),
			new Hardware("disk", "disk0", "disk", "storage/disk"),
		]));

		using var provider = services.BuildServiceProvider();

		var profile = HardwareProfile.Load(provider);
		Assert.Equal(3, profile.Count);
		Assert.NotNull(profile.Mainboard);
		Assert.Single(profile.Memories);
		Assert.Single(profile.Storages);
		Assert.Empty(profile.Processors);

		profile = await HardwareProfile.LoadAsync(provider);
		Assert.Equal(3, profile.Count);
	}

	private sealed class Collector(IEnumerable<IHardware> hardwares) : IHardwareCollector
	{
		public IEnumerable<IHardware> Collect() => hardwares;

		public async IAsyncEnumerable<IHardware> CollectAsync([EnumeratorCancellation] CancellationToken cancellation = default)
		{
			foreach(var hardware in hardwares)
			{
				cancellation.ThrowIfCancellationRequested();
				yield return hardware;
				await Task.Yield();
			}
		}
	}

	private sealed class UniqueHardware(string identifier, string name, string code, string type, string category) : Hardware(name, code, type, category)
	{
		private readonly string _identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));

		public override bool HasUnique(out string identifier)
		{
			identifier = _identifier;
			return true;
		}
	}
}
