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

partial class HardwareCollector
{
	[System.Runtime.Versioning.SupportedOSPlatform("Linux")]
	private static class LinuxGatherer
	{
		private const string DMI = "/sys/devices/virtual/dmi/id/";

		public static IEnumerable<IO.Hardwares.IHardware> Gather()
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

			return HardwareUtility.Create(
				serial,
				name,
				"mainboard",
				"baseboard",
				ReadDmi("board_name"),
				ReadDmi("board_version"),
				"mainboard",
				manufacturer,
				"Linux DMI mainboard information",
				properties: properties);
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

			return HardwareUtility.Create(
				null,
				name,
				version ?? "bios",
				"firmware",
				version,
				ReadDmi("bios_release"),
				"bios",
				ReadDmi("bios_vendor"),
				"Linux DMI BIOS information",
				properties: properties);
		}

		private static IEnumerable<IO.Hardwares.IHardware> GetProcessors()
		{
			var processors = ParseCpuInfo();
			var dmiProcessors = GetDmiProcessors().ToArray();

			if(processors.Count == 0)
			{
				if(dmiProcessors.Length > 0)
				{
					var dmiIndex = 0;

					foreach(var processor in dmiProcessors)
					{
						var properties = new List<IO.Hardwares.HardwareProperty>();
						Add(properties, processor, "Socket Designation", "ID", "Serial Number", "Version", "Manufacturer", "Family", "Core Count", "Thread Count", "Max Speed", "Current Speed");

						var name = HardwareUtility.Coalesce(Get(processor, "Version"), Get(processor, "Family"), "Processor");
						var identifier = HardwareUtility.Coalesce(Get(processor, "Serial Number"), CompactIdentifier(Get(processor, "ID")));

						yield return HardwareUtility.Create(
							identifier,
							name,
							"cpu" + dmiIndex++,
							"cpu",
							Get(processor, "Version"),
							Get(processor, "Family"),
							"processor/cpu",
							Get(processor, "Manufacturer"),
							"Linux DMI processor information",
							properties: properties);
					}

					yield break;
				}

				var result = HardwareUtility.Execute("lscpu", "");

				if(result.Succeeded)
				{
					var values = ParseKeyValues(result.Output);
					var name = Get(values, "Model name") ?? Get(values, "Architecture") ?? "Processor";
					var properties = new List<IO.Hardwares.HardwareProperty>();
					Add(properties, values, "Architecture", "CPU(s)", "Thread(s) per core", "Core(s) per socket", "Socket(s)", "Vendor ID", "CPU max MHz", "CPU min MHz");

					yield return HardwareUtility.Create(
						null,
						name,
						"cpu0",
						"cpu",
						name,
						Get(values, "CPU family"),
						"processor/cpu",
						Get(values, "Vendor ID"),
						"Linux CPU information",
						properties: properties);
				}

				yield break;
			}

			var index = 0;

			foreach(var processor in processors.Values)
			{
				var properties = new List<IO.Hardwares.HardwareProperty>();
				var dmi = index < dmiProcessors.Length ? dmiProcessors[index] : null;
				Add(properties, processor, "processor", "physical id", "core id", "cpu cores", "siblings", "cpu MHz", "cache size", "cpu family", "model", "stepping", "microcode");
				Add(properties, dmi, "Socket Designation", "ID", "Serial Number", "Version", "Manufacturer", "Family", "Core Count", "Thread Count", "Max Speed", "Current Speed");

				var name = HardwareUtility.Coalesce(Get(processor, "model name"), Get(processor, "Processor"), Get(dmi, "Version"), "Processor");
				var code = "cpu" + index++;
				var identifier = HardwareUtility.Coalesce(Get(processor, "Serial"), Get(dmi, "Serial Number"), CompactIdentifier(Get(dmi, "ID")));

				yield return HardwareUtility.Create(
					identifier,
					name,
					code,
					"cpu",
					name,
					HardwareUtility.Coalesce(Get(processor, "cpu family"), Get(dmi, "Family")),
					"processor/cpu",
					HardwareUtility.Coalesce(Get(processor, "vendor_id"), Get(dmi, "Manufacturer")),
					"Linux /proc/cpuinfo processor",
					properties: properties);
			}
		}

