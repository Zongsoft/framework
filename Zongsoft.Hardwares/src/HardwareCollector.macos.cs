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
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;

namespace Zongsoft.Hardwares;

partial class HardwareCollector
{
	[System.Runtime.Versioning.SupportedOSPlatform("MacOS")]
	private static class MacosGatherer
	{
		public static IEnumerable<IO.Hardwares.IHardware> Gather()
		{
			var profile = GetHardwareProfile();

			var mainboard = GetMainboard(profile);
			if(mainboard != null)
				yield return mainboard;

			var bios = GetBios(profile);
			if(bios != null)
				yield return bios;

			var processor = GetProcessor(profile);
			if(processor != null)
				yield return processor;

			foreach(var memory in GetMemories(profile))
				yield return memory;

			foreach(var disk in GetDisks())
				yield return disk;
		}

		private static IO.Hardwares.IHardware GetMainboard(IReadOnlyDictionary<string, string> profile)
		{
			var properties = new List<IO.Hardwares.HardwareProperty>();
			Add(properties, profile, "machine_name", "machine_model", "serial_number", "platform_UUID", "model_number");

			var name = HardwareUtility.Coalesce(Get(profile, "machine_name"), Get(profile, "machine_model"), "Mac");
			var model = Get(profile, "machine_model");
			var serial = HardwareUtility.Coalesce(Get(profile, "serial_number"), Get(profile, "platform_UUID"));

			return HardwareUtility.Create(
				serial,
				name,
				model ?? "mainboard",
				"mainboard",
				model,
				null,
				"mainboard",
				"Apple",
				"macOS hardware overview",
				properties: properties);
		}

		private static IO.Hardwares.IHardware GetBios(IReadOnlyDictionary<string, string> profile)
		{
			var version = HardwareUtility.Coalesce(Get(profile, "boot_rom_version"), Get(profile, "system_firmware_version"));
			var properties = new List<IO.Hardwares.HardwareProperty>();
			Add(properties, profile, "boot_rom_version", "system_firmware_version", "os_loader_version");

			if(string.IsNullOrEmpty(version) && properties.Count == 0)
				return null;

			return HardwareUtility.Create(
				null,
				version ?? "Apple Firmware",
				version ?? "bios",
				"firmware",
				version,
				null,
				"bios",
				"Apple",
				"macOS firmware information",
				properties: properties);
		}

		private static IO.Hardwares.IHardware GetProcessor(IReadOnlyDictionary<string, string> profile)
		{
			var name = HardwareUtility.Coalesce(
				Get(profile, "chip_type"),
				Get(profile, "processor_name"),
				HardwareUtility.Execute("sysctl", "-n machdep.cpu.brand_string").Output,
				"Processor");

			var properties = new List<IO.Hardwares.HardwareProperty>();
			Add(properties, profile, "chip_type", "processor_name", "processor_speed", "number_processors", "total_number_cores", "l2_cache", "l3_cache", "hyper_threading");
			HardwareUtility.Add(properties, "hw.physicalcpu", HardwareUtility.Execute("sysctl", "-n hw.physicalcpu").Output);
			HardwareUtility.Add(properties, "hw.logicalcpu", HardwareUtility.Execute("sysctl", "-n hw.logicalcpu").Output);

			return HardwareUtility.Create(
				null,
				name,
				"cpu0",
				"cpu",
				name,
				null,
				"processor/cpu",
				"Apple",
				"macOS processor information",
				properties: properties);
		}

		private static IEnumerable<IO.Hardwares.IHardware> GetMemories(IReadOnlyDictionary<string, string> profile)
		{
			var memories = GetPhysicalMemories().ToArray();

			if(memories.Length > 0)
			{
				foreach(var memory in memories)
					yield return memory;

				yield break;
			}

			var systemMemory = GetSystemMemory(profile);
			if(systemMemory != null)
				yield return systemMemory;
		}

		private static IO.Hardwares.IHardware GetSystemMemory(IReadOnlyDictionary<string, string> profile)
		{
			var bytes = HardwareUtility.ParseBytes(HardwareUtility.Execute("sysctl", "-n hw.memsize").Output);
			var properties = new List<IO.Hardwares.HardwareProperty>();
			Add(properties, profile, "memory");
			HardwareUtility.Add(properties, "Capacity", bytes);

			if(properties.Count == 0)
				return null;

			return HardwareUtility.Create(
				null,
				"System Memory",
				"memory",
				"memory",
				null,
				null,
				"memory",
				"Apple",
				"macOS total system memory",
				properties: properties);
		}

