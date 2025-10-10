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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Components;

public sealed class CommandArgumentCollection(params IEnumerable<string> arguments) : IReadOnlyCollection<string>
{
	#region 成员字段
	private readonly string[] _arguments = arguments is string[] array ? array : [.. arguments];
	#endregion

	#region 公共属性
	public int Count => _arguments.Length;
	public bool IsEmpty => _arguments.Length == 0;
	public string this[int index] => index >= 0 && index < _arguments.Length ? _arguments[index] : null;
	#endregion

	#region 公共方法
	public bool Contains(string value, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
	{
		if(string.IsNullOrEmpty(value))
			return false;

		for(int i = 0; i < _arguments.Length; i++)
		{
			if(string.Equals(_arguments[i], value, comparison))
				return true;
		}

		return false;
	}

	public bool TryGetValue<T>(int index, out T value)
	{
		if(index < 0 || index >= _arguments.Length)
		{
			value = default;
			return false;
		}

		return Common.Convert.TryConvertValue<T>(_arguments[index], out value);
	}

	public bool TryGetValue(int index, Type type, out object value)
	{
		if(index < 0 || index >= _arguments.Length)
		{
			value = default;
			return false;
		}

		return Common.Convert.TryConvertValue(_arguments[index], type, out value);
	}

	public T GetValue<T>(int index, T defaultValue = default)
	{
		if(index < 0 || index >= _arguments.Length)
			return defaultValue;

		return Common.Convert.ConvertValue<T>(_arguments[index], defaultValue);
	}
	#endregion

	#region 隐式转换
	public static implicit operator string[](CommandArgumentCollection arguments) => arguments?._arguments;
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<string> GetEnumerator()
	{
		for(int i = 0; i < _arguments.Length; i++)
			yield return _arguments[i];
	}
	#endregion
}
