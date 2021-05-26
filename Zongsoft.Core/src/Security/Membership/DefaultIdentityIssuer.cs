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
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;

using Zongsoft.Data;
using Zongsoft.Services;

namespace Zongsoft.Security.Membership
{
	[Service(typeof(IIdentityIssuer<IIdentity>))]
	public class DefaultIdentityIssuer : IIdentityIssuer<IIdentity>
	{
		#region 成员字段
		private IDataAccess _dataAccess;
		#endregion

		#region 构造函数
		public DefaultIdentityIssuer(IServiceProvider serviceProvider)
		{
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		#endregion

		#region 公共属性
		public string Name => "Zongsoft.DefaultIdentityIssuer";

		public IDataAccess DataAccess
		{
			get
			{
				if(_dataAccess == null)
					_dataAccess = this.ServiceProvider.GetDataAccess(Mapping.Security) ?? this.ServiceProvider.GetDataAccess(true);

				return _dataAccess;
			}
			set => _dataAccess = value ?? throw new ArgumentNullException();
		}

		public IServiceProvider ServiceProvider { get; }
		#endregion

		#region 签发身份
		ClaimsIdentity IIdentityIssuer.Issue(Common.InstanceData data, TimeSpan period, IDictionary<string, object> parameters)
		{
			return this.Issue(data.GetValue<IIdentity>(), period, parameters);
		}

		public ClaimsIdentity Issue(IIdentity data, TimeSpan period, IDictionary<string, object> parameters)
		{
			if(data == null)
				throw new ArgumentNullException(nameof(data));

			//从数据库中获取指定身份的用户对象
			var user = this.GetUser(data);

			if(user == null)
				return null;

			return user.Identity("Zongsoft.Authentication", this.Name, period);
		}
		#endregion

		#region 虚拟方法
		protected virtual IUser GetUser(IIdentity identity)
		{
			return this.DataAccess.Select<IUser>(Mapping.Instance.User,
				MembershipUtility.GetIdentityCondition(identity.Identity) &
				Mapping.Instance.Namespace.GetCondition(Mapping.Instance.User, identity.Namespace)).FirstOrDefault();
		}
		#endregion
	}
}
