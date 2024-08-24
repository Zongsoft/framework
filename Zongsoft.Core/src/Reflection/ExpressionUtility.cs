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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Zongsoft.Reflection
{
	internal static class ExpressionUtility
	{
		internal static string GetMemberName(Expression expression)
		{
			return GetMember(expression).Name;
		}

		internal static MemberToken GetMember(Expression expression)
		{
			var token = ResolveMemberExpression(expression, new Stack<MemberInfo>());

			if(token == null)
				throw new ArgumentException("Invalid member expression.");

			return token.Value;
		}

		private static MemberToken? ResolveMemberExpression(Expression expression, Stack<MemberInfo> stack)
		{
			if(expression.NodeType == ExpressionType.Lambda)
				return ResolveMemberExpression(((LambdaExpression)expression).Body, stack);

			switch(expression.NodeType)
			{
				case ExpressionType.MemberAccess:
					stack.Push(((MemberExpression)expression).Member);

					if(((MemberExpression)expression).Expression != null)
						return ResolveMemberExpression(((MemberExpression)expression).Expression, stack);

					break;
				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
					return ResolveMemberExpression(((UnaryExpression)expression).Operand, stack);
			}

			if(stack == null || stack.Count == 0)
				return null;

			var path = string.Empty;
			var type = typeof(object);
			MemberInfo member = null;

			while(stack.Count > 0)
			{
				member = stack.Pop();

				if(path.Length > 0)
					path += ".";

				path += member.Name;
			}

			return new MemberToken(path, member);
		}

		internal struct MemberToken
		{
			public string Name;
			public MemberInfo Member;

			public MemberToken(string name, MemberInfo member)
			{
				this.Name = name;
				this.Member = member;
			}

			public Type MemberType
			{
				get
				{
					switch(this.Member.MemberType)
					{
						case MemberTypes.Property:
							return ((PropertyInfo)this.Member).PropertyType;
						case MemberTypes.Field:
							return ((FieldInfo)this.Member).FieldType;
						default:
							return null;
					}
				}
			}
		}
	}
}
