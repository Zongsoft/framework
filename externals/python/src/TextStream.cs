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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.IO;
using System.Text;

namespace Zongsoft.Externals.Python;

internal class TextStream : Stream
{
	#region 成员字段
	private readonly bool _buffered;
	private readonly Encoding _encoding;
	private readonly TextReader _reader;
	private readonly TextWriter _writer;
	#endregion

	#region 构造函数
	public TextStream(TextReader reader, Encoding encoding = null, bool buffered = true)
	{
		_reader = reader ?? throw new ArgumentNullException(nameof(reader));
		_encoding = encoding ?? Encoding.UTF8;
		_buffered = buffered;
	}

	public TextStream(TextWriter writer, bool buffered = true) : this(writer, null, buffered) { }
	public TextStream(TextWriter writer, Encoding encoding, bool buffered = true)
	{
		_writer = writer ?? throw new ArgumentNullException(nameof(writer));
		_encoding = encoding ?? Encoding.UTF8;
		_buffered = buffered;
	}
	#endregion

	#region 公共属性
	public Encoding Encoding => _encoding;
	public TextReader Reader => _reader;
	public TextWriter Writer => _writer;

	public sealed override bool CanSeek => false;
	public sealed override bool CanWrite => _writer != null;
	public sealed override bool CanRead => _reader != null;
	#endregion

	#region 公共方法
	public sealed override void Flush()
	{
		if(!this.CanWrite)
			throw new InvalidOperationException();

		_writer.Flush();
	}

	public sealed override int Read(byte[] buffer, int offset, int count)
	{
		if(!this.CanRead)
			throw new InvalidOperationException();

		var data = new char[count];
		var countRead = _reader.Read(data, 0, count);
		return _encoding.GetBytes(data, 0, countRead, buffer, offset);
	}

	public sealed override void Write(byte[] buffer, int offset, int count)
	{
		if(!this.CanWrite)
			throw new InvalidOperationException();

		var data = _encoding.GetChars(buffer, offset, count);
		_writer.Write(data, 0, data.Length);

		if(!_buffered)
			_writer.Flush();
	}
	#endregion

	#region 无效方法
	public sealed override long Length => throw new InvalidOperationException();
	public sealed override long Position
	{
		get => throw new InvalidOperationException();
		set => throw new InvalidOperationException();
	}
	public sealed override long Seek(long offset, SeekOrigin origin) => throw new InvalidOperationException();
	public sealed override void SetLength(long value) => throw new InvalidOperationException();
	#endregion
}
