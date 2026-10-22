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

using Zongsoft.Collections;

namespace Zongsoft.Data;

public static partial class DataAccessExtension
{
	#region 递增方法
	public static ValueTask<long> IncreaseAsync<T>(this IDataAccess accessor, string member, ICondition criteria, CancellationToken cancellation) => IncreaseAsync<T>(accessor, member, criteria, 1, null, cancellation);
	public static ValueTask<long> IncreaseAsync<T>(this IDataAccess accessor, string member, ICondition criteria, DataUpdateOptions options = null, CancellationToken cancellation = default) => IncreaseAsync<T>(accessor, member, criteria, 1, options, cancellation);

	public static ValueTask<long> IncreaseAsync<T>(this IDataAccess accessor, string member, ICondition criteria, int interval, CancellationToken cancellation) => IncreaseAsync<T>(accessor, member, criteria, interval, null, cancellation);
	public static ValueTask<long> IncreaseAsync<T>(this IDataAccess accessor, string member, ICondition criteria, int interval, DataUpdateOptions options = null, CancellationToken cancellation = default) => IncreaseAsync(accessor, Model.Naming.Get<T>(), member, criteria, interval, options, cancellation);

	public static ValueTask<long> IncreaseAsync(this IDataAccess accessor, string name, string member, ICondition criteria, CancellationToken cancellation) => IncreaseAsync(accessor, name, member, criteria, 1, null, cancellation);
	public static ValueTask<long> IncreaseAsync(this IDataAccess accessor, string name, string member, ICondition criteria, DataUpdateOptions options = null, CancellationToken cancellation = default) => IncreaseAsync(accessor, name, member, criteria, 1, options, cancellation);

	public static ValueTask<long> IncreaseAsync(this IDataAccess accessor, string name, string member, ICondition criteria, int interval, CancellationToken cancellation) => IncreaseAsync(accessor, name, member, criteria, interval, null, cancellation);
	public static async ValueTask<long> IncreaseAsync(this IDataAccess accessor, string name, string member, ICondition criteria, int interval, DataUpdateOptions options = null, CancellationToken cancellation = default)
	{
		if(options == null)
			options = DataUpdateOptions.Return(ReturningKind.Newer, member);
		else
			options.Returning.Columns.Append(member, ReturningKind.Newer);

		var field = (interval > 0 ? Operand.Field(member) + interval : Operand.Field(member) - (-interval));

		if(await accessor.UpdateAsync(name, new Dictionary<string, object>([new KeyValuePair<string, object>(member, field)]), criteria, options, cancellation) > 0)
		{
			if(options.HasReturning(out var returning) && returning.Rows.Count > 0)
				return returning.Rows[0].TryGetValue(member, ReturningKind.Newer, out var value) ? Zongsoft.Common.Convert.ConvertValue<long>(value) : 0L;
			else
				return await accessor.SelectAsync<long>(name, criteria, member, cancellation).FirstOrDefault(cancellation);
		}

		return 0L;
	}
	#endregion

	#region 递减方法
	public static ValueTask<long> DecreaseAsync<T>(this IDataAccess accessor, string member, ICondition criteria, CancellationToken cancellation) => IncreaseAsync<T>(accessor, member, criteria, -1, null, cancellation);
	public static ValueTask<long> DecreaseAsync<T>(this IDataAccess accessor, string member, ICondition criteria, DataUpdateOptions options = null, CancellationToken cancellation = default) => IncreaseAsync<T>(accessor, member, criteria, -1, options, cancellation);

	public static ValueTask<long> DecreaseAsync<T>(this IDataAccess accessor, string member, ICondition criteria, int interval, CancellationToken cancellation) => IncreaseAsync<T>(accessor, member, criteria, -interval, null, cancellation);
	public static ValueTask<long> DecreaseAsync<T>(this IDataAccess accessor, string member, ICondition criteria, int interval, DataUpdateOptions options = null, CancellationToken cancellation = default) => IncreaseAsync<T>(accessor, member, criteria, -interval, options, cancellation);

	public static ValueTask<long> DecreaseAsync(this IDataAccess accessor, string name, string member, ICondition criteria, CancellationToken cancellation) => IncreaseAsync(accessor, name, member, criteria, -1, null, cancellation);
	public static ValueTask<long> DecreaseAsync(this IDataAccess accessor, string name, string member, ICondition criteria, DataUpdateOptions options = null, CancellationToken cancellation = default) => IncreaseAsync(accessor, name, member, criteria, -1, options, cancellation);

	public static ValueTask<long> DecreaseAsync(this IDataAccess accessor, string name, string member, ICondition criteria, int interval, CancellationToken cancellation) => IncreaseAsync(accessor, name, member, criteria, -interval, null, cancellation);
	public static ValueTask<long> DecreaseAsync(this IDataAccess accessor, string name, string member, ICondition criteria, int interval, DataUpdateOptions options = null, CancellationToken cancellation = default) => IncreaseAsync(accessor, name, member, criteria, -interval, options, cancellation);
	#endregion

