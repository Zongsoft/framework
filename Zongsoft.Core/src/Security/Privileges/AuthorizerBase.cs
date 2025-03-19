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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

public abstract class AuthorizerBase : IAuthorizer, IMatchable, IMatchable<ClaimsPrincipal>
{
	#region 构造函数
	protected AuthorizerBase(string name)
	{
		this.Name = name ?? string.Empty;
		this.Privileger = new();
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public Privileger Privileger { get; }
	public abstract IPrivilegeEvaluator Evaluator { get; }
	#endregion

	#region 公共方法
	public virtual ValueTask<bool> AuthorizeAsync(ClaimsIdentity user, string privilege, Parameters parameters, CancellationToken cancellation = default)
	{
		if(user == null)
			return ValueTask.FromResult(false);

		if(privilege == null)
			return ValueTask.FromResult(false);

		return ValueTask.FromResult(
			user.TryGetClaims("Privileges", out var privileges) &&
			privileges.Contains(privilege, StringComparer.OrdinalIgnoreCase)
		);
	}
	#endregion

	#region 服务匹配
	bool IMatchable.Match(object argument) => argument is ClaimsPrincipal principal && this.OnMatch(principal);
	bool IMatchable<ClaimsPrincipal>.Match(ClaimsPrincipal argument) => this.OnMatch(argument);
	protected virtual bool OnMatch(ClaimsPrincipal principal) => principal != null && principal.Identity != null && principal.Identity.IsAuthenticated;
	#endregion
}
