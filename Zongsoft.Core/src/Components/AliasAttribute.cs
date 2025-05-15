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

namespace Zongsoft.Components;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
public class AliasAttribute : Attribute
{
	#region 构造函数
	public AliasAttribute(string alias) => this.Alias = alias ?? throw new ArgumentNullException(nameof(alias));
	#endregion

	#region 公共属性
	public string Alias { get; }
	#endregion

	#region 静态方法
	public static string[] GetAliases(object target) => target switch
	{
		MemberInfo member => GetAliases(member),
		Assembly assembly => GetAliases(assembly),
		Module module => GetAliases(module),
		ParameterInfo parameter => GetAliases(parameter),
		_ => GetAliases(target?.GetType()),
	};

	public static string[] GetAliases(Module module, bool inherit = true) =>
		module == null ? [] : GetAliases(GetCustomAttributes(module, typeof(AliasAttribute), inherit));

	public static string[] GetAliases(Assembly assembly, bool inherit = true) =>
		assembly == null ? [] : GetAliases(GetCustomAttributes(assembly, typeof(AliasAttribute), inherit));

	public static string[] GetAliases(MemberInfo member, bool inherit = true) =>
		member == null ? [] : GetAliases(GetCustomAttributes(member, typeof(AliasAttribute), inherit));

	public static string[] GetAliases(ParameterInfo parameter, bool inherit = true) =>
		parameter == null ? [] : GetAliases(GetCustomAttributes(parameter, typeof(AliasAttribute), inherit));

	private static string[] GetAliases(Attribute[] attributes)
	{
		if(attributes == null || attributes.Length == 0)
			return [];

		var aliases = new string[attributes.Length];

		for(int i = 0; i < attributes.Length; i++)
			aliases[i] = ((AliasAttribute)attributes[i]).Alias;

		return aliases;
	}
	#endregion
}
