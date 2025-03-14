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
using System.Security.Claims;
using System.Collections.Generic;

using Zongsoft.Components;

namespace Zongsoft.Security.Privileges;

public abstract class AuthorizerBase : IAuthorizer
{
	protected AuthorizerBase(string name)
	{
		this.Scheme = name;
		this.Privileger = new();
	}

	public string Scheme { get; }
	public PrivilegeCategory Privileger { get; }

	public virtual bool Authorize(ClaimsIdentity user, string privilege) => this.Authorize(user.Identify(), privilege);
	public abstract bool Authorize(Identifier identifier, string privilege);
	public virtual IEnumerable<Privilege> Authorizes(ClaimsIdentity user) => this.Authorizes(user.Identify());
	public abstract IEnumerable<Privilege> Authorizes(Identifier identifier);
}
