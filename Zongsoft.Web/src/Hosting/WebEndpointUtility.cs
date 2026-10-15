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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

using Zongsoft.Communication;

namespace Zongsoft.Web.Hosting;

internal static class WebEndpointUtility
{
	public static Dictionary<string, WebEndpointProtocol> GetProtocols(IEnumerable<WebEndpointProtocol> protocols)
	{
		var result = new Dictionary<string, WebEndpointProtocol>(StringComparer.Ordinal)
		{
			["http"] = WebEndpointProtocol.Http,
		};

		foreach(var protocol in protocols ?? [])
		{
			ArgumentNullException.ThrowIfNull(protocol);

			if(!result.TryAdd(protocol.Name, protocol))
				throw Invalid(protocol.Name, "DuplicateProtocol");
		}

		return result;
	}

	public static NetworkProtocol[] Normalize(IEnumerable<NetworkProtocol> declarations, IReadOnlyDictionary<string, WebEndpointProtocol> policies, string path, bool requireRegistration)
	{
		var protocols = declarations.ToArray();

		for(var i = 0; i < protocols.Length; i++)
		{
			if(policies.TryGetValue(protocols[i].Name ?? "http", out var policy))
			{
				var normalized = policy.Normalize(protocols[i]);
				if(normalized.IsEmpty || normalized.Name != protocols[i].Name)
					throw Invalid(path, "UnsupportedProtocols");

				protocols[i] = normalized;
			}
			else if(requireRegistration)
				throw Invalid(path, "UnregisteredProtocol");
		}

		protocols = NetworkProtocol.Parse(string.Join(',', protocols), ',');
		if(protocols.Any(protocol => !protocol.IsHttp || protocol.HttpVersion == 0))
			throw Invalid(path, "UnsupportedProtocols");

		return protocols;
	}

	public static string GetName(string name)
	{
		if(!Zongsoft.Communication.NetworkProtocol.TryParse(name, out var protocol) ||
		   protocol.Carrier != null && protocol.Carrier != "http" ||
		   protocol.Name != null && protocol.Carrier != null)
			throw new ArgumentException(null, nameof(name));

		return protocol.Name ?? "http";
	}

	public static bool IsPath(string path)
	{
		if(string.IsNullOrEmpty(path) || path[0] != '/')
			return false;

		if(path.Length == 1)
			return true;

		var remaining = path.AsSpan(1);

		while(!remaining.IsEmpty)
		{
			var slash = remaining.IndexOf('/');
			var segment = slash < 0 ? remaining : remaining[..slash];

			if(segment.IsEmpty || segment.SequenceEqual(".") || segment.SequenceEqual(".."))
				return false;

			foreach(var character in segment)
			{
				if(!char.IsAsciiLetterOrDigit(character) && character is not ('-' or '.' or '_' or '~'))
					return false;
			}

			if(slash < 0)
				break;

			remaining = remaining[(slash + 1)..];
		}

		return true;
	}

	public static InvalidOperationException Invalid(string path, string reason) => new(string.Format(CultureInfo.CurrentCulture, Properties.Resources.WebEndpoint_Invalid_Message, path, reason));
	public static bool SameSocket(WebEndpoint left, WebEndpoint right) => left.Address.Port == right.Address.Port &&
		(left.IPAddress == null ? right.IPAddress == null && left.Address.Host == right.Address.Host : left.IPAddress.Equals(right.IPAddress));
}
