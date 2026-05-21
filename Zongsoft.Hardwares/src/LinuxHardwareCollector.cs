/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Hardwares library.
 *
 * The Zongsoft.Hardwares is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Hardwares is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Hardwares library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;

namespace Zongsoft.Hardwares;

internal sealed class LinuxHardwareCollector : HardwareCollectorBase
{
	private const string DMI = "/sys/devices/virtual/dmi/id/";

	public static readonly LinuxHardwareCollector Instance = new();

	private LinuxHardwareCollector() { }

	public override IEnumerable<IO.Hardwares.IHardware> Collect()
	{
		var mainboard = GetMainboard();
		if(mainboard != null)
			yield return mainboard;

		var bios = GetBios();
		if(bios != null)
			yield return bios;

		foreach(var processor in GetProcessors())
			yield return processor;

		foreach(var memory in GetMemories())
			yield return memory;

		foreach(var disk in GetDisks())
			yield return disk;
	}

	private static IO.Hardwares.IHardware GetMainboard()
	{
		var properties = new List<IO.Hardwares.HardwareProperty>();
		HardwareUtility.Add(properties, "ProductName", ReadDmi("product_name"));
		HardwareUtility.Add(properties, "ProductVersion", ReadDmi("product_version"));
		HardwareUtility.Add(properties, "ProductSerial", ReadDmi("product_serial"));
		HardwareUtility.Add(properties, "ProductUuid", ReadDmi("product_uuid"));
		HardwareUtility.Add(properties, "BoardAssetTag", ReadDmi("board_asset_tag"));

		var name = HardwareUtility.Coalesce(ReadDmi("board_name"), ReadDmi("product_name"), "Mainboard");
		var manufacturer = HardwareUtility.Coalesce(ReadDmi("board_vendor"), ReadDmi("sys_vendor"));
		var serial = HardwareUtility.Coalesce(ReadDmi("board_serial"), ReadDmi("product_serial"), ReadDmi("product_uuid"));

		if(properties.Count == 0 && string.IsNullOrEmpty(manufacturer) && string.IsNullOrEmpty(serial))
			return null;

		return new UniqueHardware(serial, name, "mainboard", "baseboard", ReadDmi("board_name"), ReadDmi("board_version"), "mainboard", properties: properties)
		{
			Manufacturer = manufacturer,
			Description = "Linux DMI mainboard information",
		};
	}

	private static IO.Hardwares.IHardware GetBios()
	{
		var properties = new List<IO.Hardwares.HardwareProperty>();
		HardwareUtility.Add(properties, "BiosVendor", ReadDmi("bios_vendor"));
		HardwareUtility.Add(properties, "BiosVersion", ReadDmi("bios_version"));
		HardwareUtility.Add(properties, "BiosDate", ReadDmi("bios_date"));
		HardwareUtility.Add(properties, "BiosRelease", ReadDmi("bios_release"));

		var version = ReadDmi("bios_version");
		var name = HardwareUtility.Coalesce(version, "BIOS");

		if(properties.Count == 0 && string.IsNullOrEmpty(version))
			return null;

		return new UniqueHardware(version, name, version ?? "bios", "firmware", version, ReadDmi("bios_release"), "bios", properties: properties)
		{
			Manufacturer = ReadDmi("bios_vendor"),
			Description = "Linux DMI BIOS information",
		};
	}

	private static IEnumerable<IO.Hardwares.IHardware> GetProcessors()
	{
		var processors = ParseCpuInfo();

		if(processors.Count == 0)
		{
			var result = HardwareUtility.Execute("lscpu", "");

			if(result.Succeeded)
			{
				var values = ParseKeyValues(result.Output);
				var name = Get(values, "Model name") ?? Get(values, "Architecture") ?? "Processor";
				var properties = new List<IO.Hardwares.HardwareProperty>();
				Add(properties, values, "Architecture", "CPU(s)", "Thread(s) per core", "Core(s) per socket", "Socket(s)", "Vendor ID", "CPU max MHz", "CPU min MHz");

				yield return new UniqueHardware(Get(values, "BIOS Model name"), name, "cpu0", "cpu", name, Get(values, "CPU family"), "processor/cpu", properties: properties)
				{
					Manufacturer = Get(values, "Vendor ID"),
					Description = "Linux CPU information",
				};
			}

			yield break;
		}

		var index = 0;

		foreach(var processor in processors.Values)
		{
			var properties = new List<IO.Hardwares.HardwareProperty>();
			Add(properties, processor, "processor", "physical id", "core id", "cpu cores", "siblings", "cpu MHz", "cache size", "cpu family", "model", "stepping", "microcode");

			var name = Get(processor, "model name") ?? Get(processor, "Processor") ?? "Processor";
			var code = "cpu" + index++;
			var identifier = HardwareUtility.Coalesce(Get(processor, "Serial"), Get(processor, "physical id"), Get(processor, "processor"));

			yield return new UniqueHardware(identifier, name, code, "cpu", name, Get(processor, "cpu family"), "processor/cpu", properties: properties)
			{
				Manufacturer = Get(processor, "vendor_id"),
				Description = "Linux /proc/cpuinfo processor",
			};
		}
	}

