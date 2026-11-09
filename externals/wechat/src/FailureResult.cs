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

namespace Zongsoft.Externals.Wechat;

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
	public override string ToString() => "[" + this.Code.ToString() + "] " + this.Message;
	#endregion

	#region 嵌套结构
	public struct FailureDetail
	{
		public string Field { get; set; }
		public object Value { get; set; }
		public string Issue { get; set; }
		public string Location { get; set; }

		public override string ToString() => string.IsNullOrEmpty(this.Location) ?
			$"{this.Field}={this.Value}" :
			$"{this.Location}:{this.Field}={this.Value}";
	}

	private class FailureDetailValueConverter : JsonConverter<object>
	{
		public override bool CanConvert(Type type) => true;

		public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			switch(reader.TokenType)
			{
				case JsonTokenType.Null:
					return null;
				case JsonTokenType.String:
					return reader.GetString();
				case JsonTokenType.True:
					return true;
				case JsonTokenType.False:
					return false;
				case JsonTokenType.Number:
					return reader.TryGetInt64(out var integer) ? integer : reader.GetDouble();
			}

			return null;
		}

		public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
		{
			if(value == null)
				writer.WriteNullValue();
			else
				writer.WriteStringValue(value.ToString());
		}
	}
	#endregion
}
