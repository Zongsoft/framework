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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.ComponentModel;

namespace Zongsoft.IO
{
	[TypeConverter(typeof(LocationTypeConverter))]
	[JsonConverter(typeof(LocationJsonConverter))]
	public readonly struct PathLocation : IEquatable<PathLocation>
	{
		#region 构造函数
		public PathLocation(string path)
		{
			this.Path = path;
			this.Url = string.IsNullOrEmpty(path) ? null : Zongsoft.IO.FileSystem.GetUrl(path);
		}
		#endregion

		#region 公共属性
		public string Path { get; }
		public string Url { get; }

		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public bool IsEmpty { get => string.IsNullOrEmpty(this.Path); }
		#endregion

		#region 重写方法
		public bool Equals(PathLocation other) => string.Equals(this.Path, other.Path);
		public override bool Equals(object obj) => obj is PathLocation other && Equals(other);
		public override int GetHashCode() => this.Path.GetHashCode();
		public override string ToString() => this.Path;
		#endregion

		#region 重写符号
		public static bool operator ==(PathLocation left, PathLocation right) => left.Equals(right);
		public static bool operator !=(PathLocation left, PathLocation right) => !(left == right);

		public static implicit operator string(PathLocation location) => location.Path;
		public static explicit operator PathLocation(string path) => new PathLocation(path);
		#endregion

		#region 类型转换
		private class LocationTypeConverter : TypeConverter
		{
			public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);
			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => value is PathLocation location ? location.Path : null;
			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => new PathLocation(value as string);
		}

		private class LocationJsonConverter : JsonConverter<PathLocation>
		{
			public override PathLocation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				if(reader.TokenType == JsonTokenType.Null)
					return default;
				if(reader.TokenType == JsonTokenType.String)
					return new PathLocation(reader.GetString());

				if(reader.TokenType == JsonTokenType.StartObject)
				{
					var depth = reader.CurrentDepth;

					while(reader.Read())
					{
						if(reader.TokenType == JsonTokenType.PropertyName && string.Equals(reader.GetString(), nameof(PathLocation.Path), StringComparison.OrdinalIgnoreCase))
						{
							if(reader.Read())
								return new PathLocation(reader.GetString());
							else
								break;
						}

						if(reader.CurrentDepth == depth)
							break;
					}
				}

				return default;
			}

			public override void Write(Utf8JsonWriter writer, PathLocation value, JsonSerializerOptions options)
			{
				if(value.IsEmpty)
					writer.WriteNullValue();
				else
				{
					writer.WriteStartObject();
					writer.WriteString(options.PropertyNamingPolicy?.ConvertName(nameof(Path)) ?? nameof(Path), value.Path);
					writer.WriteString(options.PropertyNamingPolicy?.ConvertName(nameof(Url)) ?? nameof(Url), value.Url);
					writer.WriteEndObject();
				}
			}
		}
		#endregion
	}
}
