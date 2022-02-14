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
using System.Collections.Generic;

using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Common
{
	public class DataInsertExecutor : DataMutateExecutor<InsertStatement>
	{
		#region 重写方法
		protected override void OnMutating(IDataMutateContext context, InsertStatement statement)
		{
			//如果新增实体包含序号定义项则尝试处理其中的外部序号
			if(statement.Entity.HasSequences)
			{
				foreach(var field in statement.Fields)
				{
					if(field.Token.Property.IsSimplex)
					{
						var sequence = ((IDataEntitySimplexProperty)field.Token.Property).Sequence;

						if(sequence != null && sequence.IsExternal)
						{
							var value = field.Token.GetValue(context.Data);

							if(value == null || Convert.IsDBNull(value) || object.Equals(value, Zongsoft.Common.TypeExtension.GetDefaultValue(field.Token.MemberType)) || (context.Options is IDataInsertOptions options && !options.SequenceSuppressed))
								field.Token.SetValue(context.Data, Convert.ChangeType(((DataAccess)context.DataAccess).Increase(context, sequence, context.Data), field.Token.MemberType));
						}
					}
				}
			}

			//调用基类同名方法
			base.OnMutating(context, statement);
		}

		protected override bool OnMutated(IDataMutateContext context, InsertStatement statement, int count)
		{
			//执行获取新增后的自增型字段值
			if(count > 0 && statement.Sequence != null)
				context.Provider.Executor.Execute(context, statement.Sequence);

			return count > 0;
		}
		#endregion
	}
}
