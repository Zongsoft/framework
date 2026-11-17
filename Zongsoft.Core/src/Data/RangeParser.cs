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
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Zongsoft.Data;

internal static class RangeParser
{
	#region 公共方法
	public static RangeParserResult Parse(ReadOnlySpan<char> span)
	{
		span = span.Trim();

		if(!span.IsEmpty && span[0] == '(')
		{
			if(span[^1] != ')')
				return new RangeParserResult(Properties.Resources.RangeParser_MissingClosingParenthesis_Message);

			span = span[1..^1].Trim();
		}

		var separator = span.IndexOf('~');
		var minimum = separator < 0 ? span : span[..separator].TrimEnd();
		var maximum = separator < 0 ? span : span[(separator + 1)..].TrimStart();

		if(minimum is ['*'] or ['?'])
			minimum = default;

		if(maximum is ['*'] or ['?'])
			maximum = default;

		if(minimum.IndexOfAny("()*?~") >= 0 || maximum.IndexOfAny("()*?~") >= 0)
			return new RangeParserResult(Properties.Resources.InvalidFormat_Message);

		return new RangeParserResult(minimum, maximum);
	}
	#endregion

	#region 嵌套结构
	public readonly ref struct RangeParserResult
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
			this.Minimum = default;
			this.Maximum = default;
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
			return !string.IsNullOrEmpty(message);
		}
		#endregion
	}
	#endregion
}

internal static class DateTimeRangeParser
{
	#region 私有字段
	private static readonly FrozenDictionary<string, Function> _functions = new Dictionary<string, Function>(StringComparer.OrdinalIgnoreCase)
	{
		["today"] = Function.Today,
		["tomorrow"] = Function.Tomorrow,
		["yesterday"] = Function.Yesterday,
		["thisweek"] = Function.ThisWeek,
		["lastweek"] = Function.LastWeek,
		["thismonth"] = Function.ThisMonth,
		["lastmonth"] = Function.LastMonth,
		["thisyear"] = Function.ThisYear,
		["lastyear"] = Function.LastYear,
		["ago"] = Function.Ago,
		["last"] = Function.Last,
		["year"] = Function.Year,
		["month"] = Function.Month,
		["day"] = Function.Day,
	}.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 公共方法
	public static DateTimeRangeParserResult<T>? Parse<T>(ReadOnlySpan<char> span) where T : struct
	{
		var function = Function.Unknown;
		Span<DataTimeRangeFunctionArgument> arguments = stackalloc DataTimeRangeFunctionArgument[3];
		int argumentCount = 0;

		var context = new DataTimeRangeParserContext(span);

		for(int i = 0; i < span.Length; i++)
		{
			context.Move(i);

			switch(context.State)
			{
				case State.Exit:
					return null;
				case State.Error:
					return new DateTimeRangeParserResult<T>(context.ErrorMessage);
				case State.Start:
					DoStart(ref context);
					break;
				case State.Final:
					DoFinal(ref context);
					break;
				case State.Identifier:
					DoIdentifier(ref context, out function);
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
						if(argumentCount == arguments.Length)
							return new DateTimeRangeParserResult<T>(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.RangeParser_TooManyArguments_Message, function));

						arguments[argumentCount++] = new DataTimeRangeFunctionArgument(value, unit);
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
			case State.Exit:
				return null;
			case State.Error:
				return new DateTimeRangeParserResult<T>(context.ErrorMessage);
			case State.Identifier:
				//必须移动指针到最后，以便Reset正确计算内容值
				context.Move(-1);

				//获取最终的标识名
				context.Reset(State.Final, out var text);

				//更新标识名称(函数名)
				function = GetFunction(text);

				break;
			case State.Gutter:
			case State.Final:
				break;
			default:
				return new DateTimeRangeParserResult<T>(Properties.Resources.RangeParser_InvalidDateTimeFormat_Message);
		}

		var count = function switch
		{
			Function.Ago or Function.Last or Function.Year => 1,
			Function.Month => 2,
			Function.Day => 3,
			Function.Unknown => -1,
			_ => 0,
		};

		if(count < 0)
			return new DateTimeRangeParserResult<T>(Properties.Resources.RangeParser_InvalidDateTimeFormat_Message);

		if(argumentCount < count)
			return new DateTimeRangeParserResult<T>(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.RangeParser_MissingArguments_Message, function));

