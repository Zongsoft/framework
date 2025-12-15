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
	public static TValue? VariancePopulation<TValue>(this IDataAccess accessor, string name, string member, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => accessor.Aggregate<TValue>(name, DataAggregate.VariancePopulation(member), criteria, options, aggregating, aggregated);
	public static TValue? VariancePopulation<TValue>(this IDataAccess accessor, string name, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => accessor.Aggregate<TValue>(name, DataAggregate.VariancePopulation(member, distinct), criteria, options, aggregating, aggregated);
	public static TValue? VariancePopulation<T, TValue>(this IDataAccess accessor, string member, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => accessor.Aggregate<T, TValue>(DataAggregate.VariancePopulation(member), criteria, options, aggregating, aggregated);
	public static TValue? VariancePopulation<T, TValue>(this IDataAccess accessor, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => accessor.Aggregate<T, TValue>(DataAggregate.VariancePopulation(member, distinct), criteria, options, aggregating, aggregated);

	public static async ValueTask<TValue?> VariancePopulationAsync<TValue>(this IDataAccess accessor, string name, string member, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.VariancePopulation(member), null, null, null, null, cancellation);
	public static async ValueTask<TValue?> VariancePopulationAsync<TValue>(this IDataAccess accessor, string name, string member, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.VariancePopulation(member), null, options, null, null, cancellation);
	public static async ValueTask<TValue?> VariancePopulationAsync<TValue>(this IDataAccess accessor, string name, string member, ICondition criteria, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.VariancePopulation(member), criteria, null, null, null, cancellation);
	public static async ValueTask<TValue?> VariancePopulationAsync<TValue>(this IDataAccess accessor, string name, string member, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.VariancePopulation(member), criteria, options, null, null, cancellation);
	public static async ValueTask<TValue?> VariancePopulationAsync<TValue>(this IDataAccess accessor, string name, string member, bool distinct, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.VariancePopulation(member, distinct), null, null, null, null, cancellation);
	public static async ValueTask<TValue?> VariancePopulationAsync<TValue>(this IDataAccess accessor, string name, string member, bool distinct, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.VariancePopulation(member, distinct), null, options, null, null, cancellation);
	public static async ValueTask<TValue?> VariancePopulationAsync<TValue>(this IDataAccess accessor, string name, string member, bool distinct, ICondition criteria, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.VariancePopulation(member, distinct), criteria, null, null, null, cancellation);
	public static async ValueTask<TValue?> VariancePopulationAsync<TValue>(this IDataAccess accessor, string name, string member, bool distinct, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.VariancePopulation(member, distinct), criteria, options, null, null, cancellation);
	public static async ValueTask<TValue?> VariancePopulationAsync<TValue>(this IDataAccess accessor, string name, string member, bool distinct, ICondition criteria, DataAggregateOptions options, Func<DataAggregateContextBase, bool> aggregating, Action<DataAggregateContextBase> aggregated, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<TValue>(name, DataAggregate.VariancePopulation(member, distinct), criteria, options, aggregating, aggregated, cancellation);

	public static async ValueTask<TValue?> VariancePopulationAsync<T, TValue>(this IDataAccess accessor, string member, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.VariancePopulation(member), null, null, null, null, cancellation);
	public static async ValueTask<TValue?> VariancePopulationAsync<T, TValue>(this IDataAccess accessor, string member, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.VariancePopulation(member), null, options, null, null, cancellation);
	public static async ValueTask<TValue?> VariancePopulationAsync<T, TValue>(this IDataAccess accessor, string member, ICondition criteria, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.VariancePopulation(member), criteria, null, null, null, cancellation);
	public static async ValueTask<TValue?> VariancePopulationAsync<T, TValue>(this IDataAccess accessor, string member, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.VariancePopulation(member), criteria, options, null, null, cancellation);
	public static async ValueTask<TValue?> VariancePopulationAsync<T, TValue>(this IDataAccess accessor, string member, bool distinct, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.VariancePopulation(member, distinct), null, null, null, null, cancellation);
	public static async ValueTask<TValue?> VariancePopulationAsync<T, TValue>(this IDataAccess accessor, string member, bool distinct, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.VariancePopulation(member, distinct), null, options, null, null, cancellation);
	public static async ValueTask<TValue?> VariancePopulationAsync<T, TValue>(this IDataAccess accessor, string member, bool distinct, ICondition criteria, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.VariancePopulation(member, distinct), criteria, null, null, null, cancellation);
	public static async ValueTask<TValue?> VariancePopulationAsync<T, TValue>(this IDataAccess accessor, string member, bool distinct, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.VariancePopulation(member, distinct), criteria, options, null, null, cancellation);
	public static async ValueTask<TValue?> VariancePopulationAsync<T, TValue>(this IDataAccess accessor, string member, bool distinct, ICondition criteria, DataAggregateOptions options, Func<DataAggregateContextBase, bool> aggregating, Action<DataAggregateContextBase> aggregated, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => await accessor.AggregateAsync<T, TValue>(DataAggregate.VariancePopulation(member, distinct), criteria, options, aggregating, aggregated, cancellation);
}
