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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Components;

using Zongsoft.Security.Privileges.Models;

namespace Zongsoft.Security.Privileges;

public partial class UserService : UserServiceBase<UserModel>
{
	#region 构造函数
	public UserService() => base.Passworder = new Passworder(this);
	#endregion

	#region 操作方法
	public override async ValueTask<bool> EnableAsync(Identifier identifier, CancellationToken cancellation = default)
	{
		var criteria = this.GetCriteria(identifier);
		return criteria != null && await this.Accessor.UpdateAsync(this.Name, new { Status = UserStatus.Active }, criteria, cancellation) > 0;
	}

	public override async ValueTask<bool> DisableAsync(Identifier identifier, CancellationToken cancellation = default)
	{
		var criteria = this.GetCriteria(identifier);
		return criteria != null && await this.Accessor.UpdateAsync(this.Name, new { Status = UserStatus.Disabled }, criteria, cancellation) > 0;
	}
	#endregion

	#region 重写方法
	protected override IDataAccess Accessor => Module.Current.Accessor;
	protected override IServiceProvider Services => Module.Current.Services;
	protected override ICondition GetCriteria(Identifier identifier)
	{
		if(identifier.Validate(out uint userId))
			return Condition.Equal(nameof(UserModel.UserId), userId);

		return base.GetCriteria(identifier);
	}
	#endregion
}

partial class UserService
{
	public new class Passworder(UserService service) : PassworderBase<Passworder.UserCipher>(service)
	{
		protected override ValueTask<bool> OnVerifyAsync(string password, UserCipher cipher, CancellationToken cancellation)
		{
			//确认用户状态
			switch(cipher.Status)
			{
				case UserStatus.Disabled:
					throw new AuthenticationException(SecurityReasons.AccountDisabled);
				case UserStatus.Unapproved:
					throw new AuthenticationException(SecurityReasons.AccountUnapproved);
			}

			return base.OnVerifyAsync(password, cipher, cancellation);
		}

		public class UserCipher : Cipher
		{
			public UserCipher() => this.Name = "SHA1";

			public uint UserId { get; set; }
			public UserStatus Status { get; set; }

			public byte[] Password
			{
				get => this.Value;
				set => this.Value = value;
			}

			public long PasswordSalt
			{
				get => BitConverter.ToInt64(this.Nonce);
				set => this.Nonce = BitConverter.GetBytes(value);
			}

			public override Identifier Identifier => new(typeof(IUser), this.UserId);
		}
	}
}