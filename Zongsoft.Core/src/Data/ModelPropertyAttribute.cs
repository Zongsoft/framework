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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Data;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ModelPropertyAttribute : Attribute
{
	#region 构造函数
	public ModelPropertyAttribute(ModelPropertyRole role) => this.Role = role;
	public ModelPropertyAttribute(ModelPropertyRole role, ModelPropertyFlags flags)
	{
		this.Role = role;
		this.Flags = flags;
	}

	public ModelPropertyAttribute(ModelPropertyFlags flags) => this.Flags = flags;
	public ModelPropertyAttribute(ModelPropertyFlags flags, ModelPropertyRole role)
	{
		this.Role = role;
		this.Flags = flags;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置属性的语义角色。</summary>
	public ModelPropertyRole? Role { get; set; }

	/// <summary>获取或设置属性的标记。</summary>
	public ModelPropertyFlags? Flags { get; set; }
	#endregion
}