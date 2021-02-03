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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Serialization
{
	public static class Serializer
	{
		#region 成员字段
		private static ITextSerializer _json = JsonSerializerWrapper.Instance;
		#endregion

		#region 公共属性
		public static ITextSerializer Json
		{
			get => _json;
			set => _json = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 嵌套子类
		private class JsonSerializerWrapper : ITextSerializer
		{
			#region 单例字段
			public static readonly JsonSerializerWrapper Instance = new JsonSerializerWrapper();
			#endregion

			#region 默认配置
			private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions()
			{
				NumberHandling = JsonNumberHandling.AllowReadingFromString,
				Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
				PropertyNameCaseInsensitive = true,
				IgnoreReadOnlyProperties = false,
				Converters =
				{
					new JsonTimeSpanConverter(),
					new JsonStringEnumConverter(),
					new ModelConverterFactory(),
					new RangeConverterFactory(),
					new NullableConverterFactory(),
				},
			};
			#endregion

			#region 构造函数
			private JsonSerializerWrapper()
			{
			}
			#endregion

			#region 反序列化
			public object Deserialize(Stream stream, SerializationOptions options = null)
			{
				throw new NotImplementedException();
			}

			public object Deserialize(Stream stream, Type type, SerializationOptions options = null)
			{
				using(var reader = new StreamReader(stream, Encoding.UTF8))
				{
					return JsonSerializer.Deserialize(reader.ReadToEnd(), type, GetOptions(options));
				}
			}

			public T Deserialize<T>(Stream stream, SerializationOptions options = null)
			{
				using(var reader = new StreamReader(stream, Encoding.UTF8))
				{
					return JsonSerializer.Deserialize<T>(reader.ReadToEnd(), GetOptions(options));
				}
			}

			public ValueTask<object> DeserializeAsync(Stream stream, SerializationOptions options = null, CancellationToken cancellationToken = default)
			{
				throw new NotImplementedException();
			}

			public ValueTask<object> DeserializeAsync(Stream stream, Type type, SerializationOptions options = null, CancellationToken cancellationToken = default)
			{
				return JsonSerializer.DeserializeAsync(stream, type, GetOptions(options), cancellationToken);
			}

			public ValueTask<T> DeserializeAsync<T>(Stream stream, SerializationOptions options = null, CancellationToken cancellationToken = default)
			{
				return JsonSerializer.DeserializeAsync<T>(stream, GetOptions(options), cancellationToken);
			}

			public object Deserialize(string text, TextSerializationOptions options = null)
			{
				throw new NotImplementedException();
			}

			public object Deserialize(string text, Type type, TextSerializationOptions options = null)
			{
				return JsonSerializer.Deserialize(text, type, GetOptions(options));
			}

			public T Deserialize<T>(string text, TextSerializationOptions options = null)
			{
				return JsonSerializer.Deserialize<T>(text, GetOptions(options));
			}

			public ValueTask<object> DeserializeAsync(string text, TextSerializationOptions options = null, CancellationToken cancellationToken = default)
			{
				throw new NotImplementedException();
			}

			public ValueTask<object> DeserializeAsync(string text, Type type, TextSerializationOptions options = null, CancellationToken cancellationToken = default)
			{
				using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
				{
					return JsonSerializer.DeserializeAsync(stream, type, GetOptions(options), cancellationToken);
				}
			}

			public ValueTask<T> DeserializeAsync<T>(string text, TextSerializationOptions options = null, CancellationToken cancellationToken = default)
			{
				using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
				{
					return JsonSerializer.DeserializeAsync<T>(stream, GetOptions(options), cancellationToken);
				}
			}
			#endregion

			#region 序列方法
			public void Serialize(Stream stream, object graph, Type type = null, SerializationOptions options = null)
			{
				using(Utf8JsonWriter writer = new Utf8JsonWriter(stream))
				{
					JsonSerializer.Serialize(writer, graph, type, GetOptions(options));
				}
			}

			public string Serialize(object graph, TextSerializationOptions options = null)
			{
				return JsonSerializer.Serialize(graph, GetOptions(options));
			}

			public Task SerializeAsync(Stream stream, object graph, Type type = null, SerializationOptions options = null, CancellationToken cancellationToken = default)
			{
				if(stream == null)
					throw new ArgumentNullException(nameof(stream));

				return JsonSerializer.SerializeAsync(stream, graph, type, GetOptions(options), cancellationToken);
			}

			public async Task<string> SerializeAsync(object graph, TextSerializationOptions options = null, CancellationToken cancellationToken = default)
			{
				using(var stream = new MemoryStream())
				{
					await JsonSerializer.SerializeAsync(stream, graph, GetOptions(options), cancellationToken);

					using(var reader = new StreamReader(stream, Encoding.UTF8, false))
					{
						return await reader.ReadToEndAsync();
					}
				}
			}
			#endregion

			#region 私有方法
			private static JsonSerializerOptions GetOptions(SerializationOptions options)
			{
				if(options == null)
					return DefaultOptions;

				if(options is TextSerializationOptions text)
					return GetOptions(text);

				var ignores = JsonIgnoreCondition.Never;

				if(options.IgnoreNull)
					ignores = JsonIgnoreCondition.WhenWritingNull;
				else if(options.IgnoreZero)
					ignores = JsonIgnoreCondition.WhenWritingDefault;

				return new JsonSerializerOptions()
				{
					Encoder = DefaultOptions.Encoder,
					PropertyNameCaseInsensitive = true,
					MaxDepth = options.MaximumDepth,
					NumberHandling = JsonNumberHandling.AllowReadingFromString,
					DefaultIgnoreCondition = ignores,
					IgnoreReadOnlyProperties = false,
					Converters =
					{
						new JsonTimeSpanConverter(),
						new JsonStringEnumConverter(),
						new ModelConverterFactory(),
						new RangeConverterFactory(),
						new NullableConverterFactory(),
					},
				};
			}

			private static JsonSerializerOptions GetOptions(TextSerializationOptions options)
			{
				if(options == null)
					return DefaultOptions;

				JsonNamingPolicy naming = null;

				switch(options.NamingConvention)
				{
					case SerializationNamingConvention.Camel:
						naming = JsonNamingConvention.Camel;
						break;
					case SerializationNamingConvention.Pascal:
						naming = JsonNamingConvention.Pascal;
						break;
				}

				var ignores = JsonIgnoreCondition.Never;

				if(options.IgnoreNull)
					ignores = JsonIgnoreCondition.WhenWritingNull;
				else if(options.IgnoreZero)
					ignores = JsonIgnoreCondition.WhenWritingDefault;

				return new JsonSerializerOptions()
				{
					Encoder = DefaultOptions.Encoder,
					PropertyNameCaseInsensitive = true,
					MaxDepth = options.MaximumDepth,
					WriteIndented = options.Indented,
					NumberHandling = JsonNumberHandling.AllowReadingFromString,
					DefaultIgnoreCondition = ignores,
					IgnoreReadOnlyProperties = false,
					PropertyNamingPolicy = naming,
					DictionaryKeyPolicy = naming,
					Converters =
					{
						new JsonTimeSpanConverter(),
						new JsonStringEnumConverter(naming),
						new ModelConverterFactory(),
						new RangeConverterFactory(),
						new NullableConverterFactory(),
					},
				};
			}
			#endregion
		}

		private static class JsonNamingConvention
		{
			public static readonly JsonNamingPolicy Camel = new LetterCaseNamingPolicy(chr => char.ToLowerInvariant(chr));
			public static readonly JsonNamingPolicy Pascal = new LetterCaseNamingPolicy(chr => char.ToUpperInvariant(chr));

			private class LetterCaseNamingPolicy : JsonNamingPolicy
			{
				#region 成员字段
				private readonly Func<char, char> _converter;
				#endregion

				#region 构造函数
				public LetterCaseNamingPolicy(Func<char, char> converter)
				{
					_converter = converter ?? throw new ArgumentNullException(nameof(converter));
				}
				#endregion

				#region 公共方法
				public override string ConvertName(string name)
				{
					if(string.IsNullOrEmpty(name))
						return name;

					char[] chars = name.ToCharArray();
					FixCasing(chars, _converter);
					return new string(chars);
				}
				#endregion

				#region 私有方法
				private static void FixCasing(Span<char> chars, Func<char, char> converter)
				{
					var initial = true;

					for(int i = 0; i < chars.Length; i++)
					{
						if(initial)
						{
							if(char.IsLetter(chars[i]))
							{
								chars[i] = converter(chars[i]);
								initial = false;
							}
						}
						else
						{
							if(!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
								initial = true;
						}
					}
				}
				#endregion
			}
		}

		private class JsonTimeSpanConverter : JsonConverter<TimeSpan>
		{
			public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				if(reader.TokenType == JsonTokenType.Number)
					return TimeSpan.FromSeconds(reader.GetDouble());
				else
					return TimeSpan.Parse(reader.GetString());
			}

			public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
			{
				writer.WriteStringValue(value.ToString());
			}
		}

		private class RangeConverterFactory : JsonConverterFactory
		{
			public override bool CanConvert(Type type)
			{
				return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Data.Range<>);
			}

			public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
			{
				return (JsonConverter)Activator.CreateInstance(typeof(RangeConverter<>).MakeGenericType(new Type[] { type }));
			}

			private class RangeConverter<T> : JsonConverter<T> where T : struct
			{
				public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
				{
					switch(reader.TokenType)
					{
						case JsonTokenType.String:
							return (T)Common.Convert.ConvertValue(reader.GetString(), type);
						case JsonTokenType.StartObject:
							object minimum = null, maximum = null;

							while(reader.Read())
							{
								if(reader.TokenType == JsonTokenType.EndObject)
									break;

								if(reader.TokenType != JsonTokenType.PropertyName)
									throw new JsonException();

								var name = reader.GetString();

								if(string.Equals(name, "minimum", StringComparison.OrdinalIgnoreCase))
									minimum = GetValue(ref reader, type.GenericTypeArguments[0]);
								else if(string.Equals(name, "maximum", StringComparison.OrdinalIgnoreCase))
									maximum = GetValue(ref reader, type.GenericTypeArguments[0]);
							}

							return (T)Data.Range.Create(type, minimum, maximum);
					}

					return default;
				}

				public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
				{
					writer.WriteStringValue(value.ToString());
				}

				private object GetValue(ref Utf8JsonReader reader, Type type)
				{
					if(reader.TokenType == JsonTokenType.PropertyName)
						reader.Read();

					switch(reader.TokenType)
					{
						case JsonTokenType.None:
						case JsonTokenType.Null:
							return null;
						case JsonTokenType.True:
							return true;
						case JsonTokenType.False:
							return false;
						case JsonTokenType.String:
							return reader.GetString();
						case JsonTokenType.Number:
							return Type.GetTypeCode(type) switch
							{
								TypeCode.Byte => reader.GetByte(),
								TypeCode.SByte => reader.GetSByte(),
								TypeCode.Int16 => reader.GetInt16(),
								TypeCode.UInt16 => reader.GetUInt16(),
								TypeCode.Int32 => reader.GetInt32(),
								TypeCode.UInt32 => reader.GetUInt32(),
								TypeCode.Int64 => reader.GetInt64(),
								TypeCode.UInt64 => reader.GetUInt64(),
								TypeCode.Single => reader.GetSingle(),
								TypeCode.Double => reader.GetDouble(),
								TypeCode.Decimal => reader.GetDecimal(),
								_ => throw new JsonException(),
							};
						default:
							throw new JsonException();
					}
				}
			}
		}

		private class NullableConverterFactory : JsonConverterFactory
		{
			public override bool CanConvert(Type type)
			{
				return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
			}

			public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
			{
				return (JsonConverter)Activator.CreateInstance(typeof(NullableConverter<>).MakeGenericType(new Type[] { type }));
			}

			private class NullableConverter<T> : JsonConverter<T>
			{
				public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
				{
					var underlyingType = Nullable.GetUnderlyingType(type);

					switch(reader.TokenType)
					{
						case JsonTokenType.Null:
							return default;
						case JsonTokenType.True:
							return (T)Common.Convert.ConvertValue(true, underlyingType);
						case JsonTokenType.False:
							return (T)Common.Convert.ConvertValue(false, underlyingType);
						case JsonTokenType.String:
							return (T)Common.Convert.ConvertValue(reader.GetString(), underlyingType);
						case JsonTokenType.Number:
							return Type.GetTypeCode(underlyingType) switch
							{
								TypeCode.Byte => (T)Convert.ChangeType(reader.GetByte(), underlyingType),
								TypeCode.SByte => (T)Convert.ChangeType(reader.GetSByte(), underlyingType),
								TypeCode.Int16 => (T)Convert.ChangeType(reader.GetInt16(), underlyingType),
								TypeCode.UInt16 => (T)Convert.ChangeType(reader.GetUInt16(), underlyingType),
								TypeCode.Int32 => (T)Convert.ChangeType(reader.GetInt32(), underlyingType),
								TypeCode.UInt32 => (T)Convert.ChangeType(reader.GetUInt32(), underlyingType),
								TypeCode.Int64 => (T)Convert.ChangeType(reader.GetInt64(), underlyingType),
								TypeCode.UInt64 => (T)Convert.ChangeType(reader.GetUInt64(), underlyingType),
								TypeCode.Single => (T)Convert.ChangeType(reader.GetSingle(), underlyingType),
								TypeCode.Double => (T)Convert.ChangeType(reader.GetDouble(), underlyingType),
								TypeCode.Decimal => (T)Convert.ChangeType(reader.GetDecimal(), underlyingType),
								_ => (T)(object)null,
							};
						default:
							return (T)(object)null;
					}
				}

				public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
				{
					if(value == null)
					{
						writer.WriteNullValue();
						return;
					}

					switch(value)
					{
						case bool _:
							writer.WriteBooleanValue((bool)Convert.ChangeType(value, TypeCode.Boolean));
							break;
						case byte _:
							writer.WriteNumberValue((byte)Convert.ChangeType(value, TypeCode.Byte));
							break;
						case sbyte _:
							writer.WriteNumberValue((sbyte)Convert.ChangeType(value, TypeCode.SByte));
							break;
						case short _:
							writer.WriteNumberValue((short)Convert.ChangeType(value, TypeCode.Int16));
							break;
						case ushort _:
							writer.WriteNumberValue((ushort)Convert.ChangeType(value, TypeCode.UInt16));
							break;
						case int _:
							writer.WriteNumberValue((int)Convert.ChangeType(value, TypeCode.Int32));
							break;
						case uint _:
							writer.WriteNumberValue((uint)Convert.ChangeType(value, TypeCode.UInt32));
							break;
						case long _:
							writer.WriteNumberValue((long)Convert.ChangeType(value, TypeCode.Int64));
							break;
						case ulong _:
							writer.WriteNumberValue((ulong)Convert.ChangeType(value, TypeCode.UInt64));
							break;
						case float _:
							writer.WriteNumberValue((float)Convert.ChangeType(value, TypeCode.Single));
							break;
						case double _:
							writer.WriteNumberValue((double)Convert.ChangeType(value, TypeCode.Double));
							break;
						case decimal _:
							writer.WriteNumberValue((decimal)Convert.ChangeType(value, TypeCode.Decimal));
							break;
						case DateTime _:
							writer.WriteStringValue((DateTime)Convert.ChangeType(value, TypeCode.DateTime));
							break;
						case DateTimeOffset _:
							writer.WriteStringValue((DateTimeOffset)Convert.ChangeType(value, typeof(DateTimeOffset)));
							break;
						case TimeSpan _:
							writer.WriteStringValue(value.ToString());
							break;
						case Guid _:
							writer.WriteStringValue((Guid)Convert.ChangeType(value, typeof(Guid)));
							break;
						default:
							writer.WriteStringValue(Common.Convert.ConvertValue<string>(value));
							break;
					}
				}
			}
		}

		private class ModelConverterFactory : JsonConverterFactory
		{
			public override bool CanConvert(Type type)
			{
				return (type.IsInterface || type.IsAbstract) && !Common.TypeExtension.IsEnumerable(type);
			}

			public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
			{
				return (JsonConverter)Activator.CreateInstance(typeof(ModelConverter<>).MakeGenericType(new Type[] { type }));
			}

			private class ModelConverter<T> : JsonConverter<T> where T : class
			{
				private static readonly JsonEncodedText _TYPE_KEY_ = JsonEncodedText.Encode("$type");
				private static readonly JsonEncodedText _TYPE_VALUE_ = JsonEncodedText.Encode(typeof(T).FullName);

				public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
				{
					if(reader.TokenType != JsonTokenType.StartObject)
						throw new JsonException();

					var model = (Data.IModel)Data.Model.Build<T>();

					while(reader.Read())
					{
						if(reader.TokenType == JsonTokenType.EndObject)
							break;

						if(reader.TokenType != JsonTokenType.PropertyName)
							throw new JsonException();

						var member = GetMember(type, reader.GetString(), out var memberType);
						reader.Read();

						if(member == null)
						{
							if(reader.TokenType == JsonTokenType.StartArray || reader.TokenType == JsonTokenType.StartObject)
								reader.Skip();

							continue;
						}

						if(memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(Nullable<>))
							memberType = Nullable.GetUnderlyingType(memberType);

						switch(reader.TokenType)
						{
							case JsonTokenType.None:
							case JsonTokenType.Null:
								model.TrySetValue(member.Name, null);
								break;
							case JsonTokenType.True:
								model.TrySetValue(member.Name, true);
								break;
							case JsonTokenType.False:
								model.TrySetValue(member.Name, false);
								break;
							case JsonTokenType.String:
								if(memberType == typeof(DateTime))
									model.TrySetValue(member.Name, reader.GetDateTime());
								else if(memberType == typeof(DateTimeOffset))
									model.TrySetValue(member.Name, reader.GetDateTimeOffset());
								else if(memberType.IsEnum)
									model.TrySetValue(member.Name, Enum.Parse(memberType, reader.GetString(), true));
								else
									model.TrySetValue(member.Name, Common.Convert.ConvertValue(reader.GetString(), memberType));
								break;
							case JsonTokenType.Number:
								switch(Type.GetTypeCode(memberType))
								{
									case TypeCode.Byte:
										model.TrySetValue(member.Name, reader.GetByte());
										break;
									case TypeCode.SByte:
										model.TrySetValue(member.Name, reader.GetSByte());
										break;
									case TypeCode.Int16:
										model.TrySetValue(member.Name, reader.GetInt16());
										break;
									case TypeCode.Int32:
										model.TrySetValue(member.Name, reader.GetInt32());
										break;
									case TypeCode.Int64:
										model.TrySetValue(member.Name, reader.GetInt64());
										break;
									case TypeCode.UInt16:
										model.TrySetValue(member.Name, reader.GetUInt16());
										break;
									case TypeCode.UInt32:
										model.TrySetValue(member.Name, reader.GetUInt32());
										break;
									case TypeCode.UInt64:
										model.TrySetValue(member.Name, reader.GetUInt64());
										break;
									case TypeCode.Single:
										model.TrySetValue(member.Name, reader.GetSingle());
										break;
									case TypeCode.Double:
										model.TrySetValue(member.Name, reader.GetDouble());
										break;
									case TypeCode.Decimal:
										model.TrySetValue(member.Name, reader.GetDecimal());
										break;
									default:
										if(memberType.IsEnum)
											model.TrySetValue(member.Name, Enum.ToObject(memberType, reader.GetInt64()));
										break;
								}
								break;
							default:
								var value = JsonSerializer.Deserialize(ref reader, memberType, options);

								if(!model.TrySetValue(member.Name, value))
								{
									var collectionType = GetImplementedContract(memberType, typeof(ICollection<>));

									if(collectionType != null && Reflection.Reflector.TryGetValue(member, (T)model, out var target))
									{
										var add = collectionType.GetTypeInfo().GetDeclaredMethod("Add");

										foreach(var item in (System.Collections.IEnumerable)value)
										{
											add.Invoke(target, new object[] { item });
										}
									}
								}

								break;
						}
					}

					return (T)model;
				}

				public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
				{
					if(value == null)
					{
						writer.WriteNullValue();
						return;
					}

					writer.WriteStartObject();

					foreach(var property in value.GetType().BaseType.GetTypeInfo().DeclaredProperties)
					{
						//如果是只读属性并且忽略只读属性则跳过
						if(!property.CanWrite && options.IgnoreReadOnlyProperties)
							continue;

						var key = options.PropertyNamingPolicy == null ? property.Name : options.PropertyNamingPolicy.ConvertName(property.Name);
						var propertyValue = Reflection.Reflector.GetValue(property, ref value);

						if(propertyValue == null || Convert.IsDBNull(propertyValue))
						{
							if(!options.IgnoreNullValues && options.DefaultIgnoreCondition != JsonIgnoreCondition.WhenWritingNull)
								writer.WriteNull(key);

							continue;
						}

						if(property.PropertyType.IsEnum)
						{
							writer.WritePropertyName(key);
							JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);

							continue;
						}

						switch(Type.GetTypeCode(property.PropertyType))
						{
							case TypeCode.String:
								writer.WriteString(key, (string)propertyValue);
								continue;
							case TypeCode.Boolean:
								writer.WriteBoolean(key, (bool)propertyValue);
								continue;
							case TypeCode.DateTime:
								writer.WriteString(key, (DateTime)propertyValue);
								continue;
							case TypeCode.Byte:
								writer.WriteNumber(key, (byte)propertyValue);
								continue;
							case TypeCode.SByte:
								writer.WriteNumber(key, (sbyte)propertyValue);
								continue;
							case TypeCode.Int16:
								writer.WriteNumber(key, (short)propertyValue);
								continue;
							case TypeCode.Int32:
								writer.WriteNumber(key, (int)propertyValue);
								continue;
							case TypeCode.Int64:
								writer.WriteNumber(key, (long)propertyValue);
								continue;
							case TypeCode.UInt16:
								writer.WriteNumber(key, (ushort)propertyValue);
								continue;
							case TypeCode.UInt32:
								writer.WriteNumber(key, (uint)propertyValue);
								continue;
							case TypeCode.UInt64:
								writer.WriteNumber(key, (ulong)propertyValue);
								continue;
							case TypeCode.Single:
								writer.WriteNumber(key, (float)propertyValue);
								continue;
							case TypeCode.Double:
								writer.WriteNumber(key, (double)propertyValue);
								continue;
							case TypeCode.Decimal:
								writer.WriteNumber(key, (decimal)propertyValue);
								continue;
						}

						switch(propertyValue)
						{
							case Guid _:
								writer.WriteString(key, (Guid)propertyValue);
								continue;
							case TimeSpan _:
								writer.WriteString(key, propertyValue.ToString());
								continue;
							case DateTimeOffset _:
								writer.WriteString(key, (DateTimeOffset)propertyValue);
								continue;
						}

						if(TryGetString(property, propertyValue, out var text))
						{
							writer.WriteString(key, text);
							continue;
						}

						writer.WritePropertyName(key);
						JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
					}

					writer.WriteEndObject();
				}

				private static bool TryGetString(PropertyInfo property, object value, out string text)
				{
					text = null;

					if(value == null)
						return false;

					var converter = TypeDescriptor.GetConverter(property) ??
					                TypeDescriptor.GetConverter(value.GetType());

					if(converter != null && converter.GetType() != typeof(TypeConverter) && converter.CanConvertTo(typeof(string)))
					{
						text = (string)converter.ConvertTo(value, typeof(string));
						return true;
					}

					return false;
				}

				private static MemberInfo GetMember(Type type, string name, out Type memberType)
				{
					var typeInfo = type.GetTypeInfo();
					var contracts = typeInfo.GetInterfaces();
					var index = 0;

					do
					{
						foreach(var property in typeInfo.DeclaredProperties)
						{
							if(property.CanRead && property.GetMethod.IsPublic && !property.GetMethod.IsStatic && string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase))
							{
								memberType = property.PropertyType;
								return property;
							}
						}

						if(typeInfo.BaseType != null)
							typeInfo = typeInfo.BaseType.GetTypeInfo();
						else if(contracts != null && contracts.Length > 0 && index < contracts.Length)
							typeInfo = contracts[index++].GetTypeInfo();
						else
							typeInfo = null;
					}
					while(typeInfo != null && typeInfo != typeof(object).GetTypeInfo());

					memberType = null;
					return null;
				}

				private static Type GetImplementedContract(Type actual, params Type[] expectedTypes)
				{
					if(actual.IsGenericType && expectedTypes.Contains(actual.GetGenericTypeDefinition()))
						return actual;

					var contracts = actual.GetTypeInfo().ImplementedInterfaces;

					foreach(var contract in contracts)
					{
						if(contract.IsGenericType && expectedTypes.Contains(contract.GetGenericTypeDefinition()))
							return contract;
					}

					return null;
				}
			}
		}
		#endregion
	}
}
