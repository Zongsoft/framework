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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data;

partial class DataAccessBase
{
	#region 公共方法
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(function, member, default(ICondition), default(DataAggregateOptions), cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(function, member, criteria, default(DataAggregateOptions), cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), new DataAggregate(function, member), criteria, options, null, null, cancellation);

	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, string alias, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(function, member, alias, default(ICondition), default(DataAggregateOptions), cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, string alias, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(function, member, alias, criteria, default(DataAggregateOptions), cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), new DataAggregate(function, member, alias), criteria, options, null, null, cancellation);

	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, bool distinct, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(function, member, distinct, default(ICondition), default(DataAggregateOptions), cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, bool distinct, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(function, member, distinct, criteria, default(DataAggregateOptions), cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), new DataAggregate(function, member, distinct), criteria, options, null, null, cancellation);

	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, bool distinct, string alias, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(function, member, distinct, alias, default(ICondition), default(DataAggregateOptions), cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, bool distinct, string alias, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(function, member, distinct, alias, criteria, default(DataAggregateOptions), cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), new DataAggregate(function, member, distinct, alias), criteria, options, null, null, cancellation);

	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregate aggregate, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(aggregate, default(ICondition), default(DataAggregateOptions), default(Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregate aggregate, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(aggregate, criteria, default(DataAggregateOptions), default(Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregate aggregate, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(aggregate, criteria, options, default(Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregate aggregate, ICondition criteria, DataAggregateOptions options, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(aggregate, criteria, options, aggregating, default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating = null, Func<DataAggregateContextBase, CancellationToken, ValueTask> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), aggregate, criteria, options, aggregating, aggregated, cancellation);

	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, function, member, default(ICondition), default(DataAggregateOptions), cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, function, member, criteria, default(DataAggregateOptions), cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, new DataAggregate(function, member), criteria, options, null, null, cancellation);

	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, string alias, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, function, member, alias, default(ICondition), default(DataAggregateOptions), cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, string alias, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, function, member, alias, criteria, default(DataAggregateOptions), cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, new DataAggregate(function, member, alias), criteria, options, null, null, cancellation);

	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, bool distinct, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, function, member, distinct, default(ICondition), default(DataAggregateOptions), cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, bool distinct, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, function, member, distinct, criteria, default(DataAggregateOptions), cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, new DataAggregate(function, member, distinct), criteria, options, null, null, cancellation);

	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, bool distinct, string alias, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, function, member, distinct, alias, default(ICondition), default(DataAggregateOptions), cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, bool distinct, string alias, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, function, member, distinct, alias, criteria, default(DataAggregateOptions), cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, new DataAggregate(function, member, distinct, alias), criteria, options, null, null, cancellation);

	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregate aggregate, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, aggregate, default(ICondition), default(DataAggregateOptions), default(Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregate aggregate, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, aggregate, criteria, default(DataAggregateOptions), default(Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregate aggregate, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, aggregate, criteria, options, default(Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregate aggregate, ICondition criteria, DataAggregateOptions options, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, aggregate, criteria, options, aggregating, default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public async ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating = null, Func<DataAggregateContextBase, CancellationToken, ValueTask> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		using var context = this.CreateAggregateContext(name, aggregate, criteria, options);

		//处理数据访问操作前的回调
		if(aggregating != null && await aggregating(context, cancellation))
			return context.GetValue<TValue>();

		//激发“Aggregating”事件，如果被中断则返回
		if(await this.OnAggregatingAsync(context, cancellation))
			return context.GetValue<TValue>();

		//调用数据访问过滤器前事件
		await this.OnFilteringAsync(context, cancellation);

		//执行聚合操作方法
		await this.OnAggregateAsync(context, cancellation);

		//调用数据访问过滤器后事件
		await this.OnFilteredAsync(context, cancellation);

		//激发“Aggregated”事件
		await this.OnAggregatedAsync(context, cancellation);

		//处理数据访问操作后的回调
		if(aggregated != null)
			await aggregated(context, cancellation);

		var value = context.GetValue<TValue>();

		//返回最终的结果
		return value;
	}
	#endregion

	#region 保护方法
	protected abstract ValueTask OnAggregateAsync(DataAggregateContextBase context, CancellationToken cancellation);
	#endregion
}
