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
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Zongsoft.Web.Formatters
{
	public class JsonInputFormatter : TextInputFormatter, IInputFormatterExceptionPolicy
	{
		#region 成员字段
		private readonly Serialization.TextSerializationOptions _options;
		#endregion

		#region 构造函数
		public JsonInputFormatter(Serialization.TextSerializationOptions options = null)
		{
			_options = options ?? Serialization.Serializer.Json.Options;

			SupportedEncodings.Add(UTF8EncodingWithoutBOM);
			SupportedEncodings.Add(UTF16EncodingLittleEndian);
			SupportedMediaTypes.Add("application/json");
			SupportedMediaTypes.Add("text/jsona");
			SupportedMediaTypes.Add("application/*+json");
		}
		#endregion

		#region 公共属性
		public Serialization.TextSerializationOptions Options => _options;
		InputFormatterExceptionPolicy IInputFormatterExceptionPolicy.ExceptionPolicy => InputFormatterExceptionPolicy.MalformedInputExceptions;
		#endregion

		#region 重写方法
		public sealed override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
		{
			if(context == null)
				throw new ArgumentNullException(nameof(context));

			if(encoding == null)
				throw new ArgumentNullException(nameof(encoding));

			var httpContext = context.HttpContext;
			var inputStream = GetInputStream(httpContext, encoding);

			object model;

			try
			{
				model = await Serialization.Serializer.Json.DeserializeAsync(inputStream, context.ModelType, _options);
			}
			catch(JsonException exception)
			{
				var path = exception.Path;
				var formatterException = new InputFormatterException(exception.Message, exception);

				context.ModelState.TryAddModelError(path, formatterException, context.Metadata);
				return InputFormatterResult.Failure();
			}
			catch(Exception exception) when(exception is FormatException || exception is OverflowException)
			{
				context.ModelState.TryAddModelError(string.Empty, exception, context.Metadata);
				return InputFormatterResult.Failure();
			}
			finally
			{
				if(inputStream is TranscodingReadStream transcoding)
				{
					await transcoding.DisposeAsync();
				}
			}

			if(model == null && !context.TreatEmptyInputAsDefaultValue)
				return InputFormatterResult.NoValue();
			else
				return InputFormatterResult.Success(model);
		}
		#endregion

		#region 私有方法
		private Stream GetInputStream(HttpContext httpContext, Encoding encoding)
		{
			if(encoding.CodePage == Encoding.UTF8.CodePage)
				return httpContext.Request.Body;

			return new TranscodingReadStream(httpContext.Request.Body, encoding);
		}
		#endregion

		#region 嵌套子类
		internal sealed class TranscodingReadStream : Stream
		{
			private static readonly int OverflowBufferSize = Encoding.UTF8.GetMaxByteCount(1); // The most number of bytes used to represent a single UTF char

			internal const int MaxByteBufferSize = 4096;
			internal const int MaxCharBufferSize = 3 * MaxByteBufferSize;

			private readonly Stream _stream;
			private readonly Decoder _decoder;

			private ArraySegment<byte> _byteBuffer;
			private ArraySegment<char> _charBuffer;
			private ArraySegment<byte> _overflowBuffer;
			private bool _disposed;

			public TranscodingReadStream(Stream input, Encoding sourceEncoding)
			{
				_stream = input;

				_byteBuffer = new ArraySegment<byte>(
					ArrayPool<byte>.Shared.Rent(MaxByteBufferSize),
					0,
					count: 0);

				var maxCharBufferSize = Math.Min(MaxCharBufferSize, sourceEncoding.GetMaxCharCount(MaxByteBufferSize));
				_charBuffer = new ArraySegment<char>(
					ArrayPool<char>.Shared.Rent(maxCharBufferSize),
					0,
					count: 0);

				_overflowBuffer = new ArraySegment<byte>(
					ArrayPool<byte>.Shared.Rent(OverflowBufferSize),
					0,
					count: 0);

				_decoder = sourceEncoding.GetDecoder();
			}

			public override bool CanRead => true;
			public override bool CanSeek => false;
			public override bool CanWrite => false;
			public override long Length => throw new NotSupportedException();
			public override long Position
			{
				get => throw new NotSupportedException();
				set => throw new NotSupportedException();
			}

			internal int ByteBufferCount => _byteBuffer.Count;
			internal int CharBufferCount => _charBuffer.Count;
			internal int OverflowCount => _overflowBuffer.Count;

			public override void Flush() => throw new NotSupportedException();
			public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

			public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellation)
			{
				if(count < 0)
					throw new ArgumentOutOfRangeException(nameof(count));
				if(offset < 0 || offset >= buffer.Length)
					throw new ArgumentOutOfRangeException(nameof(offset));
				if(buffer.Length - offset < count)
					throw new ArgumentOutOfRangeException(nameof(count));

				if(count == 0)
					return 0;

				var readBuffer = new ArraySegment<byte>(buffer, offset, count);

				if(_overflowBuffer.Count > 0)
				{
					var bytesToCopy = Math.Min(count, _overflowBuffer.Count);
					_overflowBuffer.Slice(0, bytesToCopy).CopyTo(readBuffer);
					_overflowBuffer = _overflowBuffer.Slice(bytesToCopy);
					return bytesToCopy;
				}

				if(_charBuffer.Count == 0)
				{
					await ReadInputChars(cancellation);
				}

				var operationStatus = Utf8.FromUtf16(_charBuffer, readBuffer, out var charsRead, out var bytesWritten, isFinalBlock: false);
				_charBuffer = _charBuffer.Slice(charsRead);

				switch(operationStatus)
				{
					case OperationStatus.Done:
						return bytesWritten;
					case OperationStatus.DestinationTooSmall:
						if(bytesWritten != 0)
							return bytesWritten;

						Utf8.FromUtf16(_charBuffer, _overflowBuffer.Array, out var overFlowChars, out var overflowBytes, isFinalBlock: false);
						Debug.Assert(overflowBytes > 0 && overFlowChars > 0, "We expect writes to the overflow buffer to always succeed since it is large enough to accomodate at least one char.");

						_charBuffer = _charBuffer.Slice(overFlowChars);
						Debug.Assert(readBuffer.Count < overflowBytes);

						_overflowBuffer.Array.AsSpan(0, readBuffer.Count).CopyTo(readBuffer);
						_overflowBuffer = new ArraySegment<byte>(
							_overflowBuffer.Array,
							readBuffer.Count,
							overflowBytes - readBuffer.Count);

						Debug.Assert(_overflowBuffer.Count != 0);
						return readBuffer.Count;
					default:
						Debug.Fail("We should never see this");
						throw new InvalidOperationException();
				}
			}

			private async Task ReadInputChars(CancellationToken cancellation)
			{
				Buffer.BlockCopy(
					_byteBuffer.Array,
					_byteBuffer.Offset,
					_byteBuffer.Array,
					0,
					_byteBuffer.Count);

				var readBytes = await _stream.ReadAsync(_byteBuffer.Array.AsMemory(_byteBuffer.Count), cancellation);
				_byteBuffer = new ArraySegment<byte>(_byteBuffer.Array, 0, _byteBuffer.Count + readBytes);

				Debug.Assert(_charBuffer.Count == 0, "We should only expect to read more input chars once all buffered content is read");

				_decoder.Convert(
					_byteBuffer.AsSpan(),
					_charBuffer.Array,
					flush: readBytes == 0,
					out var bytesUsed,
					out var charsUsed,
					out _);

				_byteBuffer = _byteBuffer.Slice(bytesUsed);
				_charBuffer = new ArraySegment<char>(_charBuffer.Array, 0, charsUsed);
			}

			public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
			public override void SetLength(long value) => throw new NotSupportedException();
			public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

			protected override void Dispose(bool disposing)
			{
				if(!_disposed)
				{
					_disposed = true;
					ArrayPool<char>.Shared.Return(_charBuffer.Array);
					ArrayPool<byte>.Shared.Return(_byteBuffer.Array);
					ArrayPool<byte>.Shared.Return(_overflowBuffer.Array);
				}
			}
		}
		#endregion
	}
}
