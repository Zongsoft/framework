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
	public static int Count(this IDataAccess accessor, string name, ICondition criteria = null, DataAggregateOptions options = null) => accessor.Aggregate<int>(name, DataAggregateFunction.Count, null, criteria, options) ?? 0;
	public static int Count(this IDataAccess accessor, string name, bool distinct, ICondition criteria = null, DataAggregateOptions options = null) => accessor.Aggregate<int>(name, DataAggregateFunction.Count, null, distinct, criteria, options) ?? 0;
	public static int Count(this IDataAccess accessor, string name, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) => accessor.Aggregate<int>(name, DataAggregate.Count(null, distinct, alias), criteria, options, aggregating, aggregated) ?? 0;
	public static int Count<T>(this IDataAccess accessor, ICondition criteria = null, DataAggregateOptions options = null) => accessor.Aggregate<T, int>(DataAggregateFunction.Count, null, criteria, options) ?? 0;
	public static int Count<T>(this IDataAccess accessor, bool distinct, ICondition criteria = null, DataAggregateOptions options = null) => accessor.Aggregate<T, int>(DataAggregateFunction.Count, null, distinct, criteria, options) ?? 0;
	public static int Count<T>(this IDataAccess accessor, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) => accessor.Aggregate<T, int>(DataAggregate.Count(null, distinct, alias), criteria, options, aggregating, aggregated) ?? 0;

	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<int>(name, DataAggregateFunction.Count, null, criteria, options, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<int>(name, DataAggregateFunction.Count, null, distinct, criteria, options, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<int>(name, DataAggregateFunction.Count, null, distinct, alias, criteria, options, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<int>(name, DataAggregate.Count(null, distinct, alias), criteria, options, aggregating, aggregated, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<T, int>(DataAggregateFunction.Count, null, criteria, options, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<T, int>(DataAggregateFunction.Count, null, distinct, criteria, options, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<T, int>(DataAggregateFunction.Count, null, distinct, alias, criteria, options, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<T, int>(DataAggregate.Count(null, distinct, alias), criteria, options, aggregating, aggregated, cancellation) ?? 0;

	public static int Count(this IDataAccess accessor, string name, string member, ICondition criteria = null, DataAggregateOptions options = null) => accessor.Aggregate<int>(name, DataAggregateFunction.Count, member, criteria, options) ?? 0;
	public static int Count(this IDataAccess accessor, string name, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null) => accessor.Aggregate<int>(name, DataAggregateFunction.Count, member, alias, criteria, options) ?? 0;
	public static int Count(this IDataAccess accessor, string name, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null) => accessor.Aggregate<int>(name, DataAggregateFunction.Count, member, distinct, criteria, options) ?? 0;
	public static int Count(this IDataAccess accessor, string name, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) => accessor.Aggregate<int>(name, DataAggregate.Count(member, distinct, alias), criteria, options, aggregating, aggregated) ?? 0;
	public static int Count<T>(this IDataAccess accessor, string member, ICondition criteria = null, DataAggregateOptions options = null) => accessor.Aggregate<T, int>(DataAggregateFunction.Count, member, criteria, options) ?? 0;
	public static int Count<T>(this IDataAccess accessor, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null) => accessor.Aggregate<T, int>(DataAggregateFunction.Count, member, alias, criteria, options) ?? 0;
	public static int Count<T>(this IDataAccess accessor, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null) => accessor.Aggregate<T, int>(DataAggregateFunction.Count, member, distinct, criteria, options) ?? 0;
	public static int Count<T>(this IDataAccess accessor, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) => accessor.Aggregate<T, int>(DataAggregate.Count(member, distinct, alias), criteria, options, aggregating, aggregated) ?? 0;

	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<int>(name, DataAggregateFunction.Count, member, criteria, options, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<int>(name, DataAggregateFunction.Count, member, alias, criteria, options, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<int>(name, DataAggregateFunction.Count, member, distinct, criteria, options, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<int>(name, DataAggregate.Count(member, distinct, alias), criteria, options, aggregating, aggregated, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<T, int>(DataAggregateFunction.Count, member, criteria, options, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<T, int>(DataAggregateFunction.Count, member, alias, criteria, options, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<T, int>(DataAggregateFunction.Count, member, distinct, criteria, options, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<T, int>(DataAggregate.Count(member, distinct, alias), criteria, options, aggregating, aggregated, cancellation) ?? 0;
}
