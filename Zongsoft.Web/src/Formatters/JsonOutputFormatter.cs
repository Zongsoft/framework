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
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Zongsoft.Web.Formatters
{
	public class JsonOutputFormatter : TextOutputFormatter
	{
		#region 成员字段
		private JsonSerializerOptions _options;
		#endregion

		#region 构造函数
		public JsonOutputFormatter()
		{
			_options = new JsonSerializerOptions()
			{
				PropertyNameCaseInsensitive = true,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
			};

			SupportedEncodings.Add(Encoding.UTF8);
			SupportedEncodings.Add(Encoding.Unicode);

			SupportedMediaTypes.Add("application/json");
			SupportedMediaTypes.Add("text/json");
			SupportedMediaTypes.Add("application/*+json");
		}
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
				await JsonSerializer.SerializeAsync(writeStream, context.Object, objectType, options ?? _options);

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
		private Stream GetWriteStream(HttpContext context, Encoding encoding, out JsonSerializerOptions options)
		{
			options = null;

			if(context.Request.Headers.TryGetValue("x-json-behaviors", out var header))
			{
				ParseBehaviors(header.ToString());
			}

			if(encoding.CodePage == Encoding.UTF8.CodePage)
				return context.Response.Body;

			return new TranscodingWriteStream(context.Response.Body, encoding);
		}

		private static void ParseBehaviors(string text)
		{
			var parts = Common.StringExtension.Slice(text.ToString(), ';');
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

			public override async Task FlushAsync(CancellationToken cancellationToken)
			{
				await _stream.FlushAsync(cancellationToken);
			}

			public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

			public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

			public override void SetLength(long value) => throw new NotSupportedException();

			public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

			public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			{
				if(count <= 0)
					throw new ArgumentOutOfRangeException(nameof(count));
				if(offset < 0 || offset >= buffer.Length)
					throw new ArgumentOutOfRangeException(nameof(offset));
				if(buffer.Length - offset < count)
					throw new ArgumentOutOfRangeException(nameof(count));

				var bufferSegment = new ArraySegment<byte>(buffer, offset, count);
				return WriteAsync(bufferSegment, cancellationToken);
			}

			private async Task WriteAsync( ArraySegment<byte> bufferSegment, CancellationToken cancellationToken)
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
						await WriteBufferAsync(cancellationToken);
					}
				}
			}

			private async Task WriteBufferAsync(CancellationToken cancellationToken)
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

					await _stream.WriteAsync(byteBuffer.AsMemory(0, bytesUsed), cancellationToken);
					charsWritten += charsEncoded;
				}

				ArrayPool<byte>.Shared.Return(byteBuffer);
				_charsDecoded = 0;
			}

			public async Task FinalWriteAsync(CancellationToken cancellationToken)
			{
				await WriteBufferAsync(cancellationToken);
				var byteBuffer = ArrayPool<byte>.Shared.Rent(_maxByteBufferSize);
				var encoderCompleted = false;

				while(!encoderCompleted)
				{
					_encoder.Convert(
						Array.Empty<char>(),
						byteBuffer,
						flush: true,
						out _,
						out var bytesUsed,
						out encoderCompleted);

					await _stream.WriteAsync(byteBuffer.AsMemory(0, bytesUsed), cancellationToken);
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
}
