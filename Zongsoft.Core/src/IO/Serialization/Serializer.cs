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

namespace Zongsoft.Runtime.Serialization
{
	public static class Serializer
	{
		#region 成员字段
		private static ITextSerializer _json;
		#endregion

		#region 公共属性
		public static ITextSerializer Json
		{
			get => _json ?? JsonSerializerInner.Default;
			set => _json = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 嵌套子类
		private class JsonSerializerInner : ITextSerializer
		{
			#region 单例字段
			public static readonly JsonSerializerInner Default = new JsonSerializerInner();
			#endregion

			#region 默认配置
			private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions()
			{
				PropertyNameCaseInsensitive = true,
				IgnoreNullValues = false,
				IgnoreReadOnlyProperties = true,
				Converters =
				{
					new JsonStringEnumConverter(),
					new ModelConverterFactory()
				},
			};
			#endregion

			#region 构造函数
			public JsonSerializerInner()
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
					PropertyNameCaseInsensitive = true,
					MaxDepth = settings.MaximumDepth,
					IgnoreNullValues = settings.IgnoreNull,
					IgnoreReadOnlyProperties = true,
					Converters =
					{
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
						naming = JsonNamingPolicy.CamelCase;
						break;
					case SerializationNamingConvention.Pascal:
						naming = PascalCaseNamingPlicy.Instance;
						break;
				}

				return new JsonSerializerOptions()
				{
					PropertyNameCaseInsensitive = true,
					MaxDepth = settings.MaximumDepth,
					WriteIndented = settings.Indented,
					IgnoreNullValues = settings.IgnoreNull,
					IgnoreReadOnlyProperties = true,
					PropertyNamingPolicy = naming,
					DictionaryKeyPolicy = naming,
					Converters =
					{
						new JsonStringEnumConverter(naming),
						new ModelConverterFactory()
					},
				};
			}
			#endregion
		}

		private class PascalCaseNamingPlicy : JsonNamingPolicy
		{
			public static readonly PascalCaseNamingPlicy Instance = new PascalCaseNamingPlicy();

			private PascalCaseNamingPlicy()
			{
			}

			public override string ConvertName(string name)
			{
				if(string.IsNullOrEmpty(name))
					return name;

				return string.Create(name.Length, name, (chars, name) =>
				{
					name.AsSpan().CopyTo(chars);
					FixCasing(chars);
				});
			}

			private static void FixCasing(Span<char> chars)
			{
				for(int i = 0; i < chars.Length; i++)
				{
					if(i == 1 && !char.IsLower(chars[i]))
						break;

					bool hasNext = (i + 1 < chars.Length);

					//如果下个字符是大写则终止
					if(i > 0 && hasNext && !char.IsLower(chars[i + 1]))
					{
						//如果下个字符是空格，则在退出前将当前字符转换为大写
						if(chars[i + 1] == ' ')
							chars[i] = char.ToUpperInvariant(chars[i]);

						break;
					}

					chars[i] = char.ToUpperInvariant(chars[i]);
				}
			}
		}

		private class ModelConverterFactory : JsonConverterFactory
		{
			public override bool CanConvert(Type type)
			{
				return type.IsInterface || type.IsAbstract;
			}

			public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
			{
				JsonConverter converter = (JsonConverter)Activator.CreateInstance(
				   typeof(ModelConverter<>).MakeGenericType(new Type[] { type }),
				   BindingFlags.Instance | BindingFlags.Public,
				   binder: null,
				   args: new object[] { options },
				   culture: null);

				return converter;
			}

			private class ModelConverter<T> : JsonConverter<T> where T : class
			{
				private static readonly JsonEncodedText _TYPE_KEY_ = JsonEncodedText.Encode("$type");
				private static readonly JsonEncodedText _TYPE_VALUE_ = JsonEncodedText.Encode(typeof(T).FullName);

				private readonly Type _type;
				private readonly JsonConverter<T> _converter;

				public ModelConverter(JsonSerializerOptions options)
				{
					_type = typeof(T);
					_converter = (JsonConverter<T>)options.GetConverter(typeof(T));
				}

				public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
				{
					if(reader.TokenType != JsonTokenType.StartObject)
						throw new JsonException();

					var model = (Zongsoft.Data.IModel)Zongsoft.Data.Model.Build<T>();

					while(reader.Read())
					{
						if(reader.TokenType == JsonTokenType.EndObject)
							break;

						if(reader.TokenType != JsonTokenType.PropertyName)
							throw new JsonException();

						var key = reader.GetString();
						reader.Read();

						switch(reader.TokenType)
						{
							case JsonTokenType.None:
							case JsonTokenType.Null:
								model.TrySetValue(key, null);
								break;
							case JsonTokenType.True:
								model.TrySetValue(key, true);
								break;
							case JsonTokenType.False:
								model.TrySetValue(key, false);
								break;
							case JsonTokenType.String:
								model.TrySetValue(key, reader.GetString());
								break;
							case JsonTokenType.Number:
								if(reader.TryGetInt32(out var integer))
									model.TrySetValue(key, integer);
								else if(reader.TryGetDouble(out var numeric))
									model.TrySetValue(key, numeric);
								break;
							default:
								var memberType = GetMemberType(key);

								if(memberType != null)
								{
									var value = JsonSerializer.Deserialize(ref reader, memberType, options);
									model.TrySetValue(key, value);
								}

								break;
						}
					}

					return (T)model;
				}

				public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
				{
					writer.WriteStartObject();

					writer.WriteString(_TYPE_KEY_, _TYPE_VALUE_);

					if(_converter != null)
						_converter.Write(writer, value, options);
					else
						JsonSerializer.Serialize(writer, value, options);

					writer.WriteEndObject();
				}

				private Type GetMemberType(string name)
				{
					return _type.GetProperty(name)?.PropertyType ??
					       _type.GetField(name)?.FieldType;
				}
			}
		}
		#endregion
	}
}
