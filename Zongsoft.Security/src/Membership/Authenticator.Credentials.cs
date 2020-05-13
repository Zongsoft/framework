/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Security.Claims;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Runtime.Caching;

namespace Zongsoft.Security.Membership
{
	partial class Authenticator
	{
		#region 成员字段
		private ICache _cache;
		#endregion

		#region 公共属性
		public ICache Cache
		{
			get => _cache ?? (_cache = this.CacheProvider?.GetService(Modules.Security) ?? this.CacheProvider?.GetService(string.Empty));
			set => _cache = value ?? throw new ArgumentNullException();
		}

		[ServiceDependency]
		public IServiceProvider<ICache> CacheProvider
		{
			get; set;
		}
		#endregion

		public void Register(AuthenticationContext context)
		{
		}

		public void Unregister(string credentialId)
		{
		}

		public void Renew(string credentialId)
		{
		}

		public ClaimsIdentity GetIdentity(string credentialId)
		{
			return null;
		}
	}
}
