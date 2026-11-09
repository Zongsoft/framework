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

using System;

namespace Zongsoft.Security.Privileges;

/// <summary>表示权限系统的用户接口。</summary>
public interface IUser : Zongsoft.Components.IIdentifiable
{
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
	/// <remarks>提示：真(<c>True</c>)表示男，假(<c>False</c>)表示女，空(<c>null</c>)表示未定义。</remarks>
	bool? Gender { get; set; }

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
