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

using Zongsoft.Serialization;

namespace Zongsoft.Externals.Aliyun.Telecom
{
	public class FallbackRequestContent
	{
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
	}
}
