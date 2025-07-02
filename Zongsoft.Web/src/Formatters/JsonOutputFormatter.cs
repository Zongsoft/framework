﻿/*
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
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Linq;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Zongsoft.Web.Formatters;

public class JsonOutputFormatter : TextOutputFormatter
{
	#region 成员字段
	private readonly Serialization.TextSerializationOptions _options;
	#endregion

	#region 构造函数
	public JsonOutputFormatter(Serialization.TextSerializationOptions options = null)
	{
		_options = options ?? Serialization.TextSerializationOptions.Default;

		this.SupportedEncodings.Add(Encoding.UTF8);
		this.SupportedEncodings.Add(Encoding.Unicode);
		this.SupportedMediaTypes.Add("application/json");
		this.SupportedMediaTypes.Add("text/json");
		this.SupportedMediaTypes.Add("application/*+json");
	}
	#endregion

	#region 公共属性
	public Serialization.TextSerializationOptions Options => _options;
	#endregion

	#region 重写方法
	public sealed override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
	{
		if(context == null)
			throw new ArgumentNullException(nameof(context));

		if(selectedEncoding == null)
			throw new ArgumentNullException(nameof(selectedEncoding));

		var httpContext = context.HttpContext;
		var writeStream = GetWriteStream(httpContext, selectedEncoding, out var options);

		try
		{
			var objectType = context.Object?.GetType() ?? context.ObjectType;
			await Serialization.Serializer.Json.SerializeAsync(writeStream, context.Object, objectType, options ?? _options);

			if(writeStream is TranscodingWriteStream transcodingStream)
			{
				await transcodingStream.FinalWriteAsync(CancellationToken.None);
			}

			await writeStream.FlushAsync();
		}
		finally
		{
			if(writeStream is TranscodingWriteStream transcodingStream)
			{
				await transcodingStream.DisposeAsync();
			}
		}
	}
	#endregion

	#region 私有方法
	private Stream GetWriteStream(HttpContext context, Encoding encoding, out Serialization.TextSerializationOptions options)
	{
		options = _options;

		if(context.Request.Headers.TryGetValue(Http.Headers.JsonBehaviors, out var behaviors))
			options = GetSerializationOptions(behaviors, _options);

		if(encoding.CodePage == Encoding.UTF8.CodePage)
			return context.Response.Body;

		return new TranscodingWriteStream(context.Response.Body, encoding);
	}

	private static Serialization.TextSerializationOptions GetSerializationOptions(string behaviors, Serialization.TextSerializationOptions defaultOptions)
	{
		if(string.IsNullOrEmpty(behaviors))
			return null;

		var parts = Common.StringExtension.Slice(behaviors, ';');
		var options = new Serialization.TextSerializationOptions()
		{
			Indented = parts.Contains("Indented", StringComparer.OrdinalIgnoreCase) || defaultOptions.Indented,
			IgnoreNull = defaultOptions.IgnoreNull,
			IgnoreZero = defaultOptions.IgnoreZero,
			IgnoreEmpty = defaultOptions.IgnoreEmpty,
			IncludeFields = defaultOptions.IncludeFields,
			MaximumDepth = defaultOptions.MaximumDepth,
			NamingConvention = defaultOptions.NamingConvention,
			Typified = defaultOptions.Typified,
		};

		foreach(var part in parts)
		{
			var index = part.IndexOf(':');

			if(index > 0 && index < part.Length - 1)
				SetOption(options, part[..index].Trim(), part[(index + 1)..].Trim());
		}

		return options;
	}

	private static void SetOption(Serialization.TextSerializationOptions options, string key, string value)
	{
		if(string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
			return;

		switch(key.ToLowerInvariant())
		{
			case "ignores":
				options.Ignores(value);
				break;
			case "casing":
				if(string.Equals(value, "camel", StringComparison.OrdinalIgnoreCase))
					options.NamingConvention = Serialization.SerializationNamingConvention.Camel;
				else if(string.Equals(value, "pascal", StringComparison.OrdinalIgnoreCase))
					options.NamingConvention = Serialization.SerializationNamingConvention.Pascal;
				else
					options.NamingConvention = Serialization.SerializationNamingConvention.None;

				break;
		}
	}
	#endregion

	#region 嵌套子类
	internal sealed class TranscodingWriteStream : Stream
	{
		internal const int MaxCharBufferSize = 4096;
		internal const int MaxByteBufferSize = 4 * MaxCharBufferSize;
		private readonly int _maxByteBufferSize;

		private readonly Stream _stream;
		private readonly Decoder _decoder;
		private readonly Encoder _encoder;
		private readonly char[] _charBuffer;
		private int _charsDecoded;
		private bool _disposed;

		public TranscodingWriteStream(Stream stream, Encoding targetEncoding)
		{
			_stream = stream;
			_charBuffer = ArrayPool<char>.Shared.Rent(MaxCharBufferSize);
			_maxByteBufferSize = Math.Min(MaxByteBufferSize, targetEncoding.GetMaxByteCount(MaxCharBufferSize));
			_decoder = Encoding.UTF8.GetDecoder();
			_encoder = targetEncoding.GetEncoder();
		}

		public override bool CanRead => false;
		public override bool CanSeek => false;
		public override bool CanWrite => true;
		public override long Length => throw new NotSupportedException();
		public override long Position { get; set; }

		public override void Flush() => throw new NotSupportedException();
		public override Task FlushAsync(CancellationToken cancellation) => _stream.FlushAsync(cancellation);
		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellation)
		{
			if(count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			if(offset < 0 || offset >= buffer.Length)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if(buffer.Length - offset < count)
				throw new ArgumentOutOfRangeException(nameof(count));

			var bufferSegment = new ArraySegment<byte>(buffer, offset, count);
			return WriteAsync(bufferSegment, cancellation);
		}

		private async Task WriteAsync( ArraySegment<byte> bufferSegment, CancellationToken cancellation)
		{
			var decoderCompleted = false;
			while(!decoderCompleted)
			{
				_decoder.Convert(
					bufferSegment,
					_charBuffer.AsSpan(_charsDecoded),
					flush: false,
					out var bytesDecoded,
					out var charsDecoded,
					out decoderCompleted);

				_charsDecoded += charsDecoded;
				bufferSegment = bufferSegment.Slice(bytesDecoded);

				if(!decoderCompleted)
				{
					await WriteBufferAsync(cancellation);
				}
			}
		}

		private async Task WriteBufferAsync(CancellationToken cancellation)
		{
			var encoderCompleted = false;
			var charsWritten = 0;
			var byteBuffer = ArrayPool<byte>.Shared.Rent(_maxByteBufferSize);

			while(!encoderCompleted && charsWritten < _charsDecoded)
			{
				_encoder.Convert(
					_charBuffer.AsSpan(charsWritten, _charsDecoded - charsWritten),
					byteBuffer,
					flush: false,
					out var charsEncoded,
					out var bytesUsed,
					out encoderCompleted);

				await _stream.WriteAsync(byteBuffer.AsMemory(0, bytesUsed), cancellation);
				charsWritten += charsEncoded;
			}

			ArrayPool<byte>.Shared.Return(byteBuffer);
			_charsDecoded = 0;
		}

		public async Task FinalWriteAsync(CancellationToken cancellation)
		{
			await WriteBufferAsync(cancellation);
			var byteBuffer = ArrayPool<byte>.Shared.Rent(_maxByteBufferSize);
			var encoderCompleted = false;

			while(!encoderCompleted)
			{
				_encoder.Convert(
					[],
					byteBuffer,
					flush: true,
					out _,
					out var bytesUsed,
					out encoderCompleted);

				await _stream.WriteAsync(byteBuffer.AsMemory(0, bytesUsed), cancellation);
			}

			ArrayPool<byte>.Shared.Return(byteBuffer);
		}

		protected override void Dispose(bool disposing)
		{
			if(!_disposed)
			{
				_disposed = true;
				ArrayPool<char>.Shared.Return(_charBuffer);
			}
		}
	}
	#endregion
}