	#region 批量更新
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable<T> items, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, Model.Naming.Get<T>(), items, string.Empty, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable<T> items, DataUpdateOptions options, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, Model.Naming.Get<T>(), items, string.Empty, options, null, null, cancellation);

	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable<T> items, string schema, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, Model.Naming.Get<T>(), items, schema, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable<T> items, string schema, DataUpdateOptions options, CancellationToken cancellation) => UpdateManyAsync<T>(accessor, items, schema, options, default(Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable<T> items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating, CancellationToken cancellation) => UpdateManyAsync<T>(accessor, items, schema, options, updating, default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable<T> items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating = null, Func<DataUpdateContextBase, CancellationToken, ValueTask> updated = null, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, Model.Naming.Get<T>(), items, schema, options, updating, updated, cancellation);

	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable items, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, Model.Naming.Get<T>(), items, string.Empty, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable items, DataUpdateOptions options, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, Model.Naming.Get<T>(), items, string.Empty, options, null, null, cancellation);

	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable items, string schema, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, Model.Naming.Get<T>(), items, schema, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable items, string schema, DataUpdateOptions options, CancellationToken cancellation) => UpdateManyAsync<T>(accessor, items, schema, options, default(Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating, CancellationToken cancellation) => UpdateManyAsync<T>(accessor, items, schema, options, updating, default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating = null, Func<DataUpdateContextBase, CancellationToken, ValueTask> updated = null, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, Model.Naming.Get<T>(), items, schema, options, updating, updated, cancellation);

	public static ValueTask<int> UpdateManyAsync(this IDataAccess accessor, string name, IEnumerable items, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, name, items, string.Empty, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync(this IDataAccess accessor, string name, IEnumerable items, DataUpdateOptions options, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, name, items, string.Empty, options, null, null, cancellation);

	public static ValueTask<int> UpdateManyAsync(this IDataAccess accessor, string name, IEnumerable items, string schema, CancellationToken cancellation = default) =>
		UpdateManyAsync(accessor, name, items, schema, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync(this IDataAccess accessor, string name, IEnumerable items, string schema, DataUpdateOptions options, CancellationToken cancellation) => UpdateManyAsync(accessor, name, items, schema, options, default(Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	public static ValueTask<int> UpdateManyAsync(this IDataAccess accessor, string name, IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating, CancellationToken cancellation) => UpdateManyAsync(accessor, name, items, schema, options, updating, default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	public static async ValueTask<int> UpdateManyAsync(this IDataAccess accessor, string name, IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating = null, Func<DataUpdateContextBase, CancellationToken, ValueTask> updated = null, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(items == null)
			return 0;

		await using var transaction = new Transaction();

		try
		{
			int count = 0;

			foreach(var item in items)
				count += await accessor.UpdateAsync(name, item, (ICondition)null, schema, options, updating, updated, cancellation);

			//提交事务（异步等待真实提交完成，避免阻塞调用线程）
			await transaction.CommitAsync(cancellation);

			//返回受影响的记录数
			return count;
		}
		catch(Exception exception)
		{
			await transaction.RollbackAsync(exception);
			throw;
		}
	}

	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IAsyncEnumerable<T> items, CancellationToken cancellation = default) =>
		UpdateManyAsync<T>(accessor, Model.Naming.Get<T>(), items, string.Empty, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IAsyncEnumerable<T> items, DataUpdateOptions options, CancellationToken cancellation = default) =>
		UpdateManyAsync<T>(accessor, Model.Naming.Get<T>(), items, string.Empty, options, null, null, cancellation);

	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IAsyncEnumerable<T> items, string schema, CancellationToken cancellation = default) =>
		UpdateManyAsync<T>(accessor, Model.Naming.Get<T>(), items, schema, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IAsyncEnumerable<T> items, string schema, DataUpdateOptions options, CancellationToken cancellation) => UpdateManyAsync<T>(accessor, items, schema, options, default(Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IAsyncEnumerable<T> items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating, CancellationToken cancellation) => UpdateManyAsync<T>(accessor, items, schema, options, updating, default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, IAsyncEnumerable<T> items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating = null, Func<DataUpdateContextBase, CancellationToken, ValueTask> updated = null, CancellationToken cancellation = default) =>
		UpdateManyAsync<T>(accessor, Model.Naming.Get<T>(), items, schema, options, updating, updated, cancellation);

	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, string name, IAsyncEnumerable<T> items, CancellationToken cancellation = default) =>
		UpdateManyAsync<T>(accessor, name, items, string.Empty, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, string name, IAsyncEnumerable<T> items, DataUpdateOptions options, CancellationToken cancellation = default) =>
		UpdateManyAsync<T>(accessor, name, items, string.Empty, options, null, null, cancellation);

	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, string name, IAsyncEnumerable<T> items, string schema, CancellationToken cancellation = default) =>
		UpdateManyAsync<T>(accessor, name, items, schema, null, null, null, cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, string name, IAsyncEnumerable<T> items, string schema, DataUpdateOptions options, CancellationToken cancellation) => UpdateManyAsync<T>(accessor, name, items, schema, options, default(Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	public static ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, string name, IAsyncEnumerable<T> items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating, CancellationToken cancellation) => UpdateManyAsync<T>(accessor, name, items, schema, options, updating, default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	public static async ValueTask<int> UpdateManyAsync<T>(this IDataAccess accessor, string name, IAsyncEnumerable<T> items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating = null, Func<DataUpdateContextBase, CancellationToken, ValueTask> updated = null, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(items == null)
			return 0;

		await using var transaction = new Transaction();

		try
		{
			int count = 0;

			await foreach(var item in items.WithCancellation(cancellation))
				count += await accessor.UpdateAsync(name, item, (ICondition)null, schema, options, updating, updated, cancellation);

			//提交事务（异步等待真实提交完成，避免阻塞调用线程）
			await transaction.CommitAsync(cancellation);

			//返回受影响的记录数
			return count;
		}
		catch(Exception exception)
		{
			await transaction.RollbackAsync(exception);
			throw;
		}
	}
	#endregion
}
