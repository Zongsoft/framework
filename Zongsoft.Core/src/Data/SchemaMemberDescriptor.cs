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

/// <summary>描述派生解析器提供的数据模式成员。</summary>
public sealed class SchemaMemberDescriptor
{
	#region 构造函数
	public SchemaMemberDescriptor(string name, MemberInfo member, IEnumerable<string> dependencies = null)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));
		if(member == null)
			throw new ArgumentNullException(nameof(member));

		this.Name = name;
		this.Member = member;
		this.Dependencies = dependencies == null ? [] : [.. dependencies];
		this.Type = member switch
		{
			FieldInfo info => info.FieldType,
			PropertyInfo property => property.PropertyType,
			MethodInfo method => method.ReturnType,
			_ => null,
		};
	}
	#endregion

	#region 公共属性
	/// <summary>获取模式成员的名称。</summary>
	public string Name { get; }
	/// <summary>获取模式成员的类型。</summary>
	public Type Type { get; }
	/// <summary>获取模式成员对应的模型字段、属性。</summary>
	public MemberInfo Member { get; }
	/// <summary>获取该模式成员依赖的成员路径集。</summary>
	public IReadOnlyList<string> Dependencies { get; }
	/// <summary>获取一个值，指示该模式成员是否声明了依赖成员。</summary>
	public bool HasDependencies => this.Dependencies != null && this.Dependencies.Count > 0;
	#endregion

	#region 重写方法
	public override string ToString()
	{
		if(this.Type == null)
			return this.Dependencies == null || this.Dependencies.Count == 0 ?
				this.Name : $"{this.Name}({string.Join(',', this.Dependencies)})";

		return this.Dependencies == null || this.Dependencies.Count == 0 ?
			$"{this.Name}:{this.Type.Name}" : $"{this.Name}({string.Join(',', this.Dependencies)}):{this.Type.Name}";
	}
	#endregion
}
