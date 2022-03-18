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
using System.Collections.Generic;

namespace Zongsoft.Data
{
	internal static class RangeParser
	{
		#region 公共方法
		public static RangeParserResult Parse<T>(ReadOnlySpan<char> span, int start = 0) where T : struct
		{
			var minimum = ReadOnlySpan<char>.Empty;
			var maximum = ReadOnlySpan<char>.Empty;
			var context = new RangeParserContext(span);

			for(int i = start; i < span.Length; i++)
			{
				context.Move(i);

				switch(context.State)
				{
					case State.Error:
						return new RangeParserResult(context.ErrorMessage);
					case State.Start:
						DoStart(ref context);
						break;
					case State.Final:
						DoFinal(ref context);
						break;
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

				//必须移动指针到最后，以便Reset正确计算内容值
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

		#region 状态处理
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
			/// <summary>无标记</summary>
			None = 0,
			/// <summary>圆括号包裹</summary>
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
		#endregion
	}

	internal static class DateTimeRangeParser
	{
		#region 公共方法
		public static DateTimeRangeParserResult? Parse<T>(ReadOnlySpan<char> span, int start = 0) where T : struct
		{
			var name = string.Empty;
			IList<DataTimeRangeFunctionArgument> arguments = null;

			var context = new DataTimeRangeParserContext(span);

			for(int i = start; i < span.Length; i++)
			{
				context.Move(i);

				switch(context.State)
				{
					case State.Exit:
						return null;
					case State.Error:
						return new DateTimeRangeParserResult(context.ErrorMessage);
					case State.Start:
						DoStart(ref context);
						break;
					case State.Final:
						DoFinal(ref context);
						break;
					case State.Identifier:
						DoIdentifier(ref context, out name);
						break;
					case State.Gutter:
						DoGutter(ref context);
						break;
					case State.Method:
						DoMethod(ref context);
						break;
					case State.Number:
						if(DoNumber(ref context, out var value, out var unit))
						{
							if(arguments == null)
								arguments = new List<DataTimeRangeFunctionArgument>();

							arguments.Add(new DataTimeRangeFunctionArgument(value, unit));
						}
						break;
					case State.Argument:
						DoArgument(ref context);
						break;
					case State.ArgumentFinal:
						DoArgumentFinal(ref context);
						break;
				}
			}

			switch(context.State)
			{
				case State.Identifier:
					//必须移动指针到最后，以便Reset正确计算内容值
					context.Move(-1);

					//获取最终的标识名
					context.Reset(State.Final, out var text);

					//更新标识名称(函数名)
					name = text.ToString();

					break;
				case State.Gutter:
				case State.Final:
					break;
				default:
					return new DateTimeRangeParserResult($"Invalid datetime range expression format.");
			}

			switch(name.ToLowerInvariant())
			{
				case "today":
					return new DateTimeRangeParserResult(Range.GetToday());
				case "yesterday":
					return new DateTimeRangeParserResult(Range.GetYesterday());
				case "thisweek":
					return new DateTimeRangeParserResult(Range.GetThisWeek());
				case "thismonth":
					return new DateTimeRangeParserResult(Range.GetThisMonth());
				case "thisyear":
					return new DateTimeRangeParserResult(Range.GetThisYear());
				case "lastyear":
					return new DateTimeRangeParserResult(Range.GetLastYear());
				case "ago":
					if(arguments == null || arguments.Count < 1)
						return new DateTimeRangeParserResult($"The Ago range function is missing a required parameter.");

					if(arguments.Count > 1)
						return new DateTimeRangeParserResult($"The Ago range function has too many parameters.");

					return new DateTimeRangeParserResult(Range.GetAgo(arguments[0].Value, arguments[0].Unit));
				case "last":
					if(arguments == null || arguments.Count < 1)
						return new DateTimeRangeParserResult($"The Last range function is missing a required parameter.");

					if(arguments.Count > 1)
						return new DateTimeRangeParserResult($"The Last range function has too many parameters.");

					return new DateTimeRangeParserResult(Range.GetLast(arguments[0].Value, arguments[0].Unit));
				case "year":
					if(arguments == null || arguments.Count < 1)
						return new DateTimeRangeParserResult($"The Year range function is missing a required parameter.");

					if(arguments.Count > 1)
						return new DateTimeRangeParserResult($"The Year range function has too many parameters.");

					return new DateTimeRangeParserResult(Range.GetYear(arguments[0].Value));
				case "month":
					if(arguments == null || arguments.Count < 2)
						return new DateTimeRangeParserResult($"The Month range function is missing required parameters.");

					if(arguments.Count > 2)
						return new DateTimeRangeParserResult($"The Month range function has too many parameters.");

					return new DateTimeRangeParserResult(Range.GetMonth(arguments[0].Value, arguments[1].Value));
				case "day":
					if(arguments == null || arguments.Count < 3)
						return new DateTimeRangeParserResult($"The Day range function is missing required parameters.");

					if(arguments.Count > 3)
						return new DateTimeRangeParserResult($"The Day range function has too many parameters.");

					return new DateTimeRangeParserResult(Range.GetDay(arguments[0].Value, arguments[1].Value, arguments[2].Value));
				default:
					return new DateTimeRangeParserResult($"Invalid datetime range function name: {name}.");
			}
		}
		#endregion

		#region 状态处理
		private static void DoStart(ref DataTimeRangeParserContext context)
		{
			if(context.IsWhitespace)
				return;

			if(context.IsLetter)
				context.Accept(State.Identifier);
			else
				context.Accept(State.Exit);
		}

		private static void DoFinal(ref DataTimeRangeParserContext context)
		{
			if(!context.IsWhitespace)
				context.Error("The datatime range expression contains redundant content.");
		}

		private static void DoIdentifier(ref DataTimeRangeParserContext context, out string name)
		{
			if(context.Character == '(')
			{
				context.Reset(State.Method, out var text);
				name = text.ToString();
			}
			else if(context.IsWhitespace)
			{
				context.Reset(State.Gutter, out var text);
				name = text.ToString();
			}
			else if(context.IsLetterOrDigit)
			{
				name = null;
				context.Accept(State.Identifier);
			}
			else
			{
				name = null;
				context.Error($"The invalid character '{context.Character}' is at {context.Index + 1} character.");
			}
		}

		private static void DoGutter(ref DataTimeRangeParserContext context)
		{
			if(context.IsWhitespace)
				return;

			if(context.Character == '(')
				context.Reset(State.Method);
			else
				context.Error($"Invalid datetime range expression format.");
		}

		private static void DoMethod(ref DataTimeRangeParserContext context)
		{
			if(context.IsWhitespace)
				return;

			if(context.IsDigit)
				context.Accept(State.Number);
			else if(context.Character == ')')
				context.Reset(State.Final);
			else
				context.Error($"The function parameter of the datetime range contains the illegal character '{context.Character}' at {context.Index + 1} position.");
		}

		private static bool DoNumber(ref DataTimeRangeParserContext context, out int value, out char unit)
		{
			static int GetNumberValue(ref DataTimeRangeParserContext context, State state)
			{
				context.Reset(state, out var text);

				if(text.Length > 0)
					return int.Parse(text.ToString());

				context.Error($"Missing datetime range function parameter value.");
				return 0;
			}

			if(context.IsWhitespace)
			{
				unit = '\0';
				value = GetNumberValue(ref context, State.ArgumentFinal);
				return true;
			}

			switch(context.Character)
			{
				case ')':
					unit = '\0';
					value = GetNumberValue(ref context, State.Final);
					return true;
				case ',':
					unit = '\0';
					value = GetNumberValue(ref context, State.Argument);
					return true;
				case 'Y':
				case 'y':
				case 'M':
				case 'D':
				case 'd':
				case 'H':
				case 'h':
				case 'm':
				case 'S':
				case 's':
					unit = context.Character;
					value = GetNumberValue(ref context, State.ArgumentFinal);
					return true;
				default:
					if(context.IsDigit)
						context.Accept(State.Number);
					else
						context.Error($"The argument in the datetime range expression must be a number.");

					value = 0;
					unit = '\0';

					return false;
			}
		}

		private static void DoArgument(ref DataTimeRangeParserContext context)
		{
			if(context.IsWhitespace)
				return;

			if(context.IsDigit)
				context.Accept(State.Number);
			else
				context.Error($"The function parameter of the datetime range contains the illegal character '{context.Character}' at {context.Index + 1} position.");
		}

		private static void DoArgumentFinal(ref DataTimeRangeParserContext context)
		{
			if(context.IsWhitespace)
				return;

			switch(context.Character)
			{
				case ')':
					context.Reset(State.Final);
					break;
				case ',':
					context.Reset(State.Argument);
					break;
				default:
					context.Error($"The function parameter of the datetime range contains the illegal character '{context.Character}' at {context.Index + 1} position.");
					break;
			}
		}
		#endregion

		#region 嵌套结构
		public struct DateTimeRangeParserResult
		{
			#region 私有字段
			private readonly string _message;
			#endregion

			#region 公共字段
			public readonly DateTime? Minimum;
			public readonly DateTime? Maximum;
			#endregion

			#region 构造函数
			public DateTimeRangeParserResult(string message)
			{
				_message = message;
				this.Minimum = DateTime.MinValue;
				this.Maximum = DateTime.MaxValue;
			}

			public DateTimeRangeParserResult(Range<DateTime> duration)
			{
				_message = null;
				this.Minimum = duration.Minimum;
				this.Maximum = duration.Maximum;
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

		private ref struct DataTimeRangeParserContext
		{
			#region 私有字段
			private readonly ReadOnlySpan<char> _text;
			private State _state;
			private char _character;
			private int _index;
			private int _count;
			private int _whitespaces;
			private string _errorMessage;
			#endregion

			#region 构造函数
			public DataTimeRangeParserContext(ReadOnlySpan<char> text)
			{
				_text = text;
				_state = State.Start;
				_index = 0;
				_character = '\0';
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
			public bool IsLetter { get => char.IsLetter(_character); }
			public bool IsDigit { get => char.IsDigit(_character); }
			public bool IsLetterOrDigit { get => char.IsLetterOrDigit(_character); }
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

			public void Reset(State state)
			{
				_count = 0;
				_whitespaces = 0;
				_state = state;
			}

			public void Reset(State state, out ReadOnlySpan<char> value)
			{
				value = _count > 0 ? _text.Slice(_index - _count - _whitespaces, _count) : ReadOnlySpan<char>.Empty;

				_count = 0;
				_whitespaces = 0;
				_state = state;
			}

			public void Reset(out ReadOnlySpan<char> value)
			{
				value = _count > 0 ? _text.Slice(_index - _count - _whitespaces, _count) : ReadOnlySpan<char>.Empty;

				_count = 0;
				_whitespaces = 0;
			}

			public void Accept(State state)
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
			}
			#endregion
		}

		private struct DataTimeRangeFunctionArgument
		{
			#region 构造函数
			public DataTimeRangeFunctionArgument(int value, char unit = '\0')
			{
				this.Value = value;
				this.Unit = unit;
			}
			#endregion

			#region 公共字段
			public int Value;
			public char Unit;
			#endregion
		}
		#endregion

		#region 枚举定义
		private enum State
		{
			Start,
			Final,
			Error,
			Exit,

			Identifier,
			Gutter,
			Method,
			Number,
			Argument,
			ArgumentFinal,
		}
		#endregion
	}
}
