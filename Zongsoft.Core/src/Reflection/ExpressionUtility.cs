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
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Zongsoft.Reflection;

internal static class ExpressionUtility
{
	internal static string GetMemberName(Expression expression) => GetMember(expression).Name;
	internal static MemberToken GetMember(Expression expression)
	{
		var token = ResolveMemberExpression(expression, new Stack<MemberInfo>());
		return token ?? throw new ArgumentException("Invalid member expression.");
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

	internal struct MemberToken(string name, MemberInfo member)
	{
		public string Name = name;
		public MemberInfo Member = member;

		public readonly Type MemberType => this.Member.MemberType switch
		{
			MemberTypes.Property => ((PropertyInfo)this.Member).PropertyType,
			MemberTypes.Field => ((FieldInfo)this.Member).FieldType,
			_ => null,
		};
	}
}
