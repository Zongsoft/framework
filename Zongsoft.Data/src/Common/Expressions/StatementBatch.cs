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
using System.Linq;
using System.Collections.Generic;

namespace Zongsoft.Data.Common.Expressions;

internal static class StatementBatch
{
	public static int GetSize(IBatchStatement statement)
	{
		ArgumentNullException.ThrowIfNull(statement);
		var capacity = statement.BatchSize;
		var parameterLimit = statement.ParameterLimit;
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

		if(parameterLimit <= 0 || statement.Parameters.Count <= parameterLimit)
			return capacity;

		var values = statement.Values;
		var fields = statement.Fields.Count;
		if(values.Count == 0 || fields == 0)
			return capacity;

		var parameters = values.OfType<ParameterExpression>().ToHashSet();
		var reserved = statement.Parameters.Count(parameter => !parameters.Contains(parameter));
		var maximum = values.Chunk(fields).Max(row => row.OfType<ParameterExpression>().Distinct().Count());
		var size = maximum == 0 ? 0 : (parameterLimit - reserved) / maximum;

		if(size <= 0)
			throw new ArgumentOutOfRangeException(nameof(statement));

		return Math.Min(capacity, size);
	}

	public static IEnumerable<object> Split(IBatchStatement statement, object data)
	{
		var size = GetSize(statement);
		var values = statement.Values;
		var fields = statement.Fields.Count;

		if(fields == 0 || !Utility.IsMultiple(data, out var items))
		{
			yield return data;
			yield break;
		}

		if(values.Count <= fields * size && size > 1)
		{
			yield return data;
			yield break;
		}

		var original = values.ToArray();
		var parameters = statement.Parameters.ToArray();
		var rowParameters = original.OfType<ParameterExpression>().ToHashSet();
		var shared = parameters.Where(parameter => !rowParameters.Contains(parameter)).ToArray();
		var offset = 0;
		var batch = Utility.CreateList(data);

		try
		{
			foreach(var item in items)
			{
				batch.Add(item);
				if(batch.Count < size)
					continue;

				Apply(batch.Count);
				yield return size == 1 ? batch[0] : batch;
				batch = Utility.CreateList(data);
			}

			if(batch.Count > 0)
			{
				Apply(batch.Count);
				yield return batch;
			}
		}
		finally
		{
			values.Clear();
			foreach(var value in original)
				values.Add(value);

			statement.Parameters.Clear();
			foreach(var parameter in parameters)
				statement.Parameters.Add(parameter);
		}

		void Apply(int count)
		{
			values.Clear();
			var selected = new HashSet<ParameterExpression>(shared);

			for(int index = 0; index < count * fields; index++)
			{
				var value = original[offset++];
				values.Add(value);

				if(value is ParameterExpression parameter)
					selected.Add(parameter);
			}

			statement.Parameters.Clear();
			foreach(var parameter in parameters)
			{
				if(selected.Contains(parameter))
					statement.Parameters.Add(parameter);
			}
		}
	}
}
