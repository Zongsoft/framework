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
 * This file is part of Zongsoft.Messaging.ZeroMQ library.
 *
 * The Zongsoft.Messaging.ZeroMQ is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.ZeroMQ is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.ZeroMQ library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Messaging.ZeroMQ;

internal static class Protocol
{
	public const string Name = "Zongsoft.Messaging.ZeroMQ";
	public const string Version = "1.0";
	public const int MaxHeaderSize = 16 * 1024;
	public const int MaxTopicSize = 1024;
	public const int MaxIdentifierSize = 256;
	public const int MaxOptionCount = 32;
	public const int MaxPayloadSize = 64 * 1024 * 1024;

	public static string WelcomePrefix => $"\0{Name}\n";
	public static string GetAddress(string server, ushort port) => port == 0 ? $"tcp://{server}" : $"tcp://{server}:{port}";
	public static string GetWelcome(string epoch) => $"{WelcomePrefix}{Headers.Version}:{Version}\n{Headers.Epoch}:{epoch}\0";
	public static string GetDiscoveryRequest(string instance) => $"{Name}\n{Headers.Version}:{Version}\n{Headers.Command}:{Commands.Discover}\n{Headers.Instance}:{instance}";
	public static string GetDiscoveryResponse(string epoch, int control, int incoming, int outgoing) => control > 0 ?
		$"{Name}\n{Headers.Version}:{Version}\n{Headers.Epoch}:{epoch}\n{Headers.Ports}:{control},{incoming},{outgoing}" :
		$"{Name}\n{Headers.Version}:{Version}\n{Headers.Epoch}:{epoch}\n{Headers.Ports}:{incoming},{outgoing}";
	public static string GetDiscoveryError(string error) => $"{Name}\n{Headers.Version}:{Version}\n{Headers.Error}:{error}";

	public static bool TryParseDiscoveryRequest(string request)
	{
		if(string.IsNullOrWhiteSpace(request))
			return false;

		var lines = request.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		return lines.Length == 4 &&
			string.Equals(lines[0], Name, StringComparison.Ordinal) &&
			string.Equals(lines[1], $"{Headers.Version}:{Version}", StringComparison.Ordinal) &&
			string.Equals(lines[2], $"{Headers.Command}:{Commands.Discover}", StringComparison.Ordinal) &&
			lines[3].StartsWith($"{Headers.Instance}:", StringComparison.Ordinal) &&
			lines[3].Length > Headers.Instance.Length + 1;
	}

	public static bool TryParseDiscoveryResponse(string response, out string epoch, out ushort control, out ushort incoming, out ushort outgoing)
	{
		epoch = null;
		control = 0;
		incoming = 0;
		outgoing = 0;

		if(string.IsNullOrWhiteSpace(response))
			return false;

		var lines = response.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if(lines.Length != 4 || !string.Equals(lines[0], Name, StringComparison.Ordinal) ||
		   !string.Equals(lines[1], $"{Headers.Version}:{Version}", StringComparison.Ordinal) ||
		   !lines[2].StartsWith($"{Headers.Epoch}:", StringComparison.Ordinal) ||
		   !lines[3].StartsWith($"{Headers.Ports}:", StringComparison.Ordinal))
			return false;

		epoch = lines[2][(Headers.Epoch.Length + 1)..];
		var values = lines[3][(Headers.Ports.Length + 1)..].Split(',', StringSplitOptions.TrimEntries);
		if(values.Length == 2)
		{
			if(!ushort.TryParse(values[0], out incoming) || !ushort.TryParse(values[1], out outgoing))
				return false;
		}
		else if(values.Length == 3)
		{
			if(!ushort.TryParse(values[0], out control) || !ushort.TryParse(values[1], out incoming) || !ushort.TryParse(values[2], out outgoing))
				return false;
		}
		else
		{
			return false;
		}

		return epoch.Length == 32 && incoming > 0 && outgoing > 0 && incoming != outgoing &&
			(control == 0 || incoming != control && outgoing != control);
	}

	public static class Headers
	{
		public const string Version = "Protocol-Version";
		public const string Epoch = nameof(Epoch);
		public const string Ports = nameof(Ports);
		public const string Command = nameof(Command);
		public const string Instance = nameof(Instance);
		public const string Identifier = nameof(Identifier);
		public const string Identity = nameof(Identity);
		public const string Tags = nameof(Tags);
		public const string Compression = nameof(Compression);
		public const string Error = nameof(Error);
	}

	public static class Commands
	{
		public const string Discover = nameof(Discover);
		public const string Register = "REGISTER";
		public const string Unregister = "UNREGISTER";
		public const string Ping = "PING";
		public const string Publish = "PUBLISH";
		public const string Acknowledge = "ACK";
		public const string Registered = "REGISTERED";
		public const string Deliver = "DELIVER";
		public const string Accepted = "ACCEPTED";
		public const string Unroutable = "UNROUTABLE";
		public const string Error = "ERROR";
	}

	public static class Errors
	{
		public const string InvalidRequest = nameof(InvalidRequest);
		public const string InvalidPublish = nameof(InvalidPublish);
		public const string IdentifierConflict = nameof(IdentifierConflict);
		public const string Expired = nameof(Expired);
		public const string StorageBusy = nameof(StorageBusy);
		public const string StorageFailure = nameof(StorageFailure);
	}
}
