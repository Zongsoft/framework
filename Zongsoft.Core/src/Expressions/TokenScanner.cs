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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Expressions;

/// <summary>
/// 表示词法分析的分词扫描器。
/// </summary>
public class TokenScanner : IEnumerable<Token>, IDisposable
{
	#region 成员字段
	private Lexer _lexer;
	private ITokenReader _reader;
	#endregion

	#region 构造函数
	internal TokenScanner(Lexer lexer, string text)
	{
		if(lexer == null)
			throw new ArgumentNullException(nameof(lexer));

		_lexer = lexer;
		_reader = new TokenStringReader(text);
	}

	internal TokenScanner(Lexer lexer, Stream stream)
	{
		if(lexer == null)
			throw new ArgumentNullException(nameof(lexer));

		_lexer = lexer;
		_reader = new TokenStreamReader(stream);
	}
	#endregion

	#region 公共方法
	public Token Scan()
	{
		if(_lexer == null)
			throw new ObjectDisposedException(nameof(TokenScanner));

		foreach(var tokenizer in _lexer.Tokenizers)
		{
			//跳过中间的所有空白字符
			SkipWhitespaces(_reader);

			//执行分词操作
			var result = tokenizer.Tokenize((TextReader)_reader);

			//重新定位读取器的指针位置
			_reader.Seek(result.Offset, SeekOrigin.Current);

			if(result.Token != null)
				return result.Token;
		}

		if(_reader.Peek() > 0)
			throw new SyntaxException($"Illegal '{(char)_reader.Read()}' at {_reader.Position + 1} character in the expression.");

		return null;
	}
	#endregion

	#region 私有方法
	private static void SkipWhitespaces(ITokenReader reader)
	{
		int value;

		while((value = reader.Read()) > 0)
		{
			if(!char.IsWhiteSpace((char)value))
			{
				reader.Seek(-1, SeekOrigin.Current);
				return;
			}
		}
	}
	#endregion

	#region 遍历方法
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<Token> GetEnumerator()
	{
		Token token;

		while((token = this.Scan()) != null)
		{
			yield return token;
		}
	}
	#endregion

	#region 释放方法
	void IDisposable.Dispose()
	{
		_lexer = null;
		_reader.Dispose();
	}
	#endregion

	#region 嵌套子类
	internal interface ITokenReader : IDisposable
	{
		int Position { get; }
		int Seek(int offset, SeekOrigin origin);
		int Peek();
		int Read();
	}

	private class TokenStreamReader : StreamReader, ITokenReader
	{
		public TokenStreamReader(Stream stream) : base(stream) { }
		public int Position => (int)this.BaseStream.Position;
		public int Seek(int offset, SeekOrigin origin) => (int)this.BaseStream.Seek(offset, origin);
	}

	private class TokenStringReader : TextReader, ITokenReader
	{
		#region 成员字段
		private readonly string _text;
		private volatile int _position;
		#endregion

		#region 构造函数
		public TokenStringReader(string text)
		{
			if(string.IsNullOrEmpty(text))
				throw new ArgumentNullException(nameof(text));

			_text = text;
			_position = -1;
		}
		#endregion

		#region 公共属性
		public int Position
		{
			get => _position;
			set
			{
				if(value < -1)
					throw new ArgumentOutOfRangeException();

				if(value > _text.Length)
					throw new ArgumentOutOfRangeException();

				_position = value;
			}
		}
		#endregion

		#region 公共方法
		public int Seek(int offset, SeekOrigin origin)
		{
			var position = _position;

			switch(origin)
			{
				case SeekOrigin.Begin:
					position = offset;
					break;
				case SeekOrigin.End:
					position = (_text.Length - 1) - offset;
					break;
				case SeekOrigin.Current:
					position += offset;
					break;
			}

			return this.Position = position;
		}

		public override int Peek()
		{
			var position = _position;

			if(position >= 0 && position < _text.Length)
				return _text[position];

			return -1;
		}

		public override int Read()
		{
			var position = _position;

			while((position = _position) < _text.Length)
			{
				if(Interlocked.CompareExchange(ref _position, position + 1, position) == position)
					break;
			}

			if(position < _text.Length - 1)
				return _text[position + 1];

			return -1;
		}
		#endregion
	}
	#endregion
}
