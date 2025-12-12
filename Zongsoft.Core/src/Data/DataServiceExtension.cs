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

public static partial class DataServiceExtension
{
	#region 公共方法
	public static Metadata.IDataEntity GetEntity(this IDataService service) => Mapping.Entities.TryGetValue(service.Name, out var entity) ? entity : null;
	#endregion

	#region 递增方法
	public static long Increase(this IDataService service, string member, ICondition criteria, DataUpdateOptions options = null) => Increase(service, member, criteria, 1, options);
	public static long Increase(this IDataService service, string member, ICondition criteria, int interval, DataUpdateOptions options = null)
	{
		if(options == null)
			options = DataUpdateOptions.Return(ReturningKind.Newer, member);
		else
			options.Returning.Columns.Append(member, ReturningKind.Newer);

		var field = (interval > 0 ? Operand.Field(member) + interval : Operand.Field(member) - (-interval));

		if(service.Update(new Dictionary<string, object>([new KeyValuePair<string, object>(member, field)]), criteria, options) > 0)
			return options.Returning.Rows.First().TryGetValue(member, ReturningKind.Newer, out var value) ? Zongsoft.Common.Convert.ConvertValue<long>(value) : 0L;

		return 0L;
	}

	public static ValueTask<long> IncreaseAsync(this IDataService service, string member, ICondition criteria, CancellationToken cancellation = default) => IncreaseAsync(service, member, criteria, 1, null, cancellation);
	public static ValueTask<long> IncreaseAsync(this IDataService service, string member, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync(service, member, criteria, 1, options, cancellation);
	public static ValueTask<long> IncreaseAsync(this IDataService service, string member, ICondition criteria, int interval, CancellationToken cancellation = default) => IncreaseAsync(service, member, criteria, interval, null, cancellation);
	public static async ValueTask<long> IncreaseAsync(this IDataService service, string member, ICondition criteria, int interval, DataUpdateOptions options, CancellationToken cancellation = default)
	{
		if(options == null)
			options = DataUpdateOptions.Return(ReturningKind.Newer, member);
		else
			options.Returning.Columns.Append(member, ReturningKind.Newer);

		var field = (interval > 0 ? Operand.Field(member) + interval : Operand.Field(member) - (-interval));

		if(await service.UpdateAsync(new Dictionary<string, object>([new KeyValuePair<string, object>(member, field)]), criteria, options, cancellation) > 0)
			return options.Returning.Rows.First().TryGetValue(member, ReturningKind.Newer, out var value) ? Zongsoft.Common.Convert.ConvertValue<long>(value) : 0L;

		return 0L;
	}
	#endregion

	#region 递减方法
	public static long Decrease(this IDataService service, string member, ICondition criteria, DataUpdateOptions options = null) => Increase(service, member, criteria, -1, options);
	public static long Decrease(this IDataService service, string member, ICondition criteria, int interval, DataUpdateOptions options = null) => Increase(service, member, criteria, -interval, options);
	public static ValueTask<long> DecreaseAsync(this IDataService service, string member, ICondition criteria, CancellationToken cancellation = default) => IncreaseAsync(service, member, criteria, -1, null, cancellation);
	public static ValueTask<long> DecreaseAsync(this IDataService service, string member, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync(service, member, criteria, -1, options, cancellation);
	public static ValueTask<long> DecreaseAsync(this IDataService service, string member, ICondition criteria, int interval, CancellationToken cancellation = default) => IncreaseAsync(service, member, criteria, -interval, null, cancellation);
	public static ValueTask<long> DecreaseAsync(this IDataService service, string member, ICondition criteria, int interval, DataUpdateOptions options, CancellationToken cancellation = default) => IncreaseAsync(service, member, criteria, -interval, options, cancellation);
	#endregion

	#region 批量更新
	public static int UpdateMany(this IDataService service, IEnumerable items, DataUpdateOptions options = null) => UpdateMany(service, items, string.Empty, options);
	public static int UpdateMany(this IDataService service, IEnumerable items, string schema, DataUpdateOptions options = null)
	{
		if(service == null)
			throw new ArgumentNullException(nameof(service));

		if(items == null)
			return 0;

		var transaction = new Zongsoft.Transactions.Transaction();

		try
		{
			int count = 0;

			foreach(var item in items)
				count += service.Update(item, schema, options);

			//提交事务
			transaction.Commit();

			//返回更新记录数
			return count;
		}
		catch
		{
			//回滚事务
			transaction.Rollback();

			throw;
		}
	}

	public static int UpdateMany(this IDataService service, string key, IEnumerable items, DataUpdateOptions options = null) => UpdateMany(service, key, items, null, options);
	public static int UpdateMany(this IDataService service, string key, IEnumerable items, string schema, DataUpdateOptions options = null)
	{
		if(service == null)
			throw new ArgumentNullException(nameof(service));

		if(items == null)
			return 0;

		var transaction = new Zongsoft.Transactions.Transaction();

		try
		{
			int count = 0;

			foreach(var item in items)
				count += service.Update(key, item, schema, options);

			//提交事务
			transaction.Commit();

			//返回更新记录数
			return count;
		}
		catch
		{
			//回滚事务
			transaction.Rollback();

			throw;
		}
	}

	public static ValueTask<int> UpdateManyAsync(this IDataService service, IEnumerable items, DataUpdateOptions options = null, CancellationToken cancellation = default) => UpdateManyAsync(service, items, string.Empty, options, cancellation);
	public static async ValueTask<int> UpdateManyAsync(this IDataService service, IEnumerable items, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default)
	{
		if(service == null)
			throw new ArgumentNullException(nameof(service));

		if(items == null)
			return 0;

		var transaction = new Zongsoft.Transactions.Transaction();

		try
		{
			int count = 0;

			foreach(var item in items)
				count += await service.UpdateAsync(item, schema, options, cancellation);

			//提交事务
			transaction.Commit();

			//返回更新记录数
			return count;
		}
		catch
		{
			//回滚事务
			transaction.Rollback();

			throw;
		}
	}

	public static ValueTask<int> UpdateManyAsync(this IDataService service, string key, IEnumerable items, DataUpdateOptions options = null, CancellationToken cancellation = default) => UpdateManyAsync(service, key, items, null, options, cancellation);
	public static async ValueTask<int> UpdateManyAsync(this IDataService service, string key, IEnumerable items, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default)
	{
		if(service == null)
			throw new ArgumentNullException(nameof(service));

		if(items == null)
			return 0;

		var transaction = new Zongsoft.Transactions.Transaction();

		try
		{
			int count = 0;

			foreach(var item in items)
				count += await service.UpdateAsync(key, item, schema, options, cancellation);

			//提交事务
			transaction.Commit();

			//返回更新记录数
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
