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
using System.Net;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace Zongsoft.Hardwares;

partial class HardwareCollector
{
	internal static IEnumerable<IO.Hardwares.IHardware> GetNetworks()
	{
		NetworkInterface[] adapters;

		try
		{
			adapters = NetworkInterface.GetAllNetworkInterfaces();
		}
		catch(NetworkInformationException)
		{
			yield break;
		}

		if(adapters == null || adapters.Length == 0)
			yield break;

		for(int i = 0; i < adapters.Length; i++)
		{
			var adapter = adapters[i];

			if(adapter == null)
				continue;

			var properties = new List<IO.Hardwares.HardwareProperty>();
			var physicalAddress = GetPhysicalAddress(adapter);

			HardwareUtility.Add(properties, nameof(NetworkInterface.Id), adapter.Id);
			HardwareUtility.Add(properties, nameof(NetworkInterface.Name), adapter.Name);
			HardwareUtility.Add(properties, nameof(NetworkInterface.Description), adapter.Description);
			HardwareUtility.Add(properties, nameof(NetworkInterface.NetworkInterfaceType), adapter.NetworkInterfaceType);
			HardwareUtility.Add(properties, nameof(NetworkInterface.OperationalStatus), adapter.OperationalStatus);
			HardwareUtility.Add(properties, nameof(NetworkInterface.Speed), adapter.Speed);
			HardwareUtility.Add(properties, nameof(NetworkInterface.IsReceiveOnly), adapter.IsReceiveOnly);
			HardwareUtility.Add(properties, nameof(NetworkInterface.SupportsMulticast), adapter.SupportsMulticast);
			HardwareUtility.Add(properties, nameof(PhysicalAddress), physicalAddress);

			AddStatistics(properties, adapter);

			var components = GetNetworkComponents(adapter);
			var name = HardwareUtility.Coalesce(adapter.Name, adapter.Description, "Network Adapter");
			var code = HardwareUtility.Coalesce(adapter.Id, physicalAddress, adapter.Name, "network" + i);

			yield return HardwareUtility.Create(
				physicalAddress,
				name,
				code,
				"network",
				adapter.Description,
				adapter.NetworkInterfaceType.ToString(),
				"network/adapter",
				null,
				"Network adapter",
				properties: properties,
				components: components);
		}
	}

	private static IEnumerable<IO.Hardwares.HardwareComponent> GetNetworkComponents(NetworkInterface adapter)
	{
		var properties = GetIPProperties(adapter);

		if(properties == null)
			return [];

		var components = new List<IO.Hardwares.HardwareComponent>();
		var ipProperties = new List<IO.Hardwares.HardwareProperty>();

		HardwareUtility.Add(ipProperties, nameof(IPInterfaceProperties.DnsSuffix), properties.DnsSuffix);

		if(!OperatingSystem.IsMacOS())
			HardwareUtility.Add(ipProperties, PropertyNames.IsDnsEnabled, properties.IsDnsEnabled);

		if(OperatingSystem.IsWindows())
			HardwareUtility.Add(ipProperties, PropertyNames.IsDynamicDnsEnabled, properties.IsDynamicDnsEnabled);

		AddIPv4Properties(ipProperties, properties);
		AddIPv6Properties(ipProperties, properties);

		if(ipProperties.Count > 0)
			components.Add(new IO.Hardwares.HardwareComponent("IP Configuration", "ip", "network/ip", "IP interface configuration", ipProperties));

		AddAddressComponents(components, "Unicast Address", "unicast", "network/ip/unicast", "Unicast IP address", properties.UnicastAddresses);
		AddAddressComponents(components, "Multicast Address", "multicast", "network/ip/multicast", "Multicast IP address", properties.MulticastAddresses);
		AddAddressComponents(components, "Gateway Address", "gateway", "network/ip/gateway", "Gateway IP address", properties.GatewayAddresses);
		AddIPAddressComponents(components, "DNS Address", "dns", "network/ip/dns", "DNS server address", properties.DnsAddresses);

		if(OperatingSystem.IsWindows())
			AddAddressComponents(components, "Anycast Address", "anycast", "network/ip/anycast", "Anycast IP address", properties.AnycastAddresses);

		if(!OperatingSystem.IsMacOS())
		{
			AddIPAddressComponents(components, "DHCP Address", "dhcp", "network/ip/dhcp", "DHCP server address", properties.DhcpServerAddresses);
			AddIPAddressComponents(components, "WINS Address", "wins", "network/ip/wins", "WINS server address", properties.WinsServersAddresses);
		}

		return components;
	}

