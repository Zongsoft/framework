/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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

namespace Zongsoft.Data
{
	public static class DataAccessExtension
	{
		public static TValue? Count<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, IDataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.Count, member, criteria, options);
		public static TValue? Sum<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, IDataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.Sum, member, criteria, options);
		public static TValue? Average<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, IDataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.Average, member, criteria, options);
		public static TValue? Median<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, IDataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.Median, member, criteria, options);
		public static TValue? Maximum<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, IDataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.Maximum, member, criteria, options);
		public static TValue? Minimum<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, IDataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.Minimum, member, criteria, options);
		public static TValue? Deviation<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, IDataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.Deviation, member, criteria, options);
		public static TValue? DeviationPopulation<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, IDataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.DeviationPopulation, member, criteria, options);
		public static TValue? Variance<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, IDataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.Variance, member, criteria, options);
		public static TValue? VariancePopulation<TValue>(this IDataAccess dataAccess, string name, string member, ICondition criteria = null, IDataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, DataAggregateFunction.VariancePopulation, member, criteria, options);

		public static TValue? Count<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, IDataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
		public static TValue? Sum<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, IDataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
		public static TValue? Average<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, IDataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
		public static TValue? Median<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, IDataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
		public static TValue? Maximum<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, IDataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
		public static TValue? Minimum<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, IDataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
		public static TValue? Deviation<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, IDataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
		public static TValue? DeviationPopulation<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, IDataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
		public static TValue? Variance<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, IDataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
		public static TValue? VariancePopulation<TValue>(this IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria = null, IDataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => dataAccess.Aggregate<TValue>(name, aggregate, criteria, options, aggregating, aggregated);
	}
}
