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

namespace Zongsoft.Data.TDengine;

public sealed class TDengineStatementSlotEvaluator : StatementSlotEvaluatorBase
{
	public static readonly TDengineStatementSlotEvaluator Instance = new();

	public override string Evaluate(IDataAccessContext context, IStatementBase statement, StatementSlot slot)
	{
		if(slot == null || slot.Value == null)
			return null;

		if(context is IDataMutateContextBase ctx)
		{
			var data = ctx.Data;
			var values = slot.Value switch
			{
				IEnumerable<IDataEntityProperty> properties => properties.Select(property => Reflection.Reflector.GetValue(ref data, property.Name)).ToArray(),
				IEnumerable<DataEntityPropertyToken> tokens => tokens.Select(token => token.GetValue(data)).ToArray(),
				_ => null,
			};

			return slot.Place switch
			{
				"Table" when values != null => TDengineUtility.GetTableName(statement.Table.Entity.GetTableName(), values),
				_ => base.Evaluate(context, statement, slot),
			};
		}

		return null;
	}
}
