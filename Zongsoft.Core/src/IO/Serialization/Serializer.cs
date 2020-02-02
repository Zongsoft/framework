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
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

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
			get => _json ?? JsonSerializerEx.Default;
			set => _json = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 嵌套子类
		private class JsonSerializerEx : ITextSerializer
		{
			#region 单例字段
			public static readonly JsonSerializerEx Default = new JsonSerializerEx();
			#endregion

			#region 默认配置
			private static readonly JsonSerializerOptions DefaultOptions;
			#endregion

			#region 静态构造
			static JsonSerializerEx()
			{
				DefaultOptions = new JsonSerializerOptions()
				{
					PropertyNameCaseInsensitive = true,
					IgnoreNullValues = false,
				};

				DefaultOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
			}
			#endregion

			#region 构造函数
			public JsonSerializerEx()
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

				var options = new JsonSerializerOptions()
				{
					PropertyNameCaseInsensitive = true,
					MaxDepth = settings.MaximumDepth,
					IgnoreNullValues = settings.IgnoreNull,
				};

				options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

				return options;
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

				var options = new JsonSerializerOptions()
				{
					PropertyNameCaseInsensitive = true,
					MaxDepth = settings.MaximumDepth,
					WriteIndented = settings.Indented,
					IgnoreNullValues = settings.IgnoreNull,
					PropertyNamingPolicy = naming,
					DictionaryKeyPolicy = naming,
				};

				options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(naming));

				return options;
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
		#endregion
	}
}
