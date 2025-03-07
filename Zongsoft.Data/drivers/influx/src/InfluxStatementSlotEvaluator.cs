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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.Influx library.
 *
 * The Zongsoft.Data.Influx is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.Influx is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.Influx library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Collections.Generic;

using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Influx;

public sealed class InfluxStatementSlotEvaluator : StatementSlotEvaluatorBase
{
	public static readonly InfluxStatementSlotEvaluator Instance = new();

	public override string Evaluate(IDataAccessContext context, IStatementBase statement, StatementSlot slot)
	{
		if(slot == null || slot.Value == null)
			return null;

		if(context is IDataMutateContextBase ctx)
		{
			var data = ctx.Data;
			var text = slot.Value switch
			{
				IEnumerable<IDataEntityProperty> properties => string.Join('_', properties.Select(property => Reflection.Reflector.GetValue(ref data, property.Name)?.ToString())),
				IEnumerable<DataEntityPropertyToken> tokens => string.Join('_', tokens.Select(token => token.GetValue(data)?.ToString())),
				_ => base.Evaluate(context, statement, slot),
			};

			if(string.IsNullOrEmpty(text))
				return null;

			return slot.Place switch
			{
				"Table" => InfluxUtility.GetTableName(text),
				_ => text,
			};
		}

		return null;
	}
}
