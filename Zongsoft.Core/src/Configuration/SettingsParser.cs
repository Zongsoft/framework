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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Configuration;

internal static class SettingsParser
{
	#region 解析方法
	public static Setting Parse(ReadOnlySpan<char> text, Action<string> onError)
	{
		if(text.IsEmpty)
			return null;

		var context = new Context(text);

		while(context.Move())
		{
			switch(context.State)
			{
				case State.None:
					DoNone(ref context);
					break;
				case State.Key:
					DoKey(ref context);
					break;
				case State.Value:
					DoValue(ref context);
					break;
				case State.Error:
					onError(context.ErrorMessage);
					return null;
			}
		}

		return null;
	}
	#endregion

	#region 状态处理
	private static void DoNone(ref Context context)
	{
		if(context.IsWhitespace)
			return;

		if(context.IsLetterOrDigitOrUnderscore)
			context.Accept(State.Key);
		else
			context.Error($"An illegal character '{context.Character}' was found at position {context.Offset + context.Index}.");
	}

	private static void DoKey(ref Context context)
	{
		if(context.IsLetterOrDigitOrUnderscore)
			return;

		if(context.IsWhitespace)
			context.Accept(State.Key);
		else if(context.Character == '=')
			context.Accept(State.Value);
	}

	private static void DoValue(ref Context context)
	{
	}

	private static void DoAssign(ref Context context)
	{
	}

	private static void DoDelimiter(ref Context context)
	{
	}
	#endregion

	#region 嵌套结构
	private ref struct Context
	{
		#region 私有字段
		private readonly ReadOnlySpan<char> _text;
		private readonly int _offset;
		private State _state;
		private char _character;
		private int _index;
		private Flags _flags;
		private int _count;
		private int _whitespaces;
		private string _errorMessage;
		#endregion

		#region 构造函数
		public Context(ReadOnlySpan<char> text, int offset = 0)
		{
			_text = text;
			_offset = offset;
			_state = State.None;
			_index = 0;
			_character = '\0';
			_flags = Flags.None;
			_count = 0;
			_whitespaces = 0;
			_errorMessage = null;
		}
		#endregion

		#region 公共属性
		public readonly State State => _state;
		public readonly int Index => _index;
		public readonly int Offset => _offset;
		public readonly char Character => _character;
		public readonly string ErrorMessage => _errorMessage;
		public readonly bool IsWhitespace => char.IsWhiteSpace(_character);
		public readonly bool IsLetterOrUnderscore =>
			(this.Character >= 'a' && this.Character <= 'z') ||
			(this.Character >= 'A' && this.Character <= 'Z') || this.Character == '_';

		public readonly bool IsLetterOrDigitOrUnderscore =>
			(this.Character >= 'a' && this.Character <= 'z') ||
			(this.Character >= 'A' && this.Character <= 'Z') ||
			(this.Character >= '0' && this.Character <= '9') || this.Character == '_';
		#endregion

		#region 公共方法
		public bool Move()
		{
			if(_index < _text.Length)
			{
				_index++;
				return true;
			}

			return false;
		}

		public bool Move(int index)
		{
			if(index >= 0 && index < _text.Length)
			{
				_index = index;
				_character = _text[index];
				return true;
			}

			_index = _text.Length;
			_character = '\0';
			return false;
		}

		public void Error(string message)
		{
			_state = State.Error;
			_errorMessage = message;
		}

		public void Reset(State state, Flags flags = Flags.None)
		{
			_count = 0;
			_whitespaces = 0;
			_state = state;

			if(flags != 0)
				_flags |= flags;
		}

		public void Reset(out ReadOnlySpan<char> value)
		{
			value = _count > 0 ? _text.Slice(_index - _count - _whitespaces, _count) : ReadOnlySpan<char>.Empty;

			_count = 0;
			_whitespaces = 0;
		}

		public void Reset(State state, out ReadOnlySpan<char> value)
		{
			value = _count > 0 ? _text.Slice(_index - _count - _whitespaces, _count) : ReadOnlySpan<char>.Empty;

			_count = 0;
			_whitespaces = 0;
			_state = state;
		}

		public void Accept(State state, Flags flags = Flags.None)
		{
			if(char.IsWhiteSpace(_character))
			{
				if(_count > 0)
					_whitespaces++;
			}
			else
			{
				_count += _whitespaces + 1;
				_whitespaces = 0;
			}

			_state = state;

			if(flags != 0)
				_flags |= flags;
		}

		public readonly bool HasFlags(Flags flags) => (_flags & flags) == flags;
		#endregion
	}
	#endregion

	#region 枚举定义
	private enum State
	{
		None,
		Key,
		Value,
		Assign,
		Delimiter,
		Error = 99,
	}

	[Flags]
	private enum Flags
	{
		None = 0,
		SingleQuotation = 1, //单引号
		DoubleQuotation = 2, //双引号
	}
	#endregion
}
