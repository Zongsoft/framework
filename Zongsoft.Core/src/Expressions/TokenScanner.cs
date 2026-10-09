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

namespace Zongsoft.Expressions;

/// <summary>表示词法分析的分词扫描器。</summary>
public ref struct TokenScanner
{
	#region 成员字段
	private Lexer _lexer;
	private ReadOnlySpan<char> _text;
	private readonly StreamReader _source;
	private int _position;
	#endregion

	#region 构造函数
	internal TokenScanner(Lexer lexer, ReadOnlySpan<char> text)
	{
		_lexer = lexer ?? throw new ArgumentNullException(nameof(lexer));
		_text = text;
	}

	internal TokenScanner(Lexer lexer, Stream stream)
	{
		_lexer = lexer ?? throw new ArgumentNullException(nameof(lexer));
		_source = new StreamReader(stream ?? throw new ArgumentNullException(nameof(stream)), Encoding.UTF8, true);

		try
		{
			_text = _source.ReadToEnd().AsSpan();
		}
		catch
		{
			_source.Dispose();
			throw;
		}
	}
	#endregion

	#region 公共方法
	public Token Scan() => this.Scan(out _, out _);

	/// <summary>读取下一个词素，并返回其在原文中的起始位置与长度。</summary>
	/// <param name="position">当前词素在原文中的起始位置。</param>
	/// <param name="length">当前词素占用的字符数。</param>
	/// <returns>返回当前词素，当到达原文末尾时返回空。</returns>
	public Token Scan(out int position, out int length)
	{
		ObjectDisposedException.ThrowIf(_lexer == null, typeof(TokenScanner));

		while(_position < _text.Length && char.IsWhiteSpace(_text[_position]))
			_position++;

		position = _position;
		length = 0;

		if(_position == _text.Length)
			return null;

		var remaining = _text[_position..];

		for(int i = 0; i < _lexer.Tokenizers.Count; i++)
		{
			var result = _lexer.Tokenizers[i].Tokenize(remaining);

			if(result.Token != null)
			{
				if(result.Length <= 0 || result.Length > remaining.Length)
					throw new InvalidOperationException(Properties.Resources.TokenScanner_InvalidLength_Message);

				length = result.Length;
				_position += length;
				return result.Token;
			}
		}

		throw new SyntaxException(string.Format(Properties.Resources.TokenScanner_IllegalCharacter_Message, remaining[0], position + 1));
	}
	#endregion

	#region 遍历方法
	/// <summary>获取从当前扫描位置开始的独立枚举器，枚举不会推进此扫描器或释放输入流。</summary>
	/// <returns>返回的词素枚举器。</returns>
	public readonly Enumerator GetEnumerator() => new(this);
	#endregion

	#region 释放方法
	public void Dispose()
	{
		_lexer = null;
		_text = default;
		_source?.Dispose();
	}
	#endregion

	#region 嵌套子类
	public ref struct Enumerator(TokenScanner scanner)
	{
		private TokenScanner _scanner = scanner;
		public Token Current { get; private set; }
		public bool MoveNext() => (this.Current = _scanner.Scan()) != null;
	}
	#endregion
}
