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

partial class DataAccessExtension
{
	public static TValue? Sum<TValue>(this IDataAccess accessor, string name, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => accessor.Aggregate<TValue>(name, DataAggregateFunction.Sum, member, criteria, options);
	public static TValue? Sum<TValue>(this IDataAccess accessor, string name, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => accessor.Aggregate<TValue>(name, DataAggregateFunction.Sum, member, alias, criteria, options);
	public static TValue? Sum<TValue>(this IDataAccess accessor, string name, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => accessor.Aggregate<TValue>(name, DataAggregateFunction.Sum, member, distinct, criteria, options);
	public static TValue? Sum<TValue>(this IDataAccess accessor, string name, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => accessor.Aggregate<TValue>(name, DataAggregate.Sum(member, distinct, alias), criteria, options, aggregating, aggregated);
	public static TValue? Sum<T, TValue>(this IDataAccess accessor, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => accessor.Aggregate<T, TValue>(DataAggregateFunction.Sum, member, criteria, options);
	public static TValue? Sum<T, TValue>(this IDataAccess accessor, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => accessor.Aggregate<T, TValue>(DataAggregateFunction.Sum, member, alias, criteria, options);
	public static TValue? Sum<T, TValue>(this IDataAccess accessor, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => accessor.Aggregate<T, TValue>(DataAggregateFunction.Sum, member, distinct, criteria, options);
	public static TValue? Sum<T, TValue>(this IDataAccess accessor, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => accessor.Aggregate<T, TValue>(DataAggregate.Sum(member, distinct, alias), criteria, options, aggregating, aggregated);

	public static ValueTask<TValue?> SumAsync<TValue>(this IDataAccess accessor, string name, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => accessor.AggregateAsync<TValue>(name, DataAggregateFunction.Sum, member, criteria, options, cancellation);
	public static ValueTask<TValue?> SumAsync<TValue>(this IDataAccess accessor, string name, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => accessor.AggregateAsync<TValue>(name, DataAggregateFunction.Sum, member, alias, criteria, options, cancellation);
	public static ValueTask<TValue?> SumAsync<TValue>(this IDataAccess accessor, string name, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => accessor.AggregateAsync<TValue>(name, DataAggregateFunction.Sum, member, distinct, criteria, options, cancellation);
	public static ValueTask<TValue?> SumAsync<TValue>(this IDataAccess accessor, string name, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => accessor.AggregateAsync<TValue>(name, DataAggregate.Sum(member, distinct, alias), criteria, options, aggregating, aggregated, cancellation);
	public static ValueTask<TValue?> SumAsync<T, TValue>(this IDataAccess accessor, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => accessor.AggregateAsync<T, TValue>(DataAggregateFunction.Sum, member, criteria, options, cancellation);
	public static ValueTask<TValue?> SumAsync<T, TValue>(this IDataAccess accessor, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => accessor.AggregateAsync<T, TValue>(DataAggregateFunction.Sum, member, alias, criteria, options, cancellation);
	public static ValueTask<TValue?> SumAsync<T, TValue>(this IDataAccess accessor, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => accessor.AggregateAsync<T, TValue>(DataAggregateFunction.Sum, member, distinct, criteria, options, cancellation);
	public static ValueTask<TValue?> SumAsync<T, TValue>(this IDataAccess accessor, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => accessor.AggregateAsync<T, TValue>(DataAggregate.Sum(member, distinct, alias), criteria, options, aggregating, aggregated, cancellation);
}
