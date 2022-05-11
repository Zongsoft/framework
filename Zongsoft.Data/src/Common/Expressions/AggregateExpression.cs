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

namespace Zongsoft.Data.Common.Expressions
{
	public class AggregateExpression : MethodExpression
	{
		#region 构造函数
		public AggregateExpression(DataAggregateFunction function, bool distinct, params IExpression[] arguments) : base(function.ToString(), MethodType.Function, arguments)
		{
			this.Function = function;
			this.Distinct = distinct;
		}

		public AggregateExpression(DataAggregate aggregate, params IExpression[] arguments) : base(aggregate.Function.ToString(), MethodType.Function, arguments)
		{
			this.Function = aggregate.Function;
			this.Distinct = aggregate.Distinct;

			if(string.IsNullOrEmpty(aggregate.Alias))
			{
				if(string.IsNullOrEmpty(aggregate.Name) || aggregate.Name == "*")
					this.Alias = aggregate.Function.ToString();
				else
					this.Alias = aggregate.Name + aggregate.Function.ToString();
			}
		}
		#endregion

		#region 公共属性
		public bool Distinct { get; }
		public new DataAggregateFunction Function { get; }
		#endregion

		#region 静态方法
		public static AggregateExpression Aggregate(DataAggregate aggregate, IExpression argument)
		{
			if(argument is FieldIdentifier field)
				field.Alias = null;

			return new AggregateExpression(aggregate, argument == null ? Array.Empty<IExpression>() : new IExpression[] { argument });
		}
		#endregion
	}
}
