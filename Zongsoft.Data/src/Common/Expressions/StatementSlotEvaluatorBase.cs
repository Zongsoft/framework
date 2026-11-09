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

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions;

public abstract class StatementSlotEvaluatorBase : IStatementSlotEvaluator
{
	public virtual string Evaluate(IDataAccessContext context, IStatementBase statement, StatementSlot slot)
	{
		if(slot == null || string.IsNullOrEmpty(slot.Name))
			return null;

		if(context is IDataMutateContextBase ctx)
		{
			object value;
			var data = ctx.Data;

			switch(slot.Value)
			{
				case string text:
					return Zongsoft.Reflection.Reflector.TryGetValue(ref data, text, out value) && value != null ? value.ToString() : null;
				case IDataEntityProperty property:
					return Zongsoft.Reflection.Reflector.TryGetValue(ref data, property.Name, out value) && value != null ? value.ToString() : null;
				case DataEntityPropertyToken token:
					return token.TryGetValue(data, null, out value) && value != null ? value.ToString() : null;
			}
		}

		return null;
	}
}
