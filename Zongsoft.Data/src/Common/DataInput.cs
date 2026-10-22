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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zongsoft.Data.Common;

internal static class DataInput
{
	#region 公共方法
	public static bool TryGetAsync(object data, out IAsyncEnumerable<object> source, out Type elementType)
	{
		if(!Collections.Enumerable.IsAsyncEnumerable(data, out elementType))
		{
			source = null;
			return false;
		}

		source = data as IAsyncEnumerable<object> ??
			(IAsyncEnumerable<object>)Activator.CreateInstance(typeof(AsyncInput<>).MakeGenericType(elementType), [data]);

		return true;
	}

	public static IAsyncEnumerable<object> EnumerateAsync(object data) =>
		TryGetAsync(data, out var source, out _) ? source : Collections.Enumerable.EnumerateAsync<object>(data);

	public static async IAsyncEnumerable<IList> BatchAsync(IAsyncEnumerable<object> source, Type elementType, [EnumeratorCancellation] CancellationToken cancellation)
	{
		const int CAPACITY = 256;
		var batch = Utility.CreateList(elementType);

		await foreach(var item in source.WithCancellation(cancellation).ConfigureAwait(false))
		{
			batch.Add(item);
			if(batch.Count < CAPACITY)
				continue;

			yield return batch;
			batch = Utility.CreateList(elementType);
		}

		if(batch.Count > 0)
			yield return batch;
	}
	#endregion

	#region 嵌套子类
	private sealed class AsyncInput<T>(IAsyncEnumerable<T> source) : IAsyncEnumerable<object>
	{
		public async IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellation = default)
		{
			await foreach(var item in source.WithCancellation(cancellation).ConfigureAwait(false))
				yield return item;
		}
	}
	#endregion
}
