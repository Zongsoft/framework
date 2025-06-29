﻿/*
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

namespace Zongsoft.Components.Features;

public class Argument
{
	#region 构造函数
	public Argument(Exception exception)
	{
		this.Value = null;
		this.Exception = exception;
	}

	public Argument(object value, Exception exception = null)
	{
		this.Value = value;
		this.Exception = exception;
	}
	#endregion

	#region 公共属性
	public object Value { get; }
	public Exception Exception { get; }
	#endregion

	#region 公共方法
	public bool HasException(out Exception exception) => (exception = this.Exception) is not null;
	#endregion

	#region 重写方法
	public override string ToString() => this.HasException(out var exception) ? $"[{exception.GetType().Name}] {exception.Message}" : $"{this.Value}";
	#endregion
}

public class Argument<T>
{
	#region 构造函数
	public Argument(Exception exception)
	{
		this.Value = default;
		this.Exception = exception;
	}

	public Argument(T value, Exception exception = null)
	{
		this.Value = value;
		this.Exception = exception;
	}
	#endregion

	#region 公共属性
	public T Value { get; }
	public Exception Exception { get; }
	#endregion

	#region 公共方法
	public bool HasException(out Exception exception) => (exception = this.Exception) is not null;
	#endregion

	#region 重写方法
	public override string ToString() => this.HasException(out var exception) ? $"[{exception.GetType().Name}] {exception.Message}" : $"{this.Value}";
	#endregion

	#region 类型转换
	public static implicit operator Argument(Argument<T> argument) => argument is null ? default : new(argument.Value, argument.Exception);
	public static explicit operator Argument<T>(Argument argument) => argument is null ? default : (argument.Value is T value ? new(value, argument.Exception) : throw new InvalidCastException());
	#endregion
}
