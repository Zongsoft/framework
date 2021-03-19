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

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Metadata
{
	public static class DataEntityPropertyExtension
	{
		/// <summary>
		/// 获取指定实体属性对应的字段名以及返回的别名。
		/// </summary>
		/// <param name="property">当前的实体属性。</param>
		/// <param name="alias">输出参数，对应的返回别名。详细说明请参考该方法的备注说明。</param>
		/// <returns>当前属性对应的字段名。</returns>
		/// <remarks>
		///		<para>注意：如果当前实体属性的字段名不同于属性名，则<paramref name="alias"/>输出参数值即为属性名，必须确保查询返回的字段标识都为对应的属性名，以便后续实体组装时进行字段与属性的匹配。</para>
		/// </remarks>
		public static string GetFieldName(this IDataEntityProperty property, out string alias)
		{
			if(property == null)
				throw new ArgumentNullException(nameof(property));

			var simplex = property.IsSimplex ?
				(IDataEntitySimplexProperty)property :
				throw new ArgumentException($"The specified '{property.Name}' property is not a simplex property, so it has no field name(alias).");

			if(string.IsNullOrEmpty(simplex.Alias))
			{
				alias = null;
				return property.Name;
			}
			else
			{
				alias = property.Name;
				return simplex.Alias;
			}
		}

		/// <summary>
		/// 获取指定导航属性是否有约束定义。
		/// </summary>
		/// <param name="property">指定的导航属性。</param>
		/// <returns>如果指定的导航属性定义了约束则返回真(True)，否则返回假(False)。</returns>
		public static bool HasConstraints(this IDataEntityComplexProperty property)
		{
			return property != null && property.Constraints != null && property.Constraints.Length > 0;
		}

		/// <summary>
		/// 获取指定导航属性约束项值的常量表达式。
		/// </summary>
		/// <param name="property">指定的导航属性。</param>
		/// <param name="constraint">指定的导航属性的约束项。</param>
		/// <returns>返回的约束项值对应关联属性数据类型的表达式。</returns>
		public static IExpression GetConstraintValue(this IDataEntityComplexProperty property, DataAssociationConstraint constraint)
		{
			if(constraint.Value == null)
				return ConstantExpression.Null;

			var entity = constraint.Actor == DataAssociationConstraintActor.Principal ? property.Entity : property.Foreign;

			//获取指定导航属性的关联属性
			if(!entity.Properties.TryGet(constraint.Name, out var constraintProperty))
				throw new DataException($"The specified '{constraint.Name}' constraint does not exist in the '{property.Entity.Name}.{property.Name}' navigation property.");

			//如果约束项的关联属性不是简单属性则抛出异常
			if(!constraintProperty.IsSimplex)
				throw new DataException($"The specified '{constraint.Name}' constraint association property is not a simplex property.");

			//返回约束项值转换成关联属性数据类型的常量表达式
			return Expression.Constant(Zongsoft.Common.Convert.ConvertValue(constraint.Value, Utility.FromDbType(((IDataEntitySimplexProperty)constraintProperty).Type)));
		}
	}
}
