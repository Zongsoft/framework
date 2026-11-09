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

namespace Zongsoft.Externals.Wechat.Paying;

public class FallbackMessage
{
	#region 公共属性
	[JsonPropertyName("id")]
	public string Identifier { get; set; }

	[JsonPropertyName("create_time")]
	public DateTime Timestamp { get; set; }

	[JsonPropertyName("event_type")]
	public string Status { get; set; }

	[JsonPropertyName("summary")]
	public string Description { get; set; }

	[JsonPropertyName("resource_type")]
	public string ResourceType { get; set; }

	[JsonPropertyName("resource")]
	public ResourceInfo Resource { get; set; }
	#endregion

	public struct ResourceInfo
	{
		#region 公共属性
		[JsonPropertyName("original_type")]
		public string Source { get; set; }
		[JsonPropertyName("algorithm")]
		public string Algorithm { get; set; }
		[JsonPropertyName("nonce")]
		public string Nonce { get; set; }
		[JsonPropertyName("associated_data")]
		public string AssociatedData { get; set; }
		[JsonPropertyName("ciphertext")]
		public string Ciphertext { get; set; }
		#endregion
	}
}
