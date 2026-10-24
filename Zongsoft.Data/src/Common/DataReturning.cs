/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Copyright (C) 2015-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Runtime.CompilerServices;

namespace Zongsoft.Data.Common;

internal static class DataReturning
{
	private static readonly ConditionalWeakTable<IDataAccessContext, Returning.RowCollection> _buffers = new();

	public static void Populate(IDataAccessContext context, IDataRecord record)
	{
		if(!_buffers.TryGetValue(context, out var buffer))
		{
			var options = GetOptions(context);
			if(options == null || !options.HasReturning(out var returning))
				return;

			buffer = _buffers.GetValue(context, _ => new Returning.RowCollection(returning.Columns));
		}

		buffer.Populate(record);
	}

	public static void Complete(IDataAccessContext context)
	{
		if(!_buffers.TryGetValue(context, out var buffer))
			return;

		try
		{
			var options = GetOptions(context);
			if(options != null && options.HasReturning(out var returning))
				returning.Rows.AddRange(buffer);
		}
		finally { Discard(context); }
	}

	public static void Discard(IDataAccessContext context) => _buffers.Remove(context);

	private static IDataMutateOptions GetOptions(IDataAccessContext context) => context switch
	{
		DataDeleteContext deletion => deletion.Options,
		DataUpdateContext updation => updation.Options,
		DataUpsertContext upsertion => upsertion.Options,
		DataInsertContext insertion => insertion.Options,
		_ => null,
	};
}
