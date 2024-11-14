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

namespace Zongsoft.Serialization
{
	public static partial class Serializer
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
			private static readonly JsonSerializerOptions DefaultOptions = SerializerExtension.GetOptions();
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
			SerializationOptions ISerializer.Options => this.Options;
			#endregion

			#region 反序列化
			public object Deserialize(Stream stream, SerializationOptions options = null) => throw new NotSupportedException();
			public object Deserialize(Stream stream, Type type, SerializationOptions options = null)
			{
				using(var reader = new StreamReader(stream, Encoding.UTF8))
				{
					return JsonSerializer.Deserialize(reader.ReadToEnd(), type, options.ToOptions());
				}
			}
			public T Deserialize<T>(Stream stream, SerializationOptions options = null)
			{
				using(var reader = new StreamReader(stream, Encoding.UTF8))
				{
					return JsonSerializer.Deserialize<T>(reader.ReadToEnd(), options.ToOptions());
				}
			}

			public object Deserialize(ReadOnlySpan<byte> buffer, SerializationOptions options = null) => throw new NotSupportedException();
			public object Deserialize(ReadOnlySpan<byte> buffer, Type type, SerializationOptions options = null) => JsonSerializer.Deserialize(buffer, type, options.ToOptions());
			public T Deserialize<T>(ReadOnlySpan<byte> buffer, SerializationOptions options = null) => JsonSerializer.Deserialize<T>(buffer, options.ToOptions());

			public ValueTask<object> DeserializeAsync(Stream stream, SerializationOptions options = null, CancellationToken cancellation = default) => throw new NotImplementedException();
			public ValueTask<object> DeserializeAsync(Stream stream, Type type, SerializationOptions options = null, CancellationToken cancellation = default) =>
				JsonSerializer.DeserializeAsync(stream, type, options.ToOptions(), cancellation);
			public ValueTask<T> DeserializeAsync<T>(Stream stream, SerializationOptions options = null, CancellationToken cancellation = default) =>
				JsonSerializer.DeserializeAsync<T>(stream, options.ToOptions(), cancellation);

			public ValueTask<object> DeserializeAsync(ReadOnlySpan<byte> buffer, SerializationOptions options = null, CancellationToken cancellation = default) => throw new NotImplementedException();
			public ValueTask<object> DeserializeAsync(ReadOnlySpan<byte> buffer, Type type, SerializationOptions options = null, CancellationToken cancellation = default) =>
				JsonSerializer.DeserializeAsync(new MemoryStream(buffer.ToArray()), type, options.ToOptions(), cancellation);
			public ValueTask<T> DeserializeAsync<T>(ReadOnlySpan<byte> buffer, SerializationOptions options = null, CancellationToken cancellation = default) =>
				JsonSerializer.DeserializeAsync<T>(new MemoryStream(buffer.ToArray()), options.ToOptions(), cancellation);

			public object Deserialize(string text, TextSerializationOptions options = null) => throw new NotImplementedException();
			public object Deserialize(string text, Type type, TextSerializationOptions options = null) => JsonSerializer.Deserialize(text, type, options.ToOptions());
			public T Deserialize<T>(string text, TextSerializationOptions options = null) => JsonSerializer.Deserialize<T>(text, options.ToOptions());
			public ValueTask<object> DeserializeAsync(string text, TextSerializationOptions options = null, CancellationToken cancellation = default) => throw new NotImplementedException();

			public ValueTask<object> DeserializeAsync(string text, Type type, TextSerializationOptions options = null, CancellationToken cancellation = default)
			{
				using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
				{
					return JsonSerializer.DeserializeAsync(stream, type, options.ToOptions(), cancellation);
				}
			}

			public ValueTask<T> DeserializeAsync<T>(string text, TextSerializationOptions options = null, CancellationToken cancellation = default)
			{
				using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
				{
					return JsonSerializer.DeserializeAsync<T>(stream, options.ToOptions(), cancellation);
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
					JsonSerializer.Serialize(writer, graph, type ?? graph.GetType(), options.ToOptions());
				}
			}

			public string Serialize(object graph, TextSerializationOptions options = null) => graph == null ? null : JsonSerializer.Serialize(graph, options.ToOptions());

			public ValueTask SerializeAsync(Stream stream, object graph, Type type = null, SerializationOptions options = null, CancellationToken cancellation = default)
			{
				if(graph == null)
					return ValueTask.CompletedTask;

				if(stream == null)
					throw new ArgumentNullException(nameof(stream));

				return new ValueTask(JsonSerializer.SerializeAsync(stream, graph, type, options.ToOptions(), cancellation));
			}

			public async ValueTask<string> SerializeAsync(object graph, TextSerializationOptions options = null, CancellationToken cancellation = default)
			{
				if(graph == null)
					return null;

				using(var stream = new MemoryStream())
				{
					await JsonSerializer.SerializeAsync(stream, graph, options.ToOptions(), cancellation);

					if(stream.CanSeek)
						stream.Position = 0;

					using(var reader = new StreamReader(stream, Encoding.UTF8, false))
					{
						return await reader.ReadToEndAsync();
					}
				}
			}
			#endregion
		}
		#endregion
	}
}
