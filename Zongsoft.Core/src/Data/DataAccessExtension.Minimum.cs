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

namespace Zongsoft.Data;

partial class DataAccessExtension
{
	#region 静态方法
	public static async ValueTask<TValue?> MinimumAsync<TValue>(this IDataAccess accessor, string name, string member, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.Minimum(member), null, null, null, null, cancellation);
	public static async ValueTask<TValue?> MinimumAsync<TValue>(this IDataAccess accessor, string name, string member, DataAggregateOptions options, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.Minimum(member), null, options, null, null, cancellation);
	public static async ValueTask<TValue?> MinimumAsync<TValue>(this IDataAccess accessor, string name, string member, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.Minimum(member), criteria, null, null, null, cancellation);
	public static async ValueTask<TValue?> MinimumAsync<TValue>(this IDataAccess accessor, string name, string member, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.Minimum(member), criteria, options, null, null, cancellation);
	public static ValueTask<TValue?> MinimumAsync<TValue>(this IDataAccess accessor, string name, string member, ICondition criteria, DataAggregateOptions options, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => MinimumAsync<TValue>(accessor, name, member, criteria, options, aggregating, default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public static async ValueTask<TValue?> MinimumAsync<TValue>(this IDataAccess accessor, string name, string member, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating = null, Func<DataAggregateContextBase, CancellationToken, ValueTask> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.Minimum(member), criteria, options, aggregating, aggregated, cancellation);

	public static async ValueTask<TValue?> MinimumAsync<TValue>(this IDataAccess accessor, string name, string member, bool distinct, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.Minimum(member, distinct), null, null, null, null, cancellation);
	public static async ValueTask<TValue?> MinimumAsync<TValue>(this IDataAccess accessor, string name, string member, bool distinct, DataAggregateOptions options, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.Minimum(member, distinct), null, options, null, null, cancellation);
	public static async ValueTask<TValue?> MinimumAsync<TValue>(this IDataAccess accessor, string name, string member, bool distinct, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.Minimum(member, distinct), criteria, null, null, null, cancellation);
	public static async ValueTask<TValue?> MinimumAsync<TValue>(this IDataAccess accessor, string name, string member, bool distinct, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.Minimum(member, distinct), criteria, options, null, null, cancellation);
	public static ValueTask<TValue?> MinimumAsync<TValue>(this IDataAccess accessor, string name, string member, bool distinct, ICondition criteria, DataAggregateOptions options, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => MinimumAsync<TValue>(accessor, name, member, distinct, criteria, options, aggregating, default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public static async ValueTask<TValue?> MinimumAsync<TValue>(this IDataAccess accessor, string name, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating = null, Func<DataAggregateContextBase, CancellationToken, ValueTask> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.Minimum(member, distinct), criteria, options, aggregating, aggregated, cancellation);

	public static async ValueTask<TValue?> MinimumAsync<T, TValue>(this IDataAccess accessor, string member, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.Minimum(member), null, null, null, null, cancellation);
	public static async ValueTask<TValue?> MinimumAsync<T, TValue>(this IDataAccess accessor, string member, DataAggregateOptions options, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.Minimum(member), null, options, null, null, cancellation);
	public static async ValueTask<TValue?> MinimumAsync<T, TValue>(this IDataAccess accessor, string member, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.Minimum(member), criteria, null, null, null, cancellation);
	public static async ValueTask<TValue?> MinimumAsync<T, TValue>(this IDataAccess accessor, string member, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.Minimum(member), criteria, options, null, null, cancellation);
	public static ValueTask<TValue?> MinimumAsync<T, TValue>(this IDataAccess accessor, string member, ICondition criteria, DataAggregateOptions options, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => MinimumAsync<T, TValue>(accessor, member, criteria, options, aggregating, default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public static async ValueTask<TValue?> MinimumAsync<T, TValue>(this IDataAccess accessor, string member, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating = null, Func<DataAggregateContextBase, CancellationToken, ValueTask> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.Minimum(member), criteria, options, aggregating, aggregated, cancellation);

	public static async ValueTask<TValue?> MinimumAsync<T, TValue>(this IDataAccess accessor, string member, bool distinct, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.Minimum(member, distinct), null, null, null, null, cancellation);
	public static async ValueTask<TValue?> MinimumAsync<T, TValue>(this IDataAccess accessor, string member, bool distinct, DataAggregateOptions options, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.Minimum(member, distinct), null, options, null, null, cancellation);
	public static async ValueTask<TValue?> MinimumAsync<T, TValue>(this IDataAccess accessor, string member, bool distinct, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.Minimum(member, distinct), criteria, null, null, null, cancellation);
	public static async ValueTask<TValue?> MinimumAsync<T, TValue>(this IDataAccess accessor, string member, bool distinct, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.Minimum(member, distinct), criteria, options, null, null, cancellation);
	public static ValueTask<TValue?> MinimumAsync<T, TValue>(this IDataAccess accessor, string member, bool distinct, ICondition criteria, DataAggregateOptions options, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => MinimumAsync<T, TValue>(accessor, member, distinct, criteria, options, aggregating, default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	public static async ValueTask<TValue?> MinimumAsync<T, TValue>(this IDataAccess accessor, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating = null, Func<DataAggregateContextBase, CancellationToken, ValueTask> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.Minimum(member, distinct), criteria, options, aggregating, aggregated, cancellation);
	#endregion
}
