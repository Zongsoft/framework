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

namespace Zongsoft.Security.Privileges;

/// <summary>提供内置角色名称及角色标识类别。</summary>
public static class Roles
{
	#region 常量定义
	/// <summary>系统管理员角色名。</summary>
	public const string Administrators = nameof(Administrators);

	/// <summary>安全管理员角色名。</summary>
	public const string Security = nameof(Security);
	#endregion

	#region 嵌套子类
	/// <summary>提供角色标识的逻辑类别名称。</summary>
	public static class Identifiers
	{
		/// <summary>角色标识的逻辑类别名称。</summary>
		public const string Role = "Zongsoft.Security.Role";
	}
	#endregion
}
