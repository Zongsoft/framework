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

namespace Zongsoft.Collections;

[JsonConverter(typeof(ParametersConverter))]
partial class Parameters
{
	private class ParametersConverter : JsonConverter<Parameters>
	{
		#region 公共方法
		public override Parameters Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if(reader.TokenType != JsonTokenType.StartObject)
				return null;

			var root = reader.CurrentDepth;
			var parameters = new Parameters();

			Type type = null;
			string name = null;

			while(reader.Read() && reader.CurrentDepth > root)
			{
				switch(reader.TokenType)
				{
					case JsonTokenType.PropertyName:
						var key = reader.GetString();

						if(key != null && key.Length > 1 && key[0] == '$')
						{
							name = null;
							type = Common.TypeAlias.Parse(key[1..]);
						}
						else
						{
							name = key;
							type = null;
						}
						break;
					case JsonTokenType.Null:
						if(type == null)
							parameters.SetValue(name, null);
						else
							parameters.SetValue(type, null);
						break;
					case JsonTokenType.True:
					case JsonTokenType.False:
						if(type == null)
							parameters.SetValue(name, reader.GetBoolean());
						else
							parameters.SetValue(type, reader.GetBoolean());
						break;
					case JsonTokenType.Number:
						if(type == null)
							parameters.SetValue(name, Serialization.Json.Converters.ObjectConverter.GetNumber(ref reader));
						else
							parameters.SetValue(type, Serialization.Json.Converters.ObjectConverter.GetNumber(ref reader));
						break;
					case JsonTokenType.String:
						if(type == null)
							parameters.SetValue(name, reader.GetString());
						else
							parameters.SetValue(type, reader.GetString());
						break;
					case JsonTokenType.StartObject:
						if(type == null)
							parameters.SetValue(name, GetParameterValue(ref reader, options));
						else
							parameters.SetValue(type, GetParameterValue(ref reader, options));
						break;
				}

				if(reader.CurrentDepth > root + 1)
					reader.Skip();
			}

			return parameters;
		}

		public override void Write(Utf8JsonWriter writer, Parameters parameters, JsonSerializerOptions options)
		{
			writer.WriteStartObject();

			foreach(var parameter in parameters)
			{
				if(parameter.Key is Type type)
					writer.WritePropertyName($"${GetTypeName(type)}");
				else
					writer.WritePropertyName(parameter.Key.ToString());

				Serialization.Json.Converters.ObjectConverter.Default.Write(writer, parameter.Value, options);
			}

			writer.WriteEndObject();
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetTypeName(Type type) => Common.TypeAlias.GetAlias(Data.Model.GetModelType(type));
		private static object GetParameterValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
		{
			Type type = null;
			var root = reader.CurrentDepth;

			while(reader.Read() && reader.CurrentDepth > root)
			{
				if(reader.TokenType == JsonTokenType.PropertyName)
				{
					var name = reader.GetString();

					if(name == "$type")
					{
						if(reader.Read())
						{
							type = Common.TypeAlias.Parse(reader.GetString());
							if(type == null)
								return null;
						}
					}
					else if(name == "$value")
					{
						if(reader.Read())
							return Serialization.Json.Converters.ObjectConverter.GetValue(ref reader, type, options);
					}
				}
			}

			return null;
		}
		#endregion
	}
}