		private static IEnumerable<Dictionary<string, string>> GetDmiProcessors()
		{
			var result = HardwareUtility.Execute("dmidecode", "-t processor", 8000);

			if(!result.Succeeded)
				yield break;

			foreach(var section in ParseDmidecodeSections(result.Output, "Processor Information"))
				yield return section;
		}

		private static string CompactIdentifier(string value) => string.IsNullOrEmpty(value) ? null : new string(value.Where(chr => !char.IsWhiteSpace(chr)).ToArray());

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
				yield return HardwareUtility.Create(
					null,
					"System Memory",
					"memory",
					"memory",
					null,
					null,
					"memory",
					null,
					"Linux total system memory",
					properties: properties);
		}

		private static IEnumerable<IO.Hardwares.IHardware> GetDmiMemories()
		{
			var result = HardwareUtility.Execute("dmidecode", "-t memory", 8000);

			if(!result.Succeeded)
				yield break;

			var index = 0;
			var arrays = ParseDmidecodeSections(result.Output, "Physical Memory Array").ToArray();
			var controllers = ParseDmidecodeSections(result.Output, "Memory Controller Information").ToArray();

			foreach(var section in ParseDmidecodeSections(result.Output, "Memory Device"))
			{
				var size = Get(section, "Size");

				if(string.IsNullOrEmpty(size) || size.Contains("No Module Installed", StringComparison.OrdinalIgnoreCase))
					continue;

				var properties = new List<IO.Hardwares.HardwareProperty>();
				Add(properties, section, "Size", "Form Factor", "Locator", "Bank Locator", "Type", "Type Detail", "Speed", "Configured Memory Speed", "Manufacturer", "Serial Number", "Part Number", "Asset Tag");

				var locator = HardwareUtility.Coalesce(Get(section, "Locator"), Get(section, "Bank Locator"), "DIMM" + index);
				var serial = Get(section, "Serial Number");
				var components = GetDmiMemoryComponents(arrays, controllers);

				yield return HardwareUtility.Create(
					serial,
					locator,
					"memory" + index++,
					"dimm",
					Get(section, "Part Number"),
					null,
					"memory/dimm",
					Get(section, "Manufacturer"),
					"Linux DMI memory device",
					properties: properties,
					components: components);
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
			var result = HardwareUtility.Execute("lsblk", "-J -b -o NAME,KNAME,PATH,MODEL,SERIAL,SIZE,TYPE,VENDOR,TRAN,ROTA,FSTYPE,MOUNTPOINT,LABEL,UUID,PARTUUID,PARTLABEL", 8000);

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
					var components = GetLsblkDiskComponents(device);

					yield return HardwareUtility.Create(
						serial,
						name,
						code,
						"disk",
						Get(device, "model"),
						null,
						"storage/disk",
						Get(device, "vendor"),
						"Linux block disk",
						properties: properties,
						components: components);
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

				var components = GetSysfsDiskComponents(directory, name);

				yield return HardwareUtility.Create(
					serial,
					HardwareUtility.Coalesce(model, name),
					"/dev/" + name,
					"disk",
					model,
					null,
					"storage/disk",
					vendor,
					"Linux sysfs block disk",
					properties: properties,
					components: components);
			}
		}

		private static IEnumerable<IO.Hardwares.HardwareComponent> GetLsblkDiskComponents(JsonElement disk)
		{
			if(disk.ValueKind != JsonValueKind.Object || !disk.TryGetProperty("children", out var children) || children.ValueKind != JsonValueKind.Array)
				return [];

			var components = new List<IO.Hardwares.HardwareComponent>();

			foreach(var child in children.EnumerateArray())
				AddLsblkDiskComponent(components, child);

			return components;
		}

		private static void AddLsblkDiskComponent(List<IO.Hardwares.HardwareComponent> components, JsonElement element)
		{
			if(components == null || element.ValueKind != JsonValueKind.Object)
				return;

			var type = Get(element, "type");

			if(!string.Equals(type, "part", StringComparison.OrdinalIgnoreCase) &&
			   !string.Equals(type, "raid", StringComparison.OrdinalIgnoreCase) &&
			   !string.Equals(type, "crypt", StringComparison.OrdinalIgnoreCase) &&
			   !string.Equals(type, "lvm", StringComparison.OrdinalIgnoreCase))
				return;

			var properties = new List<IO.Hardwares.HardwareProperty>();
			Add(properties, element, "name", "kname", "path", "size", "type", "fstype", "mountpoint", "label", "uuid", "partuuid", "partlabel");

			var children = new List<IO.Hardwares.HardwareComponent>();

			if(element.TryGetProperty("children", out var childElements) && childElements.ValueKind == JsonValueKind.Array)
			{
				foreach(var child in childElements.EnumerateArray())
					AddLsblkDiskComponent(children, child);
			}

			var name = HardwareUtility.Coalesce(Get(element, "partlabel"), Get(element, "label"), Get(element, "path"), Get(element, "name"), "Partition");
			var code = HardwareUtility.Coalesce(Get(element, "path"), Get(element, "partuuid"), Get(element, "uuid"), Get(element, "name"));

			components.Add(new IO.Hardwares.HardwareComponent(
				name,
				code,
				"storage/partition",
				"Disk partition",
				properties,
				children));
		}

		private static IEnumerable<IO.Hardwares.HardwareComponent> GetSysfsDiskComponents(string directory, string disk)
		{
			if(string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(disk))
				return [];

			var components = new List<IO.Hardwares.HardwareComponent>();

			foreach(var partition in Directory.EnumerateDirectories(directory, disk + "*"))
			{
				if(!File.Exists(Path.Combine(partition, "partition")))
					continue;

				var name = Path.GetFileName(partition);
				var sectors = HardwareUtility.ReadText(Path.Combine(partition, "size"));
				var start = HardwareUtility.ReadText(Path.Combine(partition, "start"));
				var properties = new List<IO.Hardwares.HardwareProperty>();

				HardwareUtility.Add(properties, "Name", name);
				HardwareUtility.Add(properties, "Path", "/dev/" + name);
				HardwareUtility.Add(properties, "Start", start);

				if(ulong.TryParse(sectors, out var count))
					HardwareUtility.Add(properties, "Size", count * 512UL);

				components.Add(new IO.Hardwares.HardwareComponent(
					"/dev/" + name,
					"/dev/" + name,
					"storage/partition",
					"Disk partition",
					properties));
			}

			return components;
		}

		private static IEnumerable<IO.Hardwares.HardwareComponent> GetDmiMemoryComponents(
			IEnumerable<IReadOnlyDictionary<string, string>> arrays,
			IEnumerable<IReadOnlyDictionary<string, string>> controllers)
		{
			var components = new List<IO.Hardwares.HardwareComponent>();
			var index = 0;

			foreach(var array in arrays ?? [])
			{
				var properties = new List<IO.Hardwares.HardwareProperty>();
				Add(properties, array, "Location", "Use", "Error Correction Type", "Maximum Capacity", "Number Of Devices");

				if(properties.Count > 0)
					components.Add(new IO.Hardwares.HardwareComponent("Physical Memory Array", "array" + index++, "memory/array", "Physical memory array", properties));
			}

			index = 0;

			foreach(var controller in controllers ?? [])
			{
				var properties = new List<IO.Hardwares.HardwareProperty>();
				Add(properties, controller, "Error Detecting Method", "Error Correcting Capabilities", "Supported Interleave", "Current Interleave", "Maximum Memory Module Size", "Maximum Total Memory Size", "Supported Speeds", "Supported Memory Types", "Memory Module Voltage", "Associated Memory Slots");

				if(properties.Count > 0)
					components.Add(new IO.Hardwares.HardwareComponent("Memory Controller", "controller" + index++, "memory/controller", "Memory controller", properties));
			}

			return components;
		}

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
			var readTitle = false;

			foreach(var line in (text ?? string.Empty).Split('\n'))
			{
				var content = line.TrimEnd();

				if(content.StartsWith("Handle ", StringComparison.OrdinalIgnoreCase))
				{
					if(current != null)
						yield return current;

					current = null;
					readTitle = true;
					continue;
				}

				if(readTitle)
				{
					if(string.Equals(content.Trim(), sectionName, StringComparison.OrdinalIgnoreCase))
						current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

					readTitle = false;
					continue;
				}

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

		private static string ReadDmi(string name) => HardwareUtility.ReadText(DMI + name);
		private static bool IsVirtualBlockDevice(string name) =>
			string.IsNullOrEmpty(name) ||
			name.StartsWith("loop", StringComparison.OrdinalIgnoreCase) ||
			name.StartsWith("ram", StringComparison.OrdinalIgnoreCase) ||
			name.StartsWith("dm-", StringComparison.OrdinalIgnoreCase);
	}
}
