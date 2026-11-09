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

public class FallbackRequestContent
{
	#region 公共属性
	[JsonPropertyName("role")]
	[SerializationMember("role")]
	public string Actor { get; set; }

	[JsonPropertyName("identity")]
	[SerializationMember("identity")]
	public string Identity { get; set; }

	[JsonPropertyName("words")]
	[SerializationMember("words")]
	public string Value { get; set; }

	[JsonPropertyName("is_playing")]
	[SerializationMember("is_playing")]
	public bool Playing { get; set; }

	[JsonPropertyName("current_media_code")]
	[SerializationMember("current_media_code")]
	public string VoiceCode { get; set; }

	[JsonPropertyName("dynamic_id")]
	[SerializationMember("dynamic_id")]
	public string Extra { get; set; }

	[JsonPropertyName("dtmf_digits")]
	[SerializationMember("dtmf_digits")]
	public string Digits { get; set; }

	[JsonPropertyName("cc_name")]
	[SerializationMember("cc_name")]
	public string TransferName { get; set; }

	[JsonPropertyName("transfer_status")]
	[SerializationMember("transfer_status")]
	public string TransferStatus { get; set; }

	[JsonPropertyName("fail_cause")]
	[SerializationMember("fail_cause")]
	public string TransferReason { get; set; }

	[JsonPropertyName("is_monitor\t")]
	[SerializationMember("is_monitor\t")]
	public bool IsTransferMonitor { get; set; }
	#endregion
}
