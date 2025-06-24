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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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