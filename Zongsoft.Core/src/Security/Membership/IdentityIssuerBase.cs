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
using System.Collections.Generic;
using System.Security.Claims;

using Zongsoft.Data;
using Zongsoft.Services;

namespace Zongsoft.Security.Membership
{
	public abstract class IdentityIssuerBase<TUser> : IIdentityIssuer<IIdentityTicket> where TUser : IUser
	{
		#region 构造函数
		protected IdentityIssuerBase(string name, IServiceProvider serviceProvider)
		{
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		#endregion

		#region 公共属性
		public string Name { get; }

		[ServiceDependency(IsRequired = true)]
		public IServiceAccessor<IDataAccess> DataAccess { get; set; }

		public IServiceProvider ServiceProvider { get; }
		#endregion

		#region 签发身份
		ClaimsIdentity IIdentityIssuer.Issue(object data, TimeSpan period, IDictionary<string, object> parameters)
		{
			return this.Issue(data as IIdentityTicket, period, parameters);
		}

		public ClaimsIdentity Issue(IIdentityTicket data, TimeSpan period, IDictionary<string, object> parameters)
		{
			if(data == null)
				throw new ArgumentNullException(nameof(data));

			//从数据库中获取指定身份的用户对象
			var user = this.GetUser(data);

			if(user == null)
				return null;

			return this.Identity(user, period);
		}
		#endregion

		#region 虚拟方法
		protected abstract TUser GetUser(IIdentityTicket ticket);

		protected virtual ClaimsIdentity Identity(TUser user, TimeSpan period)
		{
			return user.Identity(this.Name, this.Name, period);
		}
		#endregion
	}
}