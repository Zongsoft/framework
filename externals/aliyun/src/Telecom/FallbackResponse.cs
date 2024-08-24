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
}
