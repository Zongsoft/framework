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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data;

public static partial class DataAccessExtension
{
	#region 递增方法
	public static long Increase<T>(this IDataAccess accessor, string member, ICondition criteria, DataUpdateOptions options = null) => Increase<T>(accessor, member, criteria, 1, options);
	public static long Increase<T>(this IDataAccess accessor, string member, ICondition criteria, int interval, DataUpdateOptions options = null) => Increase(accessor, Model.Naming.Get<T>(), member, criteria, interval, options);
	public static long Increase(this IDataAccess accessor, string name, string member, ICondition criteria, DataUpdateOptions options = null) => Increase(accessor, name, member, criteria, 1, options);
	public static long Increase(this IDataAccess accessor, string name, string member, ICondition criteria, int interval, DataUpdateOptions options = null)
	{
		if(options == null)
			options = DataUpdateOptions.Return(ReturningKind.Newer, member);
		else
			options.Returning.Columns.Append(member, ReturningKind.Newer);

		var field = (interval > 0 ? Operand.Field(member) + interval : Operand.Field(member) - (-interval));

		if(accessor.Update(name, new Dictionary<string, object>([new KeyValuePair<string, object>(member, field)]), criteria, options) > 0)
			return options.Returning.Rows.First().TryGetValue(member, ReturningKind.Newer, out var value) ? Zongsoft.Common.Convert.ConvertValue<long>(value) : 0L;

		return 0L;
	}

