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

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Configuration.Options;

namespace Zongsoft.Security.Membership
{
	[Service(typeof(IAuthenticator))]
	public class IdentityAuthenticator : IdentityAuthenticatorBase
	{
		#region 公共属性
		[Options("Security/Membership/Authentication")]
		public Configuration.AuthenticationOptions Options { get; set; }
		#endregion

		#region 重写方法
		protected override uint GetPassword(string identity, string @namespace, out byte[] password, out long passwordSalt, out UserStatus status, out DateTime? statusTimestamp)
		{
			if(string.IsNullOrWhiteSpace(@namespace))
				@namespace = null;

			ICondition criteria = Utility.GetIdentityCondition(identity);

			if(@namespace != "*" && @namespace != "?" && Mapping.Instance.Namespace != null)
				criteria = criteria.And(Mapping.Instance.Namespace.GetCondition(Mapping.Instance.User, @namespace));

			var user = this.ServiceProvider.GetDataAccess().Select<UserSecret>(Mapping.Instance.User, criteria).FirstOrDefault();

			if(user.UserId == 0)
			{
				password = null;
				passwordSalt = 0;
				status = UserStatus.Active;
				statusTimestamp = null;
			}
			else
			{
				password = user.Password;
				passwordSalt = user.PasswordSalt;
				status = user.Status;
				statusTimestamp = user.StatusTimestamp;
			}

			return user.UserId;
		}

		protected override IUserModel GetUser(IIdentityTicket ticket)
		{
			ICondition criteria = Utility.GetIdentityCondition(ticket.Identity);

			if(ticket.Namespace != "*" && ticket.Namespace != "?" && Mapping.Instance.Namespace != null)
				criteria = criteria.And(Mapping.Instance.Namespace.GetCondition(Mapping.Instance.User, ticket.Namespace));

			return this.ServiceProvider.GetDataAccess().Select<User>(Mapping.Instance.User, criteria).FirstOrDefault();
		}

		protected override TimeSpan GetPeriod(string scenario)
		{
			var period = TimeSpan.Zero;

			//获取指定场景对应的凭证有效期
			if(this.Options != null && this.Options.Expiration.TryGet(scenario, out var option))
				period = option.Period;

			return period > TimeSpan.Zero ? period : TimeSpan.FromHours(4);
		}
		#endregion

		#region 嵌套结构
		private struct UserSecret
		{
			public uint UserId { get; set; }
			public byte[] Password { get; set; }
			public long PasswordSalt { get; set; }
			public UserStatus Status { get; set; }
			public DateTime? StatusTimestamp { get; set; }
		}

		private class User : IUserModel, IUserIdentity
		{
			public uint UserId { get; set; }
			public string Name { get; set; }
			public string Nickname { get; set; }
			public string Namespace { get; set; }
			public string Email { get; set; }
			public string Phone { get; set; }
			public UserStatus Status { get; set; }
			public DateTime? StatusTimestamp { get; set; }
			public DateTime Creation { get; set; }
			public DateTime? Modification { get; set; }
			public string Description { get; set; }
		}
		#endregion
	}
}
