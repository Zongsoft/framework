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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data;

public static class DataServiceExtension
{
	#region 公共方法
	public static Metadata.IDataEntity GetEntity(this IDataService service) => Mapping.Entities.TryGetValue(service.Name, out var entity) ? entity : null;
	#endregion

	#region 聚合方法
	public static TValue? Count<TValue>(this IDataService service, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.Count, member, criteria, options);
	public static TValue? Count<TValue>(this IDataService service, string member, string key, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.Count, member, key, options);
	public static TValue? Count<TKey1, TValue>(this IDataService service, string member, TKey1 key1, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TValue>(DataAggregateFunction.Count, member, key1, options);
	public static TValue? Count<TKey1, TKey2, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TValue>(DataAggregateFunction.Count, member, key1, key2, options);
	public static TValue? Count<TKey1, TKey2, TKey3, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TValue>(DataAggregateFunction.Count, member, key1, key2, key3, options);
	public static TValue? Count<TKey1, TKey2, TKey3, TKey4, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregateFunction.Count, member, key1, key2, key3, key4, options);
	public static TValue? Count<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregateFunction.Count, member, key1, key2, key3, key4, key5, options);

	public static TValue? Sum<TValue>(this IDataService service, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.Sum, member, criteria, options);
	public static TValue? Sum<TValue>(this IDataService service, string member, string key, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.Sum, member, key, options);
	public static TValue? Sum<TKey1, TValue>(this IDataService service, string member, TKey1 key1, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TValue>(DataAggregateFunction.Sum, member, key1, options);
	public static TValue? Sum<TKey1, TKey2, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TValue>(DataAggregateFunction.Sum, member, key1, key2, options);
	public static TValue? Sum<TKey1, TKey2, TKey3, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TValue>(DataAggregateFunction.Sum, member, key1, key2, key3, options);
	public static TValue? Sum<TKey1, TKey2, TKey3, TKey4, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregateFunction.Sum, member, key1, key2, key3, key4, options);
	public static TValue? Sum<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregateFunction.Sum, member, key1, key2, key3, key4, key5, options);

	public static TValue? Average<TValue>(this IDataService service, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.Average, member, criteria, options);
	public static TValue? Average<TValue>(this IDataService service, string member, string key, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.Average, member, key, options);
	public static TValue? Average<TKey1, TValue>(this IDataService service, string member, TKey1 key1, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TValue>(DataAggregateFunction.Average, member, key1, options);
	public static TValue? Average<TKey1, TKey2, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TValue>(DataAggregateFunction.Average, member, key1, key2, options);
	public static TValue? Average<TKey1, TKey2, TKey3, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TValue>(DataAggregateFunction.Average, member, key1, key2, key3, options);
	public static TValue? Average<TKey1, TKey2, TKey3, TKey4, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregateFunction.Average, member, key1, key2, key3, key4, options);
	public static TValue? Average<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregateFunction.Average, member, key1, key2, key3, key4, key5, options);

	public static TValue? Median<TValue>(this IDataService service, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.Median, member, criteria, options);
	public static TValue? Median<TValue>(this IDataService service, string member, string key, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.Median, member, key, options);
	public static TValue? Median<TKey1, TValue>(this IDataService service, string member, TKey1 key1, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TValue>(DataAggregateFunction.Median, member, key1, options);
	public static TValue? Median<TKey1, TKey2, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TValue>(DataAggregateFunction.Median, member, key1, key2, options);
	public static TValue? Median<TKey1, TKey2, TKey3, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TValue>(DataAggregateFunction.Median, member, key1, key2, key3, options);
	public static TValue? Median<TKey1, TKey2, TKey3, TKey4, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregateFunction.Median, member, key1, key2, key3, key4, options);
	public static TValue? Median<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregateFunction.Median, member, key1, key2, key3, key4, key5, options);

	public static TValue? Maximum<TValue>(this IDataService service, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.Maximum, member, criteria, options);
	public static TValue? Maximum<TValue>(this IDataService service, string member, string key, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.Maximum, member, key, options);
	public static TValue? Maximum<TKey1, TValue>(this IDataService service, string member, TKey1 key1, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TValue>(DataAggregateFunction.Maximum, member, key1, options);
	public static TValue? Maximum<TKey1, TKey2, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TValue>(DataAggregateFunction.Maximum, member, key1, key2, options);
	public static TValue? Maximum<TKey1, TKey2, TKey3, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TValue>(DataAggregateFunction.Maximum, member, key1, key2, key3, options);
	public static TValue? Maximum<TKey1, TKey2, TKey3, TKey4, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregateFunction.Maximum, member, key1, key2, key3, key4, options);
	public static TValue? Maximum<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregateFunction.Maximum, member, key1, key2, key3, key4, key5, options);

	public static TValue? Minimum<TValue>(this IDataService service, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.Minimum, member, criteria, options);
	public static TValue? Minimum<TValue>(this IDataService service, string member, string key, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.Minimum, member, key, options);
	public static TValue? Minimum<TKey1, TValue>(this IDataService service, string member, TKey1 key1, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TValue>(DataAggregateFunction.Minimum, member, key1, options);
	public static TValue? Minimum<TKey1, TKey2, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TValue>(DataAggregateFunction.Minimum, member, key1, key2, options);
	public static TValue? Minimum<TKey1, TKey2, TKey3, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TValue>(DataAggregateFunction.Minimum, member, key1, key2, key3, options);
	public static TValue? Minimum<TKey1, TKey2, TKey3, TKey4, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregateFunction.Minimum, member, key1, key2, key3, key4, options);
	public static TValue? Minimum<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregateFunction.Minimum, member, key1, key2, key3, key4, key5, options);

	public static TValue? Deviation<TValue>(this IDataService service, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.Deviation, member, criteria, options);
	public static TValue? Deviation<TValue>(this IDataService service, string member, string key, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.Deviation, member, key, options);
	public static TValue? Deviation<TKey1, TValue>(this IDataService service, string member, TKey1 key1, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TValue>(DataAggregateFunction.Deviation, member, key1, options);
	public static TValue? Deviation<TKey1, TKey2, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TValue>(DataAggregateFunction.Deviation, member, key1, key2, options);
	public static TValue? Deviation<TKey1, TKey2, TKey3, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TValue>(DataAggregateFunction.Deviation, member, key1, key2, key3, options);
	public static TValue? Deviation<TKey1, TKey2, TKey3, TKey4, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregateFunction.Deviation, member, key1, key2, key3, key4, options);
	public static TValue? Deviation<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregateFunction.Deviation, member, key1, key2, key3, key4, key5, options);

	public static TValue? DeviationPopulation<TValue>(this IDataService service, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.DeviationPopulation, member, criteria, options);
	public static TValue? DeviationPopulation<TValue>(this IDataService service, string member, string key, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.DeviationPopulation, member, key, options);
	public static TValue? DeviationPopulation<TKey1, TValue>(this IDataService service, string member, TKey1 key1, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TValue>(DataAggregateFunction.DeviationPopulation, member, key1, options);
	public static TValue? DeviationPopulation<TKey1, TKey2, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TValue>(DataAggregateFunction.DeviationPopulation, member, key1, key2, options);
	public static TValue? DeviationPopulation<TKey1, TKey2, TKey3, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TValue>(DataAggregateFunction.DeviationPopulation, member, key1, key2, key3, options);
	public static TValue? DeviationPopulation<TKey1, TKey2, TKey3, TKey4, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregateFunction.DeviationPopulation, member, key1, key2, key3, key4, options);
	public static TValue? DeviationPopulation<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregateFunction.DeviationPopulation, member, key1, key2, key3, key4, key5, options);

	public static TValue? Variance<TValue>(this IDataService service, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.Variance, member, criteria, options);
	public static TValue? Variance<TValue>(this IDataService service, string member, string key, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.Variance, member, key, options);
	public static TValue? Variance<TKey1, TValue>(this IDataService service, string member, TKey1 key1, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TValue>(DataAggregateFunction.Variance, member, key1, options);
	public static TValue? Variance<TKey1, TKey2, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TValue>(DataAggregateFunction.Variance, member, key1, key2, options);
	public static TValue? Variance<TKey1, TKey2, TKey3, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TValue>(DataAggregateFunction.Variance, member, key1, key2, key3, options);
	public static TValue? Variance<TKey1, TKey2, TKey3, TKey4, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregateFunction.Variance, member, key1, key2, key3, key4, options);
	public static TValue? Variance<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregateFunction.Variance, member, key1, key2, key3, key4, key5, options);

	public static TValue? VariancePopulation<TValue>(this IDataService service, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.VariancePopulation, member, criteria, options);
	public static TValue? VariancePopulation<TValue>(this IDataService service, string member, string key, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => service.Aggregate<TValue>(DataAggregateFunction.VariancePopulation, member, key, options);
	public static TValue? VariancePopulation<TKey1, TValue>(this IDataService service, string member, TKey1 key1, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TValue>(DataAggregateFunction.VariancePopulation, member, key1, options);
	public static TValue? VariancePopulation<TKey1, TKey2, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TValue>(DataAggregateFunction.VariancePopulation, member, key1, key2, options);
	public static TValue? VariancePopulation<TKey1, TKey2, TKey3, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TValue>(DataAggregateFunction.VariancePopulation, member, key1, key2, key3, options);
	public static TValue? VariancePopulation<TKey1, TKey2, TKey3, TKey4, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregateFunction.VariancePopulation, member, key1, key2, key3, key4, options);
	public static TValue? VariancePopulation<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(this IDataService service, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => service.Aggregate<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregateFunction.VariancePopulation, member, key1, key2, key3, key4, key5, options);
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
