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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Security.Privileges;

/// <summary>
/// 表示权限系统的用户接口。
/// </summary>
public interface IUser : Zongsoft.Components.IIdentifiable
{
	#region 常量定义
	/// <summary>系统管理员用户名。</summary>
	public const string Administrator = nameof(Administrator);
	#endregion

	#region 属性定义
	/// <summary>获取或设置用户名称。</summary>
	string Name { get; set; }

	/// <summary>获取或设置用户的邮箱地址。</summary>
	string Email { get; set; }

	/// <summary>获取或设置用户的电话号码。</summary>
	string Phone { get; set; }

	/// <summary>获取或设置用户是否可用。</summary>
	bool Enabled { get; set; }

	/// <summary>获取或设置用户性别。</summary>
	string Gender { get; set; }

	/// <summary>获取或设置用户头像。</summary>
	string Avatar { get; set; }

	/// <summary>获取或设置用户昵称。</summary>
	string Nickname { get; set; }

	/// <summary>获取或设置用户所属的命名空间。</summary>
	string Namespace { get; set; }

	/// <summary>获取或设置用户的描述信息。</summary>
	string Description { get; set; }
	#endregion
}
