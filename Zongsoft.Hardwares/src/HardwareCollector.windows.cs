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
using System.Management;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Zongsoft.Hardwares;

partial class HardwareCollector
{
	[System.Runtime.Versioning.SupportedOSPlatform("Windows")]
	private static class WindowsGatherer
	{
		public static IEnumerable<IO.Hardwares.IHardware> Gather()
		{
			foreach(var hardware in GetMainboards())
				yield return hardware;

			foreach(var hardware in GetBioses())
				yield return hardware;

			foreach(var hardware in GetProcessors())
				yield return hardware;

			foreach(var hardware in GetMemories())
				yield return hardware;

			foreach(var hardware in GetDisks())
				yield return hardware;
		}

		private static IEnumerable<IO.Hardwares.IHardware> GetMainboards()
		{
			foreach(var item in Query("SELECT * FROM Win32_BaseBoard"))
			{
				var properties = new List<IO.Hardwares.HardwareProperty>();
				Add(properties, item, "Product", "Version", "SerialNumber", "AssetTag", "HostingBoard", "HotSwappable", "Removable", "Replaceable");

				var name = Get(item, "Name", "Product", "Caption") ?? "Mainboard";
				var code = Get(item, "Tag", "Product", "SerialNumber");
				var serial = Get(item, "SerialNumber");

				yield return HardwareUtility.Create(
					serial,
					name,
					code,
					"baseboard",
					Get(item, "Product"),
					Get(item, "Version"),
					"mainboard",
					Get(item, "Manufacturer"),
					Get(item, "Description"),
					properties: properties);
			}
		}

		private static IEnumerable<IO.Hardwares.IHardware> GetBioses()
		{
			foreach(var item in Query("SELECT * FROM Win32_BIOS"))
			{
				var properties = new List<IO.Hardwares.HardwareProperty>();
				Add(properties, item, "SMBIOSBIOSVersion", "BIOSVersion", "ReleaseDate", "SerialNumber", "Version", "PrimaryBIOS");

				var name = Get(item, "Name", "Caption", "SMBIOSBIOSVersion") ?? "BIOS";
				var code = Get(item, "SMBIOSBIOSVersion", "Version", "SerialNumber");
				var serial = Get(item, "SerialNumber");

				yield return HardwareUtility.Create(
					serial,
					name,
					code,
					"firmware",
					Get(item, "SMBIOSBIOSVersion", "Version"),
					null,
					"bios",
					Get(item, "Manufacturer"),
					Get(item, "Description"),
					properties: properties);
			}
		}

		private static IEnumerable<IO.Hardwares.IHardware> GetProcessors()
		{
			foreach(var item in Query("SELECT * FROM Win32_Processor"))
			{
				var properties = new List<IO.Hardwares.HardwareProperty>();
				Add(properties, item, "DeviceID", "ProcessorId", "SocketDesignation", "NumberOfCores", "NumberOfLogicalProcessors", "MaxClockSpeed", "CurrentClockSpeed", "Architecture", "AddressWidth", "DataWidth", "L2CacheSize", "L3CacheSize");

				var name = Get(item, "Name", "Caption") ?? "Processor";
				var code = Get(item, "DeviceID", "ProcessorId");
				var identifier = Get(item, "ProcessorId");

				yield return HardwareUtility.Create(
					identifier,
					name,
					code,
					"cpu",
					Get(item, "Name"),
					Get(item, "Family"),
					"processor/cpu",
					Get(item, "Manufacturer"),
					Get(item, "Description"),
					properties: properties);
			}
		}

		private static IEnumerable<IO.Hardwares.IHardware> GetMemories()
		{
			foreach(var item in Query("SELECT * FROM Win32_PhysicalMemory"))
			{
				var properties = new List<IO.Hardwares.HardwareProperty>();
				Add(properties, item, "BankLabel", "DeviceLocator", "Capacity", "Speed", "ConfiguredClockSpeed", "MemoryType", "FormFactor", "PartNumber", "SerialNumber", "Tag");

				var locator = Get(item, "DeviceLocator", "BankLabel", "Tag");
				var partNumber = Get(item, "PartNumber");
				var name = HardwareUtility.Coalesce(partNumber, Get(item, "Manufacturer"), locator, "Physical Memory");
				var code = Get(item, "Tag", "DeviceLocator", "BankLabel", "SerialNumber");
				var serial = Get(item, "SerialNumber");
				var components = GetMemoryComponents(Get(item, "DeviceLocator"), Get(item, "BankLabel"));

				yield return HardwareUtility.Create(
					serial,
					name,
					code,
					"dimm",
					partNumber,
					null,
					"memory/dimm",
					Get(item, "Manufacturer"),
					"Physical memory module",
					properties: properties,
					components: components);
			}
		}

		private static IEnumerable<IO.Hardwares.IHardware> GetDisks()
		{
			foreach(var item in Query("SELECT * FROM Win32_DiskDrive"))
			{
				var properties = new List<IO.Hardwares.HardwareProperty>();
				Add(properties, item, "DeviceID", "PNPDeviceID", "SerialNumber", "FirmwareRevision", "InterfaceType", "MediaType", "Size", "Partitions", "BytesPerSector", "SectorsPerTrack", "TotalHeads", "TotalSectors", "TotalTracks");

				var name = Get(item, "Model", "Caption", "Name") ?? "Disk Drive";
				var code = Get(item, "DeviceID", "PNPDeviceID", "SerialNumber");
				var serial = Get(item, "SerialNumber");
				var components = GetDiskComponents(item);

				yield return HardwareUtility.Create(
					serial,
					name,
					code,
					"disk",
					Get(item, "Model"),
					Get(item, "FirmwareRevision"),
					"storage/disk",
					Get(item, "Manufacturer"),
					Get(item, "Description"),
					properties: properties,
					components: components);
			}
		}

