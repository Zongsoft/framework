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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Common;

public abstract class PredicationBase<T> : IPredication<T>, Services.IMatchable
{
	#region 成员字段
	private readonly string _name;
	#endregion

	#region 构造函数
	protected PredicationBase(string name)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		_name = name.Trim();
	}
	#endregion

	#region 公共属性
	public virtual string Name => _name;
	#endregion

	#region 断言方法
	public virtual ValueTask<bool> PredicateAsync(T argument, CancellationToken cancellation = default) => this.PredicateAsync(argument, null, cancellation);
	public abstract ValueTask<bool> PredicateAsync(T argument, Collections.Parameters parameters, CancellationToken cancellation = default);
	ValueTask<bool> IPredication.PredicateAsync(object argument, CancellationToken cancellation) => this.PredicateAsync(this.OnConvert(argument), cancellation);
	ValueTask<bool> IPredication.PredicateAsync(object argument, Collections.Parameters parameters, CancellationToken cancellation) => this.PredicateAsync(this.OnConvert(argument), parameters, cancellation);
	#endregion

	#region 虚拟方法
	protected virtual T OnConvert(object argument) => Zongsoft.Common.Convert.ConvertValue<T>(argument);
	#endregion

	#region 服务匹配
	public virtual bool Match(string argument) => string.Equals(this.Name, argument, StringComparison.OrdinalIgnoreCase);
	bool Services.IMatchable.Match(object argument) => this.Match(argument as string);
	#endregion

	#region 重写方法
	public override string ToString() => $"{this.Name} ({this.GetType().Name})";
	#endregion
}
