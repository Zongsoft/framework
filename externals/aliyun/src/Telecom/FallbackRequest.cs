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

public class FallbackRequest
{
	#region 公共属性
	[JsonPropertyName("call_id")]
	[SerializationMember("call_id")]
	public string Identifier { get; set; }

	[JsonPropertyName("timestamp")]
	[SerializationMember("timestamp")]
	public long Timestamp { get; set; }

	[JsonPropertyName("content_type")]
	[SerializationMember("content_type")]
	public FallbackRequestContentType ContentType { get; set; }

	[JsonPropertyName("content")]
	[SerializationMember("content")]
	public FallbackRequestContent Content { get; set; }
	#endregion

	#region 公共方法
	public FallbackResponse Reply(FallbackRequest request, FallbackResponseAction action, string voice = null, object parameter = null)
	{
		if(request == null)
			return new FallbackResponse();

		return request == null ? new FallbackResponse() : new FallbackResponse
		{
			Action = action,
			Voice = voice,
			VoiceParameter = parameter,
			Identifier = request.Identifier,
			Extra = request.Content?.Extra,
		};
	}
	#endregion
}
