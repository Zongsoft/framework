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
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;

namespace Zongsoft.Data
{
	public struct Range<T> where T : struct, IComparable<T>
	{
		#region 单例字段
		public static readonly Range<T> Empty = new Range<T>();
		#endregion

		#region 成员字段
		private T? _minimum;
		private T? _maximum;
		#endregion

		#region 构造函数
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
			get => _minimum;
			set => _minimum = this.EnsureMinimum(value);
		}

		public T? Maximum
		{
			get => _maximum;
			set => _maximum = this.EnsureMaximum(value);
		}

		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public bool HasValue
		{
			get => _minimum.HasValue || _maximum.HasValue;
		}

		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public bool IsEmpty
		{
			get => _minimum == null && _maximum == null;
		}

		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public bool IsZero
		{
			get => (_minimum == null || Comparer<T>.Default.Compare(_minimum.Value, default) == 0) &&
			       (_maximum == null || Comparer<T>.Default.Compare(_maximum.Value, default) == 0);
		}
		#endregion

		#region 公共方法
		public bool Contains(T value)
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

		public Condition ToCondition(string name)
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
		public static Range<T> Parse(string text)
		{
			Range<T> result;

			if(TryParse(text, out result))
				return result;

			throw new ArgumentException(string.Format("Invalid value '{0}' of the argument.", text));
		}

		public static bool TryParse(string text, out Range<T> result)
		{
			result = default(Range<T>);

			if(string.IsNullOrWhiteSpace(text))
				return false;

			text = text.Trim().Trim('(', ')');

			if(string.IsNullOrWhiteSpace(text))
				return false;

			var parts = text.Split('~').Select(p => p.Trim().Trim('?', '*')).ToArray();

			if(!string.IsNullOrWhiteSpace(parts[0]))
			{
				T from;

				if(!Zongsoft.Common.Convert.TryConvertValue(parts[0], out from))
					return false;

				result.Minimum = from;
			}

			if(parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
			{
				T to;

				if(!Zongsoft.Common.Convert.TryConvertValue(parts[1], out to))
					return false;

				result.Maximum = to;
			}

			//最后返回真（即使输出参数为空）
			return true;
		}
		#endregion

		#region 私有方法
		private T? EnsureMinimum(T? value)
		{
			if(value != null)
			{
				var to = _maximum;

				if(to != null && to.Value.CompareTo(value.Value) < 0)
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

				if(from != null && from.Value.CompareTo(value.Value) > 0)
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
					return string.Format("(~{0})", _maximum.ToString());
			}
			else
			{
				if(_maximum == null)
					return string.Format("({0})", _minimum.ToString());
				else
					return string.Format("({0}~{1})", _minimum.ToString(), _maximum.ToString());
			}
		}
		#endregion
	}
}