		private static IEnumerable<IO.Hardwares.IHardware> GetPhysicalMemories()
		{
			var index = 0;

			foreach(var entry in GetSystemProfile("SPMemoryDataType"))
			{
				if(!IsPhysicalMemory(entry))
					continue;

				var properties = new List<IO.Hardwares.HardwareProperty>();
				Add(properties, entry, "_name", "dimm_size", "dimm_type", "dimm_speed", "dimm_status", "manufacturer", "part_number", "serial_number");

				var locator = Get(entry, "_name");
				var serial = Get(entry, "serial_number");
				var partNumber = Get(entry, "part_number");
				var name = HardwareUtility.Coalesce(partNumber, locator, "Physical Memory");
				var code = HardwareUtility.Coalesce(locator, serial, "memory" + index);
				var components = GetMemoryComponents(locator);

				yield return HardwareUtility.Create(
					serial,
					name,
					code,
					"dimm",
					partNumber,
					null,
					"memory/dimm",
					HardwareUtility.Coalesce(Get(entry, "manufacturer"), "Apple"),
					"macOS physical memory module",
					properties: properties,
					components: components);

				index++;
			}
		}

		private static IEnumerable<IO.Hardwares.IHardware> GetDisks()
		{
			var index = 0;

			foreach(var entry in GetStorageEntries(GetSystemProfileEntries("SPNVMeDataType", "SPSerialATADataType", "SPStorageDataType")))
			{
				if(IsVolume(entry.Values))
					continue;

				var properties = new List<IO.Hardwares.HardwareProperty>();
				Add(properties, entry.Values, "_name", "device_model", "bsd_name", "serial_num", "size", "medium_type", "protocol", "removable_media", "smart_status", "spsata_revision");

				var name = HardwareUtility.Coalesce(Get(entry.Values, "device_model"), Get(entry.Values, "_name"), Get(entry.Values, "bsd_name"), "Disk");
				var serial = Get(entry.Values, "serial_num");
				var code = HardwareUtility.Coalesce(Get(entry.Values, "bsd_name"), serial, "disk" + index);
				var components = GetDiskComponents(entry);

				if(properties.Count == 0)
					continue;

				yield return HardwareUtility.Create(
					serial,
					name,
					code,
					"disk",
					Get(entry.Values, "device_model"),
					Get(entry.Values, "spsata_revision"),
					"storage/disk",
					HardwareUtility.Coalesce(Get(entry.Values, "manufacturer"), "Apple"),
					"macOS storage device",
					properties: properties,
					components: components);

				index++;
			}
		}

		private static IReadOnlyDictionary<string, string> GetHardwareProfile()
		{
			var profiles = GetSystemProfile("SPHardwareDataType");
			return profiles.FirstOrDefault() ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}

		private static IEnumerable<Dictionary<string, string>> GetSystemProfile(params string[] dataTypes)
		{
			var arguments = string.Join(' ', dataTypes ?? []) + " -json";
			var result = HardwareUtility.Execute("system_profiler", arguments, 15000);

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
				foreach(var child in document.RootElement.EnumerateObject())
				{
					if(child.Value.ValueKind != JsonValueKind.Array)
						continue;

					foreach(var item in child.Value.EnumerateArray())
					{
						foreach(var entry in Flatten(item))
							yield return entry;
					}
				}
			}
		}

