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

namespace Zongsoft.Components.Features;

public class Argument(Exception exception = null)
{
	#region 成员字段
	private Exception _exception = exception;
	#endregion

	#region 公共方法
	public void Error(Exception exception) => _exception = exception;
	public bool HasError<TException>() where TException : Exception => _exception is TException;
	public bool HasError<TException>(out TException exception) where TException : Exception => (exception = _exception as TException) is not null;
	public bool HasError(out Exception exception) => (exception = _exception) is not null;
	#endregion

	#region 重写方法
	public override string ToString() => this.HasError(out var exception) ? $"[{exception.GetType().Name}] {exception.Message}" : base.ToString();
	#endregion
}

public class Argument<T> : Argument
{
	#region 构造函数
	public Argument(Exception exception) : base(exception) => this.Value = default;
	public Argument(T value, Exception exception = null) : base(exception) => this.Value = value;
	#endregion

	#region 公共属性
	public T Value { get; }
	#endregion

	#region 重写方法
	public override string ToString() => this.HasError(out var exception) ? $"[{exception.GetType().Name}] {exception.Message}" : $"{this.Value}";
	#endregion
}

public class Argument<T, TResult> : Argument<T>
{
	#region 构造函数
	public Argument(Exception exception) : base(exception) => this.Result = default;
	public Argument(T value, Exception exception = null) : base(value, exception) => this.Result = default;
	public Argument(T value, TResult result, Exception exception = null) : base(value, exception) => this.Result = result;
	#endregion

	#region 公共属性
	public TResult Result { get; set; }
	#endregion

	#region 重写方法
	public override string ToString() => this.HasError(out var exception) ? $"[{exception.GetType().Name}] {exception.Message}" : $"{this.Result}";
	#endregion
}
