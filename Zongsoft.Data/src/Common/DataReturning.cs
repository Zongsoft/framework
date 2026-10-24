/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Copyright (C) 2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Data;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zongsoft.Data.Common;

internal static class DataReturning
{
	private static readonly ConditionalWeakTable<IDataAccessContext, List<DataTable>> _buffers = new();

	public static void Populate(IDataAccessContext context, IDataRecord record)
	{
		var options = GetOptions(context);
		if(options == null || !options.HasReturning(out _))
			return;

		var tables = _buffers.GetOrCreateValue(context);
		var table = tables.Count > 0 ? tables[^1] : null;

		if(table == null || !Matches(table, record))
		{
			table = new DataTable();
			for(int i = 0; i < record.FieldCount; i++)
				table.Columns.Add(record.GetName(i), typeof(object));

			tables.Add(table);
		}

		var values = new object[record.FieldCount];
		record.GetValues(values);
		table.Rows.Add(values);
	}

	public static void Complete(IDataAccessContext context)
	{
		if(!_buffers.TryGetValue(context, out var tables))
			return;

		try
		{
			var options = GetOptions(context);
			if(options != null && options.HasReturning(out var returning))
			{
				foreach(var table in tables)
				{
					using var reader = table.CreateDataReader();
					while(reader.Read())
						returning.Rows.Populate(reader);
				}
			}
		}
		finally
		{
			Discard(context);
		}
	}

	public static void Discard(IDataAccessContext context)
	{
		if(!_buffers.TryGetValue(context, out var tables))
			return;

		_buffers.Remove(context);
		foreach(var table in tables)
			table.Dispose();
	}

	private static bool Matches(DataTable table, IDataRecord record)
	{
		if(table.Columns.Count != record.FieldCount)
			return false;

		for(int i = 0; i < record.FieldCount; i++)
		{
			if(table.Columns[i].ColumnName != record.GetName(i))
				return false;
		}

		return true;
	}

	private static IDataMutateOptions GetOptions(IDataAccessContext context) => context switch
	{
		DataInsertContext insertion => insertion.Options,
		DataUpdateContext updation => updation.Options,
		DataUpsertContext upsertion => upsertion.Options,
		DataDeleteContext deletion => deletion.Options,
		_ => null,
	};
}
