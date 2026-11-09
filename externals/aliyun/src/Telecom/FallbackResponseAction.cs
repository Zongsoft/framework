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

[JsonConverter(typeof(FallbackResponseActionConverter))]
public enum FallbackResponseAction
{
	/// <summary>播放下一段语音。</summary>
	Play,
	/// <summary>打断当前正在播放的语音。</summary>
	Break,
	/// <summary>继续播放当前语音。</summary>
	Continue,
	/// <summary>挂机。</summary>
	Hangup,
	/// <summary>转接。</summary>
	Transfer,
	/// <summary>不做任何处理。</summary>
	Nothing,
	/// <summary>接收dtmf消息。</summary>
	Dtmf,
	/// <summary>并行转接命令。</summary>
	ParallelTransfer,
	/// <summary>并行桥接命令 。</summary>
	ParallelBridge,
}

internal class FallbackResponseActionConverter : JsonConverter<FallbackResponseAction>
{
	public override FallbackResponseAction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.GetString() switch
		{
			"play" => FallbackResponseAction.Play,
			"break" => FallbackResponseAction.Break,
			"continue" => FallbackResponseAction.Continue,
			"hangup" => FallbackResponseAction.Hangup,
			"transfer" => FallbackResponseAction.Transfer,
			"donothing" => FallbackResponseAction.Nothing,
			"dtmf" => FallbackResponseAction.Dtmf,
			"parallel_transfer" => FallbackResponseAction.ParallelTransfer,
			"parallel_bridge" => FallbackResponseAction.ParallelBridge,
			_ => FallbackResponseAction.Continue,
		};
	}

	public override void Write(Utf8JsonWriter writer, FallbackResponseAction value, JsonSerializerOptions options)
	{
		switch(value)
		{
			case FallbackResponseAction.Play:
				writer.WriteStringValue("play");
				break;
			case FallbackResponseAction.Break:
				writer.WriteStringValue("break");
				break;
			case FallbackResponseAction.Continue:
				writer.WriteStringValue("continue");
				break;
			case FallbackResponseAction.Hangup:
				writer.WriteStringValue("hangup");
				break;
			case FallbackResponseAction.Transfer:
				writer.WriteStringValue("transfer");
				break;
			case FallbackResponseAction.Nothing:
				writer.WriteStringValue("donothing");
				break;
			case FallbackResponseAction.Dtmf:
				writer.WriteStringValue("dtmf");
				break;
			case FallbackResponseAction.ParallelTransfer:
				writer.WriteStringValue("parallel_transfer");
				break;
			case FallbackResponseAction.ParallelBridge:
				writer.WriteStringValue("parallel_bridge");
				break;
		}
	}
}
