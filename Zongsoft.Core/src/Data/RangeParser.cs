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
		public static DateTimeRangeParserResult Parse<T>(ReadOnlySpan<char> span, int start) where T : struct
		{
			var name = string.Empty;
			var value = 0;
			var unit = '\0';

			var context = new DataTimeRangeParserContext(span);

			for(int i = start; i < span.Length; i++)
			{
				context.Move(i);

				switch(context.State)
				{
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
					case State.Argument:
						DoArgument(ref context, out value, out unit);
						break;
					case State.Unit:
						DoUnit(ref context);
						break;
				}
			}

			if(context.State == State.Final)
				return (name.ToLowerInvariant()) switch
				{
					"ago" => GetAgo(value, unit),
					"last" => GetLast(value, unit),
					_ => new DateTimeRangeParserResult($"Invalid datetime range function name: {name}."),
				};

			if(context.State == State.Identifier || context.State == State.Gutter)
				return (name.ToLowerInvariant()) switch
				{
					"today" => GetToday(),
					"yesterday" => GetYesterday(),
					"thisweek" => GetThisWeek(),
					"thismonth" => GetThisMonth(),
					"thisyear" => GetThisYear(),
					"lastyear" => GetLastYear(),
					_ => new DateTimeRangeParserResult($"Invalid datetime range identifier: {name}."),
				};

			return new DateTimeRangeParserResult($"Invalid datetime range expression format.");
		}
		#endregion

		#region 区段计算
		private static DateTimeRangeParserResult GetToday()
		{
			var today = DateTime.Today;
			return new DateTimeRangeParserResult(today, today.AddSeconds((60 * 60 * 24) - 1));
		}

		private static DateTimeRangeParserResult GetYesterday()
		{
			var yesterday = DateTime.Today.AddDays(-1);
			return new DateTimeRangeParserResult(yesterday, yesterday.AddSeconds((60 * 60 * 24) - 1));
		}

		private static DateTimeRangeParserResult GetThisWeek()
		{
			var firstday = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
			return new DateTimeRangeParserResult(firstday, firstday.AddSeconds((60 * 60 * 24 * 6) - 1));
		}

		private static DateTimeRangeParserResult GetThisMonth()
		{
			var firstday = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
			return new DateTimeRangeParserResult(firstday, new DateTime(firstday.Year, firstday.Month, DateTime.DaysInMonth(firstday.Year, firstday.Month), 23, 59, 59, 999));
		}

		private static DateTimeRangeParserResult GetThisYear()
		{
			var firstday = new DateTime(DateTime.Today.Year, 1, 1);
			return new DateTimeRangeParserResult(firstday, new DateTime(firstday.Year, 12, 31, 23, 59, 59, 999));
		}

		private static DateTimeRangeParserResult GetLastYear()
		{
			var firstday = new DateTime(DateTime.Today.Year - 1, 1, 1);
			return new DateTimeRangeParserResult(firstday, new DateTime(firstday.Year, 12, 31, 23, 59, 59, 999));
		}

		private static DateTimeRangeParserResult GetAgo(int number, char unit)
		{
			if(number == 0)
				return GetToday();

			var now = DateTime.Now;

			switch(unit)
			{
				case 'Y':
				case 'y':
					return new DateTimeRangeParserResult(null, now.AddYears(-number));
				case 'M':
					return new DateTimeRangeParserResult(null, now.AddMonths(-number));
				case 'D':
				case 'd':
					return new DateTimeRangeParserResult(null, now.AddDays(-number));
				case 'H':
				case 'h':
					return new DateTimeRangeParserResult(null, now.AddHours(-number));
				case 'm':
					return new DateTimeRangeParserResult(null, now.AddMinutes(-number));
				case 'S':
				case 's':
					return new DateTimeRangeParserResult(null, now.AddSeconds(-number));
				default:
					throw new ArgumentException("Invalid datetime range unit.");
			}
		}

		private static DateTimeRangeParserResult GetLast(int number, char unit)
		{
			if(number == 0)
				return GetToday();

			var now = DateTime.Now;

			switch(unit)
			{
				case 'Y':
				case 'y':
					return new DateTimeRangeParserResult(now.AddYears(-number), now);
				case 'M':
					return new DateTimeRangeParserResult(now.AddMonths(-number), now);
				case 'D':
				case 'd':
					return new DateTimeRangeParserResult(now.AddDays(-number), now);
				case 'H':
				case 'h':
					return new DateTimeRangeParserResult(now.AddHours(-number), now);
				case 'm':
					return new DateTimeRangeParserResult(now.AddMinutes(-number), now);
				case 'S':
				case 's':
					return new DateTimeRangeParserResult(now.AddSeconds(-number), now);
				default:
					throw new ArgumentException("Invalid datetime range unit.");
			}
		}
		#endregion

		#region 私有方法
		private static void DoStart(ref DataTimeRangeParserContext context)
		{
			if(context.IsLetter)
				context.Accept(State.Identifier);
			else
				context.Error($"The invalid character '{context.Character}' is at {context.Index + 1} character.");
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
				context.Reset(State.Argument, out var value);
				name = value.ToString();
			}
			else if(context.IsWhitespace)
			{
				context.Reset(State.Gutter, out var value);
				name = value.ToString();
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
				context.Reset(State.Argument);
			else
				context.Error($"Invalid datetime range expression format.");
		}

		private static void DoArgument(ref DataTimeRangeParserContext context, out int value, out char unit)
		{
			switch(context.Character)
			{
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
					context.Reset(State.Unit, out var text);

					if(text.Length > 0)
					{
						value = int.Parse(text.ToString());
					}
					else
					{
						value = 0;
						context.Error($"Missing the argument value.");
					}

					break;
				default:
					if(context.IsDigit)
						context.Accept(State.Argument);
					else
						context.Error($"The argument in the datetime range expression must be a number.");

					value = 0;
					unit = '\0';
					break;
			}
		}

		private static void DoUnit(ref DataTimeRangeParserContext context)
		{
			if(context.IsWhitespace)
				return;

			if(context.Character == ')')
				context.Reset(State.Final);
			else
				context.Error($"The invalid character '{context.Character}' is at {context.Index + 1} character.");
		}
		#endregion

		#region 嵌套结构
		public ref struct DateTimeRangeParserResult
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

			public DateTimeRangeParserResult(DateTime? minimum, DateTime? maximum)
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
		#endregion

		#region 枚举定义
		private enum State
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
