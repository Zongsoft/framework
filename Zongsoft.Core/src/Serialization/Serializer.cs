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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
			public static readonly JsonSerializerWrapper Instance = new();
			#endregion

			#region 私有构造
			private JsonSerializerWrapper() => this.Options = new TextSerializationOptions()
			{
				IncludeFields = true,
			};
			#endregion

			#region 公共属性
			public TextSerializationOptions Options { get; }
			SerializationOptions ISerializer.Options => this.Options;
			#endregion

			#region 反序列化
			public object Deserialize(Stream stream, SerializationOptions options = null) => this.Deserialize<Dictionary<string, object>>(stream, options);
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

			public object Deserialize(ReadOnlySpan<byte> buffer, SerializationOptions options = null) => this.Deserialize<Dictionary<string, object>>(buffer, options);
			public object Deserialize(ReadOnlySpan<byte> buffer, Type type, SerializationOptions options = null) => JsonSerializer.Deserialize(buffer, type, options.ToOptions());
			public T Deserialize<T>(ReadOnlySpan<byte> buffer, SerializationOptions options = null) => JsonSerializer.Deserialize<T>(buffer, options.ToOptions());

			public ValueTask<object> DeserializeAsync(Stream stream, CancellationToken cancellation = default) => this.DeserializeAsync(stream, typeof(Dictionary<string, object>), null, cancellation);
			public ValueTask<object> DeserializeAsync(Stream stream, SerializationOptions options, CancellationToken cancellation = default) => this.DeserializeAsync(stream, typeof(Dictionary<string, object>), options, cancellation);
			public ValueTask<object> DeserializeAsync(Stream stream, Type type, CancellationToken cancellation = default) => JsonSerializer.DeserializeAsync(stream, type, TextSerializationOptions.Default.JsonOptions, cancellation);
			public ValueTask<object> DeserializeAsync(Stream stream, Type type, SerializationOptions options, CancellationToken cancellation = default) => JsonSerializer.DeserializeAsync(stream, type, options.ToOptions(), cancellation);
			public ValueTask<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellation = default) => JsonSerializer.DeserializeAsync<T>(stream, TextSerializationOptions.Default.JsonOptions, cancellation);
			public ValueTask<T> DeserializeAsync<T>(Stream stream, SerializationOptions options, CancellationToken cancellation = default) => JsonSerializer.DeserializeAsync<T>(stream, options.ToOptions(), cancellation);

			public ValueTask<object> DeserializeAsync(ReadOnlySpan<byte> buffer, CancellationToken cancellation = default) => this.DeserializeAsync(buffer, typeof(Dictionary<string, object>), null, cancellation);
			public ValueTask<object> DeserializeAsync(ReadOnlySpan<byte> buffer, SerializationOptions options, CancellationToken cancellation = default) => this.DeserializeAsync(buffer, typeof(Dictionary<string, object>), options, cancellation);
			public ValueTask<object> DeserializeAsync(ReadOnlySpan<byte> buffer, Type type, CancellationToken cancellation = default) => this.DeserializeAsync(buffer, type, null, cancellation);
			public ValueTask<object> DeserializeAsync(ReadOnlySpan<byte> buffer, Type type, SerializationOptions options, CancellationToken cancellation = default) => JsonSerializer.DeserializeAsync(new MemoryStream(buffer.ToArray()), type, options.ToOptions(), cancellation);
			public ValueTask<T> DeserializeAsync<T>(ReadOnlySpan<byte> buffer, CancellationToken cancellation = default) => this.DeserializeAsync<T>(buffer, null, cancellation);
			public ValueTask<T> DeserializeAsync<T>(ReadOnlySpan<byte> buffer, SerializationOptions options, CancellationToken cancellation = default) => JsonSerializer.DeserializeAsync<T>(new MemoryStream(buffer.ToArray()), options.ToOptions(), cancellation);

			public object Deserialize(string text, TextSerializationOptions options = null) => this.Deserialize(text, typeof(Dictionary<string, object>), options);
			public object Deserialize(string text, Type type, TextSerializationOptions options = null) => JsonSerializer.Deserialize(text, type, options.ToOptions());
			public T Deserialize<T>(string text, TextSerializationOptions options = null) => JsonSerializer.Deserialize<T>(text, options.ToOptions());

			public ValueTask<object> DeserializeAsync(string text, CancellationToken cancellation = default) => this.DeserializeAsync(text, typeof(Dictionary<string, object>), null, cancellation);
			public ValueTask<object> DeserializeAsync(string text, TextSerializationOptions options, CancellationToken cancellation = default) => this.DeserializeAsync(text, typeof(Dictionary<string, object>), options, cancellation);
			public ValueTask<object> DeserializeAsync(string text, Type type, CancellationToken cancellation = default) => this.DeserializeAsync(text, type, null, cancellation);
			public ValueTask<object> DeserializeAsync(string text, Type type, TextSerializationOptions options, CancellationToken cancellation = default)
			{
				using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
				{
					return JsonSerializer.DeserializeAsync(stream, type, options.ToOptions(), cancellation);
				}
			}
			public ValueTask<T> DeserializeAsync<T>(string text, CancellationToken cancellation = default) => this.DeserializeAsync<T>(text, null, cancellation);
			public ValueTask<T> DeserializeAsync<T>(string text, TextSerializationOptions options, CancellationToken cancellation = default)
			{
				using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
				{
					return JsonSerializer.DeserializeAsync<T>(stream, options.ToOptions(), cancellation);
				}
			}
			#endregion

			#region 序列方法
			public void Serialize(Stream stream, object graph, SerializationOptions options = null) => this.Serialize(stream, graph, null, options);
			public void Serialize(Stream stream, object graph, Type type, SerializationOptions options = null)
			{
				if(graph == null)
					return;

				using Utf8JsonWriter writer = new Utf8JsonWriter(stream);
				JsonSerializer.Serialize(writer, graph, GetSerializeType(type ?? graph.GetType()), options.ToOptions());
			}

			public byte[] Serialize(object graph, SerializationOptions options = null) => this.Serialize(graph, null, options);
			public byte[] Serialize(object graph, Type type, SerializationOptions options = null) => graph == null ? null : JsonSerializer.SerializeToUtf8Bytes(graph, GetSerializeType(type ?? graph.GetType()), options.ToOptions());

			public string Serialize(object graph, TextSerializationOptions options = null) => this.Serialize(graph, null, options);
			public string Serialize(object graph, Type type, TextSerializationOptions options = null) => graph == null ? null : JsonSerializer.Serialize(graph, GetSerializeType(type ?? graph.GetType()), options.ToOptions());

			public ValueTask SerializeAsync(Stream stream, object graph, CancellationToken cancellation = default) => this.SerializeAsync(stream, graph, null, null, cancellation);
			public ValueTask SerializeAsync(Stream stream, object graph, SerializationOptions options, CancellationToken cancellation = default) => this.SerializeAsync(stream, graph, null, options, cancellation);
			public ValueTask SerializeAsync(Stream stream, object graph, Type type, CancellationToken cancellation = default) => this.SerializeAsync(stream, graph, type, null, cancellation);
			public ValueTask SerializeAsync(Stream stream, object graph, Type type, SerializationOptions options, CancellationToken cancellation = default)
			{
				if(graph == null)
					return ValueTask.CompletedTask;

				if(stream == null)
					throw new ArgumentNullException(nameof(stream));

				return new ValueTask(JsonSerializer.SerializeAsync(stream, graph, GetSerializeType(type ?? graph.GetType()), options.ToOptions(), cancellation));
			}

			public ValueTask<string> SerializeAsync(object graph, CancellationToken cancellation = default) => this.SerializeAsync(graph, null, null, cancellation);
			public ValueTask<string> SerializeAsync(object graph, TextSerializationOptions options, CancellationToken cancellation = default) => this.SerializeAsync(graph, null, options, cancellation);
			public ValueTask<string> SerializeAsync(object graph, Type type, CancellationToken cancellation = default) => this.SerializeAsync(graph, type, null, cancellation);
			public async ValueTask<string> SerializeAsync(object graph, Type type, TextSerializationOptions options, CancellationToken cancellation = default)
			{
				if(graph == null)
					return null;

				using(var stream = new MemoryStream())
				{
					await JsonSerializer.SerializeAsync(stream, graph, GetSerializeType(type ?? graph.GetType()), options.ToOptions(), cancellation);

					if(stream.CanSeek)
						stream.Position = 0;

					using(var reader = new StreamReader(stream, Encoding.UTF8, false))
					{
						return await reader.ReadToEndAsync();
					}
				}
			}

			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private static Type GetSerializeType(Type type) => Data.Model.GetModelType(type);
			#endregion
		}
		#endregion
	}
}