	private static IEnumerable<IO.Hardwares.IHardware> GetMemories()
	{
		var memories = GetDmiMemories().ToArray();

		if(memories.Length > 0)
		{
			foreach(var memory in memories)
				yield return memory;

			yield break;
		}

		var meminfo = ParseKeyValues(HardwareUtility.ReadText("/proc/meminfo"));
		var total = HardwareUtility.ParseKilobytes(Get(meminfo, "MemTotal"));
		var properties = new List<IO.Hardwares.HardwareProperty>();
		HardwareUtility.Add(properties, "Capacity", total);
		HardwareUtility.Add(properties, "Source", "/proc/meminfo");

		if(total != null)
			yield return new UniqueHardware(null, "System Memory", "memory", "memory", null, null, "memory", properties: properties)
			{
				Description = "Linux total system memory",
			};
	}

	private static IEnumerable<IO.Hardwares.IHardware> GetDmiMemories()
	{
		var result = HardwareUtility.Execute("dmidecode", "-t memory", 8000);

		if(!result.Succeeded)
			yield break;

		var index = 0;

		foreach(var section in ParseDmidecodeSections(result.Output, "Memory Device"))
		{
			var size = Get(section, "Size");

			if(string.IsNullOrEmpty(size) || size.Contains("No Module Installed", StringComparison.OrdinalIgnoreCase))
				continue;

			var properties = new List<IO.Hardwares.HardwareProperty>();
			Add(properties, section, "Size", "Form Factor", "Locator", "Bank Locator", "Type", "Type Detail", "Speed", "Configured Memory Speed", "Manufacturer", "Serial Number", "Part Number", "Asset Tag");

			var locator = HardwareUtility.Coalesce(Get(section, "Locator"), Get(section, "Bank Locator"), "DIMM" + index);
			var serial = Get(section, "Serial Number");

			yield return new UniqueHardware(serial, locator, "memory" + index++, "dimm", Get(section, "Part Number"), null, "memory/dimm", properties: properties)
			{
				Manufacturer = Get(section, "Manufacturer"),
				Description = "Linux DMI memory device",
			};
		}
	}

	private static IEnumerable<IO.Hardwares.IHardware> GetDisks()
	{
		var disks = GetLsblkDisks().ToArray();

		if(disks.Length > 0)
		{
			foreach(var disk in disks)
				yield return disk;

			yield break;
		}

		foreach(var disk in GetSysfsDisks())
			yield return disk;
	}

	private static IEnumerable<IO.Hardwares.IHardware> GetLsblkDisks()
	{
		var result = HardwareUtility.Execute("lsblk", "-J -b -d -o NAME,KNAME,PATH,MODEL,SERIAL,SIZE,TYPE,VENDOR,TRAN,ROTA", 8000);

		if(!result.Succeeded)
			yield break;

		JsonDocument document;

		try
		{
			document = JsonDocument.Parse(result.Output);
		}
		catch(JsonException)
		{
			yield break;
		}

		using(document)
		{
			if(!document.RootElement.TryGetProperty("blockdevices", out var devices) || devices.ValueKind != JsonValueKind.Array)
				yield break;

			foreach(var device in devices.EnumerateArray())
			{
				if(!string.Equals(Get(device, "type"), "disk", StringComparison.OrdinalIgnoreCase))
					continue;

				var properties = new List<IO.Hardwares.HardwareProperty>();
				Add(properties, device, "name", "kname", "path", "vendor", "model", "serial", "size", "tran", "rota");

				var name = HardwareUtility.Coalesce(Get(device, "model"), Get(device, "path"), Get(device, "name"), "Disk");
				var code = HardwareUtility.Coalesce(Get(device, "path"), Get(device, "name"));
				var serial = Get(device, "serial");

				yield return new UniqueHardware(serial, name, code, "disk", Get(device, "model"), null, "storage/disk", properties: properties)
				{
					Manufacturer = Get(device, "vendor"),
					Description = "Linux block disk",
				};
			}
		}
	}

