﻿/*
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

[System.Reflection.DefaultMember(nameof(Authorizers))]
[System.ComponentModel.DefaultProperty(nameof(Authorizers))]
public static class Authorization
{
	#region 成员字段
	private static IAuthorizer _authorizer;
	#endregion

	#region 静态构造
	static Authorization()
	{
		Authorizers = new();
		Servicer = new();
	}
	#endregion

	#region 公共属性
	public static IAuthorizer Authorizer
	{
		get => _authorizer ??= Authorizers.Count > 0 ? Authorizers[0] : null;
		set => _authorizer = value;
	}

	public static AuthorizerCollection Authorizers { get; }

	/// <summary>获取安全授权服务提供程序。</summary>
	public static AuthorizationServicer Servicer { get; }
	#endregion

	#region 嵌套子类
	public sealed class AuthorizationServicer
	{
		internal AuthorizationServicer() { }
		public IPrivilegeService Privileges { get; set; }
	}
	#endregion
}
