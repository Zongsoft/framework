﻿/*
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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Common;

public class DataUpsertExecutor : DataMutateExecutor<UpsertStatement>
{
	#region 重写方法
	protected override void OnMutating(IDataMutateContext context, UpsertStatement statement)
	{
		//如果新增实体包含序号定义项则尝试处理其中的外部序号
		if(statement.Entity.HasSequences())
		{
			foreach(var field in statement.Fields)
			{
				if(field.Token.Property.IsSimplex)
				{
					var sequence = ((IDataEntitySimplexProperty)field.Token.Property).Sequence;

					if(sequence != null && sequence.IsExternal)
					{
						var value = field.Token.GetValue(context.Data);

						if(value == null || Convert.IsDBNull(value) || object.Equals(value, Zongsoft.Common.TypeExtension.GetDefaultValue(field.Token.MemberType)))
							field.Token.SetValue(context.Data, Convert.ChangeType(((DataAccess)context.DataAccess).Increase(context, sequence, context.Data), field.Token.MemberType));
					}
				}
			}
		}

		//调用基类同名方法
		base.OnMutating(context, statement);
	}

	protected override async ValueTask OnMutatingAsync(IDataMutateContext context, UpsertStatement statement, CancellationToken cancellation)
	{
		//如果新增实体包含序号定义项则尝试处理其中的外部序号
		if(statement.Entity.HasSequences())
		{
			foreach(var field in statement.Fields)
			{
				if(field.Token.Property.IsSimplex)
				{
					var sequence = ((IDataEntitySimplexProperty)field.Token.Property).Sequence;

					if(sequence != null && sequence.IsExternal)
					{
						var value = field.Token.GetValue(context.Data);

						if(value == null || Convert.IsDBNull(value) || object.Equals(value, Zongsoft.Common.TypeExtension.GetDefaultValue(field.Token.MemberType)))
						{
							var id = await ((DataAccess)context.DataAccess).IncreaseAsync(context, sequence, context.Data, cancellation);
							field.Token.SetValue(context.Data, Convert.ChangeType(id, field.Token.MemberType));
						}
					}
				}
			}
		}

		//调用基类同名方法
		await base.OnMutatingAsync(context, statement, cancellation);
	}

	protected override bool OnMutated(IDataMutateContext context, UpsertStatement statement, int count)
	{
		//执行获取新增后的自增型字段值
		if(count > 0 && statement.Sequence != null)
			context.Provider.Executor.Execute(context, statement.Sequence);

		//调用基类同名方法
		return base.OnMutated(context, statement, count);
	}

	protected override async ValueTask<bool> OnMutatedAsync(IDataMutateContext context, UpsertStatement statement, int count, CancellationToken cancellation)
	{
		//执行获取新增后的自增型字段值
		if(count > 0 && statement.Sequence != null)
			await context.Provider.Executor.ExecuteAsync(context, statement.Sequence, cancellation);

		return count > 0;
	}
	#endregion
}