	private static IEnumerable<IO.Hardwares.IHardware> GetSysfsDisks()
	{
		const string root = "/sys/block";

		if(!Directory.Exists(root))
			yield break;

		foreach(var directory in Directory.EnumerateDirectories(root))
		{
			var name = System.IO.Path.GetFileName(directory);

			if(IsVirtualBlockDevice(name))
				continue;

			var model = HardwareUtility.ReadText(Path.Combine(directory, "device/model"));
			var vendor = HardwareUtility.ReadText(Path.Combine(directory, "device/vendor"));
			var serial = HardwareUtility.ReadText(Path.Combine(directory, "device/serial"));
			var sectors = HardwareUtility.ReadText(Path.Combine(directory, "size"));
			var properties = new List<IO.Hardwares.HardwareProperty>();

			HardwareUtility.Add(properties, "Name", name);
			HardwareUtility.Add(properties, "Model", model);
			HardwareUtility.Add(properties, "Vendor", vendor);
			HardwareUtility.Add(properties, "Serial", serial);

			if(ulong.TryParse(sectors, out var count))
				HardwareUtility.Add(properties, "Size", count * 512UL);

			yield return new UniqueHardware(serial, HardwareUtility.Coalesce(model, name), "/dev/" + name, "disk", model, null, "storage/disk", properties: properties)
			{
				Manufacturer = vendor,
				Description = "Linux sysfs block disk",
			};
		}
	}

	private static string ReadDmi(string name) => HardwareUtility.ReadText(DMI + name);

	private static Dictionary<string, Dictionary<string, string>> ParseCpuInfo()
	{
		var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
		var text = HardwareUtility.ReadText("/proc/cpuinfo");

		if(string.IsNullOrEmpty(text))
			return result;

		var current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach(var line in text.Split('\n'))
		{
			var content = line.Trim();

			if(content.Length == 0)
			{
				AddProcessor(result, current);
				current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				continue;
			}

			var index = content.IndexOf(':');

			if(index > 0)
				current[content[..index].Trim()] = content[(index + 1)..].Trim();
		}

		AddProcessor(result, current);
		return result;
	}

	private static void AddProcessor(Dictionary<string, Dictionary<string, string>> processors, Dictionary<string, string> current)
	{
		if(current == null || current.Count == 0)
			return;

		var key = HardwareUtility.Coalesce(Get(current, "physical id"), Get(current, "Serial"), "0");

		if(!processors.ContainsKey(key))
			processors.Add(key, current);
	}

	private static IEnumerable<Dictionary<string, string>> ParseDmidecodeSections(string text, string sectionName)
	{
		Dictionary<string, string> current = null;

		foreach(var line in (text ?? string.Empty).Split('\n'))
		{
			var content = line.TrimEnd();

			if(string.Equals(content.Trim(), sectionName, StringComparison.OrdinalIgnoreCase))
			{
				if(current != null)
					yield return current;

				current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				continue;
			}

			if(current == null)
				continue;

			var index = content.IndexOf(':');

			if(index > 0)
				current[content[..index].Trim()] = content[(index + 1)..].Trim();
		}

		if(current != null)
			yield return current;
	}

	private static Dictionary<string, string> ParseKeyValues(string text)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		if(string.IsNullOrEmpty(text))
			return result;

		foreach(var line in text.Split('\n'))
		{
			var content = line.Trim();
			var index = content.IndexOf(':');

			if(index > 0)
				result[content[..index].Trim()] = content[(index + 1)..].Trim();
		}

		return result;
	}

	private static void Add(List<IO.Hardwares.HardwareProperty> properties, IReadOnlyDictionary<string, string> values, params string[] names)
	{
		for(int i = 0; i < names.Length; i++)
			HardwareUtility.Add(properties, names[i], Get(values, names[i]));
	}

	private static void Add(List<IO.Hardwares.HardwareProperty> properties, JsonElement values, params string[] names)
	{
		for(int i = 0; i < names.Length; i++)
			HardwareUtility.Add(properties, names[i], Get(values, names[i]));
	}

	private static string Get(IReadOnlyDictionary<string, string> values, string name) => values != null && values.TryGetValue(name, out var value) ? HardwareUtility.Normalize(value) : null;

	private static string Get(JsonElement element, string name)
	{
		if(element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var property))
			return null;

		return property.ValueKind switch
		{
			JsonValueKind.String => HardwareUtility.Normalize(property.GetString()),
			JsonValueKind.Number => property.GetRawText(),
			JsonValueKind.True => bool.TrueString,
			JsonValueKind.False => bool.FalseString,
			_ => null,
		};
	}

	private static bool IsVirtualBlockDevice(string name) =>
		string.IsNullOrEmpty(name) ||
		name.StartsWith("loop", StringComparison.OrdinalIgnoreCase) ||
		name.StartsWith("ram", StringComparison.OrdinalIgnoreCase) ||
		name.StartsWith("dm-", StringComparison.OrdinalIgnoreCase);
}
