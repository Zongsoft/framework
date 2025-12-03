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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data;

public static class DataAccessExtension
{
	#region 聚合方法
	public static TValue? Count<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.Count, member, criteria, options);
	public static TValue? Sum<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.Sum, member, criteria, options);
	public static TValue? Average<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.Average, member, criteria, options);
	public static TValue? Median<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.Median, member, criteria, options);
	public static TValue? Maximum<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.Maximum, member, criteria, options);
	public static TValue? Minimum<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.Minimum, member, criteria, options);
	public static TValue? Deviation<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.Deviation, member, criteria, options);
	public static TValue? DeviationPopulation<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.DeviationPopulation, member, criteria, options);
	public static TValue? Variance<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.Variance, member, criteria, options);
	public static TValue? VariancePopulation<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.VariancePopulation, member, criteria, options);

	public static TValue? Count<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
	public static TValue? Sum<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
	public static TValue? Average<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
	public static TValue? Median<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
	public static TValue? Maximum<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
	public static TValue? Minimum<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
	public static TValue? Deviation<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
	public static TValue? DeviationPopulation<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
	public static TValue? Variance<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
	public static TValue? VariancePopulation<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
	#endregion

	#region 递增方法
	public static long Increase<T>(this IDataAccess dataAccess, string member, ICondition criteria, DataUpdateOptions options = null) => Increase<T>(dataAccess, member, criteria, 1, options);
	public static long Increase<T>(this IDataAccess dataAccess, string member, ICondition criteria, int interval, DataUpdateOptions options = null) => Increase(dataAccess, Model.Naming.Get<T>(), member, criteria, interval, options);
	public static long Increase(this IDataAccess dataAccess, string name, string member, ICondition criteria, DataUpdateOptions options = null) => Increase(dataAccess, name, member, criteria, 1, options);
	public static long Increase(this IDataAccess dataAccess, string name, string member, ICondition criteria, int interval, DataUpdateOptions options = null)
	{
		if(options == null)
			options = DataUpdateOptions.Return([member]);
		else
			options.Returning.Newer.TryAdd(member, null);

		var field = (interval > 0 ? Operand.Field(member) + interval : Operand.Field(member) - (-interval));

		if(dataAccess.Update(name, new Dictionary<string, object>([new KeyValuePair<string, object>(member, field)]), criteria, options) > 0)
			return options.Returning.Newer.TryGetValue(member, out var value) ? Zongsoft.Common.Convert.ConvertValue<long>(value) : 0L;

		return 0L;
	}

	public static ValueTask<long> IncreaseAsync<T>(this IDataAccess dataAccess, string member, ICondition criteria, CancellationToken cancellation = default) => IncreaseAsync<T>(dataAccess, member, criteria, 1, null, cancellation);
	public static ValueTask<long> IncreaseAsync<T>(this IDataAccess dataAccess, string member, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync<T>(dataAccess, member, criteria, 1, options, cancellation);
	public static ValueTask<long> IncreaseAsync<T>(this IDataAccess dataAccess, string member, ICondition criteria, int interval, CancellationToken cancellation = default) => IncreaseAsync<T>(dataAccess, member, criteria, interval, null, cancellation);
	public static ValueTask<long> IncreaseAsync<T>(this IDataAccess dataAccess, string member, ICondition criteria, int interval, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync(dataAccess, Model.Naming.Get<T>(), member, criteria, interval, options, cancellation);
	public static ValueTask<long> IncreaseAsync(this IDataAccess dataAccess, string name, string member, ICondition criteria, CancellationToken cancellation = default) => IncreaseAsync(dataAccess, name, member, criteria, 1, null, cancellation);
	public static ValueTask<long> IncreaseAsync(this IDataAccess dataAccess, string name, string member, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync(dataAccess, name, member, criteria, 1, options, cancellation);
	public static ValueTask<long> IncreaseAsync(this IDataAccess dataAccess, string name, string member, ICondition criteria, int interval, CancellationToken cancellation = default) => IncreaseAsync(dataAccess, name, member, criteria, interval, null, cancellation);
	public static async ValueTask<long> IncreaseAsync(this IDataAccess dataAccess, string name, string member, ICondition criteria, int interval, DataUpdateOptions options, CancellationToken cancellation = default)
	{
		if(options == null)
			options = DataUpdateOptions.Return([member]);
		else
			options.Returning.Newer.TryAdd(member, null);

		var field = (interval > 0 ? Operand.Field(member) + interval : Operand.Field(member) - (-interval));

		if(await dataAccess.UpdateAsync(name, new Dictionary<string, object>([new KeyValuePair<string, object>(member, field)]), criteria, options, cancellation) > 0)
			return options.Returning.Newer.TryGetValue(member, out var value) ? Zongsoft.Common.Convert.ConvertValue<long>(value) : 0L;

		return 0L;
	}
	#endregion

	#region 递减方法
	public static long Decrease<T>(this IDataAccess dataAccess, string member, ICondition criteria, DataUpdateOptions options = null) => Increase<T>(dataAccess, member, criteria, -1, options);
	public static long Decrease<T>(this IDataAccess dataAccess, string member, ICondition criteria, int interval, DataUpdateOptions options = null) => Increase<T>(dataAccess, member, criteria, -interval, options);
	public static long Decrease(this IDataAccess dataAccess, string name, string member, ICondition criteria, DataUpdateOptions options = null) => Increase(dataAccess, name, member, criteria, -1, options);
	public static long Decrease(this IDataAccess dataAccess, string name, string member, ICondition criteria, int interval, DataUpdateOptions options = null) => Increase(dataAccess, name, member, criteria, -interval, options);
	public static ValueTask<long> DecreaseAsync<T>(this IDataAccess dataAccess, string member, ICondition criteria, CancellationToken cancellation = default) => IncreaseAsync<T>(dataAccess, member, criteria, -1, null, cancellation);
	public static ValueTask<long> DecreaseAsync<T>(this IDataAccess dataAccess, string member, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync<T>(dataAccess, member, criteria, -1, options, cancellation);
	public static ValueTask<long> DecreaseAsync<T>(this IDataAccess dataAccess, string member, ICondition criteria, int interval, CancellationToken cancellation = default) => IncreaseAsync<T>(dataAccess, member, criteria, -interval, null, cancellation);
	public static ValueTask<long> DecreaseAsync<T>(this IDataAccess dataAccess, string member, ICondition criteria, int interval, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync<T>(dataAccess, member, criteria, -interval, options, cancellation);
	public static ValueTask<long> DecreaseAsync(this IDataAccess dataAccess, string name, string member, ICondition criteria, CancellationToken cancellation = default) => IncreaseAsync(dataAccess, name, member, criteria, -1, null, cancellation);
	public static ValueTask<long> DecreaseAsync(this IDataAccess dataAccess, string name, string member, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync(dataAccess, name, member, criteria, -1, options, cancellation);
	public static ValueTask<long> DecreaseAsync(this IDataAccess dataAccess, string name, string member, ICondition criteria, int interval, CancellationToken cancellation = default) => IncreaseAsync(dataAccess, name, member, criteria, -interval, null, cancellation);
	public static ValueTask<long> DecreaseAsync(this IDataAccess dataAccess, string name, string member, ICondition criteria, int interval, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync(dataAccess, name, member, criteria, -interval, options, cancellation);
	#endregion

	#region 批量更新
	public static int UpdateMany<T>(this IDataAccess dataAccess, IEnumerable<T> items) => UpdateMany(dataAccess, Model.Naming.Get<T>(), items, string.Empty, null, null, null);
	public static int UpdateMany<T>(this IDataAccess dataAccess, IEnumerable<T> items, DataUpdateOptions options) => UpdateMany(dataAccess, Model.Naming.Get<T>(), items, string.Empty, options, null, null);
	public static int UpdateMany<T>(this IDataAccess dataAccess, IEnumerable<T> items, string schema) => UpdateMany(dataAccess, Model.Naming.Get<T>(), items, schema, null, null, null);
	public static int UpdateMany<T>(this IDataAccess dataAccess, IEnumerable<T> items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null) => UpdateMany(dataAccess, Model.Naming.Get<T>(), items, schema, options, updating, updated);
	public static int UpdateMany<T>(this IDataAccess dataAccess, IEnumerable items) => UpdateMany(dataAccess, Model.Naming.Get<T>(), items, string.Empty, null, null, null);
	public static int UpdateMany<T>(this IDataAccess dataAccess, IEnumerable items, DataUpdateOptions options) => UpdateMany(dataAccess, Model.Naming.Get<T>(), items, string.Empty, options, null, null);
	public static int UpdateMany<T>(this IDataAccess dataAccess, IEnumerable items, string schema) => UpdateMany(dataAccess, Model.Naming.Get<T>(), items, schema, null, null, null);
	public static int UpdateMany<T>(this IDataAccess dataAccess, IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null) => UpdateMany(dataAccess, Model.Naming.Get<T>(), items, schema, options, updating, updated);

	public static int UpdateMany(this IDataAccess dataAccess, string name, IEnumerable items) => UpdateMany(dataAccess, name, items, string.Empty, null, null, null);
	public static int UpdateMany(this IDataAccess dataAccess, string name, IEnumerable items, DataUpdateOptions options) => UpdateMany(dataAccess, name, items, string.Empty, options, null, null);
	public static int UpdateMany(this IDataAccess dataAccess, string name, IEnumerable items, string schema) => UpdateMany(dataAccess, name, items, schema, null, null, null);
	public static int UpdateMany(this IDataAccess dataAccess, string name, IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(items == null)
			return 0;

		var transaction = new Zongsoft.Transactions.Transaction();

		try
		{
			int count = 0;

			foreach(var item in items)
				count += dataAccess.Update(name, item, (ICondition)null, schema, options, updating, updated);

			//提交事务
			transaction.Commit();

			//返回受影响的记录数
			return count;
		}
		catch
		{
			//回滚事务
			transaction.Rollback();

			throw;
		}
	}

	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess dataAccess, IEnumerable<T> items, CancellationToken cancellation = default) =>
		UpdateManyAsync(dataAccess, Model.Naming.Get<T>(), items, string.Empty, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess dataAccess, IEnumerable<T> items, DataUpdateOptions options, CancellationToken cancellation = default) =>
		UpdateManyAsync(dataAccess, Model.Naming.Get<T>(), items, string.Empty, options, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess dataAccess, IEnumerable<T> items, string schema, CancellationToken cancellation = default) =>
		UpdateManyAsync(dataAccess, Model.Naming.Get<T>(), items, schema, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess dataAccess, IEnumerable<T> items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default) =>
		UpdateManyAsync(dataAccess, Model.Naming.Get<T>(), items, schema, options, updating, updated, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess dataAccess, IEnumerable items, CancellationToken cancellation = default) =>
		UpdateManyAsync(dataAccess, Model.Naming.Get<T>(), items, string.Empty, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess dataAccess, IEnumerable items, DataUpdateOptions options, CancellationToken cancellation = default) =>
		UpdateManyAsync(dataAccess, Model.Naming.Get<T>(), items, string.Empty, options, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess dataAccess, IEnumerable items, string schema, CancellationToken cancellation = default) =>
		UpdateManyAsync(dataAccess, Model.Naming.Get<T>(), items, schema, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess dataAccess, IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default) =>
		UpdateManyAsync(dataAccess, Model.Naming.Get<T>(), items, schema, options, updating, updated, cancellation);

	public static ValueTask<int> UpdateManyAsync(this IDataAccess dataAccess, string name, IEnumerable items, CancellationToken cancellation = default) =>
		UpdateManyAsync(dataAccess, name, items, string.Empty, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync(this IDataAccess dataAccess, string name, IEnumerable items, DataUpdateOptions options, CancellationToken cancellation = default) =>
		UpdateManyAsync(dataAccess, name, items, string.Empty, options, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync(this IDataAccess dataAccess, string name, IEnumerable items, string schema, CancellationToken cancellation = default) =>
		UpdateManyAsync(dataAccess, name, items, schema, null, null, null, cancellation);
	public static async ValueTask<int> UpdateManyAsync(this IDataAccess dataAccess, string name, IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(items == null)
			return 0;

		var transaction = new Zongsoft.Transactions.Transaction();

		try
		{
			int count = 0;

			foreach(var item in items)
				count += await dataAccess.UpdateAsync(name, item, (ICondition)null, schema, options, updating, updated, cancellation);

			//提交事务
			transaction.Commit();

			//返回受影响的记录数
			return count;
		}
		catch
		{
			//回滚事务
			transaction.Rollback();

			throw;
		}
	}
	#endregion
}
