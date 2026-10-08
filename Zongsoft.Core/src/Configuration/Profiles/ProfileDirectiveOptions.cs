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

namespace Zongsoft.Configuration.Profiles;

/// <summary>表示指定名称的 Profile 指令选项。</summary>
public partial class ProfileDirectiveOptions
{
	#region 构造函数
	public ProfileDirectiveOptions(string name, ProfileDirectiveBehavior behavior = ProfileDirectiveBehavior.None)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		if(!ProfileUtility.IsDirectiveName(name))
			throw new ArgumentException(Properties.Resources.Profiles_DirectiveNameInvalid_Message, nameof(name));

		this.Name = name;
		this.Behavior = behavior;
	}
	#endregion

	#region 公共属性
	/// <summary>获取指令名称，不包含注释符和 <c>@</c> 前缀。</summary>
	public string Name { get; }
	/// <summary>获取或设置指令行为；<see cref="ProfileDirectiveBehavior.None"/> 表示采用指令内置默认行为。</summary>
	public ProfileDirectiveBehavior Behavior
	{
		get => field;
		set => field = Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}
	#endregion

	#region 重写方法
	public override string ToString() => $"{this.Name}({this.Behavior})";
	#endregion

	#region 公共方法
	/// <summary>复制选项，保留实际派生类型。</summary>
	/// <returns>返回独立的选项副本。</returns>
	/// <remarks>默认浅复制实例字段；包含可变引用成员的派生类型应重写此方法，复制其可变成员。</remarks>
	public virtual ProfileDirectiveOptions Clone() => (ProfileDirectiveOptions)this.MemberwiseClone();
	#endregion
}
