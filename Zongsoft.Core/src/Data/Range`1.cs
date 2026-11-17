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
using System.Collections.Generic;

namespace Zongsoft.Data;

/// <summary>表示具有可选上下界的值类型范围。</summary>
/// <typeparam name="T">范围值类型。顺序比较要求支持 <see cref="Comparer{T}.Default"/>，可实现 <see cref="IComparable{T}"/> 或 <see cref="IComparable"/>。</typeparam>
public struct Range<T> : ISpanParsable<Range<T>> where T : struct
{
	#region 成员字段
	private T? _minimum;
	private T? _maximum;
	#endregion

	#region 构造函数
	public Range(T value) => _minimum = _maximum = value;
	public Range(T? minimum, T? maximum)
	{
		//如果两个参数都有值并且起始值大于截止值，则进行交换赋值
		if(minimum.HasValue && maximum.HasValue && Comparer<T>.Default.Compare(minimum.Value, maximum.Value) > 0)
		{
			_minimum = maximum;
			_maximum = minimum;

			return;
		}

		_minimum = minimum;
		_maximum = maximum;
	}
	#endregion

	#region 公共属性
	public T? Minimum
	{
		readonly get => _minimum;
		set => _minimum = this.EnsureMinimum(value);
	}

	public T? Maximum
	{
		readonly get => _maximum;
		set => _maximum = this.EnsureMaximum(value);
	}

	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public readonly bool HasValue => _minimum.HasValue || _maximum.HasValue;

	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public readonly bool IsEmpty => _minimum == null && _maximum == null;

	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public readonly bool IsZero =>
		(_minimum == null || Comparer<T>.Default.Compare(_minimum.Value, default) == 0) &&
		(_maximum == null || Comparer<T>.Default.Compare(_maximum.Value, default) == 0);
	#endregion

	#region 公共方法
	public readonly bool Contains(T value)
	{
		if(_minimum.HasValue)
		{
			if(_maximum.HasValue)
				return Comparer<T>.Default.Compare(value, _minimum.Value) >= 0 && Comparer<T>.Default.Compare(value, _maximum.Value) <= 0;
			else
				return Comparer<T>.Default.Compare(value, _minimum.Value) >= 0;
		}
		else
		{
			if(_maximum.HasValue)
				return Comparer<T>.Default.Compare(value, _maximum.Value) <= 0;
			else
				return false;
		}
	}

	public readonly Condition ToCondition(string name)
	{
		if(_minimum == null)
			return _maximum == null ? null : new Condition(name, _maximum, ConditionOperator.LessThanEqual);

		if(_maximum == null)
			return new Condition(name, _minimum, ConditionOperator.GreaterThanEqual);

		if(Comparer<T>.Default.Compare(_minimum.Value, _maximum.Value) == 0)
			return new Condition(name, _minimum, ConditionOperator.Equal);
		else
			return new Condition(name, this, ConditionOperator.Between);
	}
	#endregion

	#region 静态方法
	public static Range<T> Parse(string text) => Parse(text.AsSpan());
	public static Range<T> Parse(ReadOnlySpan<char> text)
	{
		if(text.IsEmpty)
			throw new ArgumentNullException(nameof(text));

		string message;

		if(typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateOnly) || typeof(T) == typeof(DateTimeOffset))
		{
			var duration = DateTimeRangeParser.Parse<T>(text);

			if(duration != null)
			{
				if(duration.Value.IsFailed(out message))
					throw new ArgumentException(message ?? Properties.Resources.InvalidFormat_Message);

				return duration.Value.Range;
			}
		}

		var result = RangeParser.Parse(text);

		if(result.IsFailed(out message))
			throw new ArgumentException(message ?? Properties.Resources.InvalidFormat_Message);

		T? minimum = null, maximum = null;

		if(!result.Minimum.IsEmpty)
			minimum = Common.Convert.ConvertValue<T>(result.Minimum.ToString());

		if(!result.Maximum.IsEmpty)
			maximum = Common.Convert.ConvertValue<T>(result.Maximum.ToString());

		return new Range<T>(minimum, maximum);
	}

	public static bool TryParse(string text, out Range<T> value) => TryParse(text.AsSpan(), out value);
	public static bool TryParse(ReadOnlySpan<char> text, out Range<T> value)
	{
		value = default;

		if(text.IsEmpty)
			return false;

		if(typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateOnly) || typeof(T) == typeof(DateTimeOffset))
		{
			var duration = DateTimeRangeParser.Parse<T>(text);

			if(duration.HasValue)
			{
				if(duration.Value.IsFailed(out _))
					return false;

				value = duration.Value.Range;
				return true;
			}
		}

		var result = RangeParser.Parse(text);

		if(result.IsFailed(out _))
			return false;

		T? minimum = null, maximum = null;

		if(!result.Minimum.IsEmpty)
		{
			if(!Common.Convert.TryConvertValue(result.Minimum.ToString(), out minimum))
				return false;
		}

		if(!result.Maximum.IsEmpty)
		{
			if(!Common.Convert.TryConvertValue(result.Maximum.ToString(), out maximum))
				return false;
		}

		try
		{
			value = new Range<T>(minimum, maximum);
			return true;
		}
		catch(ArgumentException)
		{
			return false;
		}
	}

	static Range<T> IParsable<Range<T>>.Parse(string text, IFormatProvider provider) => Parse(text);
	static bool IParsable<Range<T>>.TryParse(string text, IFormatProvider provider, out Range<T> result) => TryParse(text, out result);
	static Range<T> ISpanParsable<Range<T>>.Parse(ReadOnlySpan<char> text, IFormatProvider provider) => Parse(text);
	static bool ISpanParsable<Range<T>>.TryParse(ReadOnlySpan<char> text, IFormatProvider provider, out Range<T> result) => TryParse(text, out result);
	#endregion

	#region 隐式转换
	public static implicit operator Range<T>(T value) => new(value);
	public static implicit operator Range<T>(ValueTuple<T, T> tuple) => new(tuple.Item1, tuple.Item2);
	#endregion

	#region 私有方法
	private T? EnsureMinimum(T? value)
	{
		if(value != null)
		{
			var to = _maximum;

			if(to != null && Comparer<T>.Default.Compare(to.Value, value.Value) < 0)
			{
				_maximum = value;
				return to;
			}
		}

		return value;
	}

	private T? EnsureMaximum(T? value)
	{
		if(value != null)
		{
			var from = _minimum;

			if(from != null && Comparer<T>.Default.Compare(from.Value, value.Value) > 0)
			{
				_minimum = value;
				return from;
			}
		}

		return value;
	}
	#endregion

	#region 重写方法
	public override string ToString()
	{
		if(_minimum == null)
		{
			if(_maximum == null)
				return string.Empty;
			else
				return string.Format("(?~{0})", _maximum.ToString());
		}
		else
		{
			if(_maximum == null)
				return string.Format("({0}~?)", _minimum.ToString());
			else
				return EqualityComparer<T>.Default.Equals(_minimum.Value, _maximum.Value) ?
					_minimum.ToString() :
					$"({_minimum}~{_maximum})";
		}
	}
	#endregion
}
