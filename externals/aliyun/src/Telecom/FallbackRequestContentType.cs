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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Aliyun.Telecom;

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
