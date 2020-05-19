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
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Serialization
{
	public static class Serializer
	{
		#region 成员字段
		private static ITextSerializer _json;
		#endregion

		#region 公共属性
		public static ITextSerializer Json
		{
			get => _json ?? JsonSerializerWrapper.Default;
			set => _json = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 嵌套子类
		private class JsonSerializerWrapper : ITextSerializer
		{
			#region 单例字段
			public static readonly JsonSerializerWrapper Default = new JsonSerializerWrapper();
			#endregion

			#region 默认配置
			private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions()
			{
				Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
				PropertyNameCaseInsensitive = true,
				Converters =
				{
					new TimeSpanConverter(),
					new JsonStringEnumConverter(),
					new ModelConverterFactory(),
				},
			};
			#endregion

			#region 构造函数
			private JsonSerializerWrapper()
			{
			}
			#endregion

			#region 反序列化
			public object Deserialize(Stream stream, SerializationSettings settings = null)
			{
				throw new NotImplementedException();
			}

			public object Deserialize(Stream stream, Type type, SerializationSettings settings = null)
			{
				using(var reader = new StreamReader(stream, Encoding.UTF8))
				{
					return JsonSerializer.Deserialize(reader.ReadToEnd(), type, GetOptions(settings));
				}
			}

			public T Deserialize<T>(Stream stream, SerializationSettings settings = null)
			{
				using(var reader = new StreamReader(stream, Encoding.UTF8))
				{
					return JsonSerializer.Deserialize<T>(reader.ReadToEnd(), GetOptions(settings));
				}
			}

			public ValueTask<object> DeserializeAsync(Stream stream, SerializationSettings settings = null, CancellationToken cancellationToken = default)
			{
				throw new NotImplementedException();
			}

			public ValueTask<object> DeserializeAsync(Stream stream, Type type, SerializationSettings settings = null, CancellationToken cancellationToken = default)
			{
				return JsonSerializer.DeserializeAsync(stream, type, GetOptions(settings), cancellationToken);
			}

			public ValueTask<T> DeserializeAsync<T>(Stream stream, SerializationSettings settings = null, CancellationToken cancellationToken = default)
			{
				return JsonSerializer.DeserializeAsync<T>(stream, GetOptions(settings), cancellationToken);
			}

			public object Deserialize(string text, TextSerializationSettings settings = null)
			{
				throw new NotImplementedException();
			}

			public object Deserialize(string text, Type type, TextSerializationSettings settings = null)
			{
				return JsonSerializer.Deserialize(text, type, GetOptions(settings));
			}

			public T Deserialize<T>(string text, TextSerializationSettings settings = null)
			{
				return JsonSerializer.Deserialize<T>(text, GetOptions(settings));
			}

			public ValueTask<object> DeserializeAsync(string text, TextSerializationSettings settings = null, CancellationToken cancellationToken = default)
			{
				throw new NotImplementedException();
			}

			public ValueTask<object> DeserializeAsync(string text, Type type, TextSerializationSettings settings = null, CancellationToken cancellationToken = default)
			{
				using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
				{
					return JsonSerializer.DeserializeAsync(stream, type, GetOptions(settings), cancellationToken);
				}
			}

			public ValueTask<T> DeserializeAsync<T>(string text, TextSerializationSettings settings = null, CancellationToken cancellationToken = default)
			{
				using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
				{
					return JsonSerializer.DeserializeAsync<T>(stream, GetOptions(settings), cancellationToken);
				}
			}
			#endregion

			#region 序列方法
			public void Serialize(Stream stream, object graph, SerializationSettings settings = null)
			{
				using(Utf8JsonWriter writer = new Utf8JsonWriter(stream))
				{
					JsonSerializer.Serialize(writer, graph, GetOptions(settings));
				}
			}

			public string Serialize(object graph, TextSerializationSettings settings = null)
			{
				return JsonSerializer.Serialize(graph, GetOptions(settings));
			}

			public Task SerializeAsync(Stream stream, object graph, SerializationSettings settings = null, CancellationToken cancellationToken = default)
			{
				if(stream == null)
					throw new ArgumentNullException(nameof(stream));

				return JsonSerializer.SerializeAsync(stream, graph, GetOptions(settings), cancellationToken);
			}

			public async Task<string> SerializeAsync(object graph, TextSerializationSettings settings = null, CancellationToken cancellationToken = default)
			{
				using(var stream = new MemoryStream())
				{
					await JsonSerializer.SerializeAsync(stream, graph, GetOptions(settings), cancellationToken);

					using(var reader = new StreamReader(stream, Encoding.UTF8, false))
					{
						return await reader.ReadToEndAsync();
					}
				}
			}
			#endregion

			#region 私有方法
			private static JsonSerializerOptions GetOptions(SerializationSettings settings)
			{
				if(settings == null)
					return DefaultOptions;

				if(settings is TextSerializationSettings text)
					return GetOptions(text);

				return new JsonSerializerOptions()
				{
					Encoder = DefaultOptions.Encoder,
					PropertyNameCaseInsensitive = true,
					MaxDepth = settings.MaximumDepth,
					IgnoreNullValues = settings.IgnoreNull,
					IgnoreReadOnlyProperties = true,
					Converters =
					{
						new TimeSpanConverter(),
						new JsonStringEnumConverter(),
						new ModelConverterFactory()
					},
				};
			}

			private static JsonSerializerOptions GetOptions(TextSerializationSettings settings)
			{
				if(settings == null)
					return DefaultOptions;

				JsonNamingPolicy naming = null;

				switch(settings.NamingConvention)
				{
					case SerializationNamingConvention.Camel:
						naming = JsonNamingConvention.Camel;
						break;
					case SerializationNamingConvention.Pascal:
						naming = JsonNamingConvention.Pascal;
						break;
				}

				return new JsonSerializerOptions()
				{
					Encoder = DefaultOptions.Encoder,
					PropertyNameCaseInsensitive = true,
					MaxDepth = settings.MaximumDepth,
					WriteIndented = settings.Indented,
					IgnoreNullValues = settings.IgnoreNull,
					IgnoreReadOnlyProperties = true,
					PropertyNamingPolicy = naming,
					DictionaryKeyPolicy = naming,
					Converters =
					{
						new TimeSpanConverter(),
						new JsonStringEnumConverter(naming),
						new ModelConverterFactory()
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

		private class TimeSpanConverter : JsonConverter<TimeSpan>
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

				public override bool CanConvert(Type type)
				{
					return type.IsInterface || type.IsAbstract;
				}

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
							continue;

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
									model.TrySetValue(member.Name, reader.GetString());
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

					if(value is Data.IModel model)
					{
						writer.WriteStartObject();
						writer.WriteString(_TYPE_KEY_, _TYPE_VALUE_);

						//JsonSerializer.Serialize(writer, model.GetChanges(), options);

						writer.WriteEndObject();
					}
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

						foreach(var field in typeInfo.DeclaredFields)
						{
							if(!field.IsInitOnly && field.IsPublic && !field.IsStatic && string.Equals(name, field.Name, StringComparison.OrdinalIgnoreCase))
							{
								memberType = field.FieldType;
								return field;
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
			}
		}
		#endregion
	}
}
