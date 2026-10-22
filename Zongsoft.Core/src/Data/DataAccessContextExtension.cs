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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Data;

public static class DataAccessContextExtension
{
	#region 上下文数据
	extension(IDataAccessContextBase context)
	{
		public string Driver => context switch
		{
			DataExistContextBase exists => exists.Driver,
			DataSelectContextBase select => select.Driver,
			DataDeleteContextBase delete => delete.Driver,
			DataInsertContextBase insert => insert.Driver,
			DataUpdateContextBase update => update.Driver,
			DataUpsertContextBase upsert => upsert.Driver,
			DataImportContextBase import => import.Driver,
			DataAggregateContextBase aggregate => aggregate.Driver,
			_ => null
		};
	}

	public static bool IsMultiple(this IDataMutateContextBase context)
	{
		if(context is DataInsertContextBase insertion)
			return insertion.IsMultiple;
		if(context is DataUpsertContextBase upsertion)
			return upsertion.IsMultiple;

		return false;
	}

	public static IEnumerable<IDataDictionary<T>> GetDataDictionaries<T>(this DataInsertContextBase context) => GetDataDictionaries<T>((IDataMutateContextBase)context);
	public static IEnumerable<IDataDictionary<T>> GetDataDictionaries<T>(this DataUpsertContextBase context) => GetDataDictionaries<T>((IDataMutateContextBase)context);
	public static IEnumerable<IDataDictionary<T>> GetDataDictionaries<T>(this DataUpdateContextBase context) => GetDataDictionaries<T>((IDataMutateContextBase)context);

	private static IEnumerable<IDataDictionary<T>> GetDataDictionaries<T>(this IDataMutateContextBase context)
	{
		if(context == null)
			throw new ArgumentNullException(nameof(context));

		if(context.Count < 1)
			return [];

		if(context.IsMultiple())
			return DataDictionary.GetDictionaries<T>((System.Collections.IEnumerable)context.Data);

		return [DataDictionary.GetDictionary<T>(context.Data)];
	}

	public static IAsyncEnumerable<IDataDictionary<T>> GetDataDictionariesAsync<T>(this DataInsertContextBase context, CancellationToken cancellation = default) => GetDataDictionariesAsync<T>((IDataMutateContextBase)context, cancellation);
	public static IAsyncEnumerable<IDataDictionary<T>> GetDataDictionariesAsync<T>(this DataUpsertContextBase context, CancellationToken cancellation = default) => GetDataDictionariesAsync<T>((IDataMutateContextBase)context, cancellation);
	public static IAsyncEnumerable<IDataDictionary<T>> GetDataDictionariesAsync<T>(this DataUpdateContextBase context, CancellationToken cancellation = default) => GetDataDictionariesAsync<T>((IDataMutateContextBase)context, cancellation);

	private static IAsyncEnumerable<IDataDictionary<T>> GetDataDictionariesAsync<T>(IDataMutateContextBase context, CancellationToken cancellation)
	{
		ArgumentNullException.ThrowIfNull(context);

		if(context.Count > 0 && context.IsMultiple() && Collections.Enumerable.IsAsyncEnumerable(context.Data, out var elementType))
		{
			var method = typeof(DataAccessContextExtension).GetMethod(nameof(EnumerateDataDictionariesAsync), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(typeof(T), elementType);
			return (IAsyncEnumerable<IDataDictionary<T>>)method.Invoke(null, [context.Data, cancellation]);
		}

		return Collections.Enumerable.EnumerateAsync<IDataDictionary<T>>(GetDataDictionaries<T>(context), cancellation);
	}

	private static async IAsyncEnumerable<IDataDictionary<T>> EnumerateDataDictionariesAsync<T, TItem>(IAsyncEnumerable<TItem> items, [EnumeratorCancellation] CancellationToken cancellation)
	{
		await foreach(var item in items.WithCancellation(cancellation))
		{
			if(item != null)
				yield return DataDictionary.GetDictionary<T>(item);
		}
	}
	#endregion

	#region 数据验证
	public static ValueTask<DataValidatorResult> ValidateAsync(this IDataMutateContextBase context, DataAccessMethod method, Metadata.IDataEntityProperty property, CancellationToken cancellation = default)
	{
		if(context == null)
			throw new ArgumentNullException(nameof(context));

		if(!context.Options.ValidatorSuppressed)
		{
			var validator = context.Validator;

			if(validator != null)
			{
				switch(method)
				{
					case DataAccessMethod.Insert:
						return validator.OnInsertAsync(context, property, cancellation);
					case DataAccessMethod.Update:
						return validator.OnUpdateAsync(context, property, cancellation);
				}
			}
		}

		return ValueTask.FromResult(default(DataValidatorResult));
	}

	public static ValueTask<DataValidatorResult> ValidateAsync(this DataInsertContextBase context, Metadata.IDataEntityProperty property, CancellationToken cancellation = default)
	{
		return ValidateAsync(context, DataAccessMethod.Insert, property, cancellation);
	}

	public static ValueTask<DataValidatorResult> ValidateAsync(this DataUpdateContextBase context, Metadata.IDataEntityProperty property, CancellationToken cancellation = default)
	{
		return ValidateAsync(context, DataAccessMethod.Update, property, cancellation);
	}
	#endregion
}