		private static IEnumerable<ProfileEntry> GetSystemProfileEntries(params string[] dataTypes)
		{
			var arguments = string.Join(' ', dataTypes ?? []) + " -json";
			var result = HardwareUtility.Execute("system_profiler", arguments, 15000);

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
				foreach(var child in document.RootElement.EnumerateObject())
				{
					if(child.Value.ValueKind != JsonValueKind.Array)
						continue;

					foreach(var item in child.Value.EnumerateArray())
					{
						var entry = CreateEntry(item);

						if(entry != null)
							yield return entry;
					}
				}
			}
		}

		private static IEnumerable<ProfileEntry> GetStorageEntries(IEnumerable<ProfileEntry> entries)
		{
			if(entries == null)
				yield break;

			foreach(var entry in entries)
			{
				if(entry == null)
					continue;

				if(HasStorageDeviceValues(entry.Values))
					yield return entry;

				foreach(var child in GetStorageEntries(entry.Children))
					yield return child;
			}
		}

		private static IEnumerable<IO.Hardwares.HardwareComponent> GetDiskComponents(ProfileEntry disk)
		{
			if(disk == null)
				return [];

			var components = new List<IO.Hardwares.HardwareComponent>();
			AddVolumeComponents(components, disk.Children);
			return components;
		}

		private static void AddVolumeComponents(List<IO.Hardwares.HardwareComponent> components, IEnumerable<ProfileEntry> entries)
		{
			if(components == null || entries == null)
				return;

			var index = components.Count;

			foreach(var entry in entries)
			{
				if(entry == null)
					continue;

				if(IsVolume(entry.Values))
				{
					var properties = new List<IO.Hardwares.HardwareProperty>();
					Add(properties, entry.Values, "_name", "bsd_name", "mount_point", "file_system", "size", "free_space_in_bytes", "writable", "volume_uuid");

					var name = HardwareUtility.Coalesce(Get(entry.Values, "_name"), Get(entry.Values, "bsd_name"), "Volume");
					var code = HardwareUtility.Coalesce(Get(entry.Values, "bsd_name"), Get(entry.Values, "volume_uuid"), "volume" + index);

					components.Add(new IO.Hardwares.HardwareComponent(
						name,
						code,
						"storage/volume",
						"Disk volume",
						properties));

					index++;
				}

				AddVolumeComponents(components, entry.Children);
			}
		}

		private static ProfileEntry CreateEntry(JsonElement element)
		{
			if(element.ValueKind != JsonValueKind.Object)
				return null;

			var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var children = new List<ProfileEntry>();

			foreach(var property in element.EnumerateObject())
			{
				if(property.Value.ValueKind == JsonValueKind.Array)
				{
					foreach(var item in property.Value.EnumerateArray())
					{
						var child = CreateEntry(item);

						if(child != null)
							children.Add(child);
					}

					continue;
				}

				var value = GetValue(property.Value);

				if(!string.IsNullOrEmpty(value))
					values[property.Name] = value;
			}

			return values.Count == 0 && children.Count == 0 ? null : new ProfileEntry(values, children);
		}

		private static IEnumerable<Dictionary<string, string>> Flatten(JsonElement element)
		{
			if(element.ValueKind != JsonValueKind.Object)
				yield break;

			var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var children = new List<JsonElement>();

			foreach(var property in element.EnumerateObject())
			{
				if(property.Value.ValueKind == JsonValueKind.Array)
				{
					children.AddRange(property.Value.EnumerateArray());
					continue;
				}

				var value = GetValue(property.Value);

				if(!string.IsNullOrEmpty(value))
					result[property.Name] = value;
			}

			if(result.Count > 0)
				yield return result;

			for(int i = 0; i < children.Count; i++)
			{
				foreach(var child in Flatten(children[i]))
					yield return child;
			}
		}

		private static bool IsVolume(IReadOnlyDictionary<string, string> values) => values != null &&
			(values.ContainsKey("mount_point") || values.ContainsKey("file_system") || values.ContainsKey("free_space_in_bytes"));

		private static bool IsPhysicalMemory(IReadOnlyDictionary<string, string> values) => values != null &&
			(values.ContainsKey("dimm_size") || values.ContainsKey("part_number") || values.ContainsKey("serial_number"));

		private static bool HasStorageDeviceValues(IReadOnlyDictionary<string, string> values) => values != null &&
			!IsVolume(values) &&
			(values.ContainsKey("device_model") || values.ContainsKey("serial_num") || values.ContainsKey("smart_status") || values.ContainsKey("medium_type") || values.ContainsKey("spsata_revision"));

		private static IEnumerable<IO.Hardwares.HardwareComponent> GetMemoryComponents(string locator)
		{
			if(string.IsNullOrEmpty(locator))
				return [];

			var components = new List<IO.Hardwares.HardwareComponent>();
			var parts = locator.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			for(int i = 0; i < parts.Length; i++)
			{
				if(parts[i].StartsWith("BANK", StringComparison.OrdinalIgnoreCase))
					components.Add(new IO.Hardwares.HardwareComponent(parts[i], parts[i], "memory/bank", "Memory bank"));
				else if(parts[i].StartsWith("Channel", StringComparison.OrdinalIgnoreCase))
					components.Add(new IO.Hardwares.HardwareComponent(parts[i], parts[i], "memory/channel", "Memory channel"));
			}

			return components;
		}

		private static void Add(List<IO.Hardwares.HardwareProperty> properties, IReadOnlyDictionary<string, string> values, params string[] names)
		{
			for(int i = 0; i < names.Length; i++)
				HardwareUtility.Add(properties, names[i], Get(values, names[i]));
		}

		private static string Get(IReadOnlyDictionary<string, string> values, string name) => values != null && values.TryGetValue(name, out var value) ? HardwareUtility.Normalize(value) : null;
		private static string GetValue(JsonElement element) => element.ValueKind switch
		{
			JsonValueKind.String => HardwareUtility.Normalize(element.GetString()),
			JsonValueKind.Number => element.GetRawText(),
			JsonValueKind.True => bool.TrueString,
			JsonValueKind.False => bool.FalseString,
			_ => null,
		};

		private sealed class ProfileEntry(IReadOnlyDictionary<string, string> values, IReadOnlyList<ProfileEntry> children)
		{
			public IReadOnlyDictionary<string, string> Values { get; } = values ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			public IReadOnlyList<ProfileEntry> Children { get; } = children ?? [];
		}
	}
}