	public static ValueTask<long> IncreaseAsync<T>(this IDataAccess accessor, string member, ICondition criteria, CancellationToken cancellation = default) => IncreaseAsync<T>(accessor, member, criteria, 1, null, cancellation);
	public static ValueTask<long> IncreaseAsync<T>(this IDataAccess accessor, string member, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync<T>(accessor, member, criteria, 1, options, cancellation);
	public static ValueTask<long> IncreaseAsync<T>(this IDataAccess accessor, string member, ICondition criteria, int interval, CancellationToken cancellation = default) => IncreaseAsync<T>(accessor, member, criteria, interval, null, cancellation);
	public static ValueTask<long> IncreaseAsync<T>(this IDataAccess accessor, string member, ICondition criteria, int interval, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync(accessor, Model.Naming.Get<T>(), member, criteria, interval, options, cancellation);
	public static ValueTask<long> IncreaseAsync(this IDataAccess accessor, string name, string member, ICondition criteria, CancellationToken cancellation = default) => IncreaseAsync(accessor, name, member, criteria, 1, null, cancellation);
	public static ValueTask<long> IncreaseAsync(this IDataAccess accessor, string name, string member, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync(accessor, name, member, criteria, 1, options, cancellation);
	public static ValueTask<long> IncreaseAsync(this IDataAccess accessor, string name, string member, ICondition criteria, int interval, CancellationToken cancellation = default) => IncreaseAsync(accessor, name, member, criteria, interval, null, cancellation);
	public static async ValueTask<long> IncreaseAsync(this IDataAccess accessor, string name, string member, ICondition criteria, int interval, DataUpdateOptions options, CancellationToken cancellation = default)
	{
		if(options == null)
			options = DataUpdateOptions.Return(ReturningKind.Newer, member);
		else
			options.Returning.Columns.Append(member, ReturningKind.Newer);

		var field = (interval > 0 ? Operand.Field(member) + interval : Operand.Field(member) - (-interval));

		if(await accessor.UpdateAsync(name, new Dictionary<string, object>([new KeyValuePair<string, object>(member, field)]), criteria, options, cancellation) > 0)
			return options.Returning.Rows.First().TryGetValue(member, ReturningKind.Newer, out var value) ? Zongsoft.Common.Convert.ConvertValue<long>(value) : 0L;

		return 0L;
	}
	#endregion

	#region 递减方法
	public static long Decrease<T>(this IDataAccess accessor, string member, ICondition criteria, DataUpdateOptions options = null) => Increase<T>(accessor, member, criteria, -1, options);
	public static long Decrease<T>(this IDataAccess accessor, string member, ICondition criteria, int interval, DataUpdateOptions options = null) => Increase<T>(accessor, member, criteria, -interval, options);
	public static long Decrease(this IDataAccess accessor, string name, string member, ICondition criteria, DataUpdateOptions options = null) => Increase(accessor, name, member, criteria, -1, options);
	public static long Decrease(this IDataAccess accessor, string name, string member, ICondition criteria, int interval, DataUpdateOptions options = null) => Increase(accessor, name, member, criteria, -interval, options);
	public static ValueTask<long> DecreaseAsync<T>(this IDataAccess accessor, string member, ICondition criteria, CancellationToken cancellation = default) => IncreaseAsync<T>(accessor, member, criteria, -1, null, cancellation);
	public static ValueTask<long> DecreaseAsync<T>(this IDataAccess accessor, string member, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync<T>(accessor, member, criteria, -1, options, cancellation);
	public static ValueTask<long> DecreaseAsync<T>(this IDataAccess accessor, string member, ICondition criteria, int interval, CancellationToken cancellation = default) => IncreaseAsync<T>(accessor, member, criteria, -interval, null, cancellation);
	public static ValueTask<long> DecreaseAsync<T>(this IDataAccess accessor, string member, ICondition criteria, int interval, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync<T>(accessor, member, criteria, -interval, options, cancellation);
	public static ValueTask<long> DecreaseAsync(this IDataAccess accessor, string name, string member, ICondition criteria, CancellationToken cancellation = default) => IncreaseAsync(accessor, name, member, criteria, -1, null, cancellation);
	public static ValueTask<long> DecreaseAsync(this IDataAccess accessor, string name, string member, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync(accessor, name, member, criteria, -1, options, cancellation);
	public static ValueTask<long> DecreaseAsync(this IDataAccess accessor, string name, string member, ICondition criteria, int interval, CancellationToken cancellation = default) => IncreaseAsync(accessor, name, member, criteria, -interval, null, cancellation);
	public static ValueTask<long> DecreaseAsync(this IDataAccess accessor, string name, string member, ICondition criteria, int interval, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync(accessor, name, member, criteria, -interval, options, cancellation);
	#endregion

	#region 批量更新
	public static int UpdateMany<T>(this IDataAccess accessor, IEnumerable<T> items) => UpdateMany(accessor, Model.Naming.Get<T>(), items, string.Empty, null, null, null);
	public static int UpdateMany<T>(this IDataAccess accessor, IEnumerable<T> items, DataUpdateOptions options) => UpdateMany(accessor, Model.Naming.Get<T>(), items, string.Empty, options, null, null);
	public static int UpdateMany<T>(this IDataAccess accessor, IEnumerable<T> items, string schema) => UpdateMany(accessor, Model.Naming.Get<T>(), items, schema, null, null, null);
	public static int UpdateMany<T>(this IDataAccess accessor, IEnumerable<T> items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null) => UpdateMany(accessor, Model.Naming.Get<T>(), items, schema, options, updating, updated);
	public static int UpdateMany<T>(this IDataAccess accessor, IEnumerable items) => UpdateMany(accessor, Model.Naming.Get<T>(), items, string.Empty, null, null, null);
	public static int UpdateMany<T>(this IDataAccess accessor, IEnumerable items, DataUpdateOptions options) => UpdateMany(accessor, Model.Naming.Get<T>(), items, string.Empty, options, null, null);
	public static int UpdateMany<T>(this IDataAccess accessor, IEnumerable items, string schema) => UpdateMany(accessor, Model.Naming.Get<T>(), items, schema, null, null, null);
	public static int UpdateMany<T>(this IDataAccess accessor, IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null) => UpdateMany(accessor, Model.Naming.Get<T>(), items, schema, options, updating, updated);

	public static int UpdateMany(this IDataAccess accessor, string name, IEnumerable items) => UpdateMany(accessor, name, items, string.Empty, null, null, null);
	public static int UpdateMany(this IDataAccess accessor, string name, IEnumerable items, DataUpdateOptions options) => UpdateMany(accessor, name, items, string.Empty, options, null, null);
	public static int UpdateMany(this IDataAccess accessor, string name, IEnumerable items, string schema) => UpdateMany(accessor, name, items, schema, null, null, null);
	public static int UpdateMany(this IDataAccess accessor, string name, IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null)
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
				count += accessor.Update(name, item, (ICondition)null, schema, options, updating, updated);

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

	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable<T> items, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, Model.Naming.Get<T>(), items, string.Empty, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable<T> items, DataUpdateOptions options, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, Model.Naming.Get<T>(), items, string.Empty, options, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable<T> items, string schema, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, Model.Naming.Get<T>(), items, schema, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable<T> items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, Model.Naming.Get<T>(), items, schema, options, updating, updated, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable items, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, Model.Naming.Get<T>(), items, string.Empty, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable items, DataUpdateOptions options, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, Model.Naming.Get<T>(), items, string.Empty, options, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable items, string schema, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, Model.Naming.Get<T>(), items, schema, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, Model.Naming.Get<T>(), items, schema, options, updating, updated, cancellation);

	public static ValueTask<int> UpdateManyAsync(this IDataAccess accessor, string name, IEnumerable items, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, name, items, string.Empty, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync(this IDataAccess accessor, string name, IEnumerable items, DataUpdateOptions options, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, name, items, string.Empty, options, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync(this IDataAccess accessor, string name, IEnumerable items, string schema, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, name, items, schema, null, null, null, cancellation);
	public static async ValueTask<int> UpdateManyAsync(this IDataAccess accessor, string name, IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default)
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
				count += await accessor.UpdateAsync(name, item, (ICondition)null, schema, options, updating, updated, cancellation);

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
