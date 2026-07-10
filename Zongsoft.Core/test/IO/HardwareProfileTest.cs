using System;
using System.Linq;
using System.Text.Json;
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
	public void TestJson()
	{
		var cache = new HardwareComponent(
			"cache",
			"l3",
			"cache",
			"L3 cache",
			[
				new HardwareProperty("size", 32, "MB"),
			]);

		var processor = Hardware.Unique(
			"cpu-0",
			"processor",
			"cpu0",
			"processor",
			"Ryzen 9",
			"9000",
			"processor/cpu",
			"AMD",
			"Main processor",
			properties:
			[
				new HardwareProperty("cores", 16),
				new HardwareProperty("enabled", true),
				new HardwareProperty("note", "primary"),
			],
			components: [cache]);

		var network = new Hardware("network", "eth0", "adapter", "network/ethernet");
		var profile = new HardwareProfile([processor, network]);
		var json = JsonSerializer.Serialize(profile);

		using(var document = JsonDocument.Parse(json))
		{
			Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
			Assert.Equal("cpu-0", document.RootElement[0].GetProperty("$UniqueIdentifier").GetString());
			Assert.Equal("processor", document.RootElement[0].GetProperty("Name").GetString());
		}

		var result = JsonSerializer.Deserialize<HardwareProfile>(json);

		Assert.NotNull(result);
		Assert.Equal(2, result.Count);
		Assert.Single(result.Processors);
		Assert.Single(result.Networks);

		var hardware = result["cpu-0"];
		Assert.NotNull(hardware);
		Assert.True(hardware.HasUnique(out var identifier));
		Assert.Equal("cpu-0", identifier);
		Assert.Equal("AMD", hardware.Manufacturer);
		Assert.Equal(16, hardware.Properties["cores"].Value);
		Assert.True((bool)hardware.Properties["enabled"].Value);
		Assert.Equal("primary", hardware.Properties["note"].Value);

		var component = Assert.Single(hardware.Components);
		Assert.Equal("cache", component.Name);
		Assert.Equal(32, component.Properties["size"].Value);

		var properties = JsonSerializer.Deserialize<HardwarePropertyCollection>(JsonSerializer.Serialize(hardware.Properties));
		Assert.NotNull(properties);
		Assert.Equal(16, properties["cores"].Value);

		var components = JsonSerializer.Deserialize<HardwareComponentCollection>(JsonSerializer.Serialize(hardware.Components));
		Assert.NotNull(components);
		Assert.Equal("cache", Assert.Single(components).Name);
	}

	[Fact]
	public void TestJsonWithNamingPolicy()
	{
		var options = new JsonSerializerOptions()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		};

		var hardware = Hardware.Unique("disk-0", "disk", "disk0", "disk", "storage/disk");
		var json = JsonSerializer.Serialize<IHardware>(hardware, options);

		Assert.Contains("\"$UniqueIdentifier\"", json);
		Assert.Contains("\"category\"", json);

		var result = JsonSerializer.Deserialize<IHardware>(json, options);

		Assert.NotNull(result);
		Assert.True(result.HasUnique(out var identifier));
		Assert.Equal("disk-0", identifier);
		Assert.Equal("disk0", result.Code);
		Assert.Equal("storage/disk", result.Category);
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

		profile = await HardwareProfile.LoadAsync(provider, TestContext.Current.CancellationToken);
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
