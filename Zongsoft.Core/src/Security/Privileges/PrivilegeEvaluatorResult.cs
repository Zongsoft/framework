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
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Security.Privileges;

[JsonConverter(typeof(JsonConverter))]
public partial class PrivilegeEvaluatorResult : IPrivilegeEvaluatorResult
{
	#region 构造函数
	public PrivilegeEvaluatorResult(string privilege) => this.Privilege = privilege;
	#endregion

	#region 公共属性
	public string Privilege { get; }
	#endregion
}

partial class PrivilegeEvaluatorResult
{
	private class JsonConverter : JsonConverter<PrivilegeEvaluatorResult>
	{
		public override PrivilegeEvaluatorResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if(reader.TokenType == JsonTokenType.String)
				return new PrivilegeEvaluatorResult(reader.GetString());

			return null;
		}

		public override void Write(Utf8JsonWriter writer, PrivilegeEvaluatorResult value, JsonSerializerOptions options)
		{
			if(value == null || string.IsNullOrEmpty(value.Privilege))
				return;

			writer.WriteStringValue(value.Privilege);
		}
	}
}