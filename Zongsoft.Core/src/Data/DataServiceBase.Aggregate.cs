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

partial class DataServiceBase<TModel>
{
	public TValue? Aggregate<TValue>(DataAggregate aggregate, string key, DataAggregateOptions options = null)
		where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key, options, out _), options);
	public TValue? Aggregate<TKey1, TValue>(DataAggregate aggregate, TKey1 key1, DataAggregateOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, options, out _), options);
	public TValue? Aggregate<TKey1, TKey2, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, DataAggregateOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, options, out _), options);
	public TValue? Aggregate<TKey1, TKey2, TKey3, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, key3, options, out _), options);
	public TValue? Aggregate<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, key3, key4, options, out _), options);
	public TValue? Aggregate<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5>
		where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, key3, key4, key5, options, out _), options);

	public TValue? Aggregate<TValue>(DataAggregate aggregate, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(aggregate, (ICondition)null, options);
	public TValue? Aggregate<TValue>(DataAggregate aggregate, ICondition criteria, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue>
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataAggregateOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Aggregate(aggregate.Function), options);

		//修整查询条件
		criteria = this.OnValidate(DataServiceMethod.Aggregate(aggregate.Function), criteria, options);

		//执行聚合操作
		return this.OnAggregate<TValue>(aggregate, criteria, options);
	}

	public TValue? Aggregate<TValue>(DataAggregate aggregate, Data.Condition criteria, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(aggregate, (ICondition)criteria, options);
	public TValue? Aggregate<TValue>(DataAggregate aggregate, ConditionCollection criteria, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(aggregate, (ICondition)criteria, options);

	protected virtual TValue? OnAggregate<TValue>(DataAggregate aggregate, ICondition criteria, DataAggregateOptions options) where TValue : struct, IEquatable<TValue>
	{
		return this.DataAccess.Aggregate<TValue>(this.Name, aggregate, criteria, options, ctx => this.OnAggregating(ctx), ctx => this.OnAggregated(ctx));
	}

	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, string key, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key, null, out _), null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, string key, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key, options, out _), options, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TValue>(DataAggregate aggregate, TKey1 key1, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, null, out _), null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TValue>(DataAggregate aggregate, TKey1 key1, DataAggregateOptions options, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, options, out _), options, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, null, out _), null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, DataAggregateOptions options, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, options, out _), options, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, key3, null, out _), null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, key3, options, out _), options, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, key3, key4, null, out _), null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, key3, key4, options, out _), options, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, key3, key4, key5, null, out _), null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, key3, key4, key5, options, out _), options, cancellation);

	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, (ICondition)null, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, (ICondition)null, options, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, ICondition criteria, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, criteria, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataAggregateOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Aggregate(aggregate.Function), options);

		//修整查询条件
		criteria = this.OnValidate(DataServiceMethod.Aggregate(aggregate.Function), criteria, options);

		//执行聚合操作
		return this.OnAggregateAsync<TValue>(aggregate, criteria, options, cancellation);
	}

	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, Data.Condition criteria, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, (ICondition)criteria, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, Data.Condition criteria, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, (ICondition)criteria, options, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, ConditionCollection criteria, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, (ICondition)criteria, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, ConditionCollection criteria, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, (ICondition)criteria, options, cancellation);

	protected virtual ValueTask<TValue?> OnAggregateAsync<TValue>(DataAggregate aggregate, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) where TValue : struct, IEquatable<TValue>
	{
		return this.DataAccess.AggregateAsync<TValue>(this.Name, aggregate, criteria, options, ctx => this.OnAggregating(ctx), ctx => this.OnAggregated(ctx), cancellation);
	}
}