		if(argumentCount > count)
			return new DateTimeRangeParserResult<T>(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.RangeParser_TooManyArguments_Message, function));

		if(function is Function.Ago or Function.Last)
		{
			if(arguments[0].Unit == '\0')
				return new DateTimeRangeParserResult<T>(Properties.Resources.RangeParser_InvalidDateTimeFormat_Message);
		}
		else
		{
			foreach(var argument in arguments[..argumentCount])
			{
				if(argument.Unit != '\0')
					return new DateTimeRangeParserResult<T>(Properties.Resources.RangeParser_NumericArgumentRequired_Message);
			}
		}

		return TryEvaluate(function, arguments[..argumentCount], out var range) && Range.Timing.TryConvert(range, out Range<T> result) ?
			new DateTimeRangeParserResult<T>(result) :
			new DateTimeRangeParserResult<T>(Properties.Resources.RangeParser_InvalidDateTimeFormat_Message);
	}
	#endregion

	#region 私有方法
	private static Function GetFunction(ReadOnlySpan<char> name) =>
		_functions.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(name, out var function) ? function : Function.Unknown;

	private static bool TryEvaluate(Function function, ReadOnlySpan<DataTimeRangeFunctionArgument> arguments, out Range<DateTime> range)
	{
		range = default;
		var now = function is Function.Year or Function.Month or Function.Day ? default : DateTime.Now;
		var today = now.Date;

		switch(function)
		{
			case Function.Today:
				range = Range.Timing.Day(today);
				return true;
			case Function.Tomorrow:
			case Function.Yesterday:
				if(!TryShift(today, function == Function.Tomorrow ? -1 : 1, 'd', out var day))
					return false;

				range = Range.Timing.Day(day);
				return true;
			case Function.ThisWeek:
			case Function.LastWeek:
				var days = today.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)today.DayOfWeek - 1;

				if(!TryShift(today, days + (function == Function.LastWeek ? 7 : 0), 'd', out var first) ||
				   !TryShift(first, -604799, 's', out var last))
					return false;

				range = new(first, last);
				return true;
			case Function.ThisMonth:
				range = Range.Timing.Month(today.Year, today.Month);
				return true;
			case Function.LastMonth:
				if(!TryShift(today, 1, 'M', out var month))
					return false;

				range = Range.Timing.Month(month.Year, month.Month);
				return true;
			case Function.ThisYear:
				range = Range.Timing.Year(today.Year);
				return true;
			case Function.LastYear:
				if(today.Year == 1)
					return false;

				range = Range.Timing.Year(today.Year - 1);
				return true;
			case Function.Year:
			case Function.Month:
			case Function.Day:
				var year = arguments[0].Value;

				if(year is < 1 or > 9999)
					return false;

				if(function == Function.Year)
				{
					range = Range.Timing.Year(year);
					return true;
				}

				var monthNumber = arguments[1].Value;

				if(monthNumber is < 1 or > 12)
					return false;

				if(function == Function.Month)
				{
					range = Range.Timing.Month(year, monthNumber);
					return true;
				}

				var dayNumber = arguments[2].Value;

				if(dayNumber < 1 || dayNumber > DateTime.DaysInMonth(year, monthNumber))
					return false;

				range = Range.Timing.Day(year, monthNumber, dayNumber);
				return true;
			case Function.Ago:
			case Function.Last:
				var argument = arguments[0];

				if(!TryShift(now, argument.Value, argument.Unit, out var timestamp))
					return false;

				if(function == Function.Ago)
				{
					range = new(null, timestamp);
					return true;
				}

				if(argument.Value == 0)
				{
					timestamp = argument.Unit switch
					{
						'Y' or 'y' => new(now.Year, 1, 1),
						'M' => new(now.Year, now.Month, 1),
						'D' or 'd' => today,
						'H' or 'h' => new(now.Year, now.Month, now.Day, now.Hour, 0, 0),
						'm' => new(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0),
						_ => now,
					};
				}

				range = new(timestamp, now);
				return true;
			default:
				return false;
		}
	}

	private static bool TryShift(DateTime timestamp, int number, char unit, out DateTime result)
	{
		result = default;

		if(unit is 'Y' or 'y')
		{
			if(number < timestamp.Year - 9999 || number > timestamp.Year - 1)
				return false;

			result = timestamp.AddYears(-number);
			return true;
		}

		if(unit == 'M')
		{
			if(number < (timestamp.Year - 9999) * 12 + timestamp.Month - 12 ||
				number > (timestamp.Year - 1) * 12 + timestamp.Month - 1)
				return false;

			result = timestamp.AddMonths(-number);
			return true;
		}

		var ticks = unit switch
		{
			'D' or 'd' => TimeSpan.TicksPerDay,
			'H' or 'h' => TimeSpan.TicksPerHour,
			'm' => TimeSpan.TicksPerMinute,
			'S' or 's' => TimeSpan.TicksPerSecond,
			_ => 0,
		};

		if(ticks == 0 || number > timestamp.Ticks / ticks ||
			number < 0 && -(long)number > (DateTime.MaxValue.Ticks - timestamp.Ticks) / ticks)
			return false;

		result = timestamp.AddTicks(-(long)number * ticks);
		return true;
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
			context.Error(Properties.Resources.RangeParser_RedundantContent_Message);
	}

	private static void DoIdentifier(ref DataTimeRangeParserContext context, out Function function)
	{
		if(context.Character == '(')
		{
			context.Reset(State.Method, out var text);
			function = GetFunction(text);
		}
		else if(context.IsWhitespace)
		{
			context.Reset(State.Gutter, out var text);
			function = GetFunction(text);
		}
		else if(context.IsLetterOrDigit)
		{
			function = Function.Unknown;
			context.Accept(State.Identifier);
		}
		else
		{
			function = Function.Unknown;
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
			context.Error(Properties.Resources.RangeParser_InvalidDateTimeFormat_Message);
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
					context.Error(Properties.Resources.RangeParser_NumericArgumentRequired_Message);

				value = 0;
				unit = '\0';

				return false;
		}

		static int GetNumberValue(ref DataTimeRangeParserContext context, State state)
		{
			context.Reset(state, out var text);

			if(int.TryParse(text, out var number))
				return number;

			context.Error(Properties.Resources.RangeParser_NumericArgumentRequired_Message);
			return 0;
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
	public readonly struct DateTimeRangeParserResult<T> where T : struct
	{
		#region 私有字段
		private readonly string _message;
		#endregion

		#region 公共字段
		public readonly Range<T> Range;
		#endregion

		#region 构造函数
		public DateTimeRangeParserResult(string message)
		{
			_message = message;
			this.Range = default;
		}

		public DateTimeRangeParserResult(Range<T> value)
		{
			_message = null;
			this.Range = value;
		}
		#endregion

		#region 公共方法
		public bool IsFailed(out string message)
		{
			message = _message;
			return !string.IsNullOrEmpty(message);
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
		public readonly State State => _state;
		public readonly int Index => _index;
		public readonly char Character => _character;
		public readonly string ErrorMessage => _errorMessage;
		public readonly bool IsWhitespace => char.IsWhiteSpace(_character);
		public readonly bool IsLetter => char.IsLetter(_character);
		public readonly bool IsDigit => char.IsDigit(_character);
		public readonly bool IsLetterOrDigit => char.IsLetterOrDigit(_character);
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
	private enum Function
	{
		Unknown,
		Today,
		Tomorrow,
		Yesterday,
		ThisWeek,
		LastWeek,
		ThisMonth,
		LastMonth,
		ThisYear,
		LastYear,
		Ago,
		Last,
		Year,
		Month,
		Day,
	}

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
