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

/// <summary>表示权限系统的角色接口。</summary>
public interface IRole : Zongsoft.Components.IIdentifiable
{
	#region 属性定义
	/// <summary>获取或设置角色名称。</summary>
	string Name { get; set; }

	/// <summary>获取或设置角色是否可用。</summary>
	bool Enabled { get; set; }

	/// <summary>获取或设置角色头像。</summary>
	string Avatar { get; set; }

	/// <summary>获取或设置角色昵称。</summary>
	string Nickname { get; set; }

	/// <summary>获取或设置角色所属的命名空间。</summary>
	string Namespace { get; set; }

	/// <summary>获取或设置角色的描述信息。</summary>
	string Description { get; set; }
	#endregion
}
