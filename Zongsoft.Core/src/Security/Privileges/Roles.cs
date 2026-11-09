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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
