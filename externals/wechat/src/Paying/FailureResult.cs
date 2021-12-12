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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Wechat.Paying
{
	public struct FailureResult
	{
		#region 构造函数
		public FailureResult(string code, string message)
		{
			this.Code = code;
			this.Message = message;
			this.Detail = null;
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置错误码。</summary>
		[Serialization.SerializationMember("code")]
		[JsonPropertyName("code")]
		public string Code { get; set; }

		/// <summary>获取或设置错误消息。</summary>
		[Serialization.SerializationMember("message")]
		[JsonPropertyName("message")]
		public string Message { get; set; }

		/// <summary>获取或设置详细信息。</summary>
		[Serialization.SerializationMember("detail")]
		[JsonPropertyName("detail")]
		public FailureDetail? Detail { get; set; }
		#endregion

		#region 重写方法
		public override string ToString()
		{
			return "[" + this.Code.ToString() + "] " + this.Message;
		}
		#endregion

		#region 嵌套结构
		public struct FailureDetail
		{
			public string Field { get; set; }
			public string Value { get; set; }
			public string Issue { get; set; }
			public string Location { get; set; }

			public override string ToString() => string.IsNullOrEmpty(this.Location) ?
				$"{this.Field}={this.Value}" :
				$"{this.Location}:{this.Field}={this.Value}";
		}
		#endregion
	}
}
