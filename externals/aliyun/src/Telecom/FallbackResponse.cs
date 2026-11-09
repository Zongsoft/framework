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

using Zongsoft.Serialization;

namespace Zongsoft.Externals.Aliyun.Telecom;

public class FallbackResponse
{
	#region 公共属性
	[JsonPropertyName("call_id")]
	[SerializationMember("call_id")]
	public string Identifier { get; set; }

	[JsonPropertyName("action")]
	[SerializationMember("action")]
	public FallbackResponseAction Action { get; set; }

	[JsonPropertyName("action_code")]
	[SerializationMember("action_code")]
	public string Voice { get; set; }

	[JsonPropertyName("action_code_param")]
	[SerializationMember("action_code_param")]
	public object VoiceParameter { get; set; }

	[JsonPropertyName("action_break")]
	[SerializationMember("action_break")]
	public bool Breaked { get; set; }

	[JsonPropertyName("action_code_break")]
	[SerializationMember("action_code_break")]
	public bool CanBreak { get; set; }

	[JsonPropertyName("dynamic_id")]
	[SerializationMember("dynamic_id")]
	public string Extra { get; set; }

	[JsonPropertyName("number")]
	[SerializationMember("number")]
	public string TransferNumber { get; set; }

	[JsonPropertyName("transfer_playfile")]
	[SerializationMember("transfer_playfile")]
	public string TransferVoice { get; set; }
	#endregion
}
