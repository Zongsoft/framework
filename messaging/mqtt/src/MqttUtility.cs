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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.Mqtt library.
 *
 * The Zongsoft.Messaging.Mqtt is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.Mqtt is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.Mqtt library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text;
using System.Buffers;

using MQTTnet;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace Zongsoft.Messaging.Mqtt;

internal static class MqttUtility
{
	private const string COMPRESSION_PROPERTY = "Zongsoft-Compression";
	private static ReadOnlySpan<byte> CompressionMagic => "ZCMP"u8;

	public static MqttQualityOfServiceLevel ToQoS(this MessageReliability reliability) => reliability switch
	{
		MessageReliability.MostOnce => MqttQualityOfServiceLevel.AtMostOnce,
		MessageReliability.LeastOnce => MqttQualityOfServiceLevel.AtLeastOnce,
		MessageReliability.ExactlyOnce => MqttQualityOfServiceLevel.ExactlyOnce,
		_ => MqttQualityOfServiceLevel.AtMostOnce,
	};

	public static bool IsSuccessful(this MqttClientSubscribeResult result)
	{
		const int ERROR_CODE = 0x80; //MqttClientSubscribeResultCode.UnspecifiedError

		if(result?.Items == null || result.Items.Count == 0)
			return false;

		foreach(var item in result.Items)
		{
			if((byte)item.ResultCode >= ERROR_CODE)
				return false;
		}

		return true;
	}

	public static byte[] GetPayload(this MqttApplicationMessage message)
	{
		if(message == null)
			return [];

		var payload = message.Payload.ToArray();
		if(TryGetCompression(message.UserProperties, out var compression))
			return MessageCompression.Decompress(compression, payload);

		return TryDecompress(payload, out var data) ? data : payload;
	}

	public static void SetPayload(this MqttApplicationMessageBuilder builder, ReadOnlyMemory<byte> data, MessageCompression compression, bool supportsProperties)
	{
		if(!compression.CanCompress(data.Length))
		{
			builder.WithPayloadSegment(data);
			return;
		}

		if(supportsProperties)
		{
			builder.WithPayload(compression.Compress(data.Span));
			builder.WithUserProperty(COMPRESSION_PROPERTY, Encoding.UTF8.GetBytes(compression.Name));
		}
		else
		{
			builder.WithPayload(Pack(compression, data.Span));
		}
	}

	private static byte[] Pack(MessageCompression compression, ReadOnlySpan<byte> data)
	{
		if(compression.IsEmpty)
			throw new InvalidOperationException();

		var name = Encoding.UTF8.GetBytes(compression.Name);
		if(name.Length > byte.MaxValue)
			throw Common.OperationException.Unsupported();

		var payload = compression.Compress(data);
		var result = new byte[CompressionMagic.Length + 2 + name.Length + payload.Length];
		CompressionMagic.CopyTo(result);
		result[CompressionMagic.Length] = 1;
		result[CompressionMagic.Length + 1] = (byte)name.Length;
		name.CopyTo(result, CompressionMagic.Length + 2);
		payload.CopyTo(result, CompressionMagic.Length + 2 + name.Length);
		return result;
	}

	private static bool TryDecompress(ReadOnlySpan<byte> source, out byte[] data)
	{
		var offset = CompressionMagic.Length;
		if(source.Length < offset || !source[..offset].SequenceEqual(CompressionMagic))
		{
			data = null;
			return false;
		}

		if(source.Length <= offset + 2 || source[offset] != 1)
			throw new FormatException();

		var length = source[offset + 1];
		if(length == 0 || source.Length <= offset + 2 + length)
			throw new FormatException();

		var name = Encoding.UTF8.GetString(source.Slice(offset + 2, length));
		data = MessageCompression.Decompress(name, source[(offset + 2 + length)..]);
		return true;
	}

	private static bool TryGetCompression(System.Collections.Generic.IReadOnlyCollection<MqttUserProperty> properties, out string compression)
	{
		if(properties != null)
		{
			foreach(var property in properties)
			{
				if(string.Equals(property.Name, COMPRESSION_PROPERTY, StringComparison.Ordinal))
				{
					compression = property.ReadValueAsString();
					return !string.IsNullOrWhiteSpace(compression);
				}
			}
		}

		compression = null;
		return false;
	}

}
