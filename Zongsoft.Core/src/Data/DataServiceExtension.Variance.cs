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

partial class DataServiceExtension
{
	#region 公共方法
	public static ValueTask<TValue?> VarianceAsync<TValue>(this IDataService service, string member, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TValue>(DataAggregate.Variance(member), (ICondition)null, null, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TValue>(this IDataService service, string member, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TValue>(DataAggregate.Variance(member), (ICondition)null, options, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TValue>(this IDataService service, string member, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TValue>(DataAggregate.Variance(member), criteria, null, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TValue>(this IDataService service, string member, ICondition criteria, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TValue>(DataAggregate.Variance(member), criteria, options, cancellation);

	public static ValueTask<TValue?> VarianceAsync<TValue>(this IDataService service, string member, bool distinct, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TValue>(DataAggregate.Variance(member, distinct), (ICondition)null, null, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TValue>(this IDataService service, string member, bool distinct, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TValue>(DataAggregate.Variance(member, distinct), (ICondition)null, options, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TValue>(this IDataService service, string member, bool distinct, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TValue>(DataAggregate.Variance(member, distinct), criteria, null, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TValue>(this IDataService service, string member, bool distinct, ICondition criteria, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TValue>(DataAggregate.Variance(member, distinct), criteria, options, cancellation);

	public static ValueTask<TValue?> VarianceAsync<TValue>(this IDataService service, string member, string key, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TValue>(DataAggregate.Variance(member), key, null, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TValue>(this IDataService service, string member, string key, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TValue>(DataAggregate.Variance(member), key, options, cancellation);

	public static ValueTask<TValue?> VarianceAsync<TValue>(this IDataService service, string member, bool distinct, string key, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TValue>(DataAggregate.Variance(member, distinct), key, null, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TValue>(this IDataService service, string member, bool distinct, string key, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TValue>(DataAggregate.Variance(member, distinct), key, options, cancellation);

	public static ValueTask<TValue?> VarianceAsync<TKey1, TValue>(this IDataService service, string member, TKey1 key1, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TValue>(DataAggregate.Variance(member), key1, null, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TKey1, TValue>(this IDataService service, string member, TKey1 key1, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TValue>(DataAggregate.Variance(member), key1, options, cancellation);

	public static ValueTask<TValue?> VarianceAsync<TKey1, TValue>(this IDataService service, string member, bool distinct, TKey1 key1, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TValue>(DataAggregate.Variance(member, distinct), key1, null, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TKey1, TValue>(this IDataService service, string member, bool distinct, TKey1 key1, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TValue>(DataAggregate.Variance(member, distinct), key1, options, cancellation);

	public static ValueTask<TValue?> VarianceAsync<TKey1, TKey2, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TKey2, TValue>(DataAggregate.Variance(member), key1, key2, null, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TKey1, TKey2, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TKey2, TValue>(DataAggregate.Variance(member), key1, key2, options, cancellation);

	public static ValueTask<TValue?> VarianceAsync<TKey1, TKey2, TValue>(this IDataService service, string member, bool distinct, TKey1 key1, TKey2 key2, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TKey2, TValue>(DataAggregate.Variance(member, distinct), key1, key2, null, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TKey1, TKey2, TValue>(this IDataService service, string member, bool distinct, TKey1 key1, TKey2 key2, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TKey2, TValue>(DataAggregate.Variance(member, distinct), key1, key2, options, cancellation);

	public static ValueTask<TValue?> VarianceAsync<TKey1, TKey2, TKey3, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TKey2, TKey3, TValue>(DataAggregate.Variance(member), key1, key2, key3, null, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TKey1, TKey2, TKey3, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TKey2, TKey3, TValue>(DataAggregate.Variance(member), key1, key2, key3, options, cancellation);

	public static ValueTask<TValue?> VarianceAsync<TKey1, TKey2, TKey3, TValue>(this IDataService service, string member, bool distinct, TKey1 key1, TKey2 key2, TKey3 key3, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TKey2, TKey3, TValue>(DataAggregate.Variance(member, distinct), key1, key2, key3, null, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TKey1, TKey2, TKey3, TValue>(this IDataService service, string member, bool distinct, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TKey2, TKey3, TValue>(DataAggregate.Variance(member, distinct), key1, key2, key3, options, cancellation);

	public static ValueTask<TValue?> VarianceAsync<TKey1, TKey2, TKey3, TKey4, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregate.Variance(member), key1, key2, key3, key4, null, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TKey1, TKey2, TKey3, TKey4, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregate.Variance(member), key1, key2, key3, key4, options, cancellation);

	public static ValueTask<TValue?> VarianceAsync<TKey1, TKey2, TKey3, TKey4, TValue>(this IDataService service, string member, bool distinct, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregate.Variance(member, distinct), key1, key2, key3, key4, null, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TKey1, TKey2, TKey3, TKey4, TValue>(this IDataService service, string member, bool distinct, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregate.Variance(member, distinct), key1, key2, key3, key4, options, cancellation);

	public static ValueTask<TValue?> VarianceAsync<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregate.Variance(member), key1, key2, key3, key4, key5, null, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregate.Variance(member), key1, key2, key3, key4, key5, options, cancellation);

	public static ValueTask<TValue?> VarianceAsync<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(this IDataService service, string member, bool distinct, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregate.Variance(member, distinct), key1, key2, key3, key4, key5, null, cancellation);
	public static ValueTask<TValue?> VarianceAsync<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(this IDataService service, string member, bool distinct, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => service.AggregateAsync<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregate.Variance(member, distinct), key1, key2, key3, key4, key5, options, cancellation);
	#endregion
}