	private static void AddStatistics(List<IO.Hardwares.HardwareProperty> properties, NetworkInterface adapter)
	{
		IPv4InterfaceStatistics statistics;

		try
		{
			statistics = adapter.GetIPv4Statistics();
		}
		catch(NetworkInformationException)
		{
			return;
		}

		if(statistics == null)
			return;

		HardwareUtility.Add(properties, nameof(IPv4InterfaceStatistics.BytesReceived), statistics.BytesReceived);
		HardwareUtility.Add(properties, nameof(IPv4InterfaceStatistics.BytesSent), statistics.BytesSent);
		HardwareUtility.Add(properties, nameof(IPv4InterfaceStatistics.IncomingPacketsDiscarded), statistics.IncomingPacketsDiscarded);
		HardwareUtility.Add(properties, nameof(IPv4InterfaceStatistics.IncomingPacketsWithErrors), statistics.IncomingPacketsWithErrors);
		HardwareUtility.Add(properties, nameof(IPv4InterfaceStatistics.IncomingUnknownProtocolPackets), statistics.IncomingUnknownProtocolPackets);
		HardwareUtility.Add(properties, nameof(IPv4InterfaceStatistics.NonUnicastPacketsReceived), statistics.NonUnicastPacketsReceived);
		HardwareUtility.Add(properties, nameof(IPv4InterfaceStatistics.NonUnicastPacketsSent), statistics.NonUnicastPacketsSent);
		HardwareUtility.Add(properties, nameof(IPv4InterfaceStatistics.OutgoingPacketsWithErrors), statistics.OutgoingPacketsWithErrors);
		HardwareUtility.Add(properties, nameof(IPv4InterfaceStatistics.OutputQueueLength), statistics.OutputQueueLength);
		HardwareUtility.Add(properties, nameof(IPv4InterfaceStatistics.UnicastPacketsReceived), statistics.UnicastPacketsReceived);
		HardwareUtility.Add(properties, nameof(IPv4InterfaceStatistics.UnicastPacketsSent), statistics.UnicastPacketsSent);

		if(!OperatingSystem.IsMacOS())
			HardwareUtility.Add(properties, PropertyNames.OutgoingPacketsDiscarded, statistics.OutgoingPacketsDiscarded);
	}

	private static IPInterfaceProperties GetIPProperties(NetworkInterface adapter)
	{
		try
		{
			return adapter.GetIPProperties();
		}
		catch(NetworkInformationException)
		{
			return null;
		}
	}

