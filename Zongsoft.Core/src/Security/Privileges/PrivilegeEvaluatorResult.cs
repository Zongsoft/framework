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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
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