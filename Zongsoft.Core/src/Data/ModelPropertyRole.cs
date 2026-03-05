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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Data;

/// <summary>
/// 表示属性的语义角色的枚举。
/// </summary>
public static class ModelPropertyRole
{
	#region 私有变量
	private static readonly Lazy<(string alias, string role)[]> _fields = new(() =>
	{
		var fields = typeof(ModelPropertyRole).GetFields(BindingFlags.Public | BindingFlags.Static);
		var result = new List<(string, string)>(fields.Length * 2);

		for(int i = 0; i < fields.Length; i++)
		{
			result.Add((fields[i].Name, fields[i].Name));

			var aliases = Components.AliasAttribute.GetAliases(fields[i]);
			if(aliases != null && aliases.Length > 0)
			{
				for(int j = 0; j < aliases.Length; j++)
				{
					if(!string.IsNullOrEmpty(aliases[j]))
						result.Add((aliases[j], fields[i].Name));
				}
			}
		}

		return [.. result];
	});
	#endregion

	#region 公共字段
	/// <summary>代码</summary>
	[Components.Alias("No")]
	public static readonly string Code = nameof(Code);
	/// <summary>名称</summary>
	public static readonly string Name = nameof(Name);
	/// <summary>邮箱</summary>
	public static readonly string Email = nameof(Email);
	/// <summary>性别</summary>
	public static readonly string Gender = nameof(Gender);
	/// <summary>生日</summary>
	public static readonly string Birthday = nameof(Birthday);

	/// <summary>电话</summary>
	[Components.Alias("Mobile")]
	[Components.Alias("Telephone")]
	[Components.Alias("PhoneNumber")]
	public static readonly string Phone = nameof(Phone);

	/// <summary>地址</summary>
	[Components.Alias("City")]
	[Components.Alias("Contry")]
	[Components.Alias("Street")]
	[Components.Alias("Province")]
	public static readonly string Address = nameof(Address);

	/// <summary>货币</summary>
	[Components.Alias("Money")]
	[Components.Alias("Price")]
	[Components.Alias("Amount")]
	public static readonly string Currency = nameof(Currency);

	/// <summary>密码</summary>
	[Components.Alias("Secret")]
	public static readonly string Password = nameof(Password);

	/// <summary>描述信息</summary>
	[Components.Alias("Note")]
	[Components.Alias("Notes")]
	[Components.Alias("Remark")]
	[Components.Alias("Remarks")]
	public static readonly string Description = nameof(Description);
	#endregion

	#region 公共方法
	public static string Determine(string name)
	{
		if(string.IsNullOrEmpty(name))
			return null;

		for(int i = 0; i < _fields.Value.Length; i++)
		{
			var (alias, role) = _fields.Value[i];

			if(name.StartsWith(alias, StringComparison.InvariantCultureIgnoreCase) ||
			   name.EndsWith(alias, StringComparison.InvariantCultureIgnoreCase))
				return role;
		}

		return null;
	}
	#endregion
}