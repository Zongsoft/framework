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

using Zongsoft.Serialization;

namespace Zongsoft.Externals.Aliyun.Telecom
{
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
}
