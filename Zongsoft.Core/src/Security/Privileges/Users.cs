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

/// <summary>提供内置用户名称及用户标识类别。</summary>
public static class Users
{
	#region 常量定义
	/// <summary>系统管理员用户名。</summary>
	public const string Administrator = nameof(Administrator);
	#endregion

	#region 嵌套子类
	/// <summary>提供用户标识的逻辑类别名称。</summary>
	public static class Identifiers
	{
		/// <summary>用户标识的逻辑类别名称。</summary>
		public const string User = "Zongsoft.Security.User";
	}
	#endregion
}
