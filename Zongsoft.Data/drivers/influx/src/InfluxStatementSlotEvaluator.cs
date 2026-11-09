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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
