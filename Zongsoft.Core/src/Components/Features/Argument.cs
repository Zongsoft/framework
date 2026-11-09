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
