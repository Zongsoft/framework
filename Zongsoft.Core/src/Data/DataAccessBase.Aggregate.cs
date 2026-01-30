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
	public TValue? Aggregate<T, TValue>(DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(this.GetName<T>(), new DataAggregate(function, member), criteria, options, null, null);
	public TValue? Aggregate<T, TValue>(DataAggregateFunction function, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(this.GetName<T>(), new DataAggregate(function, member, alias), criteria, options, null, null);
	public TValue? Aggregate<T, TValue>(DataAggregateFunction function, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(this.GetName<T>(), new DataAggregate(function, member, distinct), criteria, options, null, null);
	public TValue? Aggregate<T, TValue>(DataAggregateFunction function, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(this.GetName<T>(), new DataAggregate(function, member, distinct, alias), criteria, options, null, null);
	public TValue? Aggregate<T, TValue>(DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(this.GetName<T>(), aggregate, criteria, options, aggregating, aggregated);

	public TValue? Aggregate<TValue>(string name, DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(name, new DataAggregate(function, member), criteria, options, null, null);
	public TValue? Aggregate<TValue>(string name, DataAggregateFunction function, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(name, new DataAggregate(function, member, alias), criteria, options, null, null);
	public TValue? Aggregate<TValue>(string name, DataAggregateFunction function, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(name, new DataAggregate(function, member, distinct), criteria, options, null, null);
	public TValue? Aggregate<TValue>(string name, DataAggregateFunction function, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(name, new DataAggregate(function, member, distinct, alias), criteria, options, null, null);
	public TValue? Aggregate<TValue>(string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue>
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateAggregateContext(name, aggregate, criteria, options);

		//处理数据访问操作前的回调
		if(aggregating != null && aggregating(context))
			return context.GetValue<TValue>();

		//激发“Aggregating”事件，如果被中断则返回
		if(this.OnAggregating(context))
			return context.GetValue<TValue>();

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行聚合操作方法
		this.OnAggregate(context);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Aggregated”事件
		this.OnAggregated(context);

		//处理数据访问操作后的回调
		if(aggregated != null)
			aggregated(context);

		var value = context.GetValue<TValue>();

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return value;
	}

	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), new DataAggregate(function, member), criteria, options, null, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), new DataAggregate(function, member, alias), criteria, options, null, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), new DataAggregate(function, member, distinct), criteria, options, null, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), new DataAggregate(function, member, distinct, alias), criteria, options, null, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), aggregate, criteria, options, aggregating, aggregated, cancellation);

	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, new DataAggregate(function, member), criteria, options, null, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, new DataAggregate(function, member, alias), criteria, options, null, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, new DataAggregate(function, member, distinct), criteria, options, null, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, new DataAggregate(function, member, distinct, alias), criteria, options, null, null, cancellation);
	public async ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateAggregateContext(name, aggregate, criteria, options);

		//处理数据访问操作前的回调
		if(aggregating != null && aggregating(context))
			return context.GetValue<TValue>();

		//激发“Aggregating”事件，如果被中断则返回
		if(this.OnAggregating(context))
			return context.GetValue<TValue>();

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行聚合操作方法
		await this.OnAggregateAsync(context, cancellation);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Aggregated”事件
		this.OnAggregated(context);

		//处理数据访问操作后的回调
		if(aggregated != null)
			aggregated(context);

		var value = context.GetValue<TValue>();

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return value;
	}

	protected abstract void OnAggregate(DataAggregateContextBase context);
	protected abstract ValueTask OnAggregateAsync(DataAggregateContextBase context, CancellationToken cancellation);
}
