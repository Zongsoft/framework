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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Configuration;

partial class Settings
{
	#region 解析方法
	public static Settings Parse(ReadOnlySpan<char> text) => Parse(null, text);
	public static Settings Parse(string name, ReadOnlySpan<char> text)
	{
		var entries = Parse(text, message => throw new ArgumentException(message));
		return entries == null ? null : new Settings(name, text.ToString(), entries);
	}

	public static bool TryParse(ReadOnlySpan<char> text, out Settings result) => TryParse(null, text, out result);
	public static bool TryParse(string name, ReadOnlySpan<char> text, out Settings result)
	{
		var entries = Parse(text, null);
		result = entries == null ? null : new Settings(name, text.ToString(), entries);
		return result != null;
	}

	private static IEnumerable<KeyValuePair<string, string>> Parse(ReadOnlySpan<char> text, Action<string> onError)
	{
		if(text.IsEmpty)
			return [];

		var key = string.Empty;
		var context = new Context(text);
		var result = new List<KeyValuePair<string, string>>();

		while(context.Move())
		{
			switch(context.State)
			{
				case State.None:
					DoNone(ref context);
					break;
				case State.Key:
					if(DoKey(ref context, out key) && context.State == State.Delimiter)
						result.Add(new KeyValuePair<string, string>(key, null));
					break;
				case State.Value:
					if(DoValue(ref context, out var value))
						result.Add(new KeyValuePair<string, string>(key, value));
					break;
				case State.Assigner:
					if(DoAssigner(ref context) && context.State == State.Delimiter)
						result.Add(new KeyValuePair<string, string>(key, null));
					break;
				case State.Gapping:
					DoGapping(ref context);
					break;
				case State.Delimiter:
					DoDelimiter(ref context);
					break;
				case State.Error:
					onError?.Invoke(context.ErrorMessage);
					return null;
			}
		}

		if(context.State == State.Key)
			result.Add(new KeyValuePair<string, string>(context.GetValue().ToString(), null));
		else if(context.State == State.Value)
			result.Add(new KeyValuePair<string, string>(key, context.GetValue().ToString()));

		return result;
	}
	#endregion

	#region 状态处理
	private static bool DoNone(ref Context context)
	{
		if(context.IsWhitespace || context.Character == ';')
			return false;

		if(context.IsLetterOrDigitOrUnderscore)
		{
			context.Accept(State.Key);
			return true;
		}

		context.Error();
		return false;
	}

	private static bool DoKey(ref Context context, out string key)
	{
		switch(context.Character)
		{
			case '=':
				key = context.GetValue().ToString();
				if(string.IsNullOrEmpty(key))
					context.Error();

				context.Reset(State.Assigner);
				return true;
			case ';':
				key = context.GetValue().ToString();
				if(string.IsNullOrEmpty(key))
					context.Error();

				context.Reset(State.Delimiter);
				return true;
			default:
				key = null;
				context.Accept();
				return false;
		}
	}

	private static bool DoValue(ref Context context, out string value)
	{
		if(!context.HasFlags())
		{
			if(context.Character == ';')
			{
				value = context.GetValue().ToString();
				context.Reset(State.Delimiter);
				return true;
			}
		}

		if(context.Accept())
		{
			value = context.GetValue().ToString();
			context.Reset(State.Gapping);
			return true;
		}

		value = null;
		return false;
	}

	private static bool DoAssigner(ref Context context)
	{
		if(context.IsWhitespace)
			return false;

		switch(context.Character)
		{
			case '"':
				context.Reset(State.Value, Flags.DoubleQuotation);
				return true;
			case '\'':
				context.Reset(State.Value, Flags.SingleQuotation);
				return true;
			case '=':
				context.Error();
				return false;
			case ';':
				context.Reset(State.Delimiter);
				return true;
			default:
				context.Accept(State.Value);
				return true;
		}
	}

	private static bool DoGapping(ref Context context)
	{
		if(context.IsWhitespace)
			return false;

		if(context.Character == ';')
		{
			context.Reset(State.Delimiter);
			return true;
		}

		context.Error();
		return false;
	}

	private static bool DoDelimiter(ref Context context) => DoNone(ref context);
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
		private string _errorMessage;
		private int _whitespaces;
		private char[] _buffer;
		private int _bufferCount;
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
			_bufferCount = 0;
			_whitespaces = 0;
			_errorMessage = null;
			_buffer = new char[text.Length];
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
			if(_index < _text.Length - 1)
			{
				_character = _text[_index++];
				return true;
			}

			_character = '\0';
			return false;
		}

		public void Error(string message = null)
		{
			_state = State.Error;
			_errorMessage = message ?? $"An illegal character '{this.Character}' was found at position {this.Offset + this.Index}.";
		}

		public readonly ReadOnlySpan<char> GetValue() => _bufferCount > 0 ? _buffer.AsSpan(0, _bufferCount) : default;

		public void Reset(State state, Flags flags = Flags.None)
		{
			_state = state;
			_bufferCount = 0;
			_whitespaces = 0;
			_flags = flags;
		}

		public bool Accept(Flags? flags = null) => this.Accept(_state, flags);
		public bool Accept(State state, Flags? flags = null)
		{
			if(_state != state)
				_state = state;

			if(flags.HasValue)
				_flags = flags.Value;

			//如果当前位置处于“单引号”或“双引号”内部
			if(this.HasFlags(Flags.SingleQuotation) || this.HasFlags(Flags.DoubleQuotation))
			{
				if(this.HasFlags(Flags.Escaping))
				{
					_flags &= ~Flags.Escaping;
					_buffer[_bufferCount++] = Escape(_character);
				}
				else
				{
					switch(_character)
					{
						case '\\':
							_flags |= Flags.Escaping;
							return false;
						case '"':
							if(this.HasFlags(Flags.DoubleQuotation))
							{
								_flags &= ~Flags.DoubleQuotation;
								return true;
							}
							break;
						case '\'':
							if(this.HasFlags(Flags.SingleQuotation))
							{
								_flags &= ~Flags.SingleQuotation;
								return true;
							}
							break;
					}

					_buffer[_bufferCount++] = _character;
				}
			}
			else
			{
				if(char.IsWhiteSpace(_character))
				{
					if(_bufferCount > 0)
						_whitespaces++;
				}
				else
				{
					for(int i = 0; i < _whitespaces; i++)
						_buffer[_bufferCount++] = ' ';

					_whitespaces = 0;
					_buffer[_bufferCount++] = _character;
				}
			}

			return false;
		}

		public readonly bool HasFlags() => _flags != Flags.None;
		public readonly bool HasFlags(Flags flags) => (_flags & flags) == flags;
		#endregion

		#region 私有方法
		private static char Escape(char chr) => chr switch
		{
			't' => '\t',
			'n' => '\n',
			'r' => '\r',
			_ => chr,
		};
		#endregion
	}
	#endregion

	#region 枚举定义
	private enum State
	{
		None,
		Key,
		Value,
		Assigner,
		Gapping,
		Delimiter,
		Error = 99,
	}

	[Flags]
	private enum Flags
	{
		None = 0,
		Escaping = 4,        //转义符：反斜杠
		SingleQuotation = 1, //单引号
		DoubleQuotation = 2, //双引号
	}
	#endregion
}
