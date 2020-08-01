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

namespace Zongsoft.Data
{
	internal static class RangeParser
	{
		#region 公共方法
		public static RangeParserResult Parse<T>(ReadOnlySpan<char> span, int start) where T : struct
		{
			var minimum = ReadOnlySpan<char>.Empty;
			var maximum = ReadOnlySpan<char>.Empty;
			var context = new RangeParserContext(span);

			for(int i = start; i < span.Length; i++)
			{
				context.Move(i);

				switch(context.State)
				{
					case State.Start:
						DoStart(ref context);
						break;
					case State.Final:
						DoFinal(ref context);
						break;
					case State.Error:
						return new RangeParserResult(context.ErrorMessage);
					case State.Minimum:
						DoMinimum(ref context, out minimum);
						break;
					case State.MinimumFinal:
						DoMinimunFinal(ref context);
						break;
					case State.Maximum:
						DoMaximum(ref context, out maximum);
						break;
					case State.MaximumFinal:
						DoMaximumFinal(ref context);
						break;
					case State.Separator:
						DoSeparator(ref context);
						break;
				}
			}

			if(context.State != State.Final)
			{
				//未正常关闭圆括号
				if(context.HasFlags(Flags.Parenthesis))
					return new RangeParserResult("Missing the closing parenthesis.");

				//移动指针到最后
				context.Move(-1);

				switch(context.State)
				{
					case State.Minimum:
					case State.MinimumFinal:
					case State.Separator:
						context.Reset(out minimum);
						break;
					default:
						context.Reset(out maximum);
						break;
				}
			}

			if(maximum.IsEmpty && (context.State == State.Minimum || context.State == State.MinimumFinal))
				maximum = minimum;

			return new RangeParserResult(minimum, maximum);
		}
		#endregion

		#region 私有方法
		private static void DoStart(ref RangeParserContext context)
		{
			switch(context.Character)
			{
				case '(':
					context.Reset(State.Minimum, Flags.Parenthesis);
					break;
				case '*':
				case '?':
					context.Reset(State.MinimumFinal);
					break;
				case '~':
					context.Reset(State.Separator);
					break;
				default:
					if(!context.IsWhitespace)
						context.Accept(State.Minimum);
					break;
			}
		}

		private static void DoFinal(ref RangeParserContext context)
		{
			if(!context.IsWhitespace)
				context.Error("The range expression contains redundant content.");
		}

		private static void DoMinimum(ref RangeParserContext context, out ReadOnlySpan<char> value)
		{
			switch(context.Character)
			{
				case '~':
					context.Reset(State.Separator, out value);
					break;
				case '*':
				case '?':
					context.Reset(State.MinimumFinal, out value);
					break;
				case ')':
					context.Reset(State.Final, out value);
					break;
				default:
					context.Accept(State.Minimum);
					value = ReadOnlySpan<char>.Empty;
					break;
			}
		}

		private static void DoMinimunFinal(ref RangeParserContext context)
		{
			switch(context.Character)
			{
				case '~':
					context.Reset(State.Separator);
					break;
				case ')':
					context.Reset(State.Final);
					break;
				default:
					if(!context.IsWhitespace)
						context.Error($"The invalid character is at {context.Index + 1} character.");
					break;
			}
		}

		private static void DoMaximum(ref RangeParserContext context, out ReadOnlySpan<char> value)
		{
			switch(context.Character)
			{
				case '*':
				case '?':
					context.Reset(State.MaximumFinal, out value);
					break;
				case ')':
					context.Reset(State.Final, out value);
					break;
				default:
					context.Accept(State.Maximum);
					value = ReadOnlySpan<char>.Empty;
					break;
			}
		}

		private static void DoMaximumFinal(ref RangeParserContext context)
		{
			switch(context.Character)
			{
				case ')':
					context.Reset(State.Final);
					break;
				default:
					if(!context.IsWhitespace)
						context.Error($"The invalid character is at {context.Index + 1} character.");
					break;
			}
		}

		private static void DoSeparator(ref RangeParserContext context)
		{
			switch(context.Character)
			{
				case '*':
				case '?':
					context.Reset(State.MaximumFinal);
					break;
				default:
					if(!context.IsWhitespace)
						context.Accept(State.Maximum);
					break;
			}
		}
		#endregion

		#region 嵌套结构
		public ref struct RangeParserResult
		{
			#region 私有字段
			private readonly string _message;
			#endregion

			#region 公共字段
			public readonly ReadOnlySpan<char> Minimum;
			public readonly ReadOnlySpan<char> Maximum;
			#endregion

			#region 构造函数
			public RangeParserResult(string message)
			{
				_message = message;
				this.Minimum = ReadOnlySpan<char>.Empty;
				this.Maximum = ReadOnlySpan<char>.Empty;
			}

			public RangeParserResult(ReadOnlySpan<char> minimum, ReadOnlySpan<char> maximum)
			{
				_message = null;
				this.Minimum = minimum;
				this.Maximum = maximum;
			}
			#endregion

			#region 公共方法
			public bool IsFailed(out string message)
			{
				message = _message;
				return message != null && message.Length > 0;
			}
			#endregion
		}

		private ref struct RangeParserContext
		{
			#region 私有字段
			private readonly ReadOnlySpan<char> _text;
			private State _state;
			private char _character;
			private int _index;
			private Flags _flags;
			private int _count;
			private int _whitespaces;
			private string _errorMessage;
			#endregion

			#region 构造函数
			public RangeParserContext(ReadOnlySpan<char> text)
			{
				_text = text;
				_state = State.Start;
				_index = 0;
				_character = '\0';
				_flags = Flags.None;
				_count = 0;
				_whitespaces = 0;
				_errorMessage = null;
			}
			#endregion

			#region 公共属性
			public State State { get => _state; }
			public int Index { get => _index; }
			public char Character { get => _character; }
			public string ErrorMessage { get => _errorMessage; }
			public bool IsWhitespace { get => char.IsWhiteSpace(_character); }
			#endregion

			#region 公共方法
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

			public bool HasFlags(Flags flags)
			{
				return (_flags & flags) == flags;
			}
			#endregion
		}
		#endregion

		#region 枚举定义
		[Flags]
		private enum Flags
		{
			None = 0,
			Parenthesis = 1,
		}

		private enum State
		{
			Start,
			Final,
			Error,

			Minimum,
			MinimumFinal,
			Maximum,
			MaximumFinal,
			Separator,
		}

		private enum DurationState
		{
			Start,
			Final,
			Error,

			Identifier,
			Gutter,
			Argument,
			Unit
		}
		#endregion
	}
}