		private static IEnumerable<IO.Hardwares.HardwareComponent> GetDiskComponents(ManagementBaseObject disk)
		{
			var components = new List<IO.Hardwares.HardwareComponent>();
			var index = 0;

			foreach(var partition in QueryAssociators(disk, "Win32_DiskDriveToDiskPartition"))
			{
				var properties = new List<IO.Hardwares.HardwareProperty>();
				Add(properties, partition, "DeviceID", "DiskIndex", "Index", "Name", "Size", "StartingOffset", "BootPartition", "PrimaryPartition", "Type");

				var name = Get(partition, "Name", "DeviceID") ?? "Partition";
				var code = Get(partition, "DeviceID", "Index") ?? "partition" + index;
				var volumes = GetPartitionComponents(partition);

				components.Add(new IO.Hardwares.HardwareComponent(
					name,
					code,
					"storage/partition",
					"Disk partition",
					properties,
					volumes));

				index++;
			}

			return components;
		}

		private static IEnumerable<IO.Hardwares.HardwareComponent> GetPartitionComponents(ManagementBaseObject partition)
		{
			var components = new List<IO.Hardwares.HardwareComponent>();
			var index = 0;

			foreach(var logicalDisk in QueryAssociators(partition, "Win32_LogicalDiskToPartition"))
			{
				var properties = new List<IO.Hardwares.HardwareProperty>();
				Add(properties, logicalDisk, "DeviceID", "VolumeName", "VolumeSerialNumber", "FileSystem", "Size", "FreeSpace", "DriveType", "ProviderName");

				var name = Get(logicalDisk, "VolumeName", "DeviceID") ?? "Logical Disk";
				var code = Get(logicalDisk, "DeviceID", "VolumeSerialNumber") ?? "volume" + index;

				components.Add(new IO.Hardwares.HardwareComponent(
					name,
					code,
					"storage/volume",
					"Logical disk volume",
					properties));

				index++;
			}

			return components;
		}

		private static IEnumerable<IO.Hardwares.HardwareComponent> GetMemoryComponents(string locator, string bank)
		{
			var components = new List<IO.Hardwares.HardwareComponent>();

			if(!string.IsNullOrEmpty(locator))
			{
				var parts = locator.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

				for(int i = 0; i < parts.Length; i++)
				{
					if(parts[i].StartsWith("Controller", StringComparison.OrdinalIgnoreCase))
						components.Add(new IO.Hardwares.HardwareComponent(parts[i], parts[i], "memory/controller", "Memory controller"));
					else if(parts[i].StartsWith("Channel", StringComparison.OrdinalIgnoreCase))
						components.Add(new IO.Hardwares.HardwareComponent(parts[i], parts[i], "memory/channel", "Memory channel"));
				}
			}

			if(!string.IsNullOrEmpty(bank))
				components.Add(new IO.Hardwares.HardwareComponent(bank, bank, "memory/bank", "Memory bank"));

			return components;
		}

		private static IEnumerable<ManagementBaseObject> Query(string query)
		{
			ManagementObjectCollection items = null;

			try
			{
				using var searcher = new ManagementObjectSearcher("root\\CIMV2", query);
				items = searcher.Get();
			}
			catch(ManagementException)
			{
				yield break;
			}
			catch(COMException)
			{
				yield break;
			}
			catch(UnauthorizedAccessException)
			{
				yield break;
			}

			if(items == null)
				yield break;

			using(items)
			{
				foreach(ManagementBaseObject item in items)
					yield return item;
			}
		}

		private static IEnumerable<ManagementBaseObject> QueryAssociators(ManagementBaseObject source, string association)
		{
			if(source is not ManagementObject item || item.Path == null || string.IsNullOrEmpty(item.Path.RelativePath))
				yield break;

			foreach(var associated in Query($"ASSOCIATORS OF {{{item.Path.RelativePath}}} WHERE AssocClass={association}"))
				yield return associated;
		}

		private static void Add(List<IO.Hardwares.HardwareProperty> properties, ManagementBaseObject item, params string[] names)
		{
			for(int i = 0; i < names.Length; i++)
				HardwareUtility.Add(properties, names[i], GetValue(item, names[i]));
		}

		private static string Get(ManagementBaseObject item, params string[] names)
		{
			for(int i = 0; i < names.Length; i++)
			{
				var value = HardwareUtility.Normalize(GetValue(item, names[i]));

				if(!string.IsNullOrEmpty(value))
					return value;
			}

			return null;
		}

		private static object GetValue(ManagementBaseObject item, string name)
		{
			if(item == null || string.IsNullOrEmpty(name))
				return null;

			try
			{
				var value = item[name];

				if(value is string text && name.EndsWith("Date", StringComparison.OrdinalIgnoreCase))
					return TryParseDate(text, out var date) ? date : text;

				return value;
			}
			catch(ManagementException)
			{
				return null;
			}
		}

		private static bool TryParseDate(string value, out DateTime date)
		{
			try
			{
				date = ManagementDateTimeConverter.ToDateTime(value);
				return true;
			}
			catch(ArgumentOutOfRangeException)
			{
				date = default;
				return false;
			}
		}
	}
}
