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

using Zongsoft.Data;
using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Configuration.Options;

namespace Zongsoft.Security.Membership
{
	[Service(typeof(IUserProvider<User>))]
	public class UserProvider : UserProviderBase<User>
	{
		#region 构造函数
		public UserProvider(IServiceProvider serviceProvider) : base(null, serviceProvider) { }
		#endregion

		#region 公共属性
		[ServiceDependency]
		public ICensorship Censorship { get; set; }

		[Options("Security/Membership/Identity")]
		public Configuration.IdentityOptions Options { get; set; }
		#endregion

		#region 虚拟方法
		protected override User CreateUser(string identity, string @namespace, UserStatus status, string description = null)
		{
			var user = Model.Build<User>();

			return user;
		}

		protected override bool IsVerifyEmailRequired()
		{
			return (this.Options.Verification & Configuration.IdentityVerification.Email) == Configuration.IdentityVerification.Email;
		}

		protected override bool IsVerifyPhoneRequired()
		{
			return (this.Options.Verification & Configuration.IdentityVerification.Phone) == Configuration.IdentityVerification.Phone;
		}

		protected override void OnValidateName(string name)
		{
			base.OnValidateName(name);
			this.Censor(name);
		}

		protected override void OnValidatePassword(string password)
		{
			var validator = this.ServiceProvider?.GetMatchedService<IValidator<string>>("password");

			if(validator != null)
				validator.Validate(password, this.Options, message => throw new SecurityException("password.illegality", message));
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void Censor(string name)
		{
			var censorship = this.Censorship;

			if(censorship != null && censorship.IsBlocked(name, Zongsoft.Security.Censorship.KEY_NAMES, Zongsoft.Security.Censorship.KEY_SENSITIVES))
				throw new CensorshipException(string.Format("Illegal '{0}' name of user.", name));
		}
		#endregion
	}
}
