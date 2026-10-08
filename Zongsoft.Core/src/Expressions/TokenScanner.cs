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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Expressions;

/// <summary>表示词法分析的分词扫描器。</summary>
public class TokenScanner : IEnumerable<Token>, IDisposable
{
	#region 成员字段
	private Lexer _lexer;
	private readonly Reader _reader;
	private readonly TextReader _source;
	#endregion

	#region 构造函数
	internal TokenScanner(Lexer lexer, string text)
	{
		_lexer = lexer ?? throw new ArgumentNullException(nameof(lexer));
		_reader = new Reader(text ?? throw new ArgumentNullException(nameof(text)));
	}

	internal TokenScanner(Lexer lexer, Stream stream)
	{
		_lexer = lexer ?? throw new ArgumentNullException(nameof(lexer));
		_source = new StreamReader(stream ?? throw new ArgumentNullException(nameof(stream)), Encoding.UTF8, true);
		_reader = new Reader(_source.ReadToEnd());
	}
	#endregion

	#region 公共方法
	public Token Scan() => this.Scan(out _, out _);

	/// <summary>读取下一个词素，并返回其在原文中的 UTF-16 起始位置与长度。</summary>
	/// <param name="position">当前词素在原文中的起始位置。</param>
	/// <param name="length">当前词素占用的 UTF-16 字符数。</param>
	/// <returns>当前词素；到达原文末尾时返回空。</returns>
	public Token Scan(out int position, out int length)
	{
		ObjectDisposedException.ThrowIf(_lexer == null, this);

		while(_reader.Peek() >= 0 && char.IsWhiteSpace((char)_reader.Peek()))
			_reader.Read();

		position = _reader.Position;
		length = 0;

		if(_reader.Peek() < 0)
			return null;

		foreach(var tokenizer in _lexer.Tokenizers)
		{
			var result = tokenizer.Tokenize(_reader);
			_reader.Position += result.Offset;

			if(result.Token != null)
			{
				length = _reader.Position - position;
				return result.Token;
			}

			_reader.Position = position;
		}

		throw new SyntaxException(string.Format(Properties.Resources.TokenScanner_IllegalCharacter_Message, (char)_reader.Peek(), position + 1));
	}
	#endregion

	#region 遍历方法
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<Token> GetEnumerator()
	{
		Token token;

		while((token = this.Scan()) != null)
			yield return token;
	}
	#endregion

	#region 释放方法
	void IDisposable.Dispose()
	{
		_lexer = null;
		_reader.Dispose();
		_source?.Dispose();
	}
	#endregion

	#region 嵌套子类
	private sealed class Reader(string text) : TextReader
	{
		public int Position { get; set; }
		public override int Peek() => this.Position < text.Length ? text[this.Position] : -1;
		public override int Read() => this.Position < text.Length ? text[this.Position++] : -1;
	}
	#endregion
}
