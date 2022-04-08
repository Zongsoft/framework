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
using System.Collections.Concurrent;
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
			internal static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions()
			{
				NumberHandling = JsonNumberHandling.AllowReadingFromString,
				Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
				PropertyNameCaseInsensitive = true,
				IgnoreReadOnlyProperties = false,
				IncludeFields = true,
				Converters =
				{
					new JsonTimeSpanConverter(),
					new JsonStringEnumConverter(),
					new ModelConverterFactory(),
					new RangeConverterFactory(),
					new ComplexConverterFactory(),
				},
			};
			#endregion

			#region 构造函数
			private JsonSerializerWrapper()
			{
				this.Options = new TextSerializationOptions()
				{
					IncludeFields = true,
				};
			}
			#endregion

			#region 公共属性
			public TextSerializationOptions Options { get; }
			SerializationOptions ISerializer.Options { get => this.Options; }
			#endregion

			#region 反序列化
			public object Deserialize(Stream stream, SerializationOptions options = null) => throw new NotSupportedException();
			public object Deserialize(Stream stream, Type type, SerializationOptions options = null)
			{
				using(var reader = new StreamReader(stream, Encoding.UTF8))
				{
					return JsonSerializer.Deserialize(reader.ReadToEnd(), type, GetOptions(options));
				}
			}

			public object Deserialize(ReadOnlySpan<byte> buffer, SerializationOptions options = null) => throw new NotSupportedException();
			public object Deserialize(ReadOnlySpan<byte> buffer, Type type, SerializationOptions options = null) => JsonSerializer.Deserialize(buffer, type, GetOptions(options));

			public T Deserialize<T>(Stream stream, SerializationOptions options = null)
			{
				using(var reader = new StreamReader(stream, Encoding.UTF8))
				{
					return JsonSerializer.Deserialize<T>(reader.ReadToEnd(), GetOptions(options));
				}
			}
			public T Deserialize<T>(ReadOnlySpan<byte> buffer, SerializationOptions options = null) => JsonSerializer.Deserialize<T>(buffer, GetOptions(options));

			public ValueTask<object> DeserializeAsync(Stream stream, SerializationOptions options = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
			public ValueTask<object> DeserializeAsync(Stream stream, Type type, SerializationOptions options = null, CancellationToken cancellationToken = default) =>
				JsonSerializer.DeserializeAsync(stream, type, GetOptions(options), cancellationToken);
			public ValueTask<T> DeserializeAsync<T>(Stream stream, SerializationOptions options = null, CancellationToken cancellationToken = default) =>
				JsonSerializer.DeserializeAsync<T>(stream, GetOptions(options), cancellationToken);

			public object Deserialize(string text, TextSerializationOptions options = null) => throw new NotImplementedException();
			public object Deserialize(string text, Type type, TextSerializationOptions options = null) => JsonSerializer.Deserialize(text, type, GetOptions(options));
			public T Deserialize<T>(string text, TextSerializationOptions options = null) => JsonSerializer.Deserialize<T>(text, GetOptions(options));
			public ValueTask<object> DeserializeAsync(string text, TextSerializationOptions options = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

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
				if(graph == null)
					return;

				using(Utf8JsonWriter writer = new Utf8JsonWriter(stream))
				{
					JsonSerializer.Serialize(writer, graph, type ?? graph.GetType(), GetOptions(options));
				}
			}

			public string Serialize(object graph, TextSerializationOptions options = null) => graph == null ? null : JsonSerializer.Serialize(graph, GetOptions(options));

			public Task SerializeAsync(Stream stream, object graph, Type type = null, SerializationOptions options = null, CancellationToken cancellationToken = default)
			{
				if(graph == null)
					return Task.CompletedTask;

				if(stream == null)
					throw new ArgumentNullException(nameof(stream));

				return JsonSerializer.SerializeAsync(stream, graph, type, GetOptions(options), cancellationToken);
			}

			public async Task<string> SerializeAsync(object graph, TextSerializationOptions options = null, CancellationToken cancellationToken = default)
			{
				if(graph == null)
					return null;

				using(var stream = new MemoryStream())
				{
					await JsonSerializer.SerializeAsync(stream, graph, GetOptions(options), cancellationToken);

					if(stream.CanSeek)
						stream.Position = 0;

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
					Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
					PropertyNameCaseInsensitive = true,
					MaxDepth = options.MaximumDepth,
					NumberHandling = JsonNumberHandling.AllowReadingFromString,
					DefaultIgnoreCondition = ignores,
					IgnoreReadOnlyProperties = false,
					IncludeFields = options.IncludeFields,
					Converters =
					{
						new JsonTimeSpanConverter(),
						new JsonStringEnumConverter(),
						new ModelConverterFactory(),
						new RangeConverterFactory(),
						new ComplexConverterFactory(),
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
					Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
					PropertyNameCaseInsensitive = true,
					MaxDepth = options.MaximumDepth,
					WriteIndented = options.Indented,
					NumberHandling = JsonNumberHandling.AllowReadingFromString,
					DefaultIgnoreCondition = ignores,
					IgnoreReadOnlyProperties = false,
					PropertyNamingPolicy = naming,
					DictionaryKeyPolicy = naming,
					IncludeFields = options.IncludeFields,
					Converters =
					{
						new JsonTimeSpanConverter(),
						new JsonStringEnumConverter(naming),
						new ModelConverterFactory(),
						new RangeConverterFactory(),
						new ComplexConverterFactory(),
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
						case JsonTokenType.Number:
							return (T)Data.Range.Create(type, GetValue(ref reader, type.GenericTypeArguments[0]));
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

		private class ComplexConverterFactory : JsonConverterFactory
		{
			public override bool CanConvert(Type type)
			{
				return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Data.Complex<>);
			}

			public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
			{
				var converterType = typeof(ComplexConverter<>).MakeGenericType(type.GenericTypeArguments);
				return (JsonConverter)Activator.CreateInstance(converterType);
			}

			private class ComplexConverter<T> : JsonConverter<Data.Complex<T>> where T : struct, IEquatable<T>, IComparable<T>
			{
				private static readonly Common.StringExtension.TryParser<T> _parser = Type.GetTypeCode(typeof(T)) switch
				{
					TypeCode.Byte => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<byte>)byte.TryParse,
					TypeCode.SByte => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<sbyte>)sbyte.TryParse,
					TypeCode.Int16 => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<short>)short.TryParse,
					TypeCode.UInt16 => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<ushort>)ushort.TryParse,
					TypeCode.Int32 => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<int>)int.TryParse,
					TypeCode.UInt32 => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<uint>)uint.TryParse,
					TypeCode.Int64 => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<long>)long.TryParse,
					TypeCode.UInt64 => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<ulong>)ulong.TryParse,
					TypeCode.Single => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<float>)float.TryParse,
					TypeCode.Double => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<double>)double.TryParse,
					TypeCode.Decimal => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<decimal>)decimal.TryParse,
					TypeCode.Boolean => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<bool>)bool.TryParse,
					TypeCode.DateTime => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<DateTime>)DateTime.TryParse,
					_ => null,
				};

				public override Data.Complex<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
				{
					if(reader.TokenType == JsonTokenType.Number)
						return new Data.Complex<T>(reader.GetValue<T>());

					if(reader.TokenType == JsonTokenType.String)
					{
						var parts = reader.GetString()?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

						if(parts == null || parts.Length == 0)
							return default;

						if(parts.Length == 1)
						{
							if(_parser(parts[0], out var value))
								return new Data.Complex<T>(value);

							if(Data.Range.TryParse<T>(parts[0], out var range))
								return new Data.Complex<T>(range);

							throw new JsonException();
						}

						var array = new T[parts.Length];

						for(int i = 0; i < parts.Length; i++)
						{
							if(_parser(parts[i], out var value))
								array[i] = value;
							else
								throw new JsonException();
						}

						return new Data.Complex<T>(array);
					}

					if(reader.TokenType == JsonTokenType.StartArray)
					{
						var list = new List<T>();

						while(reader.Read())
						{
							if(reader.TokenType == JsonTokenType.EndArray)
								break;

							if(reader.TokenType == JsonTokenType.Number)
								list.Add(reader.GetValue<T>());
							else
								throw new JsonException();
						}

						return new Data.Complex<T>(list.ToArray());
					}

					throw new JsonException();
				}

				public override void Write(Utf8JsonWriter writer, Data.Complex<T> value, JsonSerializerOptions options)
				{
					writer.WriteStringValue(value.ToString());
				}
			}
		}

		private class ModelConverterFactory : JsonConverterFactory
		{
			public override bool CanConvert(Type type) => (type.IsInterface || type.IsAbstract) && !Common.TypeExtension.IsEnumerable(type);
			public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) =>
				(JsonConverter)Activator.CreateInstance(typeof(ModelConverter<>).MakeGenericType(type));

			private class ModelConverter<T> : JsonConverter<T> where T : class
			{
				private static readonly ConcurrentDictionary<Type, Type> _mapping_ = new ConcurrentDictionary<Type, Type>();

				public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
				{
					var actualType = _mapping_.GetOrAdd(type, key => Data.Model.Build<T>().GetType());
					return (T)JsonSerializer.Deserialize(ref reader, actualType, options);
				}

				public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
				{
					if(value == null)
						writer.WriteNullValue();
					else
						JsonSerializer.Serialize(writer, value, typeof(object), options);
				}
			}
		}
		#endregion
	}
}
