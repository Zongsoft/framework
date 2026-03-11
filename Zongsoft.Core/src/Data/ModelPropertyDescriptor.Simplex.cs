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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Reflection;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Data;

partial class ModelPropertyDescriptor
{
	public class SimplexPropertyDescriptor : ModelPropertyDescriptor
	{
		#region 构造函数
		public SimplexPropertyDescriptor() { }
		public SimplexPropertyDescriptor(MemberInfo member) => this.Populate(member);
		#endregion

		#region 公共属性
		/// <summary>获取或设置属性别名。</summary>
		public string Alias
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Alias));
				field = value;
				this.OnPropertyChanged(nameof(this.Alias));
			}
		}

		/// <summary>获取或设置数据实体属性的数据类型。</summary>
		public DataType DataType
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.DataType));
				field = value;
				this.OnPropertyChanged(nameof(this.DataType));
			}
		}

		/// <summary>获取或设置一个值，指示当前属性是否为主键。</summary>
		public bool IsPrimaryKey
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.IsPrimaryKey));
				field = value;
				this.OnPropertyChanged(nameof(this.IsPrimaryKey));
			}
		}

		/// <summary>获取或设置文本或数组属性的最大长度。</summary>
		public int Length
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Length));
				field = value;
				this.OnPropertyChanged(nameof(this.Length));
			}
		}

		/// <summary>获取或设置数值属性的精度。</summary>
		public byte Precision
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Precision));
				field = value;
				this.OnPropertyChanged(nameof(this.Precision));
			}
		}

		/// <summary>获取或设置数值属性的小数点位数。</summary>
		public byte Scale
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Scale));
				field = value;
				this.OnPropertyChanged(nameof(this.Scale));
			}
		}

		/// <summary>获取或设置默认值。</summary>
		[JsonConverter(typeof(DefaultValueJsonConverterFactory))]
		public object DefaultValue
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.DefaultValue));
				field = value;
				this.OnPropertyChanged(nameof(this.DefaultValue));
			}
		}

		/// <summary>获取或设置属性是否允许为空。</summary>
		public bool Nullable
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Nullable));
				field = value;
				this.OnPropertyChanged(nameof(this.Nullable));
			}
		}

		/// <summary>获取或设置属性是否可以参与排序。</summary>
		public bool Sortable
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Sortable));
				field = value;
				this.OnPropertyChanged(nameof(this.Sortable));
			}
		}

		/// <summary>获取或设置数据序号器元数据。</summary>
		public DataPropertySequence Sequence
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Sequence));
				field = value;
				this.OnPropertyChanged(nameof(this.Sequence));
			}
		}
		#endregion

		#region 重写方法
		internal protected override void Populate(MemberInfo member)
		{
			if(member == null)
				return;

			//调用基类同名方法
			base.Populate(member);

			var type = this.Type;
			if(type != null)
			{
				this.DataType = DataType.Get(type);
				this.Nullable = type.IsInterface || type.IsClass || Common.TypeExtension.IsNullable(type);
			}

			var attribute = member.GetCustomAttribute<ModelPropertyAttribute>(true);

			if(attribute != null)
			{
				this.Alias = attribute.Alias;
				this.Length = attribute.Length;
				this.Precision = attribute.Precision;
				this.Scale = attribute.Scale;
				this.Sortable = attribute.Sortable;

				if(attribute.Nullable.HasValue)
					this.Nullable = attribute.Nullable.Value;

				if(!string.IsNullOrWhiteSpace(attribute.Sequence))
					this.Sequence = DataPropertySequence.Parse(attribute.Sequence);

				if(attribute.Type != null)
					this.DataType = attribute.Type;

				if(this.IsPrimaryKey = attribute.IsPrimaryKey)
				{
					this.Sortable = true;
					this.Nullable = false;
				}

				if(attribute.DefaultValue != null)
				{
					if(attribute.DefaultValue is string text && DataPropertyFunction.TryParse(text, out var function))
						this.DefaultValue = function;
					else
						this.DefaultValue = Common.Convert.ConvertValue(attribute.DefaultValue, this.Type);
				}
			}
		}
		#endregion

		#region 嵌套子类
		private class DefaultValueJsonConverterFactory : JsonConverterFactory
		{
			public override bool CanConvert(Type type) => true;
			public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) => Converter.Instance;

			private class Converter : JsonConverter<object>
			{
				public static readonly Converter Instance = new();

				public override object Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
				{
					switch(reader.TokenType)
					{
						case JsonTokenType.Null:
							return null;
						case JsonTokenType.True:
							return true;
						case JsonTokenType.False:
							return false;
						case JsonTokenType.Number:
							var text = reader.GetString();

							if(int.TryParse(text, out var int32))
								return int32;
							else if(long.TryParse(text, out var int64))
								return int64;
							else if(float.TryParse(text, out var @single))
								return @single;
							else if(double.TryParse(text, out var @double))
								return @double;

							return decimal.Parse(text);
						case JsonTokenType.String:
							return DataPropertyFunction.TryParse(reader.GetString(), out var function) ? (object)function : reader.GetString();
						default:
							return Serialization.Json.Converters.ObjectConverter.Default.Read(ref reader, type, options);
					}
				}

				public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
				{
					if(value == null)
					{
						writer.WriteNullValue();
						return;
					}

					switch(Type.GetTypeCode(value.GetType()))
					{
						case TypeCode.Byte:
							writer.WriteNumberValue((byte)value);
							break;
						case TypeCode.SByte:
							writer.WriteNumberValue((sbyte)value);
							break;
						case TypeCode.Int16:
							writer.WriteNumberValue((short)value);
							break;
						case TypeCode.Int32:
							writer.WriteNumberValue((int)value);
							break;
						case TypeCode.Int64:
							writer.WriteNumberValue((long)value);
							break;
						case TypeCode.UInt16:
							writer.WriteNumberValue((ushort)value);
							break;
						case TypeCode.UInt32:
							writer.WriteNumberValue((uint)value);
							break;
						case TypeCode.UInt64:
							writer.WriteNumberValue((ulong)value);
							break;
						case TypeCode.Single:
							writer.WriteNumberValue((float)value);
							break;
						case TypeCode.Double:
							writer.WriteNumberValue((double)value);
							break;
						case TypeCode.Decimal:
							writer.WriteNumberValue((decimal)value);
							break;
						case TypeCode.Boolean:
							writer.WriteBooleanValue((bool)value);
							break;
						case TypeCode.String:
							writer.WriteStringValue((string)value);
							break;
						default:
							writer.WriteStringValue(Common.Convert.ConvertValue<string>(value));
							break;
					}
				}
			}
		}
		#endregion
	}
}
