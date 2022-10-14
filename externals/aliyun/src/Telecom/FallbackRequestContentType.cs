/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Aliyun.Telecom
{
	[JsonConverter(typeof(FallbackRequestContentTypeConverter))]
	public enum FallbackRequestContentType
	{
		Normal,
		Muting,
		Breaking,
		Transfer,
		Dtmf,
	}

	internal class FallbackRequestContentTypeConverter : JsonConverter<FallbackRequestContentType>
	{
		public override FallbackRequestContentType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return reader.GetString() switch
			{
				"normal" => FallbackRequestContentType.Normal,
				"mute" => FallbackRequestContentType.Muting,
				"dtmf" => FallbackRequestContentType.Dtmf,
				"timebreak" => FallbackRequestContentType.Breaking,
				"parallel_transfer" => FallbackRequestContentType.Transfer,
				_ => FallbackRequestContentType.Normal,
			};
		}

		public override void Write(Utf8JsonWriter writer, FallbackRequestContentType value, JsonSerializerOptions options)
		{
			switch(value)
			{
				case FallbackRequestContentType.Normal:
					writer.WriteStringValue("normal");
					break;
				case FallbackRequestContentType.Muting:
					writer.WriteStringValue("mute");
					break;
				case FallbackRequestContentType.Dtmf:
					writer.WriteStringValue("dtmf");
					break;
				case FallbackRequestContentType.Breaking:
					writer.WriteStringValue("timebreak");
					break;
				case FallbackRequestContentType.Transfer:
					writer.WriteStringValue("parallel_transfer");
					break;
			}
		}
	}
}
