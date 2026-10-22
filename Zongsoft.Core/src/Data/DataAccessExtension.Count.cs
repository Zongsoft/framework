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
	#region 静态方法
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating = null, Func<DataAggregateContextBase, CancellationToken, ValueTask> aggregated = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<int>(name, DataAggregate.Count(null), criteria, options, aggregating, aggregated, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating = null, Func<DataAggregateContextBase, CancellationToken, ValueTask> aggregated = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<T, int>(DataAggregate.Count(null), criteria, options, aggregating, aggregated, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, string member, DataAggregateOptions options, CancellationToken cancellation) => await accessor.AggregateAsync<int>(name, DataAggregate.Count(member), null, options, null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, string member, bool distinct, DataAggregateOptions options, CancellationToken cancellation) => await accessor.AggregateAsync<int>(name, DataAggregate.Count(member, distinct), null, options, null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating = null, Func<DataAggregateContextBase, CancellationToken, ValueTask> aggregated = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<int>(name, DataAggregate.Count(member, distinct), criteria, options, aggregating, aggregated, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, string member, DataAggregateOptions options, CancellationToken cancellation) => await accessor.AggregateAsync<T, int>(DataAggregate.Count(member), null, options, null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, string member, bool distinct, DataAggregateOptions options, CancellationToken cancellation) => await accessor.AggregateAsync<T, int>(DataAggregate.Count(member, distinct), null, options, null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating = null, Func<DataAggregateContextBase, CancellationToken, ValueTask> aggregated = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<T, int>(DataAggregate.Count(member, distinct), criteria, options, aggregating, aggregated, cancellation) ?? 0;
	#endregion

	#region 便利重载
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, string member, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating = null, Func<DataAggregateContextBase, CancellationToken, ValueTask> aggregated = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<int>(name, DataAggregate.Count(member), criteria, options, aggregating, aggregated, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, string member, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating = null, Func<DataAggregateContextBase, CancellationToken, ValueTask> aggregated = null, CancellationToken cancellation = default) => await accessor.AggregateAsync<T, int>(DataAggregate.Count(member), criteria, options, aggregating, aggregated, cancellation) ?? 0;
	#endregion

	#region 取消重载
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, CancellationToken cancellation) => await accessor.AggregateAsync<int>(name, DataAggregateFunction.Count, null, (ICondition)null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, ICondition criteria, CancellationToken cancellation) => await accessor.AggregateAsync<int>(name, DataAggregateFunction.Count, null, criteria, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) => await accessor.AggregateAsync<int>(name, DataAggregateFunction.Count, null, criteria, options, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, CancellationToken cancellation) => await accessor.AggregateAsync<T, int>(DataAggregateFunction.Count, null, (ICondition)null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, ICondition criteria, CancellationToken cancellation) => await accessor.AggregateAsync<T, int>(DataAggregateFunction.Count, null, criteria, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) => await accessor.AggregateAsync<T, int>(DataAggregateFunction.Count, null, criteria, options, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, string member, CancellationToken cancellation) => await accessor.AggregateAsync<int>(name, DataAggregate.Count(member), null, null, null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, string member, ICondition criteria, CancellationToken cancellation) => await accessor.AggregateAsync<int>(name, DataAggregate.Count(member), criteria, null, null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, string member, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) => await accessor.AggregateAsync<int>(name, DataAggregate.Count(member), criteria, options, null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, string member, bool distinct, CancellationToken cancellation) => await accessor.AggregateAsync<int>(name, DataAggregate.Count(member, distinct), null, null, null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, string member, bool distinct, ICondition criteria, CancellationToken cancellation) => await accessor.AggregateAsync<int>(name, DataAggregate.Count(member, distinct), criteria, null, null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync(this IDataAccess accessor, string name, string member, bool distinct, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) => await accessor.AggregateAsync<int>(name, DataAggregate.Count(member, distinct), criteria, options, null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, string member, CancellationToken cancellation) => await accessor.AggregateAsync<T, int>(DataAggregate.Count(member), null, null, null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, string member, ICondition criteria, CancellationToken cancellation) => await accessor.AggregateAsync<T, int>(DataAggregate.Count(member), criteria, null, null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, string member, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) => await accessor.AggregateAsync<T, int>(DataAggregate.Count(member), criteria, options, null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, string member, bool distinct, CancellationToken cancellation) => await accessor.AggregateAsync<T, int>(DataAggregate.Count(member, distinct), null, null, null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, string member, bool distinct, ICondition criteria, CancellationToken cancellation) => await accessor.AggregateAsync<T, int>(DataAggregate.Count(member, distinct), criteria, null, null, null, cancellation) ?? 0;
	public static async ValueTask<int> CountAsync<T>(this IDataAccess accessor, string member, bool distinct, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) => await accessor.AggregateAsync<T, int>(DataAggregate.Count(member, distinct), criteria, options, null, null, cancellation) ?? 0;
	#endregion

	#region 便利取消
	public static ValueTask<int> CountAsync(this IDataAccess accessor, string name, ICondition criteria, DataAggregateOptions options, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating, CancellationToken cancellation) => CountAsync(accessor, name, criteria, options, aggregating, default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public static ValueTask<int> CountAsync<T>(this IDataAccess accessor, ICondition criteria, DataAggregateOptions options, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating, CancellationToken cancellation) => CountAsync<T>(accessor, criteria, options, aggregating, default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public static ValueTask<int> CountAsync(this IDataAccess accessor, string name, string member, bool distinct, ICondition criteria, DataAggregateOptions options, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating, CancellationToken cancellation) => CountAsync(accessor, name, member, distinct, criteria, options, aggregating, default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public static ValueTask<int> CountAsync<T>(this IDataAccess accessor, string member, bool distinct, ICondition criteria, DataAggregateOptions options, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating, CancellationToken cancellation) => CountAsync<T>(accessor, member, distinct, criteria, options, aggregating, default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public static ValueTask<int> CountAsync(this IDataAccess accessor, string name, string member, ICondition criteria, DataAggregateOptions options, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating, CancellationToken cancellation) => CountAsync(accessor, name, member, criteria, options, aggregating, default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public static ValueTask<int> CountAsync<T>(this IDataAccess accessor, string member, ICondition criteria, DataAggregateOptions options, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating, CancellationToken cancellation) => CountAsync<T>(accessor, member, criteria, options, aggregating, default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	#endregion
}