	private static void AddIPv4Properties(List<IO.Hardwares.HardwareProperty> properties, IPInterfaceProperties ipProperties)
	{
		IPv4InterfaceProperties ipv4;

		try
		{
			ipv4 = ipProperties.GetIPv4Properties();
		}
		catch(NetworkInformationException)
		{
			return;
		}

		if(ipv4 == null)
			return;

		HardwareUtility.Add(properties, $"{nameof(IPv4InterfaceProperties)}.{nameof(IPv4InterfaceProperties.Index)}", ipv4.Index);
		HardwareUtility.Add(properties, $"{nameof(IPv4InterfaceProperties)}.{nameof(IPv4InterfaceProperties.Mtu)}", ipv4.Mtu);

		if(OperatingSystem.IsWindows())
		{
			HardwareUtility.Add(properties, PropertyNames.IsDhcpEnabled, ipv4.IsDhcpEnabled);
			HardwareUtility.Add(properties, PropertyNames.IsAutomaticPrivateAddressingActive, ipv4.IsAutomaticPrivateAddressingActive);
			HardwareUtility.Add(properties, PropertyNames.IsAutomaticPrivateAddressingEnabled, ipv4.IsAutomaticPrivateAddressingEnabled);
		}

		if(OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
		{
			HardwareUtility.Add(properties, PropertyNames.IsForwardingEnabled, ipv4.IsForwardingEnabled);
			HardwareUtility.Add(properties, PropertyNames.UsesWins, ipv4.UsesWins);
		}
	}

	private static void AddIPv6Properties(List<IO.Hardwares.HardwareProperty> properties, IPInterfaceProperties ipProperties)
	{
		IPv6InterfaceProperties ipv6;

		try
		{
			ipv6 = ipProperties.GetIPv6Properties();
		}
		catch(NetworkInformationException)
		{
			return;
		}

		if(ipv6 == null)
			return;

		HardwareUtility.Add(properties, $"{nameof(IPv6InterfaceProperties)}.{nameof(IPv6InterfaceProperties.Index)}", ipv6.Index);
		HardwareUtility.Add(properties, $"{nameof(IPv6InterfaceProperties)}.{nameof(IPv6InterfaceProperties.Mtu)}", ipv6.Mtu);
	}

	private static void AddAddressComponents<TAddress>(
		List<IO.Hardwares.HardwareComponent> components,
		string name,
		string code,
		string type,
		string description,
		IEnumerable<TAddress> addresses)
	{
		if(components == null || addresses == null)
			return;

		var index = 0;

		foreach(var address in addresses)
		{
			if(address == null)
				continue;

			var properties = new List<IO.Hardwares.HardwareProperty>();
			AddAddressProperties(properties, address);

			if(properties.Count == 0)
				continue;

			var addressText = GetAddressText(address);

			components.Add(new IO.Hardwares.HardwareComponent(
				HardwareUtility.Coalesce(addressText, name),
				HardwareUtility.Coalesce(addressText, code + index),
				type,
				description,
				properties));

			index++;
		}
	}

	private static void AddIPAddressComponents(
		List<IO.Hardwares.HardwareComponent> components,
		string name,
		string code,
		string type,
		string description,
		IEnumerable<IPAddress> addresses)
	{
		if(components == null || addresses == null)
			return;

		var index = 0;

		foreach(var address in addresses)
		{
			if(address == null)
				continue;

			var properties = new List<IO.Hardwares.HardwareProperty>();
			HardwareUtility.Add(properties, nameof(IPAddressInformation.Address), address);
			HardwareUtility.Add(properties, nameof(IPAddress.AddressFamily), address.AddressFamily);
			HardwareUtility.Add(properties, nameof(IPAddress.IsIPv6LinkLocal), address.IsIPv6LinkLocal);
			HardwareUtility.Add(properties, nameof(IPAddress.IsIPv6Multicast), address.IsIPv6Multicast);
			HardwareUtility.Add(properties, nameof(IPAddress.IsIPv6SiteLocal), address.IsIPv6SiteLocal);

			var addressText = address.ToString();

			components.Add(new IO.Hardwares.HardwareComponent(
				HardwareUtility.Coalesce(addressText, name),
				HardwareUtility.Coalesce(addressText, code + index),
				type,
				description,
				properties));

			index++;
		}
	}

	private static void AddAddressProperties(List<IO.Hardwares.HardwareProperty> properties, object address)
	{
		switch(address)
		{
			case UnicastIPAddressInformation unicast:
				HardwareUtility.Add(properties, nameof(UnicastIPAddressInformation.Address), unicast.Address);
				HardwareUtility.Add(properties, nameof(IPAddress.AddressFamily), unicast.Address?.AddressFamily);
				HardwareUtility.Add(properties, nameof(UnicastIPAddressInformation.IPv4Mask), unicast.IPv4Mask);
				HardwareUtility.Add(properties, nameof(UnicastIPAddressInformation.PrefixLength), unicast.PrefixLength);

				if(OperatingSystem.IsWindows())
				{
					HardwareUtility.Add(properties, PropertyNames.PrefixOrigin, unicast.PrefixOrigin);
					HardwareUtility.Add(properties, PropertyNames.SuffixOrigin, unicast.SuffixOrigin);
					HardwareUtility.Add(properties, PropertyNames.DuplicateAddressDetectionState, unicast.DuplicateAddressDetectionState);
					HardwareUtility.Add(properties, PropertyNames.AddressPreferredLifetime, unicast.AddressPreferredLifetime);
					HardwareUtility.Add(properties, PropertyNames.AddressValidLifetime, unicast.AddressValidLifetime);
					HardwareUtility.Add(properties, PropertyNames.DhcpLeaseLifetime, unicast.DhcpLeaseLifetime);
					HardwareUtility.Add(properties, PropertyNames.IsDnsEligible, unicast.IsDnsEligible);
					HardwareUtility.Add(properties, PropertyNames.IsTransient, unicast.IsTransient);
				}

				break;
			case IPAddressInformation item:
				HardwareUtility.Add(properties, nameof(IPAddressInformation.Address), item.Address);
				HardwareUtility.Add(properties, nameof(IPAddress.AddressFamily), item.Address?.AddressFamily);

				if(OperatingSystem.IsWindows())
				{
					HardwareUtility.Add(properties, PropertyNames.IsDnsEligible, item.IsDnsEligible);
					HardwareUtility.Add(properties, PropertyNames.IsTransient, item.IsTransient);
				}

				break;
			case GatewayIPAddressInformation gateway:
				HardwareUtility.Add(properties, nameof(GatewayIPAddressInformation.Address), gateway.Address);
				HardwareUtility.Add(properties, nameof(IPAddress.AddressFamily), gateway.Address?.AddressFamily);
				break;
		}
	}

	private static string GetAddressText(object address) => address switch
	{
		UnicastIPAddressInformation unicast => unicast.Address?.ToString(),
		IPAddressInformation item => item.Address?.ToString(),
		GatewayIPAddressInformation gateway => gateway.Address?.ToString(),
		IPAddress item => item.ToString(),
		_ => null,
	};

	private static string GetPhysicalAddress(NetworkInterface adapter)
	{
		try
		{
			var address = adapter.GetPhysicalAddress();

			if(address == null || address.GetAddressBytes().Length == 0)
				return null;

			return HardwareUtility.NormalizeIdentifier(address.ToString());
		}
		catch(NetworkInformationException)
		{
			return null;
		}
	}

	private static class PropertyNames
	{
		public const string IsDnsEnabled = nameof(IsDnsEnabled);
		public const string IsDynamicDnsEnabled = nameof(IsDynamicDnsEnabled);
		public const string OutgoingPacketsDiscarded = nameof(OutgoingPacketsDiscarded);
		public const string IsDhcpEnabled = nameof(IsDhcpEnabled);
		public const string IsAutomaticPrivateAddressingActive = nameof(IsAutomaticPrivateAddressingActive);
		public const string IsAutomaticPrivateAddressingEnabled = nameof(IsAutomaticPrivateAddressingEnabled);
		public const string IsForwardingEnabled = nameof(IsForwardingEnabled);
		public const string UsesWins = nameof(UsesWins);
		public const string PrefixOrigin = nameof(PrefixOrigin);
		public const string SuffixOrigin = nameof(SuffixOrigin);
		public const string DuplicateAddressDetectionState = nameof(DuplicateAddressDetectionState);
		public const string AddressPreferredLifetime = nameof(AddressPreferredLifetime);
		public const string AddressValidLifetime = nameof(AddressValidLifetime);
		public const string DhcpLeaseLifetime = nameof(DhcpLeaseLifetime);
		public const string IsDnsEligible = nameof(IsDnsEligible);
		public const string IsTransient = nameof(IsTransient);
	}
}
